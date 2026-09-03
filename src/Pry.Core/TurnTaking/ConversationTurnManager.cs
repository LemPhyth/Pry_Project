using Pry.Core.Memory;
using Pry.Core.Models;

namespace Pry.Core.TurnTaking;

public sealed class ConversationTurnManager : IAsyncDisposable
{
    private readonly MemoryDatabase _database;
    private readonly ReplyPlanner _planner;
    private readonly InterruptClassifier _classifier;
    private readonly TurnTakingSettings _settings;
    private readonly string _conversationId;
    private readonly string _characterId;
    private readonly object _gate = new();
    private readonly List<UserInputPart> _pendingUserInputs = [];
    private readonly List<long?> _pendingSourceMessageIds = [];
    private readonly List<bool> _pendingMemoryExtraction = [];
    private CancellationTokenSource? _debounceCancellation;
    private CancellationTokenSource? _turnCancellation;
    private DateTimeOffset? _pendingStartedAt;
    private TurnState _state;

    public ConversationTurnManager(MemoryDatabase database, ReplyPlanner planner, InterruptClassifier classifier,
        TurnTakingSettings settings, string conversationId, string characterId)
    {
        _database = database; _planner = planner; _classifier = classifier; _settings = settings;
        _conversationId = conversationId; _characterId = characterId;
    }

    public TurnState State { get { lock (_gate) return _state; } }
    public event Func<PlannedReplyMessage, Task>? AgentMessageDelivered;
    public event Action<TurnState>? StateChanged;
    public event Action<Exception>? Failed;

    public async Task<long> SubmitUserInputAsync(UserInputPart input, bool immediate = false,
        CancellationToken cancellationToken = default)
    {
        var messageId = await _database.AddMessageAsync(_conversationId, ChatRole.User, input.Text, input.SafeAttachments.FirstOrDefault(x => x.IsImage)?.Path,
            cancellationToken, input.StickerId);
        InterruptKind? interrupt = null;
        lock (_gate)
        {
            if (_state is TurnState.AgentPending or TurnState.AgentSending)
                interrupt = _settings.AutoClassifyInterrupts ? _classifier.Classify(input) : InterruptKind.SoftInterrupt;
            if (interrupt != InterruptKind.Backchannel)
            {
                _turnCancellation?.Cancel();
                _pendingUserInputs.Add(input);
                _pendingSourceMessageIds.Add(messageId);
                _pendingMemoryExtraction.Add(true);
                _pendingStartedAt ??= DateTimeOffset.UtcNow;
                SetStateLocked(TurnState.UserPending);
            }
        }
        if (interrupt == InterruptKind.Backchannel) return messageId;
        ScheduleDebounce(immediate);
        return messageId;
    }

    public void RegenerateFromExistingUserMessage(ChatMessage message)
    {
        lock (_gate)
        {
            _turnCancellation?.Cancel();
            _pendingUserInputs.Add(new UserInputPart(message.Content, message.StickerId,
                string.IsNullOrWhiteSpace(message.ImagePath) ? [] : [new ChatAttachment(message.ImagePath, ChatAttachmentKind.Image, Path.GetFileName(message.ImagePath))]));
            _pendingSourceMessageIds.Add(message.Id);
            _pendingMemoryExtraction.Add(false);
            _pendingStartedAt = DateTimeOffset.UtcNow;
            SetStateLocked(TurnState.UserPending);
            ScheduleDebounceLocked(true);
        }
    }

    public void NotifyInputActivity()
    {
        lock (_gate) if (_state == TurnState.UserPending) ScheduleDebounceLocked(false);
    }

    public void CancelAgentReply()
    {
        lock (_gate)
        {
            _turnCancellation?.Cancel();
            if (_state is TurnState.ModelThinking or TurnState.AgentPending or TurnState.AgentSending) SetStateLocked(TurnState.Idle);
        }
    }

    private void ScheduleDebounce(bool immediate)
    {
        lock (_gate) ScheduleDebounceLocked(immediate);
    }

    private void ScheduleDebounceLocked(bool immediate)
    {
        _debounceCancellation?.Cancel(); _debounceCancellation?.Dispose();
        _debounceCancellation = new CancellationTokenSource();
        var elapsed = _pendingStartedAt is null ? 0 : (DateTimeOffset.UtcNow - _pendingStartedAt.Value).TotalMilliseconds;
        var delay = immediate ? 0 : Math.Min(_settings.DebounceMs, Math.Max(0, _settings.MaxPendingMs - elapsed));
        _ = DebounceThenRespondAsync(TimeSpan.FromMilliseconds(delay), _debounceCancellation.Token);
    }

    private async Task DebounceThenRespondAsync(TimeSpan delay, CancellationToken debounceToken)
    {
        long? activePlanId = null;
        try
        {
            await Task.Delay(delay, debounceToken);
            UserInputPart[] inputs; long?[] sourceMessageIds; bool[] extractMemory;
            CancellationToken turnToken;
            lock (_gate)
            {
                inputs = [.. _pendingUserInputs]; sourceMessageIds = [.. _pendingSourceMessageIds]; extractMemory = [.. _pendingMemoryExtraction]; _pendingUserInputs.Clear(); _pendingSourceMessageIds.Clear(); _pendingMemoryExtraction.Clear(); _pendingStartedAt = null;
                _turnCancellation?.Dispose(); _turnCancellation = new CancellationTokenSource(); turnToken = _turnCancellation.Token;
                SetStateLocked(TurnState.ModelThinking);
            }
            var parts = new List<string>();
            foreach (var input in inputs)
            {
                if (!string.IsNullOrWhiteSpace(input.Text)) parts.Add(input.Text);
                else if (input.StickerId is not null) parts.Add($"[用户发送表情：{input.StickerId}]");
                foreach (var attachment in input.SafeAttachments.Where(x => !x.IsImage))
                    parts.Add(await AttachmentTextExtractor.ExtractForPromptAsync(attachment, turnToken));
            }
            var combined = string.Join("\n", parts);
            var images = inputs.SelectMany(x => x.SafeAttachments).Where(x => x.IsImage).Select(x => x.Path).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var planned = await _planner.PlanAsync(_conversationId, combined, images, _settings, turnToken);
            var planId = await _database.CreateReplyPlanAsync(_conversationId, planned, turnToken);
            activePlanId = planId;
            lock (_gate) SetStateLocked(TurnState.AgentPending);
            foreach (var message in planned)
            {
                lock (_gate) SetStateLocked(TurnState.AgentSending);
                await Task.Delay(CalculateDelay(message), turnToken);
                var delivered = message with { State = DeliveryState.Delivered };
                var messageId = await _database.DeliverPlannedMessageAsync(planId, delivered, turnToken);
                if (AgentMessageDelivered is not null) await AgentMessageDelivered(delivered with { Id = messageId });
            }
            lock (_gate) SetStateLocked(TurnState.Idle);
            await ExtractSimpleMemoriesAsync(inputs, sourceMessageIds, extractMemory, turnToken);
        }
        catch (OperationCanceledException)
        {
            if (activePlanId is not null) await _database.CancelPendingPlanMessagesAsync(activePlanId.Value, CancellationToken.None);
        }
        catch (Exception ex) { lock (_gate) SetStateLocked(TurnState.Idle); Failed?.Invoke(ex); }
    }

    private TimeSpan CalculateDelay(PlannedReplyMessage message)
    {
        var length = message.Type == ReplyMessageType.Sticker ? 4 : message.Content?.Length ?? 0;
        var style = _settings.TypingStyle;
        var estimate = 350 + Math.Pow(Math.Min(length, 80), 0.72) * 95 / Math.Max(0.25, style.Speed);
        var jitter = Random.Shared.NextDouble() * 380 * style.Burstiness;
        return TimeSpan.FromMilliseconds(Math.Clamp(estimate + jitter, style.MinDelayMs, style.MaxDelayMs));
    }

    private async Task ExtractSimpleMemoriesAsync(IReadOnlyList<UserInputPart> inputs, IReadOnlyList<long?> sourceMessageIds, IReadOnlyList<bool> extractMemory, CancellationToken cancellationToken)
    {
        for (var index = 0; index < inputs.Count; index++)
        {
            if (index < extractMemory.Count && !extractMemory[index]) continue;
            var input = inputs[index];
            var text = input.Text.Trim();
            if (text.Length is < 8 or > 300 || !new[] { "我喜欢", "我不喜欢", "我叫", "记住", "约定", "生日", "以后", "答应" }.Any(text.Contains)) continue;
            await _database.AddMemoryAsync(_characterId, "user_fact", text,
                string.Join(',', text.Split([' ', '，', '。'], StringSplitOptions.RemoveEmptyEntries).Take(5)), .65, index < sourceMessageIds.Count ? sourceMessageIds[index] : null, cancellationToken);
        }
    }

    private void SetStateLocked(TurnState state) { _state = state; StateChanged?.Invoke(state); }

    public ValueTask DisposeAsync()
    {
        lock (_gate) { _debounceCancellation?.Cancel(); _turnCancellation?.Cancel(); _debounceCancellation?.Dispose(); _turnCancellation?.Dispose(); }
        return ValueTask.CompletedTask;
    }
}

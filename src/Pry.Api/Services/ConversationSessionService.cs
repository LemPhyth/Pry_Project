using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Pry.Contracts;
using Pry.Core.Memory;
using Pry.Core.Models;
using Pry.Core.Prompting;
using Pry.Api.Runtime;
using Pry.Core.Runtime;
using Pry.Core.TurnTaking;

namespace Pry.Api.Services;

public sealed class ConversationSessionService(MemoryDatabase database, BackendRuntime runtime, MediaAssetStore media,
    ILogger<ConversationSessionService> logger, MemoryEmbeddingService? memoryRetriever = null,
    CharacterInteractionProfileService? interactionProfiles = null,
    CompanionOperationCoordinator? operations = null) : IAsyncDisposable
{
    private static readonly string[] ListeningSignals = ["嗯，我在听。", "慢慢说，我在。", "我听着呢。"];
    private readonly ConcurrentDictionary<string, Lazy<Task<ConversationSession>>> _sessions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Stack<ConversationMutationSnapshot>> _undo = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConversationEventLog> _eventLogs = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastListeningSignals = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _commandGates = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);

    public async Task<SubmitTurnResponse> SubmitAsync(string conversationId, SubmitTurnRequest request, CancellationToken token)
    {
        using var operation = operations is null ? null : await operations.EnterForegroundAsync(token);
        var content = request.StickerId is null
            ? ContractValidation.Required(request.Content, "content", 20_000)
            : request.Content?.Trim() ?? "";
        var stickerId = ContractValidation.OptionalId(request.StickerId, "stickerId");
        var attachments = await media.ResolveAttachmentsAsync(request.AttachmentIds, token);
        var commandGate = GetCommandGate(conversationId);
        await commandGate.WaitAsync(token);
        long messageId;
        try
        {
            var session = await GetAsync(conversationId, token);
            var receipt = await session.Runtime.SubmitUserInputAsync(new RuntimeUserInput(content, stickerId,
                attachments.Select(ToRuntimeAttachment).ToArray()), request.Immediate, token);
            messageId = receipt.UserMessageId ?? 0;
            session.Events.Publish("message.created", new { messageId, role = "User", content, stickerId,
                attachments = request.AttachmentIds ?? [], source = MessageSources.UserInput });
        }
        finally { commandGate.Release(); }
        return new SubmitTurnResponse(messageId);
    }

    public async Task CancelAsync(string conversationId, CancellationToken token)
    {
        using var operation = operations is null ? null : await operations.EnterForegroundAsync(token);
        var session = await GetAsync(conversationId, token);
        await session.Runtime.CancelActiveTurnAsync("client_cancelled");
        await session.Runtime.ClearPendingOutputsAsync("client_cancelled");
        await session.Runtime.DrainAsync(TimeSpan.FromSeconds(5), token);
        session.Events.Publish("turn.cancelled", new { });
    }

    public async Task<ConversationMutationResponse> DeleteMessageAsync(string conversationId, long messageId,
        CancellationToken token)
    {
        using var operation = operations is null ? null : await operations.EnterForegroundAsync(token);
        _ = await database.GetConversationAsync(conversationId, token) ?? throw new ResourceNotFoundException("conversation", conversationId);
        var message = await database.GetMessageAsync(conversationId, messageId, token) ?? throw new ResourceNotFoundException("message", messageId);
        await DrainExistingSessionAsync(conversationId, token);
        var includeFollowing = message.Role == ChatRole.User;
        var snapshot = includeFollowing
            ? await database.DeleteMessageAndFollowingAsync(conversationId, messageId, token)
            : await database.DeleteMessageAsync(conversationId, messageId, token);
        if (snapshot is null) throw new ResourceNotFoundException("message", messageId);
        PushUndo(conversationId, snapshot);
        GetEventLog(conversationId).Publish("conversation.changed", new { reason = "message_deleted", messageId,
            scope = includeFollowing ? "from_message" : "single" });
        return new ConversationMutationResponse(conversationId, messageId, includeFollowing ? "from_message" : "single",
            snapshot.Messages.Count, true);
    }

    public async Task RegenerateAsync(string conversationId, long assistantMessageId, CancellationToken token)
    {
        using var operation = operations is null ? null : await operations.EnterForegroundAsync(token);
        var message = await database.GetMessageAsync(conversationId, assistantMessageId, token) ?? throw new ResourceNotFoundException("message", assistantMessageId);
        if (message.Role != ChatRole.Assistant) throw new ApiValidationException("messageId", "只能从助手消息重新生成");
        if (message.Source is MessageSources.InteractionPreset or MessageSources.InteractionGenerated or MessageSources.Proactive)
            throw new InteractionRejectedException("interaction_not_available", "互动回应和主动消息不支持重新生成。");
        var session = await GetAsync(conversationId, token);
        await session.Runtime.CancelActiveTurnAsync("regenerate");
        await session.Runtime.ClearPendingOutputsAsync("regenerate");
        var source = await database.GetPreviousUserMessageAsync(conversationId, assistantMessageId, token)
                     ?? throw new ApiValidationException("messageId", "找不到对应的用户消息");
        var snapshot = await database.DeleteMessageAndFollowingAsync(conversationId, assistantMessageId, token)
                       ?? throw new ResourceNotFoundException("message", assistantMessageId);
        PushUndo(conversationId, snapshot);
        session.Events.Publish("conversation.changed", new { reason = "regenerating", messageId = assistantMessageId });
        await session.Runtime.RestoreTurnAsync(source.Id, ToRuntimeInput(source), token);
    }

    public async Task UndoAsync(string conversationId, CancellationToken token)
    {
        using var operation = operations is null ? null : await operations.EnterForegroundAsync(token);
        _ = await database.GetConversationAsync(conversationId, token) ?? throw new ResourceNotFoundException("conversation", conversationId);
        await DrainExistingSessionAsync(conversationId, token);
        if (!_undo.TryGetValue(conversationId, out var stack)) throw new ApiValidationException("conversationId", "没有可撤销的操作");
        ConversationMutationSnapshot snapshot;
        lock (stack)
        {
            if (!stack.TryPop(out snapshot!)) throw new ApiValidationException("conversationId", "没有可撤销的操作");
        }
        await database.RestoreConversationMutationAsync(snapshot, token);
        GetEventLog(conversationId).Publish("conversation.changed", new { reason = "mutation_undone" });
    }

    public async Task<ChatMessage> SendListeningSignalAsync(string conversationId, CancellationToken token)
    {
        using var operation = operations is null ? null : await operations.EnterForegroundAsync(token);
        _ = await database.GetConversationAsync(conversationId, token)
            ?? throw new ResourceNotFoundException("conversation", conversationId);
        var now = DateTimeOffset.UtcNow;
        if (_lastListeningSignals.TryGetValue(conversationId, out var last) && now - last < TimeSpan.FromSeconds(30))
            throw new ApiValidationException("conversationId", "倾听提示发送过于频繁");
        var session = await GetAsync(conversationId, token);
        if (!session.Runtime.IsIdle())
            throw new ApiValidationException("conversationId", "当前回合状态不允许发送倾听提示");
        var content = ListeningSignals[Random.Shared.Next(ListeningSignals.Length)];
        var id = await database.AddMessageAsync(conversationId, ChatRole.Assistant, content, null, token,
            source: MessageSources.ListeningSignal);
        _lastListeningSignals[conversationId] = now;
        var message = (await database.GetRecentMessagesAsync(conversationId, 1, token)).Single(x => x.Id == id);
        session.Events.Publish("message.created", new { messageId = id, role = ChatRole.Assistant.ToString(),
            type = ReplyMessageType.Text.ToString(), content, stickerId = (string?)null,
            source = MessageSources.ListeningSignal });
        return message;
    }

    public async Task<InteractionCommandResponse> ExecuteInteractionAsync(string conversationId,
        ExecuteInteractionRequest request, CancellationToken token)
    {
        using var operation = operations is null ? null : await operations.EnterForegroundAsync(token);
        var actionId = ContractValidation.Required(request.ActionId, "actionId", 128);
        var idempotencyKey = ContractValidation.Required(request.IdempotencyKey, "idempotencyKey", 128);
        if (!Regex.IsMatch(actionId, "^[A-Za-z0-9][A-Za-z0-9_-]{0,127}$", RegexOptions.CultureInvariant))
            throw new ApiValidationException("actionId", "ID 格式不正确");
        if (!Regex.IsMatch(idempotencyKey, "^[A-Za-z0-9][A-Za-z0-9_-]{0,127}$", RegexOptions.CultureInvariant))
            throw new ApiValidationException("idempotencyKey", "ID 格式不正确");
        var room = await database.GetConversationAsync(conversationId, token)
            ?? throw new ResourceNotFoundException("conversation", conversationId);
        var commandGate = GetCommandGate(conversationId);
        await commandGate.WaitAsync(token);
        try
        {
            var existing = await database.GetInteractionCommandAsync(conversationId, idempotencyKey, token);
            if (existing is not null)
            {
                if (existing.ActionId != actionId || existing.CharacterId != room.CharacterId)
                    throw new InteractionRejectedException("idempotency_conflict",
                        "该幂等标识已用于另一项互动，请为新手势生成新的标识。");
                return ToInteractionResponse(existing, true);
            }
            ConversationSession? activeSession = null;
            if (_sessions.TryGetValue(conversationId, out var lazy) && lazy.IsValueCreated)
                activeSession = await AwaitAndRemoveFailedAsync(conversationId, lazy);
            if (activeSession is not null && !activeSession.Runtime.IsIdle())
                throw new InteractionRejectedException("turn_busy", "当前会话正在处理消息，请稍后再互动。");
            if (string.IsNullOrWhiteSpace(room.CharacterId) || interactionProfiles is null)
                throw new InteractionRejectedException("interaction_not_available", "当前会话没有可互动的角色。");
            var profile = await interactionProfiles.GetDomainAsync(room.CharacterId, token);
            var selected = ResolveInteraction(profile, actionId);
            var result = await database.ExecuteInteractionCommandAsync(conversationId, room.CharacterId, actionId,
                idempotencyKey, selected.Text, selected.Outcome, selected.ExpressionIntent, profile.CooldownSeconds, token);
            if (result.Status == InteractionCommandWriteStatus.IdempotencyConflict)
                throw new InteractionRejectedException("idempotency_conflict", "该幂等标识已用于另一项互动。");
            if (result.Status == InteractionCommandWriteStatus.Cooldown)
                throw new InteractionRejectedException("interaction_cooldown", "互动过于频繁，请稍后再试。", result.RetryAt);
            if (result.Status == InteractionCommandWriteStatus.Created)
            {
                var events = GetEventLog(conversationId);
                events.Publish("interaction.performed", new
                {
                    actionId, characterId = room.CharacterId, result.Outcome, result.MessageId,
                    result.ExpressionIntent
                });
                if (result.MessageId is { } messageId)
                    events.Publish("message.created", new
                    {
                        messageId, role = "Assistant", type = ReplyMessageType.Text.ToString(),
                        content = selected.Text, stickerId = (string?)null, source = MessageSources.InteractionPreset
                    });
            }
            return ToInteractionResponse(result, result.Status == InteractionCommandWriteStatus.Replayed);
        }
        finally { commandGate.Release(); }
    }

    private static ResolvedInteraction ResolveInteraction(CharacterInteractionProfile profile, string actionId)
    {
        if (actionId == "poke")
            return FromReplies(profile.PokeReplies, null);
        var action = profile.Actions.FirstOrDefault(item => item.Id == actionId && item.Enabled)
            ?? throw new InteractionRejectedException("interaction_not_available", "当前角色不支持这项互动。");
        if (action.ResponseMode == InteractionResponseModes.Silent)
            return new ResolvedInteraction(null, "presentation_only", action.ExpressionIntent);
        return FromReplies(action.Replies, action.ExpressionIntent);

        static ResolvedInteraction FromReplies(IReadOnlyList<InteractionReplyPreset> source,
            string? expressionIntent)
        {
            var enabled = source.Where(item => item.Enabled).ToArray();
            if (enabled.Length == 0)
                throw new InteractionRejectedException("interaction_not_available", "当前互动没有已启用的回应预设。");
            var total = enabled.Sum(item => item.Weight);
            var target = Random.Shared.NextDouble() * total;
            foreach (var item in enabled)
            {
                target -= item.Weight;
                if (target <= 0) return new ResolvedInteraction(item.Text, "message_created", expressionIntent);
            }
            return new ResolvedInteraction(enabled[^1].Text, "message_created", expressionIntent);
        }
    }

    private static InteractionCommandResponse ToInteractionResponse(InteractionCommandWriteResult value,
        bool replayed) => new(value.ActionId, value.CharacterId, value.MessageId, value.Outcome, replayed,
        value.AcceptedAt, value.ExpressionIntent);

    public async IAsyncEnumerable<ConversationEvent> EventsAsync(string conversationId, long afterSequence,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
    {
        _ = await database.GetConversationAsync(conversationId, token)
            ?? throw new ResourceNotFoundException("conversation", conversationId);
        var events = GetEventLog(conversationId);
        var window = await database.GetProtocolEventWindowAsync(conversationId, token);
        if (afterSequence < 0)
        {
            // after=-1 的语义是"只收新事件"，它不可能越界，因此永远不该判成 buffer_expired。
            afterSequence = window.GlobalHead;
        }
        else if (window.FirstRetained is { } first && afterSequence + 1 < first)
        {
            // 判据刻意保守：误报只让客户端多取一次历史，漏报会让界面永久缺一条消息却不吭声。
            var latest = window.Latest ?? first;
            yield return new ConversationEvent(latest, "stream.resumed", DateTimeOffset.UtcNow,
                new { reason = "buffer_expired", firstRetained = first, latestSequence = latest });
        }
        await foreach (var item in events.ReadAsync(afterSequence, token)) yield return item;
    }

    private async Task<ConversationSession> GetAsync(string id, CancellationToken token)
    {
        await _lifecycleGate.WaitAsync(token);
        try
        {
            var lazy = _sessions.GetOrAdd(id, key => new Lazy<Task<ConversationSession>>(() => CreateAsync(key, token),
                LazyThreadSafetyMode.ExecutionAndPublication));
            return await AwaitAndRemoveFailedAsync(id, lazy);
        }
        finally { _lifecycleGate.Release(); }
    }

    private async Task<ConversationSession> AwaitAndRemoveFailedAsync(string id, Lazy<Task<ConversationSession>> lazy)
    {
        try { return await lazy.Value; }
        catch { _sessions.TryRemove(new KeyValuePair<string, Lazy<Task<ConversationSession>>>(id, lazy)); throw; }
    }

    private async Task<ConversationSession> CreateAsync(string conversationId, CancellationToken token)
    {
        var room = await database.GetConversationAsync(conversationId, token) ?? throw new ResourceNotFoundException("conversation", conversationId);
        var character = runtime.Characters.FirstOrDefault(x => x.Id == room.CharacterId) ?? runtime.Characters[0];
        var events = GetEventLog(conversationId);
        var composer = new DeferredReplyComposer(async (request, ct) =>
        {
            var components = await runtime.GetComponentsAsync(ct);
            var planner = new ReplyPlanner(database, (Pry.Core.Abstractions.IMemoryRetriever?)memoryRetriever ?? database,
                new PromptBuilder(), components.Router, character, character.InitialState, components.Stickers);
            return await new ReplyPlannerComposer(planner, runtime.TurnSettings).ComposeAsync(request, ct);
        });
        var store = new DatabaseRuntimeStore(database);
        ConversationRuntime? built = null;
        var sink = new ConversationRuntimeEventSink(events, logger, () => built);
        built = await ConversationRuntime.CreateAsync(new ConversationRuntimeOptions(conversationId,
            character.Id, TimeProvider.System, RuntimeSettings.FromTurnTaking(runtime.TurnSettings),
            store, composer,
            new TransitionalReplyAdvisor(),
            new StickerInterruptClassifier(new InterruptClassifier(runtime.Stickers)), sink)
        {
            CharacterNames = new[] { character.Name, character.CardName }
                .Where(x => !string.IsNullOrWhiteSpace(x)).ToArray()
        }, token);
        events.Publish("session.ready", new { conversationId, characterId = character.Id });
        return new ConversationSession(built, events, store);
    }

    private ConversationEventLog GetEventLog(string conversationId) =>
        _eventLogs.GetOrAdd(conversationId, key => new ConversationEventLog(database, key, logger));

    private SemaphoreSlim GetCommandGate(string conversationId) =>
        _commandGates.GetOrAdd(conversationId, _ => new SemaphoreSlim(1, 1));

    private void PushUndo(string conversationId, ConversationMutationSnapshot snapshot)
    {
        var stack = _undo.GetOrAdd(conversationId, _ => new Stack<ConversationMutationSnapshot>());
        lock (stack)
        {
            stack.Push(snapshot);
            if (stack.Count > 20)
            {
                var newest = stack.Take(20).ToArray(); stack.Clear();
                for (var index = newest.Length - 1; index >= 0; index--) stack.Push(newest[index]);
            }
        }
    }

    private async Task DrainExistingSessionAsync(string conversationId, CancellationToken token)
    {
        if (!_sessions.TryGetValue(conversationId, out var lazy) || !lazy.IsValueCreated) return;
        var session = await lazy.Value;
        await session.Runtime.CancelActiveTurnAsync("session_drained");
        await session.Runtime.ClearPendingOutputsAsync("session_drained");
        await session.Runtime.DrainAsync(TimeSpan.FromSeconds(5), token);
    }

    public async ValueTask DisposeAsync()
    {
        await ResetAsync();
        foreach (var gate in _commandGates.Values) gate.Dispose();
        _commandGates.Clear();
        _lifecycleGate.Dispose();
    }

    public async Task ResetAsync()
    {
        using var operation = operations is null ? null : await operations.EnterForegroundAsync(CancellationToken.None);
        await _lifecycleGate.WaitAsync();
        try { await ResetCoreAsync(); }
        finally { _lifecycleGate.Release(); }
    }

    public async Task ReconfigureAsync(Func<CancellationToken, Task> action, CancellationToken token)
    {
        using var operation = operations is null ? null : await operations.EnterForegroundAsync(token);
        await _lifecycleGate.WaitAsync(token);
        try { await ResetCoreAsync(); await action(token); }
        finally { _lifecycleGate.Release(); }
    }

    private async Task ResetCoreAsync()
    {
        foreach (var lazy in _sessions.Values)
            if (lazy.IsValueCreated) try { var session = await lazy.Value; await session.Runtime.DisposeAsync(); } catch { }
        _sessions.Clear();
        // 清掉的只是进程内实例；序号与补投窗口在 protocol_events 里，重配置不再让客户端游标作废。
        _undo.Clear(); _eventLogs.Clear();
    }

    private sealed record ResolvedInteraction(string? Text, string Outcome, string? ExpressionIntent);

    public bool HasActiveTurns => _sessions.Values.Any(lazy => lazy.IsValueCreated &&
        (!lazy.Value.IsCompletedSuccessfully || !lazy.Value.Result.Runtime.IsIdle()));

    public bool HasActiveTurnsExcept(string conversationId) => _sessions.Any(pair => pair.Key != conversationId &&
        pair.Value.IsValueCreated && (!pair.Value.Value.IsCompletedSuccessfully || !pair.Value.Value.Result.Runtime.IsIdle()));

    /// <summary>
    /// 空闲驱逐：把长时间完全安静、可安全重建的运行时从内存卸载。
    /// 丢掉的只有进程内缓冲；会话连续性与 fencing epoch 一直在持久层。
    /// </summary>
    public async Task<int> EvictIdleRuntimesAsync(CancellationToken token = default)
    {
        var at = DateTimeOffset.UtcNow;
        var evicted = 0;
        foreach (var pair in _sessions.ToArray())
        {
            var command = GetCommandGate(pair.Key);
            await command.WaitAsync(token);
            try
            {
            await _lifecycleGate.WaitAsync(token);
            try
            {
            if (!pair.Value.IsValueCreated) continue;
            ConversationSession session;
            try { session = await pair.Value.Value; } catch { continue; }
            if (!session.Runtime.CanEvict(at)) continue;
            if (!_sessions.TryRemove(new KeyValuePair<string, Lazy<Task<ConversationSession>>>(pair.Key, pair.Value)))
                continue;
            await session.Runtime.DisposeAsync();
            evicted++;
            }
            finally { _lifecycleGate.Release(); }
            }
            finally { command.Release(); }
        }
        return evicted;
    }

    public void PublishReadWatermark(string conversationId, long lastReadMessageId) =>
        GetEventLog(conversationId).Publish("conversation.read", new { conversationId, lastReadMessageId });

    public async Task NotifyActivityAsync(string conversationId, bool typing, bool returned, CancellationToken token)
    {
        var command = GetCommandGate(conversationId);
        await command.WaitAsync(token);
        try
        {
            if (!_sessions.TryGetValue(conversationId, out var lazy) || !lazy.IsValueCreated) return;
            var rt = (await lazy.Value).Runtime;
            if (returned) await rt.NotifyUserReturnedAsync(token);
            if (typing || rt.Observe().UserTyping) await rt.NotifyTypingAsync(typing, token);
        }
        finally { command.Release(); }
    }

    /// <summary>当前驻留在内存里的会话键，仅供诊断读取；读取它不会创建运行时。</summary>
    public IReadOnlyList<string> ActiveConversationIds =>
        [.. _sessions.Keys.OrderBy(x => x, StringComparer.Ordinal)];

    /// <summary>
    /// 只读运行时诊断。绝不为了"看一眼"而把运行时创建出来：未启动或已被空闲驱逐时，
    /// 只回持久层的 fencing 水位。轨迹与轮次摘要都不含消息正文。
    /// </summary>
    public async Task<ConversationRuntimeStateResponse> GetRuntimeStateAsync(string conversationId,
        CancellationToken token)
    {
        var room = await database.GetConversationAsync(conversationId, token)
            ?? throw new ResourceNotFoundException("conversation", conversationId);
        var persisted = await database.LoadRuntimeStateAsync(conversationId, token);
        var baseShape = new ConversationRuntimeStateResponse
        {
            ConversationId = conversationId,
            CharacterId = room.CharacterId,
            Started = false,
            Epoch = persisted?.Epoch ?? 0,
            Persisted = persisted is null ? null : new RuntimePersistenceSummary(persisted.Epoch,
                persisted.LastSpeakerWasCharacter, persisted.LastActivityAt, persisted.ConsecutiveCharacterTurns)
        };
        if (!_sessions.TryGetValue(conversationId, out var lazy) || !lazy.IsValueCreated) return baseShape;
        var runtime = (await lazy.Value).Runtime;
        var at = DateTimeOffset.UtcNow;
        var observed = runtime.Observe();
        return baseShape with
        {
            Started = true,
            Phase = runtime.Phase.ToString(),
            Revision = observed.Revision,
            Epoch = runtime.Epoch,
            ConversationProgress = runtime.ConversationProgress,
            UserTyping = observed.UserTyping,
            PendingInputCount = observed.PendingInputCount,
            PendingOutputCount = observed.PendingOutputCount,
            PendingOutputTurns = [.. runtime.PendingOutputs().Select(x => x.Turn.ToString()).Distinct()],
            ActiveTurn = observed.ActiveTurn?.ToString(),
            DeliveringTurn = observed.DeliveringTurn?.ToString(),
            LastUserActivity = observed.LastUserActivity,
            LastCharacterActivity = observed.LastCharacterActivity,
            IdleForMs = runtime.IdleFor(at).TotalMilliseconds,
            Evictable = runtime.CanEvict(at),
            InvalidatedTurns = runtime.InvalidatedTurns,
            SchedulerBackoffMs = runtime.SchedulerBackoffMs,
            RecentTurns = [.. observed.RecentTurns.Select(x => new RuntimeTurnSummary(x.Identity.TurnId,
                x.Identity.GenerationId, x.Identity.FencedRevision, x.OpenedAt, x.CompletedAt, x.Outcome.ToString(),
                x.OutcomeReason, x.InputCount, x.PlannedCount, x.DeliveredCount))]
        };
    }

    /// <summary>决策轨迹尾段。上限只是护栏不是权限；未启动时显式报错而不是回空数组。</summary>
    public async Task<ConversationRuntimeTraceResponse> GetRuntimeTraceAsync(string conversationId, int limit,
        CancellationToken token)
    {
        if (await database.GetConversationAsync(conversationId, token) is null)
            throw new ResourceNotFoundException("conversation", conversationId);
        var take = Math.Clamp(limit <= 0 ? 80 : limit, 1, 300);
        if (!_sessions.TryGetValue(conversationId, out var lazy) || !lazy.IsValueCreated)
            throw new ResourceNotFoundException("runtime_not_started", conversationId);
        var runtime = (await lazy.Value).Runtime;
        var snapshot = runtime.Snapshot();
        return new ConversationRuntimeTraceResponse(conversationId, snapshot.Revision, snapshot.Epoch,
            [.. runtime.TraceEntries.TakeLast(take).Select(x => new RuntimeTraceEntryResponse(x.Sequence,
                x.Kind.ToString(), x.Action, x.Event?.ToString(), x.Turn?.TurnId, x.Turn?.GenerationId,
                x.Turn?.FencedRevision, x.Reason, x.RevisionBefore, x.RevisionAfter, x.At))]);
    }

    public async Task<(ConversationRuntime Runtime, ProactiveCandidate Candidate)> CaptureProactiveAsync(
        ProactiveAttempt attempt, CancellationToken token)
    {
        var session = await GetAsync(attempt.Policy.Policy.ConversationId!, token);
        var rt = session.Runtime;
        var now = DateTimeOffset.UtcNow;
        return (rt, new ProactiveCandidate(attempt.Id, rt.ConversationId, rt.CharacterInstanceId, "idle_threshold",
            new TurnIdentity(0, 0, rt.Epoch), rt.ConversationProgress, now, now.AddMinutes(2),
            ResponsePlan.Nothing("generation_pending"), attempt.Id));
    }

    public async Task<DeliveryReport> DeliverProactiveAsync(ConversationRuntime owner, ProactiveCandidate candidate,
        Func<DeliveryPermission, RuntimePlannedMessage, CancellationToken, Task<long?>> commit, CancellationToken token)
    {
        if (!_sessions.TryGetValue(candidate.ConversationId, out var lazy) || !lazy.IsValueCreated)
            return new(candidate.CapturedIdentity, candidate.Plan.Messages.Count, 0, candidate.Plan.Messages.Count, "runtime_evicted");
        var session = await lazy.Value;
        if (!ReferenceEquals(owner, session.Runtime))
            return new(candidate.CapturedIdentity, candidate.Plan.Messages.Count, 0, candidate.Plan.Messages.Count, "runtime_replaced");
        session.Store.RegisterProactive(candidate.AttemptToken!, commit);
        try { return await owner.DeliverProactiveAsync(candidate, token); }
        finally { session.Store.RemoveProactive(candidate.AttemptToken!); }
    }

    /// <summary>
    /// 主动消息的统一发送前复核。Runtime 用实时状态判断候选是否仍然可发：
    /// 用户正在输入、有排队输出、有进行中的 Turn 或候选上下文已推进，一律不发。
    /// 该会话没有活着的运行时时返回 null，调用方必须按"保守拒绝"处理。
    /// </summary>
    public async Task<ProactiveReview?> ReviewProactiveAsync(ProactiveCandidate candidate, CancellationToken token)
    {
        if (!_sessions.TryGetValue(candidate.ConversationId, out var lazy) || !lazy.IsValueCreated) return null;
        var session = await lazy.Value;
        return await session.Runtime.ReviewProactiveAsync(candidate, token);
    }

    private static RuntimeAttachment ToRuntimeAttachment(ChatAttachment attachment) =>
        new(attachment.Path, attachment.Kind switch
        {
            ChatAttachmentKind.Image => RuntimeAttachmentKind.Image,
            ChatAttachmentKind.Text => RuntimeAttachmentKind.Text,
            ChatAttachmentKind.Csv => RuntimeAttachmentKind.Csv,
            _ => RuntimeAttachmentKind.Docx
        }, attachment.Name);

    private static RuntimeUserInput ToRuntimeInput(ChatMessage message) => new(message.Content, message.StickerId,
        string.IsNullOrWhiteSpace(message.ImagePath) ? [] :
        [new RuntimeAttachment(message.ImagePath, RuntimeAttachmentKind.Image, Path.GetFileName(message.ImagePath))]);
}

public sealed record ConversationSession(ConversationRuntime Runtime, ConversationEventLog Events, DatabaseRuntimeStore Store);

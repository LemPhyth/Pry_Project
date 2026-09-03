using System.Collections.Concurrent;
using Pry.Api.Contracts;
using Pry.Core.Memory;
using Pry.Core.Models;
using Pry.Core.Prompting;
using Pry.Core.TurnTaking;

namespace Pry.Api.Services;

public sealed class ConversationSessionService(MemoryDatabase database, BackendRuntime runtime, MediaAssetStore media,
    ILogger<ConversationSessionService> logger) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, Lazy<Task<ConversationSession>>> _sessions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Stack<ConversationMutationSnapshot>> _undo = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConversationEventLog> _eventLogs = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);

    public async Task<SubmitTurnResponse> SubmitAsync(string conversationId, SubmitTurnRequest request, CancellationToken token)
    {
        var content = request.StickerId is null
            ? ContractValidation.Required(request.Content, "content", 20_000)
            : request.Content?.Trim() ?? "";
        var stickerId = ContractValidation.OptionalId(request.StickerId, "stickerId");
        var attachments = await media.ResolveAttachmentsAsync(request.AttachmentIds, token);
        var session = await GetAsync(conversationId, token);
        var messageId = await session.Manager.SubmitUserInputAsync(new UserInputPart(content, stickerId, attachments), request.Immediate, token);
        session.Events.Publish("message.created", new { messageId, role = "User", content, stickerId,
            attachments = request.AttachmentIds ?? [] });
        return new SubmitTurnResponse(messageId);
    }

    public async Task CancelAsync(string conversationId, CancellationToken token)
    {
        var session = await GetAsync(conversationId, token); await session.Manager.CancelAndDrainAsync(token);
        session.Events.Publish("turn.cancelled", new { });
    }

    public async Task<ConversationMutationResponse> DeleteMessageAsync(string conversationId, long messageId,
        CancellationToken token)
    {
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
        var message = await database.GetMessageAsync(conversationId, assistantMessageId, token) ?? throw new ResourceNotFoundException("message", assistantMessageId);
        if (message.Role != ChatRole.Assistant) throw new ApiValidationException("messageId", "只能从助手消息重新生成");
        var session = await GetAsync(conversationId, token);
        await session.Manager.CancelAndDrainAsync(token);
        var source = await database.GetPreviousUserMessageAsync(conversationId, assistantMessageId, token)
                     ?? throw new ApiValidationException("messageId", "找不到对应的用户消息");
        var snapshot = await database.DeleteMessageAndFollowingAsync(conversationId, assistantMessageId, token)
                       ?? throw new ResourceNotFoundException("message", assistantMessageId);
        PushUndo(conversationId, snapshot);
        session.Events.Publish("conversation.changed", new { reason = "regenerating", messageId = assistantMessageId });
        session.Manager.RegenerateFromExistingUserMessage(source);
    }

    public async Task UndoAsync(string conversationId, CancellationToken token)
    {
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

    public async IAsyncEnumerable<ConversationEvent> EventsAsync(string conversationId, long afterSequence,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
    {
        var session = await GetAsync(conversationId, token);
        await foreach (var item in session.Events.ReadAsync(afterSequence, token)) yield return item;
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
        var components = await runtime.GetComponentsAsync(token);
        var character = components.ResolveCharacter(room.CharacterId, null);
        var planner = new ReplyPlanner(database, new PromptBuilder(), components.Router, character, character.InitialState, components.Stickers);
        var manager = new ConversationTurnManager(database, planner, new Pry.Core.TurnTaking.InterruptClassifier(components.Stickers),
            runtime.TurnSettings, conversationId, character.Id);
        var events = GetEventLog(conversationId);
        manager.StateChanged += state => events.Publish("turn.state", new { state = state.ToString() });
        manager.AgentMessageDelivered += message =>
        {
            events.Publish("message.created", new { messageId = message.Id, role = "Assistant", type = message.Type.ToString(), message.Content, message.StickerId });
            return Task.CompletedTask;
        };
        manager.Failed += ex =>
        {
            logger.LogError(ex, "Conversation turn failed for {ConversationId}", conversationId);
            events.Publish("turn.failed", new { code = "model_failure", message = "模型暂时无法完成回复" });
        };
        events.Publish("session.ready", new { conversationId, characterId = character.Id });
        return new ConversationSession(manager, events);
    }

    private ConversationEventLog GetEventLog(string conversationId) =>
        _eventLogs.GetOrAdd(conversationId, _ => new ConversationEventLog());

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
        var session = await lazy.Value; await session.Manager.CancelAndDrainAsync(token);
    }

    public async ValueTask DisposeAsync()
    {
        await ResetAsync();
        _lifecycleGate.Dispose();
    }

    public async Task ResetAsync()
    {
        await _lifecycleGate.WaitAsync();
        try { await ResetCoreAsync(); }
        finally { _lifecycleGate.Release(); }
    }

    public async Task ReconfigureAsync(Func<CancellationToken, Task> action, CancellationToken token)
    {
        await _lifecycleGate.WaitAsync(token);
        try { await ResetCoreAsync(); await action(token); }
        finally { _lifecycleGate.Release(); }
    }

    private async Task ResetCoreAsync()
    {
        foreach (var lazy in _sessions.Values)
            if (lazy.IsValueCreated) try { await (await lazy.Value).Manager.DisposeAsync(); } catch { }
        _sessions.Clear();
        _undo.Clear(); _eventLogs.Clear();
    }
}

public sealed record ConversationSession(ConversationTurnManager Manager, ConversationEventLog Events);

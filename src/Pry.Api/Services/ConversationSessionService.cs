using System.Collections.Concurrent;
using Pry.Api.Contracts;
using Pry.Core.Memory;
using Pry.Core.Models;
using Pry.Core.Prompting;
using Pry.Core.TurnTaking;

namespace Pry.Api.Services;

public sealed class ConversationSessionService(MemoryDatabase database, BackendRuntime runtime,
    ILogger<ConversationSessionService> logger) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, Lazy<Task<ConversationSession>>> _sessions = new(StringComparer.Ordinal);

    public async Task<SubmitTurnResponse> SubmitAsync(string conversationId, SubmitTurnRequest request, CancellationToken token)
    {
        var content = request.StickerId is null
            ? ContractValidation.Required(request.Content, "content", 20_000)
            : request.Content?.Trim() ?? "";
        var stickerId = ContractValidation.OptionalId(request.StickerId, "stickerId");
        var session = await GetAsync(conversationId, token);
        var messageId = await session.Manager.SubmitUserInputAsync(new UserInputPart(content, stickerId), request.Immediate, token);
        session.Events.Publish("message.created", new { messageId, role = "User", content, stickerId });
        return new SubmitTurnResponse(messageId);
    }

    public async Task CancelAsync(string conversationId, CancellationToken token)
    {
        var session = await GetAsync(conversationId, token); session.Manager.CancelAgentReply();
        session.Events.Publish("turn.cancelled", new { });
    }

    public async IAsyncEnumerable<ConversationEvent> EventsAsync(string conversationId, long afterSequence,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
    {
        var session = await GetAsync(conversationId, token);
        await foreach (var item in session.Events.ReadAsync(afterSequence, token)) yield return item;
    }

    private Task<ConversationSession> GetAsync(string id, CancellationToken token)
    {
        var lazy = _sessions.GetOrAdd(id, key => new Lazy<Task<ConversationSession>>(() => CreateAsync(key, token),
            LazyThreadSafetyMode.ExecutionAndPublication));
        return AwaitAndRemoveFailedAsync(id, lazy);
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
        var events = new ConversationEventLog();
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

    public async ValueTask DisposeAsync()
    {
        foreach (var lazy in _sessions.Values)
            if (lazy.IsValueCreated) try { await (await lazy.Value).Manager.DisposeAsync(); } catch { }
        _sessions.Clear();
    }
}

public sealed record ConversationSession(ConversationTurnManager Manager, ConversationEventLog Events);

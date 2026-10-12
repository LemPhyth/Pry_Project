using System.Collections.Concurrent;
using Pry.Api.Services;
using Pry.Core.Inference;
using Pry.Core.Intelligence;
using Pry.Core.Memory;
using Pry.Core.Models;
using Pry.Core.Runtime;
using Pry.Core.TurnTaking;

namespace Pry.Api.Runtime;

/// <summary>
/// Runtime 持久化端口的宿主实现。Runtime 侧不出现任何 SQL 或 SQLite 类型；
/// 所有条件写入都委托给 <see cref="MemoryDatabase"/> 的 runtime 部分。
/// </summary>
public sealed class DatabaseRuntimeStore(MemoryDatabase database) : IRuntimeStore
{
    private readonly ConcurrentDictionary<string, Func<DeliveryPermission, RuntimePlannedMessage, CancellationToken, Task<long?>>> _proactive = new();

    public void RegisterProactive(string id,
        Func<DeliveryPermission, RuntimePlannedMessage, CancellationToken, Task<long?>> commit) => _proactive[id] = commit;

    public void RemoveProactive(string id) => _proactive.TryRemove(id, out _);
    public Task<RuntimePersistenceState?> LoadStateAsync(string conversationId, CancellationToken token) =>
        database.LoadRuntimeStateAsync(conversationId, token);

    public Task SaveStateAsync(string conversationId, RuntimePersistenceState state, CancellationToken token) =>
        database.SaveRuntimeStateAsync(conversationId, state, token);

    public Task<long> ReadEpochAsync(string conversationId, CancellationToken token) =>
        database.ReadRuntimeEpochAsync(conversationId, token);

    public Task<long> BumpEpochAsync(string conversationId, CancellationToken token) =>
        database.BumpRuntimeEpochAsync(conversationId, token);

    public async Task<long> AppendUserInputAsync(string conversationId, RuntimeUserInput input, CancellationToken token)
    {
        var image = input.SafeAttachments.FirstOrDefault(x => x.IsImage);
        return await database.AddMessageAsync(conversationId, ChatRole.User, input.Text, image?.Reference, token,
            input.StickerId, MessageSources.UserInput);
    }

    public Task<DeliveryPermission> RequestDeliveryAsync(string conversationId, TurnIdentity turn, long fenceEpoch,
        IReadOnlyList<RuntimePlannedMessage> messages, string source, string? commitToken, CancellationToken token) =>
        database.RequestRuntimeDeliveryAsync(conversationId, turn, fenceEpoch, messages.Count, source, commitToken,
            messages, token);

    public Task<long?> CommitBubbleAsync(DeliveryPermission permission, int sequence, RuntimePlannedMessage message,
        CancellationToken token) => permission.Source == MessageSources.Proactive
        ? permission.CommitToken is { } id && _proactive.TryGetValue(id, out var commit)
            ? commit(permission, message, token) : Task.FromResult<long?>(null)
        : database.CommitRuntimeBubbleAsync(permission, sequence, message, token);

    public Task AbandonPlanAsync(DeliveryPermission permission, int fromSequence, CancellationToken token) =>
        database.AbandonRuntimePlanAsync(permission, fromSequence, token);

    public Task<IReadOnlyList<long>> SweepStalePlansAsync(CancellationToken token) =>
        database.SweepStaleRuntimePlansAsync(token);
}

public sealed class DeferredReplyComposer(Func<TurnRequest, CancellationToken, Task<ResponsePlan>> compose) : IReplyComposer
{
    public Task<ResponsePlan> ComposeAsync(TurnRequest request, CancellationToken token) => compose(request, token);
}

/// <summary>
/// 生成端口：复用现有 ReplyPlanner（Prompt 组装、模型选择、结构化解析都在那一侧）。
/// Runtime 交出的是 Turn 的事实与建议，拿回的是 ResponsePlan，不关心模型怎么调。
/// </summary>
public sealed class ReplyPlannerComposer(ReplyPlanner planner, TurnTakingSettings settings) : IReplyComposer
{
    public async Task<ResponsePlan> ComposeAsync(TurnRequest request, CancellationToken token)
    {
        var parts = new List<string>();
        foreach (var input in request.Inputs.Select(x => x.Input).Where(x => x is not null))
        {
            if (!string.IsNullOrWhiteSpace(input!.Text)) parts.Add(input.Text);
            else if (!string.IsNullOrWhiteSpace(input.StickerId)) parts.Add($"[用户发送表情：{input.StickerId}]");
            foreach (var attachment in input.SafeAttachments.Where(x => !x.IsImage))
                parts.Add(await AttachmentTextExtractor.ExtractForPromptAsync(ToChatAttachment(attachment), token));
        }
        var combined = string.Join("\n", parts);
        var planned = await planner.PlanAsync(request.ConversationId, combined, request.ImageReferences, settings, token,
            request.Advice?.PromptBlock);
        var messages = planned.Select(x => new RuntimePlannedMessage(x.Sequence, x.Content, x.StickerId)).ToArray();
        if (messages.Length == 0) return ResponsePlan.Nothing("empty_model_output");
        return new ResponsePlan(request.Advice?.Intent ?? ResponseIntent.ContinueTopic, messages,
            Source: MessageSources.ModelReply);
    }

    internal static ChatAttachment ToChatAttachment(RuntimeAttachment attachment) =>
        new(attachment.Reference, attachment.Kind switch
        {
            RuntimeAttachmentKind.Image => ChatAttachmentKind.Image,
            RuntimeAttachmentKind.Text => ChatAttachmentKind.Text,
            RuntimeAttachmentKind.Csv => ChatAttachmentKind.Csv,
            _ => ChatAttachmentKind.Docx
        }, attachment.Name);
}

/// <summary>复用 Core 的插话分类器，把它的结论翻译成 Runtime 的中断强度。</summary>
public sealed class StickerInterruptClassifier(InterruptClassifier classifier) : IInterruptClassifier
{
    public InterruptAssessment Classify(RuntimeUserInput input, RuntimeObservedState observed)
    {
        var kind = classifier.Classify(new UserInputPart(input.Text, input.StickerId,
            input.SafeAttachments.Select(ReplyPlannerComposer.ToChatAttachment).ToArray()));
        return kind switch
        {
            InterruptKind.Backchannel => new InterruptAssessment(InterruptStrength.Backchannel, "backchannel"),
            InterruptKind.HardInterrupt => new InterruptAssessment(InterruptStrength.Hard, "hard_signal"),
            _ => new InterruptAssessment(InterruptStrength.Soft, "soft")
        };
    }
}

/// <summary>
/// 输出通道：把 Runtime 的事件映射成客户端已经在消费的那几个 SSE 事件与负载形状。
/// 普通回复、主动消息与倾听提示都走这里，不存在第二条发送路径。
/// </summary>
public sealed class ConversationRuntimeEventSink(ConversationEventLog events, ILogger logger,
    Func<ConversationRuntime?>? runtimeAccessor = null) : IRuntimeOutputSink
{
    /// <summary>目标 Runtime 阶段到旧 TurnState 名的投影，保持客户端兼容。</summary>
    public static string ToLegacyTurnState(RuntimePhase phase) => phase switch
    {
        RuntimePhase.Planning => "ModelThinking",
        RuntimePhase.Delivering => "AgentSending",
        RuntimePhase.CollectingInput or RuntimePhase.WaitingForTurn => "UserPending",
        _ => "Idle"
    };

    public void PublishPhase(RuntimePhase phase) =>
        events.Publish("turn.state", new { state = ToLegacyTurnState(phase) });

    public Task MessageDeliveredAsync(string conversationId, long messageId, RuntimePlannedMessage message,
        string source, CancellationToken token)
    {
        events.Publish("message.created", new
        {
            messageId, role = "Assistant",
            type = (message.IsSticker ? ReplyMessageType.Sticker : ReplyMessageType.Text).ToString(),
            text = message.Text, content = message.Text, message.StickerId, source
        });
        return Task.CompletedTask;
    }

    public Task TypingIndicatorAsync(string conversationId, bool active, CancellationToken token) =>
        Task.CompletedTask;

    public Task PhaseChangedAsync(string conversationId, RuntimePhase phase, RuntimePhase previous,
        CancellationToken token)
    {
        PublishPhase(phase);
        return Task.CompletedTask;
    }

    public Task TurnTransitionAsync(string conversationId, TurnIdentity turn, RuntimeTurnOutcome outcome,
        string reason, CancellationToken token)
    {
        logger.LogDebug("Turn {TurnId}.g{Generation} 结束：{Outcome}（{Reason}）", turn.TurnId, turn.GenerationId,
            outcome, reason);
        // 只有实际交付过的轮次补发 turn.completed：取消与失败已分别由 turn.cancelled / turn.failed 表达，
        // 沉默、推迟或被围栏淘汰的轮次没有结束任何界面在等的东西，重复下发终态会把它们误显示成一次回复完成。
        if (outcome is RuntimeTurnOutcome.Completed or RuntimeTurnOutcome.PartiallyDelivered)
            events.Publish("turn.completed", new
            {
                turnId = turn.TurnId, generationId = turn.GenerationId, outcome = outcome.ToString(), reason
            });
        return Task.CompletedTask;
    }

    public async Task CapabilityRequestedAsync(string conversationId, CapabilityRequest request,
        CancellationToken token)
    {
        // Capability Registry 属于 RT-007。这里必须明确回一个不可用结果，
        // 否则等待 Capability 的那一轮会一直挂着，Runtime 再也不会投递任何内容。
        logger.LogInformation("Capability {CapabilityId} 尚未接入，按不可用返回", request.CapabilityId);
        if (runtimeAccessor?.Invoke() is not { } runtime) return;
        await runtime.AcceptAsync(RuntimeEvent.Capability(TimeProvider.System.GetUtcNow(),
            new CapabilityResult(request.CapabilityId, request.RequestedBy, false, null, "capability_unavailable")),
            token);
    }

    public Task FailureAsync(string conversationId, TurnIdentity turn, string errorCode, CancellationToken token)
    {
        events.Publish("turn.failed", new { code = errorCode, message = "模型暂时无法完成回复" });
        return Task.CompletedTask;
    }
}

using Pry.Core.Intelligence;
using Pry.Core.Models;

namespace Pry.Core.Runtime;

/// <summary>
/// ConversationRuntime 的持久阶段。可恢复的只有这些状态；进程内的输入缓冲和调度计时器属于临时状态。
/// </summary>
public enum RuntimePhase
{
    Idle,
    CollectingInput,
    WaitingForTurn,
    Planning,
    Delivering,
    Suspended
}

/// <summary>进入 Suspended 的原因。恢复只能由显式事件触发，不会自动发生。</summary>
public enum SuspendReason
{
    None,
    SuspendedByHost,
    StoreUnavailable
}

public enum RuntimeEventKind
{
    UserInput,
    TypingStarted,
    TypingStopped,
    TurnDue,
    IdleElapsed,
    UserReturned,
    ProactiveCandidate,
    CapabilityResult,
    Suspend,
    Resume,
    Invalidate
}

/// <summary>
/// 事件是 Runtime 的唯一入口。RevisionAfter 由宿主在投递持久效果后回填，
/// 用于让上层投影（SSE/Developer Mode）判断自己是否已落后于状态真相。
/// </summary>
public sealed record RuntimeEvent(RuntimeEventKind Kind, DateTimeOffset OccurredAt, int RevisionAfter = 0)
{
    public static RuntimeEvent UserInput(DateTimeOffset at, RuntimeUserInput input, bool forceTurn = false,
        int revisionAfter = 0) =>
        new(RuntimeEventKind.UserInput, at, revisionAfter) { Input = input, ForceTurn = forceTurn };

    public static RuntimeEvent UserInput(DateTimeOffset at, string text, bool forceTurn = false) =>
        UserInput(at, new RuntimeUserInput(text), forceTurn);

    public static RuntimeEvent Typing(bool started, DateTimeOffset at) =>
        new(started ? RuntimeEventKind.TypingStarted : RuntimeEventKind.TypingStopped, at);

    public static RuntimeEvent TurnDue(DateTimeOffset at) => new(RuntimeEventKind.TurnDue, at);

    public static RuntimeEvent Proactive(DateTimeOffset at, ProactiveCandidate candidate) =>
        new(RuntimeEventKind.ProactiveCandidate, at) { Candidate = candidate };

    public static RuntimeEvent Capability(DateTimeOffset at, CapabilityResult result) =>
        new(RuntimeEventKind.CapabilityResult, at) { CapabilityOutcome = result };

    public RuntimeUserInput? Input { get; init; }
    public ProactiveCandidate? Candidate { get; init; }
    public CapabilityResult? CapabilityOutcome { get; init; }
    public SuspendReason Reason { get; init; } = SuspendReason.None;
    public bool ForceTurn { get; init; }
    public string? Detail { get; init; }
}

/// <summary>一条尚未成为 Turn 的用户输入。Runtime 只看待投递的输入，不持有消息主键。</summary>
public sealed record RuntimeUserInput(string Text, string? StickerId = null,
    IReadOnlyList<RuntimeAttachment>? Attachments = null)
{
    public IReadOnlyList<RuntimeAttachment> SafeAttachments => Attachments ?? [];
}

public enum RuntimeAttachmentKind { Image, Text, Csv, Docx }

public sealed record RuntimeAttachment(string Reference, RuntimeAttachmentKind Kind, string Name)
{
    public bool IsImage => Kind == RuntimeAttachmentKind.Image;
}

/// <summary>
/// Turn 的稳定身份。GenerationId 每次重新规划递增，FencedRevision 记录该轮形成时的失效 epoch，
/// 三者共同构成 stale-result fencing 的比较依据。
/// </summary>
public sealed record TurnIdentity(long TurnId, long GenerationId, long FencedRevision)
{
    public static readonly TurnIdentity None = new(0, 0, 0);
    public bool IsNone => TurnId == 0 && GenerationId == 0;
}

/// <summary>
/// Runtime 视角的一次 Turn 终局。Intelligence 侧的"报告投递结果"契约是
/// <see cref="Pry.Core.Intelligence.TurnOutcome"/>，由宿主在 Completed/PartiallyDelivered 之后构造；
/// 这里只描述运行时事实，不重复定义学习语义。
/// </summary>
public enum RuntimeTurnOutcome
{
    Completed,
    PartiallyDelivered,
    DoNothing,
    Deferred,
    Interrupted,
    Failed,
    Evicted,
    Superseded,
    FencedOut
}

public sealed record TurnRecord(TurnIdentity Identity, DateTimeOffset OpenedAt, string CombinedInput,
    int InputCount, DateTimeOffset? CompletedAt = null, RuntimeTurnOutcome Outcome = RuntimeTurnOutcome.Completed,
    string? OutcomeReason = null, int DeliveredCount = 0, int PlannedCount = 0)
{
    public TurnRecord Close(DateTimeOffset at, RuntimeTurnOutcome outcome, string? reason, int delivered) =>
        this with { CompletedAt = at, Outcome = outcome, OutcomeReason = reason, DeliveredCount = delivered };
}

/// <summary>模型/Intelligence 输出的不是气泡，而是带投递提示的计划。</summary>
public sealed record RuntimePlannedMessage(int Sequence, string? Text, string? StickerId,
    IReadOnlyList<string>? MediaReferences = null)
{
    public bool IsSticker => StickerId is not null && string.IsNullOrWhiteSpace(Text);
}

/// <summary>
/// DeliveryScheduler 的输入提示。为 null 表示由节奏推导决定，而不是固定字符数公式；
/// 具体取舍属于 Expression/Intelligence，Runtime 只消费。
/// </summary>
public sealed record DeliveryHints(TimeSpan? FirstMessageDelay, IReadOnlyList<TimeSpan>? InterMessageDelays,
    bool EmitTypingIndicator = true)
{
    public static readonly DeliveryHints Default = new(null, null);
}

public sealed record CapabilityRequest(string CapabilityId, IReadOnlyDictionary<string, string> Arguments,
    TurnIdentity RequestedBy, string? ReplyWhenDone = null);

public sealed record CapabilityResult(string CapabilityId, TurnIdentity ForTurn, bool Succeeded, string? Output,
    string? ErrorCode);

public sealed record ResponsePlan(ResponseIntent Intent, IReadOnlyList<RuntimePlannedMessage> Messages,
    CapabilityRequest? CapabilityRequest = null, DeliveryHints? Hints = null, bool DoNothing = false,
    string? DoNothingReason = null, string Source = MessageSources.ModelReply, string? CommitToken = null,
    bool ProactiveOrigin = false)
{
    public static ResponsePlan Nothing(string reason) =>
        new(ResponseIntent.DoNothing, [], DoNothing: true, DoNothingReason: reason);

    public bool IsSilent => DoNothing || Intent == ResponseIntent.DoNothing ||
        (Messages.Count == 0 && CapabilityRequest is null);
}

/// <summary>
/// 主动消息候选。生成不等于发送，必须经过同一套 Gate、中断与投递通道。
/// CapturedProgress 是捕获时的"会话进度水位"（用户说了几次、角色发了几条）：
/// 只比较失效 epoch 不够——用户回来、聊过一轮之后，基于旧上下文生成的候选即使没被打断也已经过时。
/// </summary>
public sealed record ProactiveCandidate(string CandidateId, string ConversationId, string CharacterInstanceId,
    string TriggerReason, TurnIdentity CapturedIdentity, long CapturedProgress, DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt, ResponsePlan Plan, string? AttemptToken = null)
{
    public bool Expired(DateTimeOffset at) => at >= ExpiresAt;
}

public enum ProactiveDisposition
{
    Approved,
    Deferred,
    RejectedExpired,
    RejectedStale,
    RejectedUserActive,
    RejectedTurnBusy,
    RejectedSuspended,
    RejectedPolicy,
    RejectedRuntimeUnavailable
}

public sealed record ProactiveReview(ProactiveDisposition Disposition, string Reason, DateTimeOffset ReviewedAt,
    int Revision)
{
    public bool Approved => Disposition == ProactiveDisposition.Approved;
}

/// <summary>
/// Runtime 对一次外部投递请求的裁决。宿主必须在拿到 Approved 之后才允许落库，
/// 且每个气泡的写入都要重新以 FenceEpoch 为条件复核。
/// Source/CommitToken 决定宿主如何落库：普通回复走条件插入，
/// 主动消息仍走原有"消息+配额+账本"单事务写入，避免出现第二条不受控的发送通道。
/// </summary>
public sealed record DeliveryPermission(bool Approved, TurnIdentity Identity, long PlanId, string ConversationId,
    long FenceEpoch, string Source = MessageSources.ModelReply, string? CommitToken = null, string? Reason = null);

/// <summary>调度器对"现在是否形成 Turn"的裁决。</summary>
public enum GateDisposition { Run, Wait, Cancel, Backoff }

public sealed record GateDecision(GateDisposition Disposition, string Reason, TimeSpan? RetryIn = null)
{
    public static GateDecision Run(string reason) => new(GateDisposition.Run, reason);
    public static GateDecision Wait(string reason) => new(GateDisposition.Wait, reason);
    public static GateDecision Cancel(string reason) => new(GateDisposition.Cancel, reason);
    public static GateDecision Backoff(string reason, TimeSpan retryIn) => new(GateDisposition.Backoff, reason, retryIn);
}

/// <summary>Reply Gate 的状态类裁决。DoNothing 与 Defer 都是一等结果，不是失败。</summary>
public enum ReplyGateDisposition { Allow, DoNothing, Defer, CancelStale, DeferForCapability }

public sealed record ReplyGateResult(ReplyGateDisposition Disposition, string Reason,
    ResponseIntent Intent = ResponseIntent.Acknowledge)
{
    /// <summary>Allow 时携带 Intelligence 侧的建议，供生成端口消费；其余裁决为 null。</summary>
    public ReplyAdvice? Advice { get; init; }

    public static ReplyGateResult Allow { get; } = new(ReplyGateDisposition.Allow, "allowed");
    public static ReplyGateResult DoNothing(string reason) => new(ReplyGateDisposition.DoNothing, reason,
        ResponseIntent.DoNothing);
    public static ReplyGateResult Defer(string reason) => new(ReplyGateDisposition.Defer, reason);
    public static ReplyGateResult CancelStale(string reason) => new(ReplyGateDisposition.CancelStale, reason);
    public static ReplyGateResult DeferForCapability(string reason) => new(ReplyGateDisposition.DeferForCapability, reason);
}

/// <summary>用户在回复流中途插话时，Runtime 对"已规划未投递部分"的处理方式。</summary>
public enum InterruptionDisposition
{
    None,
    MergeIntoCurrentBuffer,
    ContinueDelivery,
    CancelRemainingBubbles,
    CancelTurnAndReplan
}

public sealed record InterruptionDecision(InterruptionDisposition Disposition, string Reason,
    IReadOnlyList<TurnIdentity> CancelledTurns)
{
    public static readonly InterruptionDecision None = new(InterruptionDisposition.None, "no_active_output", []);
}

/// <summary>跨重启/跨驱逐保住的最小连续性事实。Epoch 同时是 fencing 依据。</summary>
public sealed record RuntimePersistenceState(long Epoch, bool LastSpeakerWasCharacter, DateTimeOffset LastActivityAt,
    int ConsecutiveCharacterTurns);

public sealed record RuntimeSnapshot(string ConversationId, RuntimePhase Phase, int Revision, long Epoch,
    DateTimeOffset? LastUserActivity, DateTimeOffset? LastCharacterActivity, bool UserTyping, int PendingInputCount,
    int PendingOutputCount, TurnIdentity? ActiveTurn, IReadOnlyList<string> PendingOutputTurns);

/// <summary>Developer Mode 与 Eval 读取的决策轨迹。默认只保留尾部有限条数。</summary>
public enum RuntimeTraceKind { Event, Buffer, Gate, ReplyGate, Turn, Interruption, Delivery, Fence, Proactive, Lifecycle }

public sealed record RuntimeTraceEntry(int Sequence, RuntimeEventKind? Event, RuntimeTraceKind Kind, string Action,
    TurnIdentity? Turn, string Reason, int RevisionBefore, int RevisionAfter, DateTimeOffset At);

/// <summary>Runtime 对外（宿主与其他模块）暴露的观察点。宿主不得据此反向修改 Runtime 内部状态。</summary>
public sealed record RuntimeObservedState
{
    public required string ConversationId { get; init; }
    public required RuntimePhase Phase { get; init; }
    public required int Revision { get; init; }
    public required long Epoch { get; init; }
    public required bool UserTyping { get; init; }
    public required int PendingInputCount { get; init; }
    public required int PendingOutputCount { get; init; }
    public TurnIdentity? ActiveTurn { get; init; }
    public TurnIdentity? DeliveringTurn { get; init; }
    public DateTimeOffset? LastUserActivity { get; init; }
    public DateTimeOffset? LastCharacterActivity { get; init; }
    public int ConsecutiveCharacterTurns { get; init; }
    public IReadOnlyList<TurnRecord> RecentTurns { get; init; } = [];
}

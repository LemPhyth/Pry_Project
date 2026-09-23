using Pry.Core.Intelligence;
using Pry.Core.Models;

namespace Pry.Core.Runtime;

/// <summary>
/// Runtime 的持久化端口。实现方（宿主）是唯一可以接触 SQLite 的一侧；
/// Runtime 只通过这里读写状态、追加输入和条件投递，不持有连接、不拼接 SQL。
/// </summary>
public interface IRuntimeStore
{
    /// <summary>读取该会话的持久状态与 fencing epoch。不存在时返回 null（首次使用）。</summary>
    Task<RuntimePersistenceState?> LoadStateAsync(string conversationId, CancellationToken cancellationToken);

    Task SaveStateAsync(string conversationId, RuntimePersistenceState state, CancellationToken cancellationToken);

    /// <summary>权威 fencing epoch。用于发现本地事件之外的失效（其他写入者、其他进程、重放）。</summary>
    Task<long> ReadEpochAsync(string conversationId, CancellationToken cancellationToken);

    /// <summary>
    /// 使该会话下所有旧计划失效。必须由权威存储原子递增，这样即使旧进程还活着，
    /// 它的迟到模型结果也会在落库时被条件写入拒绝。
    /// </summary>
    Task<long> BumpEpochAsync(string conversationId, CancellationToken cancellationToken);

    Task<long> AppendUserInputAsync(string conversationId, RuntimeUserInput input, CancellationToken cancellationToken);

    /// <summary>
    /// 申请投递一张 ResponsePlan。实现必须在同一个事务内确认 fenceEpoch 仍等于会话当前 epoch；
    /// 不等时返回 Approved=false，本轮一个气泡都不会发出。
    /// </summary>
    Task<DeliveryPermission> RequestDeliveryAsync(string conversationId, TurnIdentity turn, long fenceEpoch,
        IReadOnlyList<RuntimePlannedMessage> messages, string source, string? commitToken,
        CancellationToken cancellationToken);

    /// <summary>条件投递单个气泡。返回 null 表示被权威状态拒绝（迟到结果、重启重放、计划已取消）。</summary>
    Task<long?> CommitBubbleAsync(DeliveryPermission permission, int sequence, RuntimePlannedMessage message,
        CancellationToken cancellationToken);

    /// <summary>放弃某个计划尚未投递的部分。</summary>
    Task AbandonPlanAsync(DeliveryPermission permission, int fromSequence, CancellationToken cancellationToken);

    /// <summary>启动时清扫上次进程遗留的 open 计划，返回清扫条数。恢复阶段绝不重放它们。</summary>
    Task<IReadOnlyList<long>> SweepStalePlansAsync(CancellationToken cancellationToken);
}

/// <summary>
/// 回复计划的生成端口。属于 Model Provider 领域：Runtime 交给它 Turn 的事实与 Intelligence 的建议，
/// 拿回一个 ResponsePlan。Runtime 不关心模型怎么选、prompt 怎么拼。
/// </summary>
public interface IReplyComposer
{
    Task<ResponsePlan> ComposeAsync(TurnRequest request, CancellationToken cancellationToken);
}

/// <summary>一个 Turn 的输入项：新输入需要落库，恢复的历史消息只引用已有主键。</summary>
public sealed record TurnInputPart(RuntimeUserInput? Input, long? ExistingMessageId)
{
    public static TurnInputPart NewInput(RuntimeUserInput input) => new(input, null);
    public static TurnInputPart Existing(long messageId, RuntimeUserInput rendered) => new(rendered, messageId);
}

public sealed record TurnRequest(string ConversationId, TurnIdentity Identity, DateTimeOffset At,
    IReadOnlyList<TurnInputPart> Inputs, IReadOnlyList<string> CombinedText, IReadOnlyList<string> ImageReferences,
    IReadOnlyList<string> StickerIds, long FenceEpoch, DeliveryPolicyOptions Delivery,
    ReplyAdvice? Advice = null, bool ReplanAfterCapability = false, CapabilityResult? Capability = null)
{
    public string PrimaryInput => CombinedText.Count == 0 ? "" : string.Join("\n", CombinedText);
}

public sealed record DeliveryPolicyOptions(string StyleInstruction, int MinMessages, int MaxMessages,
    int MaxMessageCharacters);

/// <summary>
/// "如果 Pry 现在要说话，该做什么"。宿主把它适配到 <see cref="ICharacterIntelligence"/>——
/// 构造 SituationSnapshot 与 CharacterStateSnapshot 需要 Memory 与角色状态，那是宿主的接线工作，
/// Runtime 不直查记忆库、也不持有角色状态。DoNothing 是合法返回。
/// </summary>
public interface IReplyAdvisor
{
    string Name { get; }
    Task<ReplyAdvice> AdviseAsync(ReplyGateContext context, CancellationToken cancellationToken);
}

public sealed record ReplyAdvice(bool ShouldReply, ResponseIntent Intent, string Reason, DeliveryHints? Hints = null);

/// <summary>Reply Gate 交给 Intelligence 侧的事实。全部来自 Runtime 状态，不含数据库或角色内部对象。</summary>
public sealed record ReplyGateContext(TurnIdentity Turn, string CombinedInput, bool DirectAddress, bool AsksQuestion,
    bool LooksIncomplete, RuntimePhase Phase, bool UserTyping, int PendingInputCount, int PendingOutputCount,
    DateTimeOffset? LastCharacterActivity, TimeSpan SinceLastCharacterActivity, int ConsecutiveCharacterTurns,
    bool ProactiveOrigin);

/// <summary>插话语义判断。Runtime 决定是否取消，"这句话算不算插话"由此端口给出。</summary>
public interface IInterruptClassifier
{
    InterruptAssessment Classify(RuntimeUserInput input, RuntimeObservedState observed);
}

public enum InterruptStrength { Backchannel, Soft, Hard }

public sealed record InterruptAssessment(InterruptStrength Strength, string Reason);

/// <summary>
/// Runtime 的唯一输出通道：气泡投递、typing 指示、Turn 终局与 Capability 请求。
/// 宿主实现它并映射到具体协议；普通回复与主动回复共用同一个通道。
/// </summary>
public interface IRuntimeOutputSink
{
    Task MessageDeliveredAsync(string conversationId, long messageId, RuntimePlannedMessage message, string source,
        CancellationToken cancellationToken);

    Task TypingIndicatorAsync(string conversationId, bool active, CancellationToken cancellationToken);

    /// <summary>阶段迁移投影。宿主据此发 SSE 状态或 Developer Mode 展示，不得反向修改 Runtime。</summary>
    Task PhaseChangedAsync(string conversationId, RuntimePhase phase, RuntimePhase previous,
        CancellationToken cancellationToken);

    Task TurnTransitionAsync(string conversationId, TurnIdentity turn, RuntimeTurnOutcome outcome, string reason,
        CancellationToken cancellationToken);

    Task CapabilityRequestedAsync(string conversationId, CapabilityRequest request, CancellationToken cancellationToken);

    Task FailureAsync(string conversationId, TurnIdentity turn, string errorCode, CancellationToken cancellationToken);
}

/// <summary>Runtime 节奏参数。默认值沿用现有 TurnTakingSettings，语义按持续 Runtime 重新定义。</summary>
public sealed record RuntimeSettings
{
    /// <summary>最后一次输入之后多久认为用户说完了。</summary>
    public int DebounceMs { get; init; } = 1200;

    /// <summary>无论输入是否结束，缓冲最长停留时间，超过后强制成 Turn，避免饿死。</summary>
    public int MaxBufferMs { get; init; } = 5000;

    /// <summary>typing 中最多额外等待的时间。</summary>
    public int TypingHoldMs { get; init; } = 4000;

    /// <summary>被判定为半句时的最小等待时间。</summary>
    public int IncompleteHoldMs { get; init; } = 900;

    /// <summary>连续插话导致 Turn 反复作废时的退避上限（MaiCore 式 idle backoff）。</summary>
    public int MaxBackoffMs { get; init; } = 6000;

    /// <summary>一次干净投递之后退避衰减比例。</summary>
    public double BackoffDecay { get; init; } = 0.5;

    /// <summary>
    /// 进入一轮之前由谁裁决：只看攒够几条，还是看回复必要性评分。
    /// 评分模式的阈值必须经 Eval 标定后才可作为发行默认值，因此默认走计数模式——
    /// 它在频率为 1 时与"有输入即成轮"的旧行为完全等价。
    /// </summary>
    public ReplyTriggerMode TriggerMode { get; init; } = ReplyTriggerMode.MessageCount;

    /// <summary>回复必要性评分参数。</summary>
    public ReplyNecessityOptions Necessity { get; init; } = new();

    /// <summary>连续空转后的检查退避参数（只作用于群聊）。</summary>
    public IdleBackoffOptions IdleBackoff { get; init; } = new();

    /// <summary>外部消息到达节奏的统计参数。</summary>
    public MessageIntervalOptions Intervals { get; init; } = new();

    public int MaxPendingTurns { get; init; } = 1;

    public int RecentTurnHistory { get; init; } = 12;

    public int TraceCapacity { get; init; } = 300;

    /// <summary>空闲超过该时长后，下一次输入被视为"用户回来了"。</summary>
    public int IdleReturnAfterMs { get; init; } = 300_000;

    /// <summary>Idle 状态下超过该时长发布一次 IdleElapsed 事件，供上层投影使用。</summary>
    public int IdleHeartbeatMs { get; init; } = 60_000;

    /// <summary>Idle 且无 pending 输入/输出持续该时长后允许驱逐。</summary>
    public int EvictAfterIdleMs { get; init; } = 900_000;

    /// <summary>主动消息候选默认存活时间。</summary>
    public int ProactiveCandidateTtlMs { get; init; } = 120_000;

    /// <summary>是否允许把一条回复拆成多个气泡。由宿主注入，Runtime 不解释措辞。</summary>
    public bool SplitRepliesDefault { get; init; } = true;

    public string StyleInstruction { get; init; } = "像即时通讯聊天一样自然、简短，不要一次倾倒过多信息。";

    public int MinReplyMessages { get; init; } = 1;

    public int MaxReplyMessages { get; init; } = 4;

    public int MaxMessageCharacters { get; init; } = 90;

    public int ListeningSignalDelayMs { get; init; } = 4500;

    /// <summary>气泡间隔推导所用的表达习惯参数；复用 Models 既有定义，不新造第二份节奏配置。</summary>
    public TypingStyle Typing { get; init; } = new();

    public static RuntimeSettings FromTurnTaking(TurnTakingSettings source) => new()
    {
        DebounceMs = source.DebounceMs,
        MaxBufferMs = source.MaxPendingMs,
        SplitRepliesDefault = source.SplitReplies,
        StyleInstruction = source.StyleInstruction,
        MinReplyMessages = source.MinReplyMessages,
        MaxReplyMessages = source.MaxReplyMessages,
        MaxMessageCharacters = source.MaxMessageCharacters,
        ListeningSignalDelayMs = source.ListeningSignalDelayMs,
        Typing = source.TypingStyle
    };
}

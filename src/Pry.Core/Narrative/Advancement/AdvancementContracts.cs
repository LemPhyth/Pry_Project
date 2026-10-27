// HDS-007 第一片·隔离原型的合同面（工单 §一 五条共用的入参与读数形状）。
//
// 形状口径：全内存、参数全走入参、零生产调用方。冷却窗口、静默窗口、调用/费用预算、
// 每次推进所需的额度与推进代次**全部由调用方随请求带进来**——本原型不持有任何
// 窗口或预算的源码侧默认值（把默认值写成源码常量就等于新增一道数字门槛）。
//
// 命名口径：类型名用领域名（推进／冷却／静默／预算／到期意图），不带验收编号。
namespace Pry.Core.Narrative.Advancement;

/// <summary>
/// 三类触发（v4:326 那一格的"用户事件、到期意图、后台场景"）。它们只是同一个入口的
/// <see cref="AdvancementRequest.Kind"/> 的三个取值，共用同一个入参形状；
/// 每次推进用的是哪一类，由读数里的 <see cref="AdvancementReadout.Kind"/> 与
/// <see cref="AdvancementReadout.TriggerReference"/> 当场点名。
/// </summary>
public enum AdvancementTriggerKind
{
    /// <summary>用户事件：真实用户输入触发的推进。</summary>
    UserEvent,

    /// <summary>到期意图：登记过的意图走到到期时刻后触发的推进。</summary>
    DueIntent,

    /// <summary>后台场景：场景条件触发的推进。</summary>
    BackgroundScene
}

/// <summary>
/// 一次推进的结局。六个值各有独立出口与独立原因文本，不存在"什么都不说"的分支；
/// 失败与被推迟也不是一个轴——被推迟的意图仍留在待观察清单里。
/// </summary>
public enum AdvancementOutcome
{
    /// <summary>三道闸与预算都放行，推进产出了一条记录。</summary>
    Advanced,

    /// <summary>同一意图还在冷却窗口内：推迟，不是丢弃。</summary>
    DeferredByCooldown,

    /// <summary>此刻处于静默窗口内：推迟，不是丢弃。</summary>
    DeferredByQuiet,

    /// <summary>调用或费用预算不够本次推进：停止推进。</summary>
    StoppedByBudget,

    /// <summary>这一次推进带着已被取代的推进代次进来：拒收，它没有生效过。</summary>
    RejectedForStaleEffect,

    /// <summary>这一次推进产出的记录没有可指认的来源：明确失败，不是静默丢弃。</summary>
    FailedForMissingSource
}

/// <summary>
/// 一条推进产出的记录所必须携带的来源指认（006 最小来源约束在本原型上的形状）。
/// 版本随来源走，调用方给什么本原型就带什么，不自行发号。
/// </summary>
public sealed record AdvancementSourceReference(string SourceId, long SourceVersion);

/// <summary>
/// 费用／调用预算的读数材料。<see cref="TotalCalls"/> 与 <see cref="ConsumedCalls"/> 之差、
/// <see cref="TotalCost"/> 与 <see cref="ConsumedCost"/> 之差就是"还剩多少"，本原型不缓存也不自行扣减，
/// 因为扣减权在宿主；"预算为零"与"预算耗尽"因此都只是这组入参的两种取值，不需要原型里有任何预设数。
/// </summary>
public sealed record AdvancementBudget(
    long TotalCalls,
    long ConsumedCalls,
    decimal TotalCost,
    decimal ConsumedCost,
    long RequiredCallsPerAdvancement,
    decimal RequiredCostPerAdvancement);

/// <summary>
/// 本次推进看到的时钟与两道窗口。时钟由调用方给（长离线恢复就是让时钟前跳）；
/// 不想让静默生效就传一个起点与终点重合的窗口，本原型不需要为此再设任何开关值。
/// </summary>
public sealed record AdvancementWindow(
    DateTime NowUtc,
    TimeSpan CooldownWindow,
    DateTime QuietWindowStartUtc,
    DateTime QuietWindowEndUtc);

/// <summary>
/// 唯一的推进入参形状。三类触发各有自己的引用字段，<see cref="Kind"/> 决定读哪一条：
/// 三条字段同时给值时只有本类那一条会被点名，这正是"三个取值不是三条路的别名"的可判面。
/// </summary>
public sealed record AdvancementRequest(
    AdvancementTriggerKind Kind,
    string IntentKey,
    string RequestId,
    string? UserEventReference,
    string? DueIntentReference,
    string? BackgroundSceneReference,
    AdvancementSourceReference? Source,
    AdvancementWindow Window,
    AdvancementBudget Budget,
    long ExpectedEffectVersion);

/// <summary>一次真正生效的推进产出的记录：来源可指认、代次可追溯、触发类别可点名。</summary>
public sealed record AdvancementRecord(
    string RequestId,
    string IntentKey,
    AdvancementTriggerKind Kind,
    string TriggerReference,
    string SourceId,
    long SourceVersion,
    long EffectVersion,
    DateTime ProducedAtUtc);

/// <summary>被推迟的意图留下的可观察登记：被哪一道推迟、最早下一次在什么时刻。</summary>
public sealed record AdvancementDeferral(
    string IntentKey,
    AdvancementTriggerKind Kind,
    string DeferredBy,
    DateTime EarliestNextAdvanceUtc);

/// <summary>
/// 观察日志的一行。每个结局都在这里留一行，包括两个失败出口（来源缺失、代次过期）——
/// "消失但不说为什么"在这份原型里没有出口。
/// </summary>
public sealed record AdvancementObservation(
    AdvancementOutcome Outcome,
    string IntentKey,
    string RequestId,
    string Detail);

/// <summary>
/// 一次推进的全部读数。哪个字段在哪个结局里有值，本文件不靠注释保证，由判据逐枚钉住绝对字面量。
/// </summary>
public sealed record AdvancementReadout(
    AdvancementTriggerKind Kind,
    string IntentKey,
    string RequestId,
    AdvancementOutcome Outcome,
    string OutcomeReason,
    string? TriggerReference,
    string? SourceId,
    long? SourceVersion,
    string? DeferredBy,
    DateTime? EarliestNextAdvanceUtc,
    long ConsumedCalls,
    long RemainingCalls,
    decimal ConsumedCost,
    decimal RemainingCost,
    string? StoppedBudgetDimension,
    long ExpectedEffectVersion,
    long ObservedEffectVersion,
    AdvancementRecord? Record);

// HDS-007 第一片·隔离原型：推进协调器（工单 §一.1／§一.2／§一.3／§一.4 的迟到面／§一.5）。
//
// 只有一个公开入口 <see cref="Advance"/>，三类触发共用同一个入参形状（§一.1 的"同入口"就是这一句）。
// 闸的顺序：来源可指认 → 冷却 → 静默 → 预算 → 推进代次。前三步决定"这次该不该推"，
// 最后一步决定"这一次推进能不能生效"；每一道闸被摘掉时只会红它自己那一格的形状。
//
// 全内存、参数全走入参、零生产调用方。本文件没有任何窗口长度或预算的源码侧默认值：
// 冷却多长、静默窗口在哪一段、每次推进要花多少调用与费用，全部由 AdvancementRequest 带进来。
namespace Pry.Core.Narrative.Advancement;

/// <summary>
/// 有界连续推进的协调器原型：一次调用读一道时钟，产出可指认的记录，或被哪一道闸推迟／停止／拒绝都当场点名。
/// </summary>
public sealed class AdvancementCoordinator
{
    private readonly List<AdvancementRecord> _records = [];
    private readonly List<AdvancementDeferral> _deferrals = [];
    private readonly List<AdvancementObservation> _observations = [];
    private readonly Dictionary<string, DateTime> _lastAdvancedByIntent = new(StringComparer.Ordinal);
    private long _effectVersion;

    /// <summary>已经生效的推进记录，按生效顺序。</summary>
    public IReadOnlyList<AdvancementRecord> ProducedRecords => _records;

    /// <summary>仍被推迟、等待各自闸门打开的意图（推迟不是丢弃就在这张清单上可观察）。</summary>
    public IReadOnlyList<AdvancementDeferral> PendingDeferrals => _deferrals;

    /// <summary>逐次结局的观察日志；两个失败出口（来源缺失、代次过期）同样留行。</summary>
    public IReadOnlyList<AdvancementObservation> Observations => _observations;

    /// <summary>当前推进代次。调用方下一次推进要么带上这个值，要么就带着旧值被拒。</summary>
    public long CurrentEffectVersion => _effectVersion;

    /// <summary>
    /// 唯一的推进入口：用户事件、到期意图、后台场景三类触发都走这里，入参形状相同。
    /// 读数里的 <see cref="AdvancementReadout.Kind"/> 点名"这一次是哪一类"。
    /// </summary>
    public AdvancementReadout Advance(AdvancementRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var triggerReference = ResolveTriggerReference(request);
        var remainingCalls = request.Budget.TotalCalls - request.Budget.ConsumedCalls;
        var remainingCost = request.Budget.TotalCost - request.Budget.ConsumedCost;

        // §一.5（006 最小来源约束）：来源不可指认就是明确失败，不是"这一条不算了"。
        if (request.Source is null || string.IsNullOrWhiteSpace(request.Source.SourceId))
        {
            var missing = BuildReadout(request, AdvancementOutcome.FailedForMissingSource,
                "source_cannot_be_pointed_to", triggerReference, remainingCalls, remainingCost, null);
            _observations.Add(new AdvancementObservation(missing.Outcome, request.IntentKey,
                request.RequestId, missing.OutcomeReason));
            return missing;
        }

        // §一.2 冷却：同一意图在冷却窗口内不得重复起推。窗口外沿那一瞬算放行。
        var cooldownOpensAt = _lastAdvancedByIntent.TryGetValue(request.IntentKey, out var lastAdvanced)
            ? lastAdvanced + request.Window.CooldownWindow
            : (DateTime?)null;
        if (cooldownOpensAt is { } cooldownEnd && cooldownEnd > request.Window.NowUtc)
        {
            var deferred = BuildReadout(request, AdvancementOutcome.DeferredByCooldown,
                "intent_inside_cooldown_window", triggerReference, remainingCalls, remainingCost, null)
                with { DeferredBy = "cooldown", EarliestNextAdvanceUtc = cooldownEnd };
            _deferrals.Add(new AdvancementDeferral(request.IntentKey, request.Kind, "cooldown", cooldownEnd));
            _observations.Add(new AdvancementObservation(deferred.Outcome, request.IntentKey,
                request.RequestId, deferred.OutcomeReason));
            return deferred;
        }

        // §一.2 静默：处于静默期时到期意图被推迟到窗口结束，推迟的意图仍留在待观察清单上。
        if (request.Window.NowUtc >= request.Window.QuietWindowStartUtc
            && request.Window.NowUtc < request.Window.QuietWindowEndUtc)
        {
            var quietEnd = request.Window.QuietWindowEndUtc;
            var deferred = BuildReadout(request, AdvancementOutcome.DeferredByQuiet,
                "intent_inside_quiet_window", triggerReference, remainingCalls, remainingCost, null)
                with { DeferredBy = "quiet", EarliestNextAdvanceUtc = quietEnd };
            _deferrals.Add(new AdvancementDeferral(request.IntentKey, request.Kind, "quiet", quietEnd));
            _observations.Add(new AdvancementObservation(deferred.Outcome, request.IntentKey,
                request.RequestId, deferred.OutcomeReason));
            return deferred;
        }

        // §一.3 预算：调用与费用两个维度各自可点名地拦下推进；"预算为零"与"预算耗尽"是这组入参的两种取值。
        if (remainingCalls < request.Budget.RequiredCallsPerAdvancement)
        {
            var stopped = BuildReadout(request, AdvancementOutcome.StoppedByBudget,
                "remaining_calls_below_required_calls", triggerReference, remainingCalls, remainingCost, "calls");
            _observations.Add(new AdvancementObservation(stopped.Outcome, request.IntentKey,
                request.RequestId, stopped.OutcomeReason));
            return stopped;
        }

        if (remainingCost < request.Budget.RequiredCostPerAdvancement)
        {
            var stopped = BuildReadout(request, AdvancementOutcome.StoppedByBudget,
                "remaining_cost_below_required_cost", triggerReference, remainingCalls, remainingCost, "cost");
            _observations.Add(new AdvancementObservation(stopped.Outcome, request.IntentKey,
                request.RequestId, stopped.OutcomeReason));
            return stopped;
        }

        // §一.4 迟到面：带着已被取代的推进代次进来的推进一律被拒，拒绝理由就是"那一次推进没有生效过"，
        // 并且一个字段都不写（对齐 005 落库面的版本语义，不复用它的类型）。
        if (request.ExpectedEffectVersion != _effectVersion)
        {
            var rejected = BuildReadout(request, AdvancementOutcome.RejectedForStaleEffect,
                "advancement_never_took_effect", triggerReference, remainingCalls, remainingCost, null);
            _observations.Add(new AdvancementObservation(rejected.Outcome, request.IntentKey,
                request.RequestId, rejected.OutcomeReason));
            return rejected;
        }

        var effectVersion = checked(_effectVersion + 1);
        var record = new AdvancementRecord(request.RequestId, request.IntentKey, request.Kind,
            triggerReference!, request.Source.SourceId, request.Source.SourceVersion, effectVersion,
            request.Window.NowUtc);
        _records.Add(record);
        _lastAdvancedByIntent[request.IntentKey] = request.Window.NowUtc;
        _deferrals.RemoveAll(x => x.IntentKey == request.IntentKey);
        _observations.Add(new AdvancementObservation(AdvancementOutcome.Advanced, request.IntentKey,
            request.RequestId, record.TriggerReference));
        _effectVersion = effectVersion;
        return BuildReadout(request, AdvancementOutcome.Advanced, "advanced", triggerReference,
            remainingCalls, remainingCost, null) with { Record = record, EarliestNextAdvanceUtc = null };
    }

    /// <summary>三类触发各自的引用字段：本类给没给，只有本类那一条会被点名。</summary>
    private static string? ResolveTriggerReference(AdvancementRequest request) => request.Kind switch
    {
        AdvancementTriggerKind.UserEvent => request.UserEventReference,
        AdvancementTriggerKind.DueIntent => request.DueIntentReference,
        AdvancementTriggerKind.BackgroundScene => request.BackgroundSceneReference,
        _ => null,
    };

    private AdvancementReadout BuildReadout(
        AdvancementRequest request,
        AdvancementOutcome outcome,
        string reason,
        string? triggerReference,
        long remainingCalls,
        decimal remainingCost,
        string? stoppedBudgetDimension)
    {
        // 来源不可指认时读数里不回显那段空白：缺失就是缺失，不装作有来源（§一.5 的 006 面）。
        string? pointableSourceId = string.IsNullOrWhiteSpace(request.Source?.SourceId)
            ? null
            : request.Source!.SourceId;
        long? pointableSourceVersion = pointableSourceId is null ? null : request.Source!.SourceVersion;

        return new AdvancementReadout(
            request.Kind,
            request.IntentKey,
            request.RequestId,
            outcome,
            reason,
            triggerReference,
            pointableSourceId,
            pointableSourceVersion,
            null,
            null,
            request.Budget.ConsumedCalls,
            remainingCalls,
            request.Budget.ConsumedCost,
            remainingCost,
            stoppedBudgetDimension,
            request.ExpectedEffectVersion,
            _effectVersion,
            null);
    }
}

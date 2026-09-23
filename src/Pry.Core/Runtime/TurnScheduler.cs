namespace Pry.Core.Runtime;

/// <summary>
/// 触发这一轮所需的事实。全部由 ConversationRuntime 从自身状态算好后传入：
/// TurnScheduler 不查数据库、不读时钟、不认识任何宿主对象。
/// </summary>
public sealed record TriggerFacts(IReadOnlyList<string> ExternalTexts, int PendingCount, bool HasAt,
    bool HasMention, bool IsGroupChat, double EffectiveFrequency, TimeSpan? AverageInterval, TimeSpan IdleFor,
    bool IdleReachedAverage, int RecentSelfReplies, int RecentWindowMessages,
    IReadOnlyList<string> CharacterNames);

/// <summary>调度一次"是否形成 Turn"的判断所需的全部事实。TurnScheduler 不读数据库、不读时钟之外的状态。</summary>
public sealed record SchedulingContext(DateTimeOffset At, RuntimePhase Phase, bool BufferHasInput,
    BufferAssessment Assessment, bool OutputInFlight, bool TurnInFlight, int PendingTurnCount,
    int MaxPendingTurns, bool ForcedByImmediate, TriggerFacts? Trigger = null);

public sealed record SchedulingOutcome(GateDecision Decision, TimeSpan AppliedBackoff, string? TriggerDetail = null);

/// <summary>
/// Turn Scheduler：决定积累到什么时候正式成为一个 Character Turn。
/// 独立于回复生成——它只回答"现在开不开 Turn"，不回答"要不要说话"（那是 Reply Gate）。
///
/// 判定分两段，顺序不可交换：
/// 1) 状态闸门——本轮是否已在飞行、输出是否未清、输入是否说完；这些是"现在不是时候"。
/// 2) 触发门控——攒够没有、值不值得进（MaiCore 式频率折算阈值 + 空窗补偿 / 回复必要性评分）、
///    以及连续空转后的退避；这些是"时候到了也不该进"。
/// 连续插话反复作废同一轮时按打断退避递增等待，让出麦克风给用户，一次干净投递后衰减。
/// </summary>
public sealed class TurnScheduler
{
    private readonly RuntimeSettings _settings;
    private readonly IdleBackoffController _idleBackoff;
    private readonly FrequencyThresholdTurnGate _countGate = new();
    private readonly ReplyNecessityTurnGate _necessityGate;
    private int _strikes;
    private DateTimeOffset? _backoffConsumedUntil;
    private string? _lastTriggerDetail;

    public TurnScheduler(RuntimeSettings settings)
    {
        _settings = settings;
        _idleBackoff = new IdleBackoffController(settings.IdleBackoff);
        _necessityGate = new ReplyNecessityTurnGate(settings.Necessity);
    }

    public int InvalidatedTurns => _strikes;

    /// <summary>连续空转轮数，供诊断与 Developer Mode 读取。</summary>
    public int ConsecutiveIdleCycles => _idleBackoff.ConsecutiveIdleCycles;

    public TimeSpan Backoff => _strikes == 0
        ? TimeSpan.Zero
        : TimeSpan.FromMilliseconds(Math.Min(_settings.MaxBackoffMs,
            _settings.DebounceMs * ((1 << Math.Min(_strikes, 6)) - 1)));

    public void NoteTurnInvalidatedBeforeDelivery()
    {
        _strikes++;
        _backoffConsumedUntil = null;
    }

    public void NoteCleanDelivery()
    {
        _backoffConsumedUntil = null;
        if (_strikes == 0) return;
        _strikes = (int)Math.Floor(_strikes * _settings.BackoffDecay);
    }

    /// <summary>
    /// 一整轮结束。什么都没投递视为一次空转，按连续次数推迟下次检查；
    /// 说过话则立刻清零——恢复活跃对话后不该继续背着历史包袱。
    /// </summary>
    public void NoteCycleEnded(DateTimeOffset at, bool idleCycle, bool isGroupChat) =>
        _idleBackoff.NoteCycleEnded(at, idleCycle, isGroupChat);

    public void Reset()
    {
        _strikes = 0;
        _backoffConsumedUntil = null;
        _idleBackoff.Reset();
    }

    public SchedulingOutcome Decide(SchedulingContext context)
    {
        var decision = Evaluate(context);
        var detail = _lastTriggerDetail;
        _lastTriggerDetail = null;
        var applied = TimeSpan.Zero;
        if (decision.Disposition == GateDisposition.Run && !context.ForcedByImmediate &&
            Backoff > TimeSpan.Zero && _backoffConsumedUntil is null)
        {
            applied = Backoff;
            // 退避必须被"消费"掉：下一次评估已经到了同一个时间点就该放行，
            // 否则每轮重新施加退避会让角色永远不再开口。
            _backoffConsumedUntil = context.At + applied;
            decision = GateDecision.Backoff("interrupt_backoff", applied);
        }
        else if (decision.Disposition == GateDisposition.Run && !context.ForcedByImmediate &&
                 _backoffConsumedUntil is { } until && context.At < until)
            decision = GateDecision.Backoff("interrupt_backoff", until - context.At);
        else if (decision.Disposition == GateDisposition.Run) _backoffConsumedUntil = null;
        return new SchedulingOutcome(decision, applied, detail);
    }

    private GateDecision Evaluate(SchedulingContext context)
    {
        if (!context.BufferHasInput) return GateDecision.Wait("no_pending_input");
        if (context.TurnInFlight) return GateDecision.Wait("turn_in_flight");
        if (context.Assessment.Complete && context.OutputInFlight && !context.ForcedByImmediate)
            return GateDecision.Backoff("output_in_flight", TimeSpan.FromMilliseconds(_settings.DebounceMs));
        if (context.PendingTurnCount >= context.MaxPendingTurns) return GateDecision.Wait("pending_turn_limit");
        if (!context.Assessment.Complete)
            return GateDecision.Backoff(context.Assessment.Reason, context.Assessment.Remaining);
        return context.Trigger is { } trigger
            ? EvaluateTrigger(context.At, trigger, context.Assessment.Reason, context.ForcedByImmediate)
            : GateDecision.Run(context.Assessment.Reason);
    }

    private GateDecision EvaluateTrigger(DateTimeOffset at, TriggerFacts trigger, string formedReason, bool forced)
    {
        var frequency = Math.Clamp(trigger.EffectiveFrequency, 0.0, 1.0);
        var pending = Math.Max(1, trigger.PendingCount);

        // 频率为 0 是宿主显式关掉了自动回复。此时消息照样进缓冲、由平台层落库，
        // 但不成轮：用 Cancel 让调用方静默消费掉，而不是把输入无限期挂在临时缓冲里。
        if (frequency <= 0) return GateDecision.Cancel("frequency_silent");

        // 被点名与被提及直接成轮：这是唯一一组不该和"攒够几条"讲条件的社交信号。
        if (forced || trigger.HasAt || trigger.HasMention) return GateDecision.Run(formedReason);

        if (_idleBackoff.ShouldDelay(at, pending, trigger.IsGroupChat, focusActive: false) is { } remaining)
            return GateDecision.Backoff("idle_backoff", remaining);

        var threshold = ReplyFrequency.Threshold(frequency, _settings.TriggerMode);
        return _settings.TriggerMode == ReplyTriggerMode.ReplyNecessity
            ? NecessityDecision(trigger, threshold, formedReason)
            : CountDecision(trigger, pending, threshold, at, formedReason);
    }

    private GateDecision NecessityDecision(TriggerFacts trigger, int threshold, string formedReason)
    {
        var result = _necessityGate.Evaluate(new ReplyNecessitySnapshot(trigger.ExternalTexts,
            trigger.PendingCount, threshold, trigger.HasAt, trigger.HasMention, trigger.IsGroupChat,
            FocusActive: false, trigger.RecentSelfReplies, trigger.RecentWindowMessages,
            trigger.EffectiveFrequency, trigger.IdleFor, trigger.IdleReachedAverage, trigger.CharacterNames));
        _lastTriggerDetail = result.Detail;
        return result.ShouldTrigger ? GateDecision.Run(formedReason) : GateDecision.Wait("necessity_below_threshold");
    }

    private GateDecision CountDecision(TriggerFacts trigger, int pending, int threshold, DateTimeOffset at,
        string formedReason)
    {
        var result = _countGate.Evaluate(new FrequencyGateInputs(pending, threshold, trigger.AverageInterval,
            trigger.IdleFor));
        _lastTriggerDetail = result.Detail;
        return result.Decision switch
        {
            TurnGateDecision.Trigger => GateDecision.Run(formedReason),
            // 空窗补偿给出的不是一个拒绝，而是一个"到点再问我一次"：调用方据此设闹钟，
            // 待处理消息才不会因为没有新输入而永久挂起。
            TurnGateDecision.Delay => GateDecision.Backoff("idle_compensation",
                result.Delay ?? TimeSpan.FromSeconds(1)),
            _ => GateDecision.Wait("below_threshold")
        };
    }
}

namespace Pry.Core.Runtime;

/// <summary>调度一次"是否形成 Turn"的判断所需的全部事实。TurnScheduler 不读数据库、不读时钟之外的状态。</summary>
public sealed record SchedulingContext(DateTimeOffset At, RuntimePhase Phase, bool BufferHasInput,
    BufferAssessment Assessment, bool OutputInFlight, bool TurnInFlight, int PendingTurnCount,
    int MaxPendingTurns, bool ForcedByImmediate);

public sealed record SchedulingOutcome(GateDecision Decision, TimeSpan AppliedBackoff);

/// <summary>
/// Turn Scheduler：决定积累到什么时候正式成为一个 Character Turn。
/// 独立于回复生成——它只回答"现在开不开 Turn"，不回答"要不要说话"（那是 Reply Gate）。
/// 连续插话反复作废同一轮时按 idle backoff 递增等待，让出麦克风给用户，一次干净投递后衰减。
/// </summary>
public sealed class TurnScheduler
{
    private readonly RuntimeSettings _settings;
    private int _strikes;
    private DateTimeOffset? _backoffConsumedUntil;

    public TurnScheduler(RuntimeSettings settings) => _settings = settings;

    public int InvalidatedTurns => _strikes;

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

    public void Reset()
    {
        _strikes = 0;
        _backoffConsumedUntil = null;
    }

    public SchedulingOutcome Decide(SchedulingContext context)
    {
        var decision = Evaluate(context);
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
        return new SchedulingOutcome(decision, applied);
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
        return GateDecision.Run(context.Assessment.Reason);
    }
}

using Pry.Core.Inference;
using Pry.Core.Intelligence;

namespace Pry.Core.Runtime;

public sealed record ConversationRuntimeOptions(string ConversationId, string CharacterInstanceId,
    TimeProvider Clock, RuntimeSettings Settings, IRuntimeStore Store, IReplyComposer Composer,
    IReplyAdvisor Advisor, IInterruptClassifier Classifier, IRuntimeOutputSink Sink)
{
    /// <summary>用于"是否像在叫角色"的节奏判断；不是社交决策。</summary>
    public IReadOnlyList<string> CharacterNames { get; init; } = [];
}

/// <summary>一次事件处理后的可观察结果。</summary>
public sealed record RuntimeReceipt(int RevisionBefore, int RevisionAfter, RuntimePhase Phase,
    RuntimeEventKind Event, string Action, long? UserMessageId = null, TurnIdentity? Turn = null);

/// <summary>
/// Pry 的持续对话运行时：一条会话一个实例，长期存在，用户输入只是对它的一次状态更新。
///
/// 所有权：
/// - 状态的唯一真相在本对象内。宿主通过事件写入、通过快照读取，不能塞一个可随意改的字典进来。
/// - 所有状态迁移在一把锁内同步完成；任何异步效果（落库、模型、投递）都携带 <see cref="TurnIdentity"/>
///   与失效 epoch，回来之后必须重新验证才允许产生外部影响。
/// - 进程内输入缓冲与计时器是临时状态，可丢；会话连续性与 fencing epoch 由 <see cref="IRuntimeStore"/> 保住。
/// </summary>
public sealed class ConversationRuntime : IAsyncDisposable
{
    private readonly ConversationRuntimeOptions _options;
    private readonly TimeProvider _clock;
    private readonly RuntimeSettings _settings;
    private readonly IRuntimeStore _store;
    private readonly IReplyComposer _composer;
    private readonly IRuntimeOutputSink _sink;
    private readonly InputBuffer _buffer;
    private readonly TurnScheduler _scheduler;
    private readonly ReplyGate _gate;
    private readonly InterruptionController _interruptions;
    private readonly DeliveryScheduler _delivery;
    private readonly RuntimeTrace _trace;
    private readonly List<TurnRecord> _recentTurns = [];
    private readonly CancellationTokenSource _lifetime = new();
    private readonly object _sync = new();
    private readonly SemaphoreSlim _eventGate = new(1, 1);

    private TurnExecution? _active;
    private RuntimePhase _phase = RuntimePhase.Idle;
    private ITimer? _wakeTimer;
    private int _revision;
    private long _epoch;
    private long _nextTurnId = 1;
    private int _outstanding;
    private TaskCompletionSource? _idleSignal;
    private bool _disposed;
    private bool _capabilityPending;
    private TurnIdentity? _capabilityTurn;
    private string? _capabilityId;
    private DateTimeOffset? _deferredUntil;

    private DateTimeOffset? _lastEventAt;
    private DateTimeOffset? _lastUserActivity;
    private DateTimeOffset? _lastCharacterActivity;
    private int _consecutiveCharacterTurns;
    private long _conversationProgress;

    private ConversationRuntime(ConversationRuntimeOptions options)
    {
        _options = options;
        _clock = options.Clock;
        _settings = options.Settings;
        _store = options.Store;
        _composer = options.Composer;
        _sink = options.Sink;
        _buffer = new InputBuffer(_settings);
        _scheduler = new TurnScheduler(_settings);
        _gate = new ReplyGate(options.Advisor);
        _interruptions = new InterruptionController(options.Classifier);
        _trace = new RuntimeTrace(_settings.TraceCapacity);
        _delivery = new DeliveryScheduler(options.ConversationId, _clock, _settings, _store, _sink,
            new ReplyPacing(new Random()), IsCurrent, ReportFailure);
    }

    public string ConversationId => _options.ConversationId;
    public string CharacterInstanceId => _options.CharacterInstanceId;
    public RuntimePhase Phase { get { lock (_sync) return _phase; } }
    public int Revision { get { lock (_sync) return _revision; } }
    public long Epoch { get { lock (_sync) return _epoch; } }

    /// <summary>会话进度水位：主动消息候选必须带上它，否则用户聊过一轮之后旧候选仍会被放行。</summary>
    public long ConversationProgress { get { lock (_sync) return _conversationProgress; } }
    public int InvalidatedTurns => _scheduler.InvalidatedTurns;

    /// <summary>当前连续插话退避时长，Developer Mode 用来解释"为什么这次没有立刻回"。</summary>
    public double SchedulerBackoffMs { get { lock (_sync) return _scheduler.Backoff.TotalMilliseconds; } }
    public IReadOnlyList<TurnRecord> RecentTurns { get { lock (_sync) return _recentTurns.ToArray(); } }
    public IReadOnlyList<RuntimeTraceEntry> TraceEntries => _trace.Entries();
    public IReadOnlyList<RuntimeTraceEntry> TraceSince(int sequence) => _trace.Since(sequence);
    public string RenderTrace() => _trace.Render();
    public IReadOnlyList<PendingBubble> PendingOutputs() => _delivery.Pending;

    /// <summary>建立运行时：读取持久状态与 fencing epoch，本地 epoch 至少追平存储值。</summary>
    public static async Task<ConversationRuntime> CreateAsync(ConversationRuntimeOptions options,
        CancellationToken cancellationToken = default)
    {
        var runtime = new ConversationRuntime(options);
        try
        {
            var stored = await options.Store.LoadStateAsync(options.ConversationId, cancellationToken);
            // A new incarnation fences even a provider still running in an evicted instance.
            var epoch = await options.Store.BumpEpochAsync(options.ConversationId, cancellationToken);
            lock (runtime._sync)
            {
                runtime._epoch = Math.Max(epoch, stored?.Epoch ?? 0);
                if (stored is not null)
                {
                    if (stored.LastSpeakerWasCharacter) runtime._lastCharacterActivity = stored.LastActivityAt;
                    else runtime._lastUserActivity = stored.LastActivityAt;
                    runtime._consecutiveCharacterTurns = stored.ConsecutiveCharacterTurns;
                }
                runtime._lastEventAt = options.Clock.GetUtcNow();
                runtime._trace.Record(null, RuntimeTraceKind.Lifecycle, "created", null,
                    $"epoch={runtime._epoch} last_speaker={(stored?.LastSpeakerWasCharacter == true ? "character" : "none")}",
                    0, runtime._revision, runtime.Now());
                runtime.ArmIdleTimerLocked();
            }
            return runtime;
        }
        catch
        {
            await runtime.DisposeAsync();
            throw;
        }
    }

    /// <summary>启动时清扫上次进程遗留的未投递计划。恢复只清账，绝不重放。</summary>
    public static Task<IReadOnlyList<long>> RecoverStalePlansAsync(IRuntimeStore store,
        CancellationToken cancellationToken = default) => store.SweepStalePlansAsync(cancellationToken);

    #region 事件入口

    public Task<RuntimeReceipt> AcceptAsync(RuntimeEvent runtimeEvent) =>
        AcceptAsync(runtimeEvent, _lifetime.Token);

    /// <summary>唯一的状态写入方式。</summary>
    public async Task<RuntimeReceipt> AcceptAsync(RuntimeEvent runtimeEvent, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        await _eventGate.WaitAsync(linked.Token);
        BeginWork();
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            // Admit only durable input. A slow/failed append cannot race a timer into composing it.
            long? messageId = runtimeEvent.Kind == RuntimeEventKind.UserInput
                ? await _store.AppendUserInputAsync(ConversationId, runtimeEvent.Input ?? new(""), linked.Token)
                : null;
            var effects = new EffectQueue();
            Transition transition;
            lock (_sync) transition = ApplyLocked(runtimeEvent, effects, _lifetime.Token);
            await effects.FlushAsync();
            return new RuntimeReceipt(transition.RevisionBefore, transition.RevisionAfter, Phase, runtimeEvent.Kind,
                transition.Action, messageId, transition.Turn);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not ObjectDisposedException)
        {
            var failed = new EffectQueue();
            lock (_sync)
            {
                InvalidateActiveLocked("event_effect_failed", failed, CancellationToken.None);
                SetPhaseLocked(RuntimePhase.Suspended, "event_effect_failed");
                CancelTimer();
            }
            await FlushQuietlyAsync(failed);
            throw;
        }
        finally { EndWork(); _eventGate.Release(); }
    }

    public Task<RuntimeReceipt> SubmitUserInputAsync(RuntimeUserInput input, bool forceTurn = false,
        CancellationToken cancellationToken = default) =>
        AcceptAsync(RuntimeEvent.UserInput(Now(), input, forceTurn), cancellationToken);

    public Task<RuntimeReceipt> NotifyTypingAsync(bool active, CancellationToken cancellationToken = default) =>
        AcceptAsync(RuntimeEvent.Typing(active, Now()), cancellationToken);

    public Task<RuntimeReceipt> NotifyUserReturnedAsync(CancellationToken cancellationToken = default) =>
        AcceptAsync(new RuntimeEvent(RuntimeEventKind.UserReturned, Now()), cancellationToken);

    public Task<RuntimeReceipt> CancelActiveTurnAsync(string reason) =>
        AcceptAsync(new RuntimeEvent(RuntimeEventKind.Invalidate, Now()) { Detail = reason });

    public Task<RuntimeReceipt> SuspendAsync(SuspendReason reason) =>
        AcceptAsync(new RuntimeEvent(RuntimeEventKind.Suspend, Now()) { Reason = reason });

    public Task<RuntimeReceipt> ResumeAsync() => AcceptAsync(new RuntimeEvent(RuntimeEventKind.Resume, Now()));

    public RuntimeObservedState Observe()
    {
        lock (_sync) return SnapshotLocked();
    }

    public RuntimeSnapshot Snapshot()
    {
        lock (_sync)
        {
            var state = SnapshotLocked();
            return new RuntimeSnapshot(ConversationId, state.Phase, state.Revision, state.Epoch,
                state.LastUserActivity, state.LastCharacterActivity, state.UserTyping, state.PendingInputCount,
                state.PendingOutputCount, state.ActiveTurn,
                _delivery.Pending.Select(x => $"{x.Turn.TurnId}.g{x.Turn.GenerationId}#{x.Message.Sequence}").ToArray());
        }
    }

    #endregion

    #region 状态迁移

    private Transition ApplyLocked(RuntimeEvent ev, EffectQueue effects, CancellationToken token)
    {
        var revisionBefore = _revision;
        var at = ev.OccurredAt;
        var returnedFromIdle = ev.Kind is RuntimeEventKind.UserInput or RuntimeEventKind.TypingStarted
            or RuntimeEventKind.TypingStopped or RuntimeEventKind.UserReturned
            && RegisterActivityLocked(at);
        _revision++;
        TurnIdentity? turn = _active?.Identity;
        var action = "ignored";

        if (ev.Kind != RuntimeEventKind.Resume && _phase == RuntimePhase.Suspended)
        {
            // 挂起期间用户消息仍然落库，但不形成 Turn、不投递。
            if (ev.Kind == RuntimeEventKind.UserInput && ev.Input is { } held)
            {
                _buffer.Add(held, at, ev.ForceTurn);
                _lastUserActivity = at;
                action = "held_suspended";
            }
            else action = "rejected_suspended";
            TraceLocked(RuntimeTraceKind.Lifecycle, action, ev.Kind, null, "suspended");
            return new Transition(action, revisionBefore, _revision, turn);
        }

        switch (ev.Kind)
        {
            case RuntimeEventKind.UserInput:
            {
                var input = ev.Input ?? new RuntimeUserInput("");
                _lastUserActivity = at;
                _consecutiveCharacterTurns = 0;
                action = ApplyUserInputLocked(input, ev.ForceTurn, at, returnedFromIdle, effects, token);
                break;
            }
            case RuntimeEventKind.TypingStarted:
            {
                _buffer.NotifyTyping(true, at);
                _lastUserActivity = at;
                action = ApplyTypingStartedLocked(at, effects, token);
                break;
            }
            case RuntimeEventKind.TypingStopped:
            {
                _buffer.NotifyTyping(false, at);
                _lastUserActivity = at;
                action = "typing_stopped";
                ArmWakeupLocked(at);
                TryFormTurnLocked(at, effects, token);
                break;
            }
            case RuntimeEventKind.TurnDue:
            {
                action = "turn_due";
                TryFormTurnLocked(at, effects, token);
                break;
            }
            case RuntimeEventKind.IdleElapsed:
            {
                action = "idle_elapsed";
                if (_phase == RuntimePhase.Idle) ArmIdleTimerLocked();
                break;
            }
            case RuntimeEventKind.UserReturned:
            {
                InvalidateActiveLocked("user_returned", effects, token);
                _buffer.NotifyTyping(false, at);
                _lastUserActivity = at;
                _conversationProgress++;
                _scheduler.Reset();
                ArmWakeupLocked(at);
                action = "user_returned";
                break;
            }
            case RuntimeEventKind.CapabilityResult:
                action = ApplyCapabilityResultLocked(ev.CapabilityOutcome, at, token);
                break;
            case RuntimeEventKind.Invalidate:
            {
                InvalidateActiveLocked(ev.Detail ?? "invalidated", effects, token);
                _buffer.Clear();
                CancelTimer();
                SetPhaseLocked(RuntimePhase.Idle, "explicit_invalidation");
                action = "invalidated";
                break;
            }
            case RuntimeEventKind.Suspend:
            {
                InvalidateActiveLocked($"suspended:{ev.Reason}", effects, token);
                SetPhaseLocked(RuntimePhase.Suspended, "state_machine");
                CancelTimer();
                action = "suspended";
                break;
            }
            case RuntimeEventKind.Resume:
            {
                SetPhaseLocked(_buffer.Count > 0 || _active is not null ? RuntimePhase.CollectingInput : RuntimePhase.Idle, "state_machine");
                _lastEventAt = at;   // 挂起期间的时间差不应被读成"用户离开了很久"
                ArmWakeupLocked(at);
                TryFormTurnLocked(at, effects, token);
                action = "resumed";
                break;
            }
        }

        TraceLocked(RuntimeTraceKind.Event, action, ev.Kind, turn, $"input_characters={ev.Input?.Text.Length ?? 0}");
        return new Transition(action, revisionBefore, _revision, turn);
    }

    private string ApplyUserInputLocked(RuntimeUserInput input, bool forceTurn, DateTimeOffset at,
        bool returnedFromIdle, EffectQueue effects, CancellationToken token)
    {
        var state = SnapshotLocked();
        var decision = _active is null && state.DeliveringTurn is { } proactiveTurn
            ? new InterruptionDecision(InterruptionDisposition.CancelRemainingBubbles, "user_superseded_proactive", [proactiveTurn])
            : _capabilityPending
            ? _interruptions.OnCapabilityUserInput(input, state)
            : _interruptions.OnUserInput(input, state);
        string action;
        switch (decision.Disposition)
        {
            case InterruptionDisposition.CancelTurnAndReplan:
                InvalidateActiveLocked(decision.Reason, effects, token);
                action = "interrupt_replan";
                break;
            case InterruptionDisposition.CancelRemainingBubbles:
                action = CancelRemainingBubblesLocked(decision, effects, token);
                break;
            case InterruptionDisposition.MergeIntoCurrentBuffer:
                action = decision.Reason;
                break;
            default:
                action = decision.Disposition == InterruptionDisposition.ContinueDelivery
                    ? decision.Reason
                    : "buffered";
                break;
        }

        // 无论是否作废了旧轮次，这条输入都必须进入缓冲：用户说过的话不会凭空消失。
        var buffered = _buffer.Add(input, at, forceTurn);
        _conversationProgress++;
        if (decision.Disposition is InterruptionDisposition.MergeIntoCurrentBuffer or InterruptionDisposition.ContinueDelivery)
        {
            buffered.AbsorbedByReply = true;
            _active?.ToleratedLate.Add(buffered);
        }
        if (returnedFromIdle)
            TraceLocked(RuntimeTraceKind.Event, "idle_return_observed", RuntimeEventKind.UserInput, null,
                $"gap_threshold_ms={_settings.IdleReturnAfterMs}");
        ArmWakeupLocked(at);
        TryFormTurnLocked(at, effects, token);
        return action;
    }

    private string CancelRemainingBubblesLocked(InterruptionDecision decision, EffectQueue effects,
        CancellationToken token)
    {
        var turn = decision.CancelledTurns.FirstOrDefault() ?? TurnIdentity.None;
        if (turn.IsNone) turn = _delivery.DeliveringTurn ?? TurnIdentity.None;
        if (turn.IsNone) return "no_pending_output";
        AbandonDeliveryLocked(turn, decision.Reason, effects, token);
        BumpEpochLocked(decision.Reason, effects, token);
        if (_active is { } active && active.Identity == turn)
        {
            active.Cancellation.Cancel();
            CloseTurnLocked(active, RuntimeTurnOutcome.Interrupted, decision.Reason, effects, token);
        }
        if (_phase == RuntimePhase.Delivering && _delivery.PendingCount == 0)
            SetPhaseLocked(_active is null ? RuntimePhase.Idle : _phase, "state_machine");
        return "interrupt_delivery";
    }

    /// <summary>从队列移除某个 Turn 的未投递气泡，并登记放弃落库的效果。</summary>
    private void AbandonDeliveryLocked(TurnIdentity turn, string reason, EffectQueue effects, CancellationToken token)
    {
        foreach (var item in _delivery.CancelTurn(turn, reason))
            effects.Add(() => _store.AbandonPlanAsync(item.Permission, item.FromSequence, token));
    }

    private string ApplyTypingStartedLocked(DateTimeOffset at, EffectQueue effects, CancellationToken token)
    {
        // 用户开始打字：所有尚未投递的排队气泡（含主动消息）作废；已经发出的不回撤。
        var before = _delivery.PendingCount;
        InvalidateActiveLocked("user_started_typing", effects, token);
        var cancelled = before - _delivery.PendingCount;
        if (_phase == RuntimePhase.Delivering && _delivery.PendingCount == 0)
            SetPhaseLocked(_active is null ? RuntimePhase.CollectingInput : _phase, "state_machine");
        ArmWakeupLocked(at);
        return cancelled > 0 ? $"typing_cancelled_output:{cancelled}" : "typing_started";
    }

    private string ApplyCapabilityResultLocked(CapabilityResult? result, DateTimeOffset at, CancellationToken token)
    {
        if (!_capabilityPending || result is null) return "capability_unexpected";
        var turn = _active;
        if (turn is null || turn.Identity != result.ForTurn || result.CapabilityId != _capabilityId)
            return "capability_result_stale";
        if (IsStaleLocked(turn.Identity)) return "capability_result_stale";
        _capabilityPending = false;
        _capabilityTurn = null;
        _capabilityId = null;
        SetPhaseLocked(RuntimePhase.Planning, "capability_resumed");
        BeginWork();
        _ = ContinueAfterCapabilityAsync(turn, result, at, token);
        return "capability_resumed";
    }

    private void InvalidateActiveLocked(string reason, EffectQueue effects, CancellationToken token)
    {
        lock (_sync)
        {
        var before = _delivery.PendingCount;
        foreach (var item in _delivery.CancelAll(reason))
            effects.Add(() => _store.AbandonPlanAsync(item.Permission, item.FromSequence, token));
        var turn = _active;
        if (turn is null)
        {
            BumpEpochLocked(reason, effects, token);
            if (_phase != RuntimePhase.Suspended) SetPhaseLocked(RuntimePhase.Idle, reason);
            return;
        }
        turn.Cancellation.Cancel();
        _scheduler.NoteTurnInvalidatedBeforeDelivery();
        // 旧轮次已经攒下的输入退回缓冲：重规划要看到的是"用户到目前为止说的全部话"。
        if (Math.Max(turn.DeliveredCount, _delivery.DeliveredFor(turn.Identity)) == 0)
            for (var index = 0; index < turn.Inputs.Count; index++) _buffer.InsertAt(index, turn.Inputs[index]);
        CloseTurnLocked(turn, RuntimeTurnOutcome.Interrupted, reason, effects, token);
        BumpEpochLocked(reason, effects, token);
        _capabilityPending = false;
        _capabilityTurn = null;
        _capabilityId = null;
        if (_phase != RuntimePhase.Suspended) SetPhaseLocked(RuntimePhase.CollectingInput, reason);
        }
    }

    private void CloseTurnLocked(TurnExecution turn, RuntimeTurnOutcome outcome, string reason, EffectQueue effects,
        CancellationToken token, int? delivered = null)
    {
        lock (_sync)
        {
        if (turn.Closed) return;
        turn.Closed = true;
        var deliveredCount = delivered ?? Math.Max(turn.DeliveredCount, _delivery.DeliveredFor(turn.Identity));
        RecordTurnLocked(turn.Record.Close(Now(), outcome, reason, deliveredCount));
        if (ReferenceEquals(_active, turn)) _active = null;
        if (outcome == RuntimeTurnOutcome.Completed) _scheduler.NoteCleanDelivery();
        TraceLocked(RuntimeTraceKind.Turn, $"closed:{outcome}", null, turn.Identity, reason);
        effects.Add(async () =>
        {
            await _sink.TurnTransitionAsync(ConversationId, turn.Identity, outcome, reason, token);
            await PersistStateAsync(token);
        });
        }
    }

    private void RecordTurnLocked(TurnRecord record)
    {
        _recentTurns.Add(record);
        while (_recentTurns.Count > _settings.RecentTurnHistory) _recentTurns.RemoveAt(0);
    }

    private TurnIdentity NewTurnIdentityLocked() => new(_nextTurnId++, 1, _epoch);

    private bool IsStaleLocked(TurnIdentity identity) => identity.FencedRevision != _epoch;

    private bool IsCurrent(TurnIdentity identity)
    {
        lock (_sync) return !_disposed && !IsStaleLocked(identity);
    }

    private void BumpEpochLocked(string reason, EffectQueue effects, CancellationToken token)
    {
        _epoch++;
        TraceLocked(RuntimeTraceKind.Fence, "epoch_bumped", null, null, $"{reason} epoch={_epoch}");
        // 本地抬高的同时必须由权威存储落地：这样即使本进程活着，它的迟到结果也会在写库时被拒绝。
        effects.Add(async () =>
        {
            var authoritative = await _store.BumpEpochAsync(ConversationId, CancellationToken.None);
            lock (_sync) if (authoritative > _epoch) _epoch = authoritative;
        });
    }

    private bool RegisterActivityLocked(DateTimeOffset at)
    {
        var previous = _lastEventAt;
        _lastEventAt = at;
        if (previous is null || at <= previous.Value) return false;
        return at - previous.Value >= TimeSpan.FromMilliseconds(_settings.IdleReturnAfterMs);
    }

    private RuntimeObservedState SnapshotLocked() => new()
    {
        ConversationId = ConversationId,
        Phase = _phase,
        Revision = _revision,
        Epoch = _epoch,
        UserTyping = _buffer.IsTypingAt(Now()),
        PendingInputCount = _buffer.Count,
        PendingOutputCount = _delivery.PendingCount,
        ActiveTurn = _active?.Identity,
        DeliveringTurn = _delivery.DeliveringTurn,
        LastUserActivity = _lastUserActivity,
        LastCharacterActivity = _lastCharacterActivity,
        ConsecutiveCharacterTurns = _consecutiveCharacterTurns,
        RecentTurns = _recentTurns.ToArray()
    };

    private sealed record Transition(string Action, int RevisionBefore, int RevisionAfter, TurnIdentity? Turn);

    #endregion

    #region Turn 形成与执行

    private void TryFormTurnLocked(DateTimeOffset at, EffectQueue effects, CancellationToken token)
    {
        if (_buffer.Count == 0)
        {
            if (_active is null && _phase is RuntimePhase.CollectingInput or RuntimePhase.WaitingForTurn)
                SetPhaseLocked(_delivery.PendingCount == 0 ? RuntimePhase.Idle : RuntimePhase.Delivering, "state_machine");
            return;
        }
        if (_active is not null) return;
        if (_deferredUntil is { } retryAt && at < retryAt)
        {
            ArmWakeupLocked(at, retryAt - at);
            return;
        }
        _deferredUntil = null;
        if (_buffer.RemoveAbsorbed() > 0)
            TraceLocked(RuntimeTraceKind.Buffer, "dropped_absorbed_backchannels", null, null,
                $"remaining={_buffer.Count}");
        if (_buffer.Count == 0)
        {
            SetPhaseLocked(_delivery.PendingCount == 0 ? RuntimePhase.Idle : RuntimePhase.Delivering, "state_machine");
            return;
        }

        var assessment = _buffer.Assess(at, _delivery.PendingCount > 0);
        var context = new SchedulingContext(at, _phase, true, assessment, _delivery.PendingCount > 0, false, 0,
            _settings.MaxPendingTurns, _buffer.HasForcedInput);
        var outcome = _scheduler.Decide(context);
        switch (outcome.Decision.Disposition)
        {
            case GateDisposition.Run:
                FormTurnLocked(assessment.Reason, at, effects, token);
                break;
            case GateDisposition.Backoff:
                SetPhaseLocked(RuntimePhase.WaitingForTurn, "state_machine");
                ArmWakeupLocked(at, outcome.Decision.RetryIn ?? assessment.Remaining);
                break;
            default:
                SetPhaseLocked(RuntimePhase.CollectingInput, "state_machine");
                break;
        }
    }

    private void FormTurnLocked(string reason, DateTimeOffset at, EffectQueue effects, CancellationToken token)
    {
        var inputs = _buffer.Drain();
        var identity = NewTurnIdentityLocked();
        var turn = new TurnExecution(identity, inputs, at, _lifetime.Token);
        turn.Record = new TurnRecord(identity, at, turn.Summary, inputs.Count);
        _active = turn;
        SetPhaseLocked(RuntimePhase.Planning, "state_machine");
        TraceLocked(RuntimeTraceKind.Turn, "formed", null, identity,
            $"{reason} inputs={inputs.Count} backoff_ms={_scheduler.Backoff.TotalMilliseconds:F0}");
        effects.Add(() =>
        {
            BeginWork();
            _ = ExecuteTurnAsync(turn, _scheduler.InvalidatedTurns > 0, _lifetime.Token);
            return Task.CompletedTask;
        });
    }

    private async Task ExecuteTurnAsync(TurnExecution turn, bool resumedAfterInterruption, CancellationToken outerToken)
    {
        var effects = new EffectQueue();
        try
        {
            var decision = await DecideGateAsync(turn, outerToken);
            if (decision.Disposition == ReplyGateDisposition.Allow && !await StillCurrentAsync(turn, outerToken))
                decision = ReplyGateResult.CancelStale("stale_before_compose");
            lock (_sync)
            {
            if (turn.Closed || !ReferenceEquals(_active, turn)) return;
            switch (decision.Disposition)
            {
                case ReplyGateDisposition.DoNothing:
                    CloseTurnLocked(turn, RuntimeTurnOutcome.DoNothing, decision.Reason, effects, outerToken);
                    return;
                case ReplyGateDisposition.CancelStale:
                    InvalidateActiveLocked(decision.Reason, effects, outerToken);
                    return;
                case ReplyGateDisposition.Defer or ReplyGateDisposition.DeferForCapability:
                    ParkTurnLocked(turn, decision.Reason, effects, outerToken);
                    return;
            }
            }

            ResponsePlan plan;
            try
            {
                plan = await _composer.ComposeAsync(BuildRequest(turn, decision.Advice), turn.Cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                CloseTurnLocked(turn, RuntimeTurnOutcome.Interrupted, "cancelled_during_compose", effects, outerToken);
                return;
            }
            catch (Exception ex)
            {
                // 失败码进 turn.failed，界面据此决定能不能让用户重试，所以不能再统一写成
                // reply_generation_failed：Provider 密钥错与限流是两件完全不同的事。
                var failureCode = ModelProviderError.CodeOf(ex, "reply_generation_failed");
                TraceLocked(RuntimeTraceKind.Turn, "compose_failed", null, turn.Identity,
                    $"{failureCode} {ex.GetType().Name}");
                effects.Add(() => _sink.FailureAsync(ConversationId, turn.Identity, failureCode, outerToken));
                CloseTurnLocked(turn, RuntimeTurnOutcome.Failed, failureCode, effects, outerToken);
                return;
            }

            // 迟到的模型结果：即使模型没有响应取消，也绝不允许进入投递。
            if (!await StillCurrentAsync(turn, outerToken))
            {
                TraceLocked(RuntimeTraceKind.Fence, "dropped_late_model_result", null, turn.Identity,
                    $"messages={plan.Messages.Count}");
                CloseTurnLocked(turn, RuntimeTurnOutcome.FencedOut, "late_model_result", effects, outerToken, 0);
                return;
            }
            if (plan.IsSilent)
            {
                CloseTurnLocked(turn, RuntimeTurnOutcome.DoNothing, plan.DoNothingReason ?? "silent_plan", effects,
                    outerToken);
                return;
            }

            var recheck = await DecideGateAsync(turn, outerToken);
            lock (_sync)
            {
            if (turn.Closed || !ReferenceEquals(_active, turn)) return;
            if (recheck.Disposition == ReplyGateDisposition.CancelStale)
            {
                TraceLocked(RuntimeTraceKind.Fence, "dropped_after_gate_recheck", null, turn.Identity, recheck.Reason);
                CloseTurnLocked(turn, RuntimeTurnOutcome.FencedOut, recheck.Reason, effects, outerToken, 0);
                return;
            }
            if (recheck.Disposition == ReplyGateDisposition.DoNothing)
            {
                CloseTurnLocked(turn, RuntimeTurnOutcome.DoNothing, recheck.Reason, effects, outerToken);
                return;
            }
            if (recheck.Disposition != ReplyGateDisposition.Allow)
            {
                ParkTurnLocked(turn, recheck.Reason, effects, outerToken);
                return;
            }

            if (plan.CapabilityRequest is { } capability)
            {
                _capabilityPending = true;
                _capabilityTurn = turn.Identity;
                _capabilityId = capability.CapabilityId;
                SetPhaseLocked(RuntimePhase.WaitingForTurn, "capability_pending");
                var requested = capability with { RequestedBy = turn.Identity };
                TraceLocked(RuntimeTraceKind.Turn, "capability_requested", null, turn.Identity, requested.CapabilityId);
                effects.Add(() => _sink.CapabilityRequestedAsync(ConversationId, requested, turn.Cancellation.Token));
                effects.Add(() => PersistStateAsync(outerToken));
                return;
            }
            }

            if (!await DeliverAsync(turn, plan, resumedAfterInterruption, effects, outerToken)) return;
        }
        catch (OperationCanceledException)
        {
            CloseTurnLocked(turn, RuntimeTurnOutcome.Interrupted, "cancelled", effects, outerToken);
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
            CloseTurnLocked(turn, RuntimeTurnOutcome.Failed, "runtime_failure", effects, outerToken);
        }
        finally
        {
            await FlushQuietlyAsync(effects);
            lock (_sync) SettleAfterTurnLocked();
            await FlushQuietlyAsync(effects);
            EndWork();
        }
    }

    /// <summary>申请投递许可，排队并等待终局。返回 false 表示本轮已在投递前失效。</summary>
    private async Task<bool> DeliverAsync(TurnExecution turn, ResponsePlan plan, bool resumedAfterInterruption,
        EffectQueue effects, CancellationToken token)
    {
        var permission = await _store.RequestDeliveryAsync(ConversationId, turn.Identity,
            turn.Identity.FencedRevision, plan.Messages, plan.Source, plan.CommitToken, token);
        if (!permission.Approved)
        {
            TraceLocked(RuntimeTraceKind.Fence, "delivery_refused_by_store", null, turn.Identity,
                permission.Reason ?? "epoch_mismatch");
            CloseTurnLocked(turn, RuntimeTurnOutcome.FencedOut, permission.Reason ?? "epoch_mismatch", effects, token, 0);
            return false;
        }
        lock (_sync)
        {
            if (!IsCurrent(turn.Identity) || turn.Closed || !ReferenceEquals(_active, turn) || _disposed)
            {
                effects.Add(() => _store.AbandonPlanAsync(permission, 0, token));
                return false;
            }
            if (_buffer.IsTypingAt(Now()))
            {
                effects.Add(() => _store.AbandonPlanAsync(permission, 0, CancellationToken.None));
                ParkTurnLocked(turn, "typing_before_delivery", effects, token);
                return false;
            }
            turn.Permission = permission;
            turn.Record = turn.Record with { PlannedCount = plan.Messages.Count };
            SetPhaseLocked(RuntimePhase.Delivering, "state_machine");
            _delivery.Enqueue(plan, permission, turn.UserInputLength, resumedAfterInterruption);
        }
        await _delivery.PumpAsync(token);
        var report = await _delivery.WaitForPlanAsync(turn.Identity, token);
        lock (_sync)
        {
            turn.DeliveredCount = report.Delivered;
            if (report.Delivered > 0)
            {
                _lastCharacterActivity = Now();
                _consecutiveCharacterTurns++;
                _conversationProgress++;
            }
        }
        var outcome = report.Planned > 0 && report.Delivered == report.Planned ? RuntimeTurnOutcome.Completed
            : report.Delivered > 0 ? RuntimeTurnOutcome.PartiallyDelivered
            : RuntimeTurnOutcome.FencedOut;
        CloseTurnLocked(turn, outcome, report.Reason, effects, token, report.Delivered);
        return true;
    }

    private async Task ContinueAfterCapabilityAsync(TurnExecution turn, CapabilityResult result, DateTimeOffset at,
        CancellationToken token)
    {
        var effects = new EffectQueue();
        try
        {
            if (turn.Closed || !IsCurrent(turn.Identity)) return;
            ResponsePlan plan;
            try
            {
                plan = await _composer.ComposeAsync(BuildRequest(turn, advice: null) with
                {
                    ReplanAfterCapability = true, Capability = result
                }, turn.Cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                CloseTurnLocked(turn, RuntimeTurnOutcome.Interrupted, "cancelled_after_capability", effects, token);
                return;
            }
            if (!await StillCurrentAsync(turn, token)) return;
            lock (_sync)
            {
                if (turn.Closed || !ReferenceEquals(_active, turn)) return;
                if (plan.IsSilent)
                {
                    CloseTurnLocked(turn, RuntimeTurnOutcome.DoNothing, "silent_capability_continuation", effects, token);
                    return;
                }
                // A single capability continuation is supported; there is no implicit tool loop.
                if (plan.CapabilityRequest is not null)
                {
                    CloseTurnLocked(turn, RuntimeTurnOutcome.Failed, "capability_loop_not_supported", effects, token);
                    return;
                }
            }
            var gate = await DecideGateAsync(turn, token);
            if (gate.Disposition != ReplyGateDisposition.Allow)
            {
                lock (_sync)
                {
                    if (turn.Closed || !ReferenceEquals(_active, turn)) return;
                    if (gate.Disposition == ReplyGateDisposition.DoNothing)
                        CloseTurnLocked(turn, RuntimeTurnOutcome.DoNothing, gate.Reason, effects, token);
                    else ParkTurnLocked(turn, gate.Reason, effects, token);
                }
                return;
            }
            if (!await DeliverAsync(turn, plan, resumedAfterInterruption: true, effects, token)) return;
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
            CloseTurnLocked(turn, RuntimeTurnOutcome.Failed, "capability_continuation_failed", effects, token);
        }
        finally
        {
            await FlushQuietlyAsync(effects);
            lock (_sync) SettleAfterTurnLocked();
            await FlushQuietlyAsync(effects);
            EndWork();
        }
    }

    /// <summary>Turn 结束后的收尾：回到正确的阶段，并检查是否还有攒着的输入要成为下一轮。</summary>
    private void SettleAfterTurnLocked()
    {
        if (_disposed || _phase == RuntimePhase.Suspended || _active is not null) return;
        SetPhaseLocked(_delivery.PendingCount > 0 ? RuntimePhase.Delivering
            : _buffer.Count > 0 ? RuntimePhase.CollectingInput : RuntimePhase.Idle, "turn_settled");
        var effects = new EffectQueue();
        if (_phase == RuntimePhase.Idle) ArmIdleTimerLocked();
        else ArmWakeupLocked(Now());
        if (_buffer.Count > 0 && _active is null) TryFormTurnLocked(Now(), effects, _lifetime.Token);
        _ = FlushQuietlyAsync(effects);
    }

    /// <summary>
    /// Turn 因状态条件（用户仍在输入、仍有排队输出）暂缓：把已攒到的输入退回缓冲，
    /// 等条件变化后重新成轮。绝不丢弃用户说过的话。
    /// </summary>
    private void ParkTurnLocked(TurnExecution turn, string reason, EffectQueue effects, CancellationToken token)
    {
        lock (_sync)
        {
        if (turn.Closed || !ReferenceEquals(_active, turn)) return;
        turn.Cancellation.Cancel();
        for (var index = 0; index < turn.Inputs.Count; index++) _buffer.InsertAt(index, turn.Inputs[index]);
        foreach (var late in turn.ToleratedLate)
            if (!_buffer.Items.Contains(late)) _buffer.Add(late.Input, late.ArrivedAt, late.ForceTurn, late.ExistingMessageId);
        CloseTurnLocked(turn, RuntimeTurnOutcome.Deferred, reason, effects, token);
        SetPhaseLocked(RuntimePhase.WaitingForTurn, "state_machine");
        TraceLocked(RuntimeTraceKind.Gate, "deferred", null, turn.Identity, reason);
        // 暂缓重试必须间隔一个 debounce，否则会形成"成轮—暂缓—立刻成轮"的空转循环。
        _deferredUntil = Now() + TimeSpan.FromMilliseconds(Math.Max(20, _settings.DebounceMs));
        ArmWakeupLocked(Now(), _deferredUntil - Now());
        }
    }

    private async Task<ReplyGateResult> DecideGateAsync(TurnExecution turn, CancellationToken token)
    {
        ReplyGateInputs inputs;
        lock (_sync)
        {
            var at = Now();
            inputs = new ReplyGateInputs(turn.Identity, !turn.Closed && ReferenceEquals(_active, turn) &&
                !IsStaleLocked(turn.Identity), _buffer.IsTypingAt(at),
                _buffer.Items.Any(x => !turn.ToleratedLate.Contains(x)),
                _capabilityPending && _capabilityTurn == turn.Identity, turn.Summary, _options.CharacterNames,
                _lastCharacterActivity,
                _lastCharacterActivity is null ? TimeSpan.Zero : at - _lastCharacterActivity.Value,
                _consecutiveCharacterTurns, _phase, _buffer.Count, _delivery.PendingCount);
        }
        var result = await _gate.DecideAsync(inputs, turn.Cancellation.Token);
        TraceLocked(RuntimeTraceKind.ReplyGate, result.Disposition.ToString(), null, turn.Identity, result.Reason);
        return result;
    }

    private TurnRequest BuildRequest(TurnExecution turn, ReplyAdvice? advice)
    {
        var inputs = turn.Inputs.Select(x => x.ExistingMessageId is { } id
            ? TurnInputPart.Existing(id, x.Input)
            : TurnInputPart.NewInput(x.Input)).ToArray();
        return new TurnRequest(ConversationId, turn.Identity, turn.StartedAt, inputs, turn.Texts, turn.Images,
            turn.Stickers, turn.Identity.FencedRevision,
            new DeliveryPolicyOptions(_settings.StyleInstruction, _settings.MinReplyMessages,
                _settings.MaxReplyMessages, _settings.MaxMessageCharacters), advice);
    }

    #endregion

    #region 主动消息：同一套审核与投递通道

    /// <summary>
    /// 发送前审核。用户正在输入、有待投递输出、有进行中的 Turn、候选已过期或上下文 epoch 已推进，
    /// 一律拒绝。主动消息不得插进 Pry 自己的回复流，也不得带着过时上下文复活。
    /// </summary>
    public async Task<ProactiveReview> ReviewProactiveAsync(ProactiveCandidate candidate,
        CancellationToken cancellationToken = default)
    {
        ProactiveReview review;
        lock (_sync)
        {
            var at = Now();
            review = EvaluateProactiveLocked(candidate, at);
            TraceLocked(RuntimeTraceKind.Proactive, review.Disposition.ToString(), RuntimeEventKind.ProactiveCandidate,
                candidate.CapturedIdentity, $"{review.Reason} candidate={candidate.CandidateId}");
        }
        if (review.Disposition != ProactiveDisposition.Deferred) return review;

        var context = new ReplyGateContext(candidate.CapturedIdentity,
            candidate.Plan.Messages.Count == 0 ? candidate.TriggerReason : candidate.Plan.Messages[0].Text ?? "",
            false, false, false, Phase, _buffer.IsTypingAt(Now()), _buffer.Count, _delivery.PendingCount,
            _lastCharacterActivity,
            _lastCharacterActivity is null ? TimeSpan.Zero : Now() - _lastCharacterActivity.Value,
            _consecutiveCharacterTurns, ProactiveOrigin: true);
        var advice = await _options.Advisor.AdviseAsync(context, cancellationToken);
        lock (_sync)
        {
            var at = Now();
            var current = EvaluateProactiveLocked(candidate, at);
            if (current.Disposition != ProactiveDisposition.Deferred) return current;
            var verdict = !advice.ShouldReply || advice.Intent == ResponseIntent.DoNothing
                ? new ProactiveReview(ProactiveDisposition.RejectedPolicy, advice.Reason, at, _revision)
                : new ProactiveReview(ProactiveDisposition.Approved, advice.Reason, at, _revision);
            TraceLocked(RuntimeTraceKind.Proactive, verdict.Disposition.ToString(), RuntimeEventKind.ProactiveCandidate,
                candidate.CapturedIdentity, $"policy:{verdict.Reason}");
            return verdict;
        }
    }

    /// <summary>
    /// 审核通过后的唯一投递入口：发送前再次核对取消状态，然后走与普通回复完全相同的 DeliveryScheduler。
    /// Delivered=0 表示发送前已被取消，宿主不得把它记成"已发送"。
    /// </summary>
    public async Task<DeliveryReport> DeliverProactiveAsync(ProactiveCandidate candidate,
        CancellationToken cancellationToken = default)
    {
        BeginWork();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        try { return await DeliverProactiveCoreAsync(candidate, linked.Token); }
        finally { EndWork(); }
    }

    private async Task<DeliveryReport> DeliverProactiveCoreAsync(ProactiveCandidate candidate,
        CancellationToken cancellationToken)
    {
        var review = await ReviewProactiveAsync(candidate, cancellationToken);
        if (!review.Approved)
            return new DeliveryReport(candidate.CapturedIdentity, candidate.Plan.Messages.Count, 0,
                candidate.Plan.Messages.Count, review.Reason);
        var plan = candidate.Plan with { ProactiveOrigin = true, CommitToken = candidate.AttemptToken ?? candidate.CandidateId };
        var planned = plan.Messages.Count;
        lock (_sync)
        {
            var blocked = EvaluateProactiveLocked(candidate, Now());
            // Approved 只由策略阶段给出；这里还没问策略，因此 Deferred 才是"可以继续"。
            if (blocked.Disposition != ProactiveDisposition.Deferred)
                return new DeliveryReport(candidate.CapturedIdentity, planned, 0, planned, blocked.Reason);
        }

        TurnIdentity deliveryIdentity;
        lock (_sync) deliveryIdentity = NewTurnIdentityLocked();
        var permission = await _store.RequestDeliveryAsync(ConversationId, deliveryIdentity, candidate.CapturedIdentity.FencedRevision,
            plan.Messages, plan.Source, plan.CommitToken, cancellationToken);
        if (!permission.Approved)
            return new DeliveryReport(candidate.CapturedIdentity, planned, 0, planned,
                permission.Reason ?? "refused_by_store_fence");

        lock (_sync)
        {
            var blocked = EvaluateProactiveLocked(candidate, Now());
            if (blocked.Disposition != ProactiveDisposition.Deferred)
            {
                var effects = new EffectQueue();
                effects.Add(() => _store.AbandonPlanAsync(permission, 0, cancellationToken));
                _ = FlushQuietlyAsync(effects);
                return new DeliveryReport(candidate.CapturedIdentity, planned, 0, planned, blocked.Reason);
            }
            SetPhaseLocked(RuntimePhase.Delivering, "state_machine");
            _delivery.Enqueue(plan, permission, 0, resumedAfterInterruption: false, candidate.ExpiresAt);
            CancelTimer();
        }
        using var cancelled = cancellationToken.Register(() =>
        {
            foreach (var item in _delivery.CancelTurn(deliveryIdentity, "proactive_cancelled"))
                _ = TrackEffectAsync(_ => _store.AbandonPlanAsync(item.Permission, item.FromSequence, CancellationToken.None));
        });
        await _delivery.PumpAsync(_lifetime.Token);
        var report = await _delivery.WaitForPlanAsync(deliveryIdentity, _lifetime.Token);
        lock (_sync)
        {
            if (report.Delivered > 0)
            {
                _lastCharacterActivity = Now();
                _consecutiveCharacterTurns++;
                _conversationProgress++;
            }
            if (_phase == RuntimePhase.Delivering && _delivery.PendingCount == 0)
                SetPhaseLocked(RuntimePhase.Idle, "proactive_settled");
            if (_phase == RuntimePhase.Idle) ArmIdleTimerLocked();
        }
        TraceLocked(RuntimeTraceKind.Proactive, $"delivered:{report.Delivered}/{report.Planned}",
            RuntimeEventKind.ProactiveCandidate, candidate.CapturedIdentity, report.Reason);
        await PersistStateAsync(CancellationToken.None);
        return report;
    }


    private ProactiveReview EvaluateProactiveLocked(ProactiveCandidate candidate, DateTimeOffset at)
    {
        if (_disposed) return new ProactiveReview(ProactiveDisposition.RejectedRuntimeUnavailable, "runtime_disposed", at, _revision);
        if (candidate.Expired(at)) return Reject(ProactiveDisposition.RejectedExpired, "candidate_expired", at);
        if (candidate.ConversationId != ConversationId || candidate.CharacterInstanceId != CharacterInstanceId)
            return Reject(ProactiveDisposition.RejectedStale, "candidate_conversation_mismatch", at);
        if (_phase == RuntimePhase.Suspended) return Reject(ProactiveDisposition.RejectedSuspended, "runtime_suspended", at);
        if (IsStaleLocked(candidate.CapturedIdentity))
            return Reject(ProactiveDisposition.RejectedStale, "candidate_context_changed", at);
        if (candidate.CapturedProgress != _conversationProgress)
            return Reject(ProactiveDisposition.RejectedStale, $"conversation_moved:{candidate.CapturedProgress}->{_conversationProgress}", at);
        if (_buffer.Count > 0 || _buffer.IsTypingAt(at)) return Reject(ProactiveDisposition.RejectedUserActive, "user_is_active", at);
        if (_active is not null || _delivery.PendingCount > 0)
            return Reject(ProactiveDisposition.RejectedTurnBusy, "output_in_progress", at);
        if (candidate.Plan.IsSilent) return Reject(ProactiveDisposition.RejectedPolicy, "candidate_is_silent", at);
        return new ProactiveReview(ProactiveDisposition.Deferred, "awaiting_policy", at, _revision);
    }


    private ProactiveReview Reject(ProactiveDisposition disposition, string reason, DateTimeOffset at) =>
        new(disposition, reason, at, _revision);

    /// <summary>把一条已落库的历史用户消息重新作为一个 Turn 的输入（重新生成入口）。</summary>
    public async Task<RuntimeReceipt> RestoreTurnAsync(long messageId, RuntimeUserInput rendered,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _eventGate.WaitAsync(cancellationToken);
        BeginWork();
        try
        {
        var effects = new EffectQueue();
        int revisionBefore;
        TurnIdentity? turn;
        lock (_sync)
        {
            var at = Now();
            revisionBefore = _revision;
            RegisterActivityLocked(at);
            _revision++;
            InvalidateActiveLocked("regenerate", effects, cancellationToken);
            _buffer.Clear();
            _buffer.Add(rendered, at, forceTurn: true, existingMessageId: messageId);
            _lastUserActivity = at;
            _scheduler.Reset();
            TryFormTurnLocked(at, effects, cancellationToken);
            turn = _active?.Identity;
        }
        await effects.FlushAsync();
        return new RuntimeReceipt(revisionBefore, _revision, Phase, RuntimeEventKind.UserInput, "restored_turn",
            messageId, turn);
        }
        finally { EndWork(); _eventGate.Release(); }
    }

    /// <summary>清空尚未投递的输出。已发出的消息不回撤。返回被清掉的气泡数。</summary>
    public async Task<int> ClearPendingOutputsAsync(string reason)
    {
        var effects = new EffectQueue();
        int cleared;
        lock (_sync)
        {
            var before = _delivery.PendingCount;
            foreach (var item in _delivery.CancelAll(reason))
                effects.Add(() => _store.AbandonPlanAsync(item.Permission, item.FromSequence, CancellationToken.None));
            cleared = before - _delivery.PendingCount;
            if (cleared > 0) BumpEpochLocked(reason, effects, CancellationToken.None);
            if (_phase == RuntimePhase.Delivering && _delivery.PendingCount == 0) _phase = RuntimePhase.Idle;
        }
        await effects.FlushAsync();
        return cleared;
    }

    /// <summary>是否可以在不丢失状态的前提下从内存中卸载。</summary>
    public bool CanEvict(DateTimeOffset at)
    {
        lock (_sync)
            return _phase == RuntimePhase.Idle && _buffer.Count == 0 && _delivery.PendingCount == 0
                && _active is null && _outstanding == 0
                && at - (_lastEventAt ?? at) >= TimeSpan.FromMilliseconds(_settings.EvictAfterIdleMs);
    }

    public bool IsIdle()
    {
        lock (_sync) return _phase == RuntimePhase.Idle && _buffer.Count == 0 && _delivery.PendingCount == 0
            && _active is null;
    }

    public TimeSpan IdleFor(DateTimeOffset at)
    {
        lock (_sync) return _lastEventAt is null ? TimeSpan.Zero : at - _lastEventAt.Value;
    }

    /// <summary>等待所有在途工作（模型调用、落库、投递）结束。用于优雅退出、驱逐与测试。</summary>
    /// <summary>
    /// 等待运行时彻底安静：没有在途工作、缓冲里不再攒着待成轮的输入、也没有排队待投递的气泡。
    /// 只看"在途任务"是不够的——一次刚被计时器接住、还没成轮的输入并不在任何 Task 里。
    /// </summary>
    public Task<bool> DrainAsync(TimeSpan timeout, CancellationToken cancellationToken = default) =>
        WaitWhileAsync(_ => !IsQuiescentLocked(), timeout, cancellationToken);

    /// <summary>只等待在途任务结束。用于退出路径：临时输入允许丢弃，不该拖住关闭。</summary>
    private Task<bool> DrainOutstandingAsync(TimeSpan timeout) =>
        WaitWhileAsync(_ => Volatile.Read(ref _outstanding) > 0, timeout, CancellationToken.None);

    private async Task<bool> WaitWhileAsync(Func<DateTimeOffset, bool> stillWaiting, TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(timeout);
        while (true)
        {
            Task waiting;
            lock (_sync)
            {
                if (!stillWaiting(Now())) return true;
                _idleSignal ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                waiting = _idleSignal.Task;
            }
            var deadline = Task.Delay(Timeout.InfiniteTimeSpan, linked.Token);
            var finished = await Task.WhenAny(waiting, deadline);
            if (finished != waiting)
            {
                lock (_sync) return !stillWaiting(Now());
            }
        }
    }

    private bool IsQuiescentLocked() =>
        _outstanding == 0 && _buffer.Count == 0 && _delivery.PendingCount == 0 &&
        (_active is null || _active.Closed || _capabilityPending);

    #endregion

    #region 基础设施

    private DateTimeOffset Now() => _clock.GetUtcNow();

    /// <summary>
    /// 阶段变更的唯一入口：迁移、轨迹和对外投影都从这里走，
    /// 避免宿主看到"状态变了但没人知道"的中间态。
    /// </summary>
    private void SetPhaseLocked(RuntimePhase next, string reason)
    {
        if (_phase == next) return;
        var previous = _phase;
        _phase = next;
        TraceLocked(RuntimeTraceKind.Lifecycle, $"phase:{previous}->{next}", null, null, reason);
        _ = TrackEffectAsync(async token => await _sink.PhaseChangedAsync(ConversationId, next, previous, token));
    }

    private async Task TrackEffectAsync(Func<CancellationToken, Task> effect)
    {
        BeginWork();
        try
        {
            await effect(_lifetime.Token);
        }
        catch (Exception ex)
        {
            lock (_sync) TraceLocked(RuntimeTraceKind.Lifecycle, "effect_failed", null, null, ex.GetType().Name);
        }
        finally
        {
            EndWork();
        }
    }

    private void TraceLocked(RuntimeTraceKind kind, string action, RuntimeEventKind? via, TurnIdentity? turn,
        string reason) => _trace.Record(via, kind, action, turn, reason, Math.Max(0, _revision - 1), _revision, Now());

    private void BeginWork()
    {
        lock (_sync) _outstanding++;
    }

    private void EndWork()
    {
        TaskCompletionSource? signal;
        lock (_sync)
        {
            if (_outstanding > 0) _outstanding--;
            if (_outstanding > 0) return;
            signal = _idleSignal;
            _idleSignal = null;
        }
        signal?.TrySetResult();
    }

    private void ReportFailure(Exception ex) =>
        TraceLocked(RuntimeTraceKind.Delivery, "delivery_failed", null, null, ex.GetType().Name);

    private Task PersistStateAsync(CancellationToken token)
    {
        RuntimePersistenceState state;
        lock (_sync)
            state = new RuntimePersistenceState(_epoch,
                (_lastCharacterActivity ?? DateTimeOffset.MinValue) > (_lastUserActivity ?? DateTimeOffset.MinValue),
                new[] { _lastCharacterActivity, _lastUserActivity }.Max() ?? Now(), _consecutiveCharacterTurns);
        return _store.SaveStateAsync(ConversationId, state, token);
    }

    /// <summary>
    /// 权威性校验：先看本地失效 epoch，再向存储确认 epoch。
    /// 存储不可用时挂起而不是继续投递；外部写入者抬高 epoch 时本轮立刻失效。
    /// </summary>
    private async Task<bool> StillCurrentAsync(TurnExecution turn, CancellationToken token)
    {
        long expected;
        lock (_sync)
        {
            if (_disposed || IsStaleLocked(turn.Identity)) return false;
            expected = turn.Identity.FencedRevision;
        }
        long authoritative;
        try
        {
            authoritative = await _store.ReadEpochAsync(ConversationId, token);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            TraceLocked(RuntimeTraceKind.Fence, "store_unavailable", null, turn.Identity, ex.GetType().Name);
            var effects = new EffectQueue();
            lock (_sync)
            {
                if (turn.Closed || !ReferenceEquals(_active, turn)) return false;
                InvalidateActiveLocked("suspended:StoreUnavailable", effects, CancellationToken.None);
                SetPhaseLocked(RuntimePhase.Suspended, "state_machine");
                CancelTimer();
            }
            await FlushQuietlyAsync(effects);
            return false;
        }
        if (authoritative == expected) return true;
        lock (_sync)
        {
            if (authoritative > _epoch) _epoch = authoritative;
            TraceLocked(RuntimeTraceKind.Fence, "authoritative_epoch_advanced", null, turn.Identity,
                $"expected={expected} actual={authoritative}");
        }
        return false;
    }

    private void ArmWakeupLocked(DateTimeOffset at, TimeSpan? overrideDelay = null)
    {
        if (_buffer.Count == 0)
        {
            CancelTimer();
            return;
        }
        if (_active is not null) return;   // 有 Turn 在飞时由它的结束钩子重新评估，不空转计时器
        var delay = overrideDelay ?? (_buffer.Assess(at, _delivery.PendingCount > 0).Complete
            ? TimeSpan.FromMilliseconds(20)
            : _buffer.Assess(at, _delivery.PendingCount > 0).Remaining);
        // 绝不按超过硬上限的时间挂计时器：桌面用户改系统时钟时也不要把一轮对话挂死。
        var ceiling = TimeSpan.FromMilliseconds(_settings.MaxBufferMs);
        if (delay > ceiling) delay = ceiling;
        if (delay <= TimeSpan.Zero) delay = TimeSpan.FromMilliseconds(20);
        ScheduleTimer(delay, RuntimeEventKind.TurnDue);
    }

    private void ArmIdleTimerLocked()
    {
        CancelTimer();
        if (_settings.IdleHeartbeatMs <= 0) return;
        ScheduleTimer(TimeSpan.FromMilliseconds(_settings.IdleHeartbeatMs), RuntimeEventKind.IdleElapsed);
    }

    private void ScheduleTimer(TimeSpan delay, RuntimeEventKind kind)
    {
        CancelTimer();
        _wakeTimer = _clock.CreateTimer(_ => _ = WakeAsync(kind), null, delay, Timeout.InfiniteTimeSpan);
    }

    /// <summary>只释放当前计时器；唤醒回调自己会重新登记下一跳。</summary>
    private void CancelTimer()
    {
        var timer = _wakeTimer;
        _wakeTimer = null;
        timer?.Dispose();
    }

    private async Task WakeAsync(RuntimeEventKind kind)
    {
        if (_disposed) return;
        try
        {
            await AcceptAsync(new RuntimeEvent(kind, Now()), _lifetime.Token);
        }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            lock (_sync) TraceLocked(RuntimeTraceKind.Lifecycle, "wake_failed", kind, null, ex.GetType().Name);
        }
    }

    private async Task FlushQuietlyAsync(EffectQueue effects)
    {
        try
        {
            await effects.FlushAsync();
        }
        catch (Exception ex)
        {
            lock (_sync) TraceLocked(RuntimeTraceKind.Lifecycle, "effect_failed", null, null, ex.GetType().Name);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        lock (_sync) CancelTimer();
        _delivery.Stop();
        _lifetime.Cancel();
        await DrainOutstandingAsync(TimeSpan.FromSeconds(2));
        var effects = new EffectQueue();
        lock (_sync)
        {
            foreach (var item in _delivery.CancelAll("runtime_disposed"))
                effects.Add(() => _store.AbandonPlanAsync(item.Permission, item.FromSequence, CancellationToken.None));
        }
        await FlushQuietlyAsync(effects);
        try { await PersistStateAsync(CancellationToken.None); } catch { /* 进程正在退出 */ }
        _lifetime.Dispose();
        _delivery.Dispose();
    }

    /// <summary>一次 Turn 的执行记录。只在 Runtime 内部可见。</summary>
    private sealed class TurnExecution(TurnIdentity identity, IReadOnlyList<BufferedInput> inputs,
        DateTimeOffset startedAt, CancellationToken lifetimeToken)
    {
        public TurnIdentity Identity { get; } = identity;
        public IReadOnlyList<BufferedInput> Inputs { get; } = inputs;
        public DateTimeOffset StartedAt { get; } = startedAt;
        public CancellationTokenSource Cancellation { get; } =
            CancellationTokenSource.CreateLinkedTokenSource(lifetimeToken);
        public List<BufferedInput> ToleratedLate { get; } = [];
        public TurnRecord Record { get; set; } = new(identity, startedAt, "", inputs.Count);
        public DeliveryPermission? Permission { get; set; }
        public int DeliveredCount { get; set; }
        public bool Closed { get; set; }

        public string Summary => string.Join("\n", Texts);
        public IReadOnlyList<string> Texts => Inputs.Select(x => InputProbe.Normalize(x.Input.Text))
            .Where(x => x.Length > 0).ToArray();
        public IReadOnlyList<string> Images => Inputs.SelectMany(x => x.Input.SafeAttachments)
            .Where(x => x.IsImage).Select(x => x.Reference).Distinct().ToArray();
        public IReadOnlyList<string> Stickers => Inputs.Select(x => x.Input.StickerId)
            .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).Distinct().ToArray();
        public double UserInputLength => Inputs.Sum(x => InputProbe.VisualLength(x.Input.Text));
    }

    /// <summary>
    /// 状态迁移产生的外部效果。在锁外按登记顺序执行，保证副作用顺序与状态顺序一致；
    /// 用户消息落库一定发生在同一事件引发的规划与投递之前。
    /// </summary>
    private sealed class EffectQueue
    {
        private readonly List<Func<Task>> _items = [];

        public long? UserMessageId { get; private set; }

        public void Add(Func<Task> effect) => _items.Add(effect);

        /// <summary>用户消息落库必须先于同一事件引发的规划与投递，因此登记在队列最前。</summary>
        public void AppendUserInput(IRuntimeStore store, string conversationId, RuntimeUserInput input,
            CancellationToken token) =>
            _items.Insert(0, async () => UserMessageId = await store.AppendUserInputAsync(conversationId, input, token));

        public async Task FlushAsync()
        {
            while (_items.Count > 0)
            {
                var next = _items[0];
                _items.RemoveAt(0);
                await next();
            }
        }
    }

    #endregion
}

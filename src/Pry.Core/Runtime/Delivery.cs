namespace Pry.Core.Runtime;

/// <summary>一个尚未投递的气泡。Permission 携带 fencing 快照，投递时由权威状态复核。</summary>
public sealed record PendingBubble(DeliveryPermission Permission, RuntimePlannedMessage Message,
    TurnIdentity Turn, DateTimeOffset NotAfter, bool AnnounceTyping, DateTimeOffset? ExpiresAt = null)
{
    public PendingBubble WithNotAfter(DateTimeOffset at) => this with { NotAfter = at };
}

/// <summary>
/// PendingOutputQueue：已规划但尚未成为气泡的输出。任何时刻都可以按 Turn 整轮取消，
/// 取消只影响未投递的部分——已经发出去的消息不回撤。
/// </summary>
public sealed class PendingOutputQueue
{
    private readonly List<PendingBubble> _items = [];
    private readonly object _gate = new();

    public int Count { get { lock (_gate) return _items.Count; } }

    public IReadOnlyList<PendingBubble> Snapshot()
    {
        lock (_gate) return _items.ToArray();
    }

    public IReadOnlyList<PendingBubble> AddRange(IEnumerable<PendingBubble> bubbles)
    {
        var added = bubbles.ToArray();
        lock (_gate)
        {
            _items.AddRange(added);
            // 主动消息与普通回复共用一条队列，按到点时间重排，保证跨 Turn 的顺序仍是投递顺序。
            _items.Sort(static (left, right) => left.NotAfter.CompareTo(right.NotAfter));
        }
        return added;
    }

    public PendingBubble? TakeDue(DateTimeOffset at, out TimeSpan wait)
    {
        lock (_gate)
        {
            if (_items.Count == 0)
            {
                wait = TimeSpan.Zero;
                return null;
            }
            if (_items[0].NotAfter > at)
            {
                wait = _items[0].NotAfter - at;
                return null;
            }
            var head = _items[0];
            _items.RemoveAt(0);
            wait = TimeSpan.Zero;
            return head;
        }
    }

    public IReadOnlyList<PendingBubble> RemoveForTurn(TurnIdentity turn)
    {
        lock (_gate)
        {
            var removed = _items.Where(x => x.Turn == turn).ToArray();
            foreach (var item in removed) _items.Remove(item);
            return removed;
        }
    }

    public IReadOnlyList<PendingBubble> RemoveAll()
    {
        lock (_gate)
        {
            var removed = _items.ToArray();
            _items.Clear();
            return removed;
        }
    }

    /// <summary>用户仍在说话时把未投递部分整体后移，而不是取消已经合法规划好的内容。</summary>
    public void ShiftAll(TimeSpan delta)
    {
        lock (_gate)
        {
            for (var index = 0; index < _items.Count; index++)
                _items[index] = _items[index].WithNotAfter(_items[index].NotAfter + delta);
        }
    }

    public TurnIdentity? PeekTurn()
    {
        lock (_gate) return _items.Count == 0 ? null : _items[0].Turn;
    }
}

/// <summary>
/// 节奏推导的输入。PreviousBubbleCharacters 表示"用户刚读完的那段话"有多长：
/// 首条气泡对应用户本轮输入，之后对应上一条气泡。
/// </summary>
public sealed record PacingContext(int BubbleIndex, int PreviousBubbleCharacters, bool ResumedAfterInterruption,
    bool ProactiveOrigin, DeliveryHints? Hints);

/// <summary>
/// 气泡节奏：组合成本 + 阅读时间 + 表达习惯抖动，并对打断后重开、主动消息单独放慢。
/// 明确不使用固定字符数切分气泡；长度只是阅读时间的一项输入。
/// </summary>
public sealed class ReplyPacing(Random random)
{
    private const double PerCharacterReadMs = 46;
    private const double BaseComposeMs = 320;

    public TimeSpan DelayFor(PacingContext context, RuntimeSettings settings)
    {
        var style = settings.Typing;
        if (context.BubbleIndex == 0 && context.Hints?.FirstMessageDelay is { } first) return Clamp(first, style);
        if (context.BubbleIndex > 0 && context.Hints?.InterMessageDelays is { } delays
            && context.BubbleIndex - 1 < delays.Count) return Clamp(delays[context.BubbleIndex - 1], style);

        var read = Math.Pow(Math.Min(context.PreviousBubbleCharacters, 240), 0.72) * PerCharacterReadMs
            / Math.Max(0.25, style.Speed);
        var baseline = BaseComposeMs / Math.Max(0.25, style.Speed) + read;
        if (context.ResumedAfterInterruption) baseline *= 1.35;
        if (context.ProactiveOrigin) baseline *= 1.6;
        var jitter = random.NextDouble() * 380 * style.Burstiness;
        return TimeSpan.FromMilliseconds(Math.Clamp(baseline + jitter, style.MinDelayMs, style.MaxDelayMs));
    }

    private static TimeSpan Clamp(TimeSpan value, TypingStyle style) =>
        TimeSpan.FromMilliseconds(Math.Clamp(value.TotalMilliseconds, style.MinDelayMs, style.MaxDelayMs));
}

public sealed record DeliveryAbandonment(DeliveryPermission Permission, int FromSequence, string Reason);

/// <summary>一个 Turn 的投递终局。Delivered 只统计真正写入成功的气泡。</summary>
public sealed record DeliveryReport(TurnIdentity Turn, int Planned, int Delivered, int Abandoned, string Reason);

/// <summary>
/// Delivery Scheduler：把 ResponsePlan 分时投递成真实聊天行为。
/// 负责首条延迟、条间延迟、typing 指示、取消未投递气泡，并在每次落库前接受权威状态复核；
/// 复核不过（本轮已失效、计划已被取消、迟到结果）时直接放弃剩余气泡。
/// </summary>
public sealed class DeliveryScheduler : IDisposable
{
    private readonly TimeProvider _clock;
    private readonly RuntimeSettings _settings;
    private readonly IRuntimeStore _store;
    private readonly IRuntimeOutputSink _sink;
    private readonly ReplyPacing _pacing;
    private readonly Func<TurnIdentity, bool> _isCurrent;
    private readonly Action<Exception> _failure;
    private readonly PendingOutputQueue _queue = new();
    private readonly CancellationTokenSource _stopping = new();
    private readonly Dictionary<TurnIdentity, PlanTracker> _plans = [];
    private readonly object _planGate = new();
    private readonly SemaphoreSlim _changed = new(0, 1);
    private PendingBubble? _inFlight;
    private int _pumping;
    private int _deliveredTotal;

    public DeliveryScheduler(string conversationId, TimeProvider clock, RuntimeSettings settings, IRuntimeStore store,
        IRuntimeOutputSink sink, ReplyPacing pacing, Func<TurnIdentity, bool> isCurrent, Action<Exception> failure)
    {
        ConversationId = conversationId;
        _clock = clock;
        _settings = settings;
        _store = store;
        _sink = sink;
        _pacing = pacing;
        _isCurrent = isCurrent;
        _failure = failure;
    }

    public string ConversationId { get; }
    public int PendingCount { get { lock (_planGate) return _queue.Count + (_inFlight is null ? 0 : 1); } }
    public IReadOnlyList<PendingBubble> Pending => _queue.Snapshot();
    public int DeliveredTotal => Volatile.Read(ref _deliveredTotal);
    public bool IsPumping => Volatile.Read(ref _pumping) == 1;
    public TurnIdentity? DeliveringTurn { get { lock (_planGate) return _inFlight?.Turn ?? _queue.PeekTurn(); } }

    public IReadOnlyList<PendingBubble> Enqueue(ResponsePlan plan, DeliveryPermission permission,
        double userInputLength, bool resumedAfterInterruption, DateTimeOffset? expiresAt = null)
    {
        var at = _clock.GetUtcNow();
        var previous = (int)userInputLength;
        var added = new List<PendingBubble>(plan.Messages.Count);
        for (var index = 0; index < plan.Messages.Count; index++)
        {
            var message = plan.Messages[index];
            at += _pacing.DelayFor(new PacingContext(index, previous, resumedAfterInterruption,
                plan.ProactiveOrigin, plan.Hints), _settings);
            previous = (int)InputProbe.VisualLength(message.Text ?? message.StickerId);
            added.Add(new PendingBubble(permission, message, permission.Identity, at,
                plan.Hints?.EmitTypingIndicator ?? true, expiresAt));
        }
        RegisterPlan(permission.Identity, plan.Messages.Count);
        _queue.AddRange(added);
        Signal();
        return added;
    }

    public IReadOnlyList<DeliveryAbandonment> CancelTurn(TurnIdentity turn, string reason)
    {
        var removed = _queue.RemoveForTurn(turn);
        IReadOnlyList<DeliveryAbandonment> abandonments;
        lock (_planGate)
        {
            abandonments = GroupAbandonments(_inFlight is { } flight && flight.Turn == turn
                ? removed.Append(flight).ToArray() : removed, reason);
            if (_plans.TryGetValue(turn, out var tracker))
            {
                tracker.Cancellation.Cancel();
                tracker.Abandoned += removed.Count;
                tracker.Reason ??= reason;
                TrySettle(turn, tracker);
            }
        }
        Signal();
        return abandonments;
    }

    public IReadOnlyList<DeliveryAbandonment> CancelAll(string reason)
    {
        TurnIdentity[] turns;
        lock (_planGate) turns = _plans.Where(x => !x.Value.Settled).Select(x => x.Key).ToArray();
        return turns.SelectMany(turn => CancelTurn(turn, reason)).ToArray();
    }

    public void ShiftPending(TimeSpan delta) => _queue.ShiftAll(delta);

    /// <summary>某个 Turn 已成功投递的气泡数，即使它尚未被等待方读取。</summary>
    public int DeliveredFor(TurnIdentity turn)
    {
        lock (_planGate) return _plans.TryGetValue(turn, out var tracker) ? tracker.Delivered : 0;
    }

    public void Stop() { _stopping.Cancel(); Signal(); }

    private void Signal()
    {
        try { _changed.Release(); } catch (SemaphoreFullException) { }
    }

    /// <summary>等待某个 Turn 的投递终局。Turn 从未进入投递时返回空报告，不抛异常。</summary>
    public Task<DeliveryReport> WaitForPlanAsync(TurnIdentity turn, CancellationToken cancellationToken)
    {
        PlanTracker tracker;
        lock (_planGate)
        {
            if (!_plans.TryGetValue(turn, out tracker!))
                return Task.FromResult(new DeliveryReport(turn, 0, 0, 0, "plan_not_enqueued"));
        }
        return tracker.Completion.Task.WaitAsync(cancellationToken);
    }

    public async Task PumpAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _pumping, 1) == 1) return;
        try
        {
            // 用循环而不是递归重入：停止时如果队列里还有气泡，自我调用会无界递归直到栈溢出。
            do
            {
                await PumpCoreAsync(cancellationToken);
            }
            while (_queue.Count > 0 && !cancellationToken.IsCancellationRequested
                && !_stopping.IsCancellationRequested);
        }
        finally
        {
            Interlocked.Exchange(ref _pumping, 0);
            // Enqueue can race the final empty check; hand the pump to its new work.
            if (_queue.Count > 0 && !cancellationToken.IsCancellationRequested && !_stopping.IsCancellationRequested)
                _ = PumpAsync(cancellationToken);
        }
    }

    private void RegisterPlan(TurnIdentity turn, int planned)
    {
        lock (_planGate)
        {
            if (_plans.Count > 24)
                foreach (var stale in _plans.Where(x => x.Value.Settled).Take(12).ToArray())
                    _plans.Remove(stale.Key);
            var tracker = new PlanTracker { Planned = planned };
            _plans[turn] = tracker;
            TrySettle(turn, tracker);
        }
    }

    private async Task PumpCoreAsync(CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stopping.Token);
        try
        {
            while (!linked.IsCancellationRequested)
            {
                var bubble = _queue.TakeDue(_clock.GetUtcNow(), out var wait);
                if (bubble is null)
                {
                    if (wait <= TimeSpan.Zero) break;
                    using var wake = CancellationTokenSource.CreateLinkedTokenSource(linked.Token);
                    var elapsed = Task.Delay(wait, _clock, wake.Token);
                    var changed = _changed.WaitAsync(wake.Token);
                    await Task.WhenAny(elapsed, changed);
                    await wake.CancelAsync();
                    continue;
                }
                PlanTracker tracker;
                lock (_planGate)
                {
                    tracker = _plans[bubble.Turn];
                    _inFlight = bubble;
                }
                using var deliveryToken = CancellationTokenSource.CreateLinkedTokenSource(linked.Token, tracker.Cancellation.Token);
                var accounted = false;
                try
                {
                    deliveryToken.Token.ThrowIfCancellationRequested();
                    if (!_isCurrent(bubble.Turn)) throw new OperationCanceledException("turn_no_longer_current");
                    if (bubble.AnnounceTyping) await _sink.TypingIndicatorAsync(ConversationId, true, deliveryToken.Token);
                    // Sink callbacks may admit a user event, so check once more immediately before commit.
                    deliveryToken.Token.ThrowIfCancellationRequested();
                    if (!_isCurrent(bubble.Turn)) throw new OperationCanceledException("turn_no_longer_current");
                    if (bubble.ExpiresAt is { } expiry && _clock.GetUtcNow() >= expiry)
                        throw new OperationCanceledException("candidate_expired");
                    var messageId = await _store.CommitBubbleAsync(bubble.Permission, bubble.Message.Sequence,
                        bubble.Message, deliveryToken.Token);
                    if (messageId is null) throw new OperationCanceledException("rejected_by_store_fence");
                    Interlocked.Increment(ref _deliveredTotal);
                    Account(bubble.Turn, delivered: true, null);
                    accounted = true;
                    await _sink.MessageDeliveredAsync(ConversationId, messageId.Value, bubble.Message,
                        bubble.Permission.Source, linked.Token);
                }
                catch (Exception ex)
                {
                    var reason = ex is OperationCanceledException ? "delivery_cancelled" : "delivery_failed";
                    if (ex is not OperationCanceledException) _failure(ex);
                    if (!accounted) Account(bubble.Turn, delivered: false, reason);
                    foreach (var abandoned in CancelTurn(bubble.Turn, reason))
                        try { await _store.AbandonPlanAsync(abandoned.Permission, abandoned.FromSequence, CancellationToken.None); }
                        catch (Exception failure) { _failure(failure); }
                }
                finally
                {
                    lock (_planGate) _inFlight = null;
                    if (bubble.AnnounceTyping)
                        try { await _sink.TypingIndicatorAsync(ConversationId, false, CancellationToken.None); }
                        catch (Exception ex) { _failure(ex); }
                }
            }
        }
        catch (OperationCanceledException)
        {
            foreach (var abandoned in CancelAll("delivery_stopped"))
                try { await _store.AbandonPlanAsync(abandoned.Permission, abandoned.FromSequence, CancellationToken.None); }
                catch (Exception ex) { _failure(ex); }
        }
        catch (Exception ex)
        {
            _failure(ex);
        }
    }

    private async Task Discard(PendingBubble bubble, string reason, CancellationToken cancellationToken)
    {
        await _store.AbandonPlanAsync(bubble.Permission, bubble.Message.Sequence, cancellationToken);
        Account(bubble.Turn, delivered: false, reason);
    }

    private void Account(TurnIdentity turn, bool delivered, string? reason)
    {
        lock (_planGate)
        {
            if (!_plans.TryGetValue(turn, out var tracker)) return;
            if (delivered) tracker.Delivered++; else tracker.Abandoned++;
            if (reason is not null) tracker.Reason ??= reason;
            TrySettle(turn, tracker);
        }
    }

    private void TrySettle(TurnIdentity turn, PlanTracker tracker)
    {
        if (tracker.Settled || tracker.Delivered + tracker.Abandoned < tracker.Planned) return;
        tracker.Settled = true;
        tracker.Completion.TrySetResult(new DeliveryReport(turn, tracker.Planned, tracker.Delivered,
            tracker.Abandoned, tracker.Reason ?? (tracker.Delivered > 0 ? "delivered" : "not_delivered")));
    }

    private static IReadOnlyList<DeliveryAbandonment> GroupAbandonments(IReadOnlyList<PendingBubble> removed, string reason) =>
        removed.GroupBy(x => x.Permission)
            .Select(group => new DeliveryAbandonment(group.Key, group.Min(x => x.Message.Sequence), reason))
            .ToArray();

    public void Dispose() => _stopping.Dispose();

    private sealed class PlanTracker
    {
        public int Planned;
        public int Delivered;
        public int Abandoned;
        public bool Settled;
        public string? Reason;
        public readonly CancellationTokenSource Cancellation = new();
        public readonly TaskCompletionSource<DeliveryReport> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}

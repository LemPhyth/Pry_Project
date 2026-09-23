// Ported from Mai-with-u/MaiBot@fb4a8bd917d6fe373df9e3c22289d229e7f1b344:
//   src/maisaka/runtime.py (_record_external_message_interval,
//   _prune_recent_external_message_intervals, _get_recent_average_external_message_interval)
// Type: ported (Python -> C#). Upstream is GPL-3.0-only; see
//   licenses/upstream/MaiBot_fb4a8bd917d6fe373df9e3c22289d229e7f1b344/LICENSE.
// Pry modifications: 2026-09-23 — clock injected as TimeProvider. Of the three shape
//   parameters, only the sample window is Pry's own choice (upstream 1800 s -> Pry 900 s);
//   the burst floor (5 s) and the average floor (30 s) equal upstream's source constants and
//   are therefore registered as upstream-derived and uncalibrated, not as tuned Pry values.
//   They must not become release defaults: see licenses/THIRD-PARTY-NOTICES.md and
//   docs/phase2-runtime-contract.md §5.

namespace Pry.Core.Runtime;

/// <summary>
/// 间隔统计的形态参数。只有 <see cref="Window"/> 是 Pry 自己定的（上游 30 分钟收为 15 分钟）；
/// <see cref="BurstFloor"/> 与 <see cref="MinimumAverage"/> 与上游源码常量同值，按"沿用上游、未经标定"
/// 登记，须经 Eval 标定替换后才能作为发行默认值。
/// </summary>
public sealed record MessageIntervalOptions
{
    /// <summary>滑动窗口长度：只统计最近这段时间内的到达间隔。</summary>
    public TimeSpan Window { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>连发抑制：短于该时长的间隔属于同一阵输入，不代表真实发言节奏，不计入样本。</summary>
    public TimeSpan BurstFloor { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 均值下限。样本极少或全是连发时，真实均值会趋近于 0，把沉默秒数极快地折算成"等效消息"，
    /// 导致角色在被刷屏后立刻自我唤醒。施加下限即堵住这条放大通道。
    /// </summary>
    public TimeSpan MinimumAverage { get; init; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// 外部消息到达节奏。空窗补偿与压力分都需要"这个会话平时多久来一条"这个事实，
/// 没有它就只能用固定阈值，而固定阈值在活跃群聊与冷门私聊之间必然一头错。
/// 连发抖动只刷新"最后一次收到"的时间戳、不产生样本：一阵刷屏期间空闲时长必须归零，
/// 否则刷屏刚停就会被判成"已经空了很久"。
/// 非线程安全：由 ConversationRuntime 在其唯一锁内调用。
/// </summary>
public sealed class MessageIntervalStats
{
    private readonly TimeProvider _clock;
    private readonly MessageIntervalOptions _options;
    private readonly Queue<(DateTimeOffset At, TimeSpan Interval)> _samples = new();

    public MessageIntervalStats(TimeProvider clock, MessageIntervalOptions? options = null)
    {
        _clock = clock;
        _options = options ?? new MessageIntervalOptions();
    }

    /// <summary>最后一次来自角色之外的消息时间。从未收到时为 null。</summary>
    public DateTimeOffset? LastExternalAt { get; private set; }

    /// <summary>当前窗口内的样本条数，仅供诊断与测试读取。</summary>
    public int SampleCount => _samples.Count;

    public void Record(DateTimeOffset at, bool fromCharacter)
    {
        if (fromCharacter) return;

        var previous = LastExternalAt;
        LastExternalAt = at;
        if (previous is null) return;

        var interval = at - previous.Value;
        if (interval < _options.BurstFloor) return;

        _samples.Enqueue((at, interval));
        Prune(at);
    }

    /// <summary>
    /// 最近窗口内的平均到达间隔。从未见过外部消息时返回 null（没有节奏可言）；
    /// 见过但暂无可用样本时回退到下限值，让空窗补偿与延迟自唤醒不至于因缺样本而永久失效，
    /// 使待处理消息在没有新消息到来时挂死。
    /// </summary>
    public TimeSpan? AverageInterval(DateTimeOffset? at = null)
    {
        Prune(at ?? _clock.GetUtcNow());
        if (_samples.Count == 0) return LastExternalAt is null ? null : _options.MinimumAverage;

        double total = 0;
        foreach (var sample in _samples) total += sample.Interval.TotalSeconds;
        var average = TimeSpan.FromSeconds(total / _samples.Count);
        return average < _options.MinimumAverage ? _options.MinimumAverage : average;
    }

    /// <summary>自最后一次外部消息起已经沉默多久。从未收到过时返回 null。</summary>
    public TimeSpan? IdleFor(DateTimeOffset at) =>
        LastExternalAt is null ? null : Longest(TimeSpan.Zero, at - LastExternalAt.Value);

    /// <summary>沉默是否已经至少达到该会话平时的到达间隔，即"按节奏本来该来一条了"。</summary>
    public bool IdleReachedAverage(DateTimeOffset at)
    {
        var average = AverageInterval(at);
        var idle = IdleFor(at);
        return average is not null && idle is not null && idle.Value >= average.Value;
    }

    public void Clear()
    {
        _samples.Clear();
        LastExternalAt = null;
    }

    private void Prune(DateTimeOffset now)
    {
        var expireBefore = now - _options.Window;
        while (_samples.TryPeek(out var oldest) && oldest.At < expireBefore) _ = _samples.Dequeue();
    }

    private static TimeSpan Longest(TimeSpan a, TimeSpan b) => a >= b ? a : b;
}

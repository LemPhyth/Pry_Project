// Ported from Mai-with-u/MaiBot@fb4a8bd917d6fe373df9e3c22289d229e7f1b344:
//   src/maisaka/idle_backoff.py (IdleBackoffController), src/maisaka/mode_policy.py
// Type: ported (Python -> C#). Upstream is GPL-3.0-only; see
//   licenses/upstream/MaiBot_fb4a8bd917d6fe373df9e3c22289d229e7f1b344/LICENSE.
// Pry modifications: 2026-09-23 — 以显式布尔代替上游的字符串原因匹配；时钟由调用方传入。
//   退避的四个量级取自上游发行默认值（src/config/official_configs.py:645-709，2026-09-23 逐值复核）：
//   Pry 改了基数（15s→10s）、封顶（300s→180s）与起算次数（2→3），积压旁路条数与上游同为 6。
//   四项一律未经 Eval 标定，不得作为发行默认值（见 docs/phase2-runtime-contract.md §5）。
namespace Pry.Core.Runtime;

/// <summary>
/// 空闲退避参数。连续"看完没说话"的轮数越多，下一次检查推迟得越久。
/// 前三项是 Pry 在上游默认量级上的自调，<see cref="BypassPendingCount"/> 与上游同值；
/// 四项一律待 Eval 标定，不作为发行结论。
/// </summary>
public sealed record IdleBackoffOptions
{
    public TimeSpan Base { get; init; } = TimeSpan.FromSeconds(10);

    public TimeSpan Cap { get; init; } = TimeSpan.FromMinutes(3);

    /// <summary>从第几轮连续空闲开始真正退避（前几轮不罚，避免偶发一次沉默就哑掉）。</summary>
    public int StartCount { get; init; } = 3;

    /// <summary>待处理消息达到该条数时无条件立即解除退避：用户已经在等了，沉默不再有意义。</summary>
    public int BypassPendingCount { get; init; } = 6;
}

/// <summary>
/// 空闲退避。和打断退避是两回事：打断退避惩罚"角色刚要说话就被抢白"，
/// 空闲退避惩罚"角色连着好几轮看了半天什么都没接"。后者不压下去，
/// 一个安静的群会变成角色反复自问的循环，每次自问都烧掉一次模型调用。
/// 只作用于群聊——私聊里用户等的是回应，沉默累积不构成打扰。
/// </summary>
public sealed class IdleBackoffController(IdleBackoffOptions? options = null)
{
    private readonly IdleBackoffOptions _options = options ?? new IdleBackoffOptions();
    private int _count;
    private DateTimeOffset? _until;

    public int ConsecutiveIdleCycles => _count;

    /// <summary>当前是否仍处于退避窗口内；在窗口内时返回还需等待的时间。</summary>
    public TimeSpan? RemainingDelay(DateTimeOffset at) =>
        _until is { } until && until > at ? until - at : null;

    /// <summary>
    /// 按本轮结束的性质维护退避状态。真正的空闲轮次累加退避，
    /// 只要说过话（哪怕只发了一条）就立刻清零——恢复活跃对话后不该背着历史包袱。
    /// </summary>
    public void NoteCycleEnded(DateTimeOffset at, bool idleCycle, bool isGroupChat)
    {
        if (!idleCycle || !isGroupChat)
        {
            Reset();
            return;
        }

        _count++;
        var backoff = BackoffForCurrentCount();
        _until = backoff > TimeSpan.Zero ? at + backoff : null;
    }

    /// <summary>退避是否应当推迟这一轮。返回 null 表示不该推迟，并由调用方据此安排下次检查。</summary>
    public TimeSpan? ShouldDelay(DateTimeOffset at, int pendingCount, bool isGroupChat, bool focusActive)
    {
        // 焦点意味着用户此刻正盯着这段对话，退避的前提（"没人等着"）已经不成立。
        if (focusActive)
        {
            Reset();
            return null;
        }

        if (!isGroupChat) return null;
        var remaining = RemainingDelay(at);
        if (remaining is null)
        {
            if (_until is not null) _until = null;
            return null;
        }

        if (_options.BypassPendingCount > 0 && pendingCount >= _options.BypassPendingCount) return null;
        return remaining;
    }

    public void Reset()
    {
        _count = 0;
        _until = null;
    }

    private TimeSpan BackoffForCurrentCount()
    {
        if (_options.Base <= TimeSpan.Zero || _options.Cap <= TimeSpan.Zero) return TimeSpan.Zero;
        var start = Math.Max(1, _options.StartCount);
        if (_count < start) return TimeSpan.Zero;

        var exponent = Math.Min(30, _count - start);
        var seconds = _options.Base.TotalSeconds * Math.Pow(2, exponent);
        return TimeSpan.FromSeconds(Math.Min(_options.Cap.TotalSeconds, seconds));
    }
}

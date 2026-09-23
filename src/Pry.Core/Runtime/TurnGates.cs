// Ported from Mai-with-u/MaiBot@fb4a8bd917d6fe373df9e3c22289d229e7f1b344:
//   src/maisaka/turn_gates.py (ReplyNecessityTurnGate, FrequencyThresholdTurnGate)
//   src/maisaka/runtime.py (_get_message_trigger_threshold)
//   src/maisaka/mode_policy.py (reply trigger mode)
// Type: ported (Python -> C#). Upstream is GPL-3.0-only; see
//   licenses/upstream/MaiBot_fb4a8bd917d6fe373df9e3c22289d229e7f1b344/LICENSE.
// Pry modifications: 2026-09-23 — 门控不再持有 runtime 引用，改为接收显式输入快照（可单测、无副作用）。
//   本文件没有自带数值：阈值与延迟全部取自调用方的参数对象，而其中多数默认值与上游同值（未标定），
//   逐值对照见 docs/phase2-runtime-contract.md §5。折算式 ceil(1/f)/ceil(1/f²) 与"补偿封顶 threshold-1"
//   是机制而非调参，按源保留。
namespace Pry.Core.Runtime;

/// <summary>进入一轮之前要不要现在就进。Delay 表示"现在不够，但按节奏算什么时候够"。</summary>
public enum TurnGateDecision
{
    Trigger,
    Wait,
    Delay
}

public sealed record TurnGateResult(TurnGateDecision Decision, string Detail, TimeSpan? Delay = null,
    int PressureScore = 0)
{
    public bool ShouldTrigger => Decision == TurnGateDecision.Trigger;
}

/// <summary>
/// 触发模式。两种模式共用同一个阈值算法，区别只在"够不够"由谁判定：
/// 消息计数模式只看条数（含空窗补偿），回复必要性模式看分数。
/// </summary>
public enum ReplyTriggerMode
{
    MessageCount,
    ReplyNecessity
}

/// <summary>
/// 回复频率折算：频率越低，需要攒够越多的消息才开一轮。
/// 这是长期在线角色的基本约束——不然一个 5% 频率的角色会被每一条群消息叫醒，
/// 而它每次醒来都要花一次模型调用去决定闭嘴。
/// </summary>
public static class ReplyFrequency
{
    /// <summary>
    /// 折算阈值。两种模式下含义不同：计数模式下它是硬性门槛（攒够这么多条才进），
    /// 必要性模式下它只是压力分的分母（积压到该值才算"用户等着了"）。
    /// 必要性模式取平方倒数，因此阈值随频率下降得快得多——低频角色要靠积压或点名信号才抬得起分，
    /// 而不是靠沉默计时。频率为 0 表示静默，返回 0 条即"永不因攒消息而触发"。
    /// </summary>
    public static int Threshold(double effectiveFrequency, ReplyTriggerMode mode)
    {
        if (effectiveFrequency <= 0) return 0;
        var inverse = mode == ReplyTriggerMode.ReplyNecessity
            ? 1.0 / (effectiveFrequency * effectiveFrequency)
            : 1.0 / effectiveFrequency;
        return Math.Max(1, (int)Math.Ceiling(inverse));
    }
}

/// <summary>回复必要性门控：把评分结果换算成触发与否。</summary>
public sealed class ReplyNecessityTurnGate(ReplyNecessityOptions? options = null)
{
    private readonly ReplyNecessityOptions _options = options ?? new ReplyNecessityOptions();

    public ReplyNecessityOptions Options => _options;

    public TurnGateResult Evaluate(ReplyNecessitySnapshot snapshot)
    {
        var scored = ReplyNecessityScorer.Score(snapshot, _options);
        return new TurnGateResult(
            ReplyNecessityScorer.ShouldTrigger(scored, _options) ? TurnGateDecision.Trigger : TurnGateDecision.Wait,
            $"必要性: {scored.Detail}", PressureScore: scored.PressureScore);
    }
}

/// <summary>
/// 消息计数门控：条数够就进；不够时按"沉默了多久 ÷ 平时多久来一条"补齐等效条数。
/// 空窗补偿解决的问题是：低频角色如果只按条数等，冷门会话可以永远攒不满而让已到达的消息挂死。
/// </summary>
public sealed class FrequencyThresholdTurnGate
{
    public TurnGateResult Evaluate(FrequencyGateInputs inputs)
    {
        if (inputs.PendingCount >= inputs.TriggerThreshold)
            return new TurnGateResult(TurnGateDecision.Trigger,
                $"pending={inputs.PendingCount} 达到阈值={inputs.TriggerThreshold}");

        // 双保险之一：纯沉默一律不触发。折算封顶是另一道，两者同时守住"至少有一条真消息"。
        // 没有这条，低频角色会在无人发言的群里靠累积沉默反复自问自答。
        if (inputs.PendingCount < 1)
            return new TurnGateResult(TurnGateDecision.Wait, "pending=0，不允许纯沉默触发");

        var average = inputs.AverageInterval;
        if (average is null || average <= TimeSpan.Zero)
            return new TurnGateResult(TurnGateDecision.Wait, "平均消息间隔不可用，无法进行空窗补偿");

        var idle = inputs.IdleFor is { } measured && measured > TimeSpan.Zero ? measured : TimeSpan.Zero;
        // 双保险之二：即便空窗无限长，折算量也封顶在 threshold-1，纯沉默永远跨不过阈值。
        var credited = Math.Min(idle.TotalSeconds / average.Value.TotalSeconds,
            Math.Max(0.0, inputs.TriggerThreshold - 1));
        var equivalent = inputs.PendingCount + credited;

        var detail = $"平均间隔={average.Value.TotalSeconds:F2}s 空窗={idle.TotalSeconds:F2}s " +
            $"空窗折算={credited:F2} 等效消息数={equivalent:F2}/{inputs.TriggerThreshold}";
        if (equivalent >= inputs.TriggerThreshold)
            return new TurnGateResult(TurnGateDecision.Trigger, $"{detail} 判定=空窗补偿");

        // 按现有节奏算"还差几条 × 平均间隔"，再扣掉已经沉默的时间：让调用方睡到刚好够的那个点，
        // 而不是回到固定的轮询节拍上。
        var needed = Math.Max(0.0, (inputs.TriggerThreshold - inputs.PendingCount) * average.Value.TotalSeconds);
        return new TurnGateResult(TurnGateDecision.Delay, detail,
            TimeSpan.FromSeconds(Math.Max(0, needed - idle.TotalSeconds)));
    }
}

/// <summary>计数门控输入。全部是已测得的事实，门控本身不碰时钟。</summary>
public sealed record FrequencyGateInputs(int PendingCount, int TriggerThreshold, TimeSpan? AverageInterval,
    TimeSpan? IdleFor);

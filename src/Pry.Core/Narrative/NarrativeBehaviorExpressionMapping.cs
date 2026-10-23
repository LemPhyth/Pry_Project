namespace Pry.Core.Narrative;

/// <summary>
/// 表达档位：行为决定换形之后、表情／口头禅侧可使用的档位。
/// <see cref="NotDetermined"/> 是<b>显式缺省档</b>——工单 §一 第 4 行要求"行为决定本身缺失时不得默认放行"，
/// 所以缺决策时落这一档，绝不就近给一个中性档蒙混过关。
/// </summary>
public enum NarrativeExpressionGear
{
    /// <summary>缺省档：没有可依据的行为决定，档位未定，表达侧不得据此放行。</summary>
    NotDetermined = 0,
    /// <summary>收敛：严肃／戒备场景，玩笑式口头禅出局。</summary>
    Restrained = 1,
    /// <summary>中性。</summary>
    Neutral = 2,
    /// <summary>温暖。</summary>
    Warm = 3,
    /// <summary>俏皮。</summary>
    Playful = 4,
}

/// <summary>行为决定的语域信号（换形的输入侧）。它是"决定"的形状，不是文本；<see cref="Unspecified"/> 表示决策缺失。</summary>
public enum BehaviorRegisterSignal
{
    Unspecified = 0,
    Neutral = 1,
    Warm = 2,
    Playful = 3,
    Serious = 4,
    Guarded = 5,
}

/// <summary>
/// 一次"行为决定"的输入形状。<see cref="SceneBlockCodes"/> 由调用方传入（哪些场景把表达挡住），
/// 本类型不自造场景清单，也不设数字门槛。
/// </summary>
public sealed record BehaviorCue(
    BehaviorRegisterSignal RegisterSignal,
    IReadOnlyList<string> SceneBlockCodes);

/// <summary>换形结果。三档都可点名；只有 <see cref="Mapped"/> 才是放行。</summary>
public enum BehaviorMappingResult
{
    /// <summary>行为决定成功换形为表达档位。</summary>
    Mapped = 0,
    /// <summary>被场景挡：给出可点名的拒绝理由码（<see cref="BehaviorExpressionOutcome.BlockingSceneCode"/>）。</summary>
    BlockedByScene = 1,
    /// <summary>行为决定缺失：不给默认放行，落显式缺省档。</summary>
    NoBehaviorDecision = 2,
}

/// <summary>
/// 换形结果。<b>不产文本</b>——只带档位与可点名拒绝理由码，正文由别处（叙事绑定）负责。
/// </summary>
public sealed record BehaviorExpressionOutcome(
    BehaviorMappingResult Result,
    NarrativeExpressionGear Gear,
    string? BlockingSceneCode);

/// <summary>
/// 行为→表达换形（HDS-010 缺口 4 的等价格，隔离原型）。只做"行为决定 → 表达档位"的换形，不产文本：
/// 被场景挡时给出可点名的拒绝理由；行为决定本身缺失时<b>不得默认放行</b>，缺省档必须是显式枚举值
/// （<see cref="NarrativeExpressionGear.NotDetermined"/>）。不引用旧主链的决策装配或渲染件。
/// </summary>
public static class NarrativeBehaviorExpressionMapping
{
    /// <summary>
    /// 决策缺失优先于场景挡（没有决定可换形时谈不上"被谁挡"）；两者都不放行。
    /// null cue 与 <see cref="BehaviorRegisterSignal.Unspecified"/> 都算决策缺失。
    /// </summary>
    public static BehaviorExpressionOutcome Map(BehaviorCue? cue)
    {
        if (cue is null || cue.RegisterSignal == BehaviorRegisterSignal.Unspecified)
        {
            return new BehaviorExpressionOutcome(BehaviorMappingResult.NoBehaviorDecision,
                NarrativeExpressionGear.NotDetermined, null);
        }

        if (cue.SceneBlockCodes is { Count: > 0 })
        {
            var blocking = cue.SceneBlockCodes[0];
            return new BehaviorExpressionOutcome(BehaviorMappingResult.BlockedByScene,
                NarrativeExpressionGear.NotDetermined, blocking);
        }

        var gear = cue.RegisterSignal switch
        {
            BehaviorRegisterSignal.Warm => NarrativeExpressionGear.Warm,
            BehaviorRegisterSignal.Playful => NarrativeExpressionGear.Playful,
            BehaviorRegisterSignal.Neutral => NarrativeExpressionGear.Neutral,
            BehaviorRegisterSignal.Serious => NarrativeExpressionGear.Restrained,
            BehaviorRegisterSignal.Guarded => NarrativeExpressionGear.Restrained,
            // 越界值不得静默放行：回到显式缺省档，且不算 Mapped。
            _ => NarrativeExpressionGear.NotDetermined,
        };

        if (gear == NarrativeExpressionGear.NotDetermined)
        {
            return new BehaviorExpressionOutcome(BehaviorMappingResult.NoBehaviorDecision, gear, null);
        }

        return new BehaviorExpressionOutcome(BehaviorMappingResult.Mapped, gear, null);
    }
}

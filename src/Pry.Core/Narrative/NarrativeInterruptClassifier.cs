namespace Pry.Core.Narrative;

/// <summary>
/// 分类结论。三档都各有判据，<see cref="Undeterminable"/> 是独立一档：
/// 工单 §一 第 3 行明令"未知／不可判"不得折叠成"新话题"或"接着说"，
/// 因此任何给不出确定信号的输入都落这里，绝不就近并进两档之一。
/// </summary>
public enum InterruptClass
{
    /// <summary>接着说（上一轮话的延续／背书式回应）。</summary>
    Continuation = 0,
    /// <summary>新话题（换轨、新问句、硬打断）。</summary>
    NewTopic = 1,
    /// <summary>不可判——信号缺失或超出已定义集合。独立一档，不折叠。</summary>
    Undeterminable = 2,
}

/// <summary>
/// 送入分类的可点名信号。它是"换形"的输入侧：分类核把观测到的信号映射到 <see cref="InterruptClass"/>。
/// 由文本启发推导出该信号这一步不在本隔离原型里做——v4 让社交必要性／频率启发退出先行决策位置，
/// 文本→信号的检测属接线轮，登记为"押接线刀／未验证"，此处只钉分类核的枚举形状与折叠禁令。
/// </summary>
public enum InterruptCue
{
    /// <summary>没有任何可用信号（→ <see cref="InterruptClass.Undeterminable"/>）。</summary>
    NoSignal = 0,
    /// <summary>接话／省略式延续（→ <see cref="InterruptClass.Continuation"/>）。</summary>
    ContinuationMarker = 1,
    /// <summary>轻背书（嗯／哦，→ <see cref="InterruptClass.Continuation"/>）。</summary>
    AcknowledgementOnly = 2,
    /// <summary>换话题信号（→ <see cref="InterruptClass.NewTopic"/>）。</summary>
    TopicShiftSignal = 3,
    /// <summary>硬打断（等等／不对／先别，→ <see cref="InterruptClass.NewTopic"/>）。</summary>
    HardRedirection = 4,
}

/// <summary>
/// 打断分类器（HDS-010 缺口 3 的等价格，隔离原型）。把一条可点名信号换形成三档结论，
/// 三档互不相同且各有判据；未知／不可判独立成档、绝不折叠。纯内存、不产文本、不引用旧主链。
/// </summary>
public static class NarrativeInterruptClassifier
{
    /// <summary>把信号分类。null 信号是编程错误（构造期抛）；未定义的信号值一律落 <see cref="InterruptClass.Undeterminable"/>。</summary>
    public static InterruptClass Classify(InterruptCue? cue)
    {
        if (cue is null)
        {
            throw new NarrativeContractException("分类必须有可点名信号，缺失即不可判而非默认某一档。",
                NarrativeContractReason.PerceivedInputUnknown);
        }

        return cue.Value switch
        {
            InterruptCue.ContinuationMarker => InterruptClass.Continuation,
            InterruptCue.AcknowledgementOnly => InterruptClass.Continuation,
            InterruptCue.TopicShiftSignal => InterruptClass.NewTopic,
            InterruptCue.HardRedirection => InterruptClass.NewTopic,
            // 含 NoSignal 与任何越界值：不可判独立成档，绝不并进 Continuation/NewTopic。
            _ => InterruptClass.Undeterminable,
        };
    }
}

namespace Pry.Core.Narrative;

/// <summary>
/// 逐段节奏的输入。<b>全部走入参</b>：仓库里没有"按阅读节奏该多快"的权威值——旧链那套
/// <c>RuntimeSettings</c> 里的每字符阅读毫秒、气泡间隔上下限都是待退役的旧数字门槛，
/// 不得当作新链权威照搬（本刀也绝不在源码里写一个字面量常数顶替）。
/// 因此 <see cref="ReadingCharactersPerSecond"/> 登记为"待标定（押到标定轮）"并令其为必填入参。
/// </summary>
public sealed record ReplyPacingInput
{
    /// <summary>每秒按多少字符推进阅读节奏（待标定、必填、必须为正）。</summary>
    public required double ReadingCharactersPerSecond { get; init; }

    /// <summary>两段之间最小间隔（必填、非负）。防止逐段以零间隔刷屏冒泡。</summary>
    public required TimeSpan MinSegmentGap { get; init; }

    /// <summary>两段之间最大间隔（必填、不小于 <see cref="MinSegmentGap"/>）。封顶长段的等待。</summary>
    public required TimeSpan MaxSegmentGap { get; init; }

    public ReplyPacingInput Validate()
    {
        if (!(ReadingCharactersPerSecond > 0) || double.IsNaN(ReadingCharactersPerSecond)
            || double.IsInfinity(ReadingCharactersPerSecond))
        {
            throw new NarrativeContractException(
                "阅读速度必须由入参给定为正数（无权威值则登记待标定，本类型不自造常数）。",
                NarrativeContractReason.ActionSegmentCountInvalid);
        }

        if (MinSegmentGap < TimeSpan.Zero)
        {
            throw new NarrativeContractException("最小段间隔不能为负。",
                NarrativeContractReason.ActionSegmentCountInvalid);
        }

        if (MaxSegmentGap < MinSegmentGap)
        {
            throw new NarrativeContractException("最大段间隔不能小于最小段间隔。",
                NarrativeContractReason.ActionSegmentCountInvalid);
        }

        return this;
    }
}

/// <summary>
/// 逐段阅读节奏（HDS-010 缺口 5 的等价格，隔离原型）。节奏只决定<b>下一段何时可发</b>，不决定内容：
/// 输入只有各段字符数与入参速度，输出只有各段可发时刻；每段可发时刻单调不减。
/// 速度、上下限一律从入参取（无权威值→待标定）；不引用旧主链投递装配、不落库。
/// </summary>
public static class NarrativeReplyPacing
{
    /// <summary>
    /// 规划各段可发时刻。首段落在 <paramref name="startAt"/>；此后每段在前一段可发时刻之上
    /// 叠加"上一段长度／阅读速度"（夹到最小／最大间隔）所得的间隔。间隔非负 ⇒ 结果单调不减。
    /// 内容与文本一律不参与：等长不同文 → 同一时刻表。
    /// </summary>
    public static IReadOnlyList<DateTimeOffset> PlanSendTimes(
        DateTimeOffset startAt,
        IReadOnlyList<int> segmentCharacterCounts,
        ReplyPacingInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        input.Validate();
        ArgumentNullException.ThrowIfNull(segmentCharacterCounts);

        var times = new List<DateTimeOffset>(segmentCharacterCounts.Count);
        if (segmentCharacterCounts.Count == 0)
        {
            return times;
        }

        for (var i = 0; i < segmentCharacterCounts.Count; i++)
        {
            var count = segmentCharacterCounts[i];
            if (count < 0)
            {
                throw new NarrativeContractException("段字符数不能为负。",
                    NarrativeContractReason.ActionSegmentCountInvalid);
            }

            if (i == 0)
            {
                times.Add(startAt);
                continue;
            }

            // 第 i 段的等待由第 i-1 段的长度决定：读得完才轮到下一段。
            var rawGap = TimeSpan.FromSeconds(segmentCharacterCounts[i - 1] / input.ReadingCharactersPerSecond);
            var gap = Clamp(rawGap, input.MinSegmentGap, input.MaxSegmentGap);
            if (gap < TimeSpan.Zero)
            {
                gap = TimeSpan.Zero; // 双保险：间隔非负，单调性由此保证。
            }

            times.Add(times[i - 1] + gap);
        }

        return times;
    }

    private static TimeSpan Clamp(TimeSpan value, TimeSpan min, TimeSpan max) =>
        value < min ? min : value > max ? max : value;
}

namespace Pry.Core.Narrative;

/// <summary>
/// 打断的两种可点名理由。工单 §一 第 2 行要求"打断"与"新输入顶替"是两种可点名理由，
/// 这里显式拆成两档；调用方说不出属于哪一档时按 <see cref="InterruptionApplyResult.RejectedUnnamedReason"/> 拒。
/// </summary>
public enum InterruptionReasonKind
{
    /// <summary>用户插话——她正在打字／正在发段时先停。</summary>
    UserInterruption = 0,
    /// <summary>新输入顶替——旧段被后到的内容取代。</summary>
    SupersededByNewInput = 1,
}

/// <summary>一次打断裁决的收场。<see cref="Applied"/> 之外的两档都是可点名的拒绝。</summary>
public enum InterruptionApplyResult
{
    /// <summary>理由可点名，作废了其中尚未送达的段。</summary>
    Applied = 0,
    /// <summary>给不出"打断／顶替"这一档理由——不猜，直接拒。</summary>
    RejectedUnnamedReason = 1,
    /// <summary>没有任何可处理的段——空操作不当作成功送达的取消。</summary>
    RejectedNothingToProcess = 2,
}

/// <summary>
/// 一条可打断段的最小形状。它复用既有 <see cref="DeliverySegmentState"/>（只读、未改一字），
/// 好让"已送达不可撤回"这条与 <c>DeliveryLedger</c> 的终态语义天然对齐：Delivered／Cancelled 都是终态。
/// </summary>
public sealed record InterruptibleSegment(
    string CommitId,
    string ActionId,
    int SegmentIndex,
    DeliverySegmentState State)
{
    public bool IsDelivered => State == DeliverySegmentState.Delivered;

    public bool IsCancellable => State == DeliverySegmentState.Pending;
}

/// <summary>
/// 打断裁决结果：作废了哪些段、按终态语义留手了哪些段、拒绝原因各可点名。
/// <see cref="Withheld"/> 里的段状态一字未动——真实发出去的话不能靠改账本收回。
/// </summary>
public sealed record InterruptionOutcome(
    InterruptionApplyResult Result,
    InterruptionReasonKind? Reason,
    IReadOnlyList<InterruptibleSegment> Cancelled,
    IReadOnlyList<InterruptibleSegment> Withheld);

/// <summary>
/// 打断控制（HDS-010 缺口 2 的等价格，隔离原型）。用户正在打字／正在发段时她先停：
/// 打断请求只作废<b>尚未送达</b>（Pending）的段；已送达（Delivered）段不可撤回，与
/// <c>DeliveryLedger</c> 既有终态语义一致（本类型不改它一字、也不构造它）。
/// "打断"与"新输入顶替"必须是两种可点名理由。<b>不产文本、不落库、不引用旧主链。</b>
/// </summary>
public static class NarrativeInterruption
{
    /// <summary>
    /// 对一批段施加一次打断。纯函数：不修改入参段的 <see cref="State"/>，只给出分类结果——
    /// 是否真正落终态由既有投递账决定，本原型只复刻"哪些段可被这次打断作废"的判定。
    /// </summary>
    public static InterruptionOutcome Apply(IReadOnlyList<InterruptibleSegment> segments, InterruptionReasonKind? reason)
    {
        ArgumentNullException.ThrowIfNull(segments);

        if (reason is not (InterruptionReasonKind.UserInterruption or InterruptionReasonKind.SupersededByNewInput))
        {
            return new InterruptionOutcome(InterruptionApplyResult.RejectedUnnamedReason, reason, [], []);
        }

        if (segments.Count == 0)
        {
            return new InterruptionOutcome(InterruptionApplyResult.RejectedNothingToProcess, reason, [], []);
        }

        var cancelled = new List<InterruptibleSegment>();
        var withheld = new List<InterruptibleSegment>();
        foreach (var segment in segments)
        {
            // 只有尚未送达的段可作废；已送达、已取消、未知一律留手，绝不翻转终态。
            if (segment.IsCancellable)
            {
                cancelled.Add(segment);
            }
            else
            {
                withheld.Add(segment);
            }
        }

        return new InterruptionOutcome(InterruptionApplyResult.Applied, reason, cancelled, withheld);
    }
}

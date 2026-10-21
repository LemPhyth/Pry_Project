namespace Pry.Core.Narrative;

/// <summary>投递账本一次操作的结果。被拒时 <see cref="Segment"/> 是账本里的既有真相（可能为 null），
/// 绝不是这次没写进去的那一份。</summary>
public sealed record SegmentOutcome(bool Accepted, DeliverySegment? Segment, NarrativeContractReason Reason, string? Detail)
{
    internal static SegmentOutcome Ok(DeliverySegment segment) => new(true, segment, NarrativeContractReason.None, null);

    internal static SegmentOutcome No(NarrativeContractReason reason, string detail, DeliverySegment? current = null) =>
        new(false, current, reason, detail);
}

/// <summary>已生效提交在投递侧需要的最小投影：原文、动作与其绑定。没有它就无法判断"这段文本从哪来"。</summary>
internal sealed record RegisteredCommit(
    CharacterInstanceId InstanceId,
    string CommitId,
    string ConversationId,
    string NarrativeProse,
    IReadOnlyDictionary<string, CommunicationAction> Actions,
    long OwnershipEpoch,
    int MaxSegmentsPerAction);

/// <summary>
/// 投递段账本（HDS-002）。它守的是"实际送达的证据"这一条线：
/// ① 唯一键 <c>(commitId, actionId, segmentIndex)</c> —— 同键第二次写即
/// <see cref="NarrativeContractReason.SegmentKeyDuplicate"/>，不存在"后写覆盖前写"；
/// ② 段的正文只能逐字来自该动作绑定的叙事切片，最后一段封口时必须还原整段绑定原文，
/// 任何一段来自第二次措辞即 <see cref="NarrativeContractReason.SegmentTextNotFromNarrative"/>；
/// ③ Delivered / Cancelled 是终态，迟到的结果翻转不了它；
/// ④ 前一段没落地，后一段不许先落。
/// 真实持久化由 HDS-005 落表；这里的判定形状与条件写一一对应。
/// </summary>
public sealed class DeliveryLedger
{
    private readonly OutboxOwnershipRegistry _ownership;
    private readonly Dictionary<string, RegisteredCommit> _commits = [];
    private readonly Dictionary<DeliverySegmentKey, DeliverySegment> _segments = [];
    private readonly Dictionary<(string CommitId, string ActionId), List<int>> _indexes = [];

    public DeliveryLedger(OutboxOwnershipRegistry ownership)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        _ownership = ownership;
    }

    public IReadOnlyList<DeliverySegment> Snapshot() => _segments.Values.ToArray();

    public DeliverySegment? Find(DeliverySegmentKey key) => _segments.TryGetValue(key, out var s) ? s : null;

    /// <summary>只有条件写成功的提交才有资格产生投递段。</summary>
    public void RegisterAppliedCommit(NarrativeCommit commit, int maxSegmentsPerAction)
    {
        ArgumentNullException.ThrowIfNull(commit);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxSegmentsPerAction, 1);
        var actions = new Dictionary<string, CommunicationAction>(StringComparer.Ordinal);
        foreach (var action in commit.Actions)
        {
            actions[action.ActionId] = action;
        }

        _commits[commit.CommitId] = new RegisteredCommit(commit.InstanceId, commit.CommitId, commit.ConversationId,
            commit.NarrativeProse, actions, commit.OwnershipEpoch, maxSegmentsPerAction);
    }

    /// <summary>追加一个待发段。唯一键冲突在这里被拒。</summary>
    public SegmentOutcome TryAppend(DeliverySegment segment, string writerId)
    {
        ArgumentNullException.ThrowIfNull(segment);
        var key = segment.Key;

        if (!_ownership.IsAuthorizedWriter(segment.InstanceId, writerId, segment.OwnershipEpoch,
                out var ownershipReason, out _))
        {
            return SegmentOutcome.No(ownershipReason,
                $"写者 {writerId} 在实例 {segment.InstanceId.Value} 上没有当前代次的票"
                + $"（带来的是 {segment.OwnershipEpoch}），无权追加投递段。");
        }

        if (!_commits.TryGetValue(key.CommitId, out var commit))
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentCommitUnknown,
                $"提交 {key.CommitId} 没有生效过，不能产生投递段。");
        }

        if (!string.Equals(commit.ConversationId, segment.ConversationId, StringComparison.Ordinal))
        {
            return SegmentOutcome.No(NarrativeContractReason.ScopeEscalation,
                "投递段落在提交作用域之外的会话上。");
        }

        if (!commit.Actions.TryGetValue(key.ActionId, out var action))
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentActionUnknown,
                $"提交 {key.CommitId} 里没有动作 {key.ActionId}。");
        }

        if (action.Mode != CommunicationMode.Immediate)
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentCountExceeded,
                $"动作 {key.ActionId} 是 {action.Mode}，不产生投递段。");
        }

        if (_segments.TryGetValue(key, out var existing))
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentKeyDuplicate,
                $"({key.CommitId}, {key.ActionId}, {key.SegmentIndex}) 已经在账本里，状态是 {existing.State}。"
                + "同键二次写入如果不撞墙，就会静默覆盖已发生的投递证据。", existing);
        }

        if (key.SegmentIndex < 0)
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentIndexNotContiguous, "段序号不能为负。");
        }

        var ceiling = Math.Min(action.PlannedSegmentCount, commit.MaxSegmentsPerAction);
        if (key.SegmentIndex >= ceiling)
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentCountExceeded,
                $"动作 {key.ActionId} 计划 {action.PlannedSegmentCount} 段、宿主上限 {commit.MaxSegmentsPerAction}，"
                + $"第 {key.SegmentIndex} 段不在范围内。");
        }

        var sequence = ActionSequence(key.CommitId, key.ActionId);
        if (key.SegmentIndex != sequence.Count)
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentIndexNotContiguous,
                $"动作 {key.ActionId} 已有 {sequence.Count} 段，第 {key.SegmentIndex} 段跳号了。");
        }

        if (string.IsNullOrEmpty(segment.Text) && string.IsNullOrEmpty(segment.StickerId))
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentStickerPayloadInvalid,
                "一段既没有正文也没有贴纸标识，无法作为送达证据。");
        }

        var binding = action.Binding;
        if (binding is null || !binding.Bound)
        {
            return SegmentOutcome.No(binding?.Failure ?? NarrativeContractReason.ActionBindingMissing,
                $"动作 {key.ActionId} 没有可用绑定，投递段无从取得正文。");
        }

        var boundSlice = commit.NarrativeProse.Substring(binding.ResolvedOffset!.Value, binding.QuotedText.Length);
        var accumulated = string.Concat(sequence.Select(s => s.Text)) + segment.Text;
        if (!boundSlice.StartsWith(accumulated, StringComparison.Ordinal))
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentTextNotFromNarrative,
                $"第 {key.SegmentIndex} 段的正文拼回绑定原文时对不上：投递文本必须逐字来自叙事切片。");
        }

        if (sequence.Count + 1 == action.PlannedSegmentCount && accumulated.Length != boundSlice.Length)
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentTextNotFromNarrative,
                "最后一段封口后必须逐字还原绑定原文，缺字即视为第二次改写。");
        }

        var pending = segment with { State = DeliverySegmentState.Pending, DeliveredMessageId = null };
        _segments[key] = pending;
        if (!_indexes.TryGetValue((key.CommitId, key.ActionId), out var indexes))
        {
            indexes = [];
            _indexes[(key.CommitId, key.ActionId)] = indexes;
        }

        indexes.Add(key.SegmentIndex);
        return SegmentOutcome.Ok(pending);
    }

    /// <summary>Pending → Delivered。前一段没落地、或这段已是终态，都被拒；
    /// <c>deliveredMessageId</c> 是"实际进入聊天历史"的证据，缺失即不承认为已送达。</summary>
    public SegmentOutcome TryMarkDelivered(DeliverySegmentKey key, string? deliveredMessageId, long ownershipEpoch,
        string writerId)
    {
        var gate = PreTransition(key, ownershipEpoch, writerId, out var current, out var instanceId);
        if (gate is not null)
        {
            return gate;
        }

        if (string.IsNullOrWhiteSpace(deliveredMessageId))
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentDeliveryEvidenceMissing,
                "没有聊天历史标识就不能把一段标成已送达：投递证据必须由真实落库支撑。", current);
        }

        var sequence = ActionSequence(key.CommitId, key.ActionId);
        var predecessor = sequence.FirstOrDefault(s => s.SegmentIndex == key.SegmentIndex - 1);
        if (predecessor is not null && predecessor.State != DeliverySegmentState.Delivered)
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentPredecessorPending,
                $"第 {key.SegmentIndex} 段之前还有第 {predecessor.SegmentIndex} 段是 {predecessor.State}："
                + "投递顺序不能被打乱。", current);
        }

        var delivered = current! with
        {
            State = DeliverySegmentState.Delivered,
            DeliveredMessageId = deliveredMessageId,
            CancelReason = null,
        };
        _segments[key] = delivered;
        return SegmentOutcome.Ok(delivered);
    }

    /// <summary>Pending → Cancelled。已送达的段落不可撤：真实发出去的话不能靠改账本收回。</summary>
    public SegmentOutcome TryMarkCancelled(DeliverySegmentKey key, string cancelReason, long ownershipEpoch, string writerId)
    {
        var gate = PreTransition(key, ownershipEpoch, writerId, out var current, out _);
        if (gate is not null)
        {
            return gate;
        }

        if (string.IsNullOrWhiteSpace(cancelReason))
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentTerminalImmutable,
                "取消必须写清原因，否则恢复时无从判断这一段为什么没发。", current);
        }

        var cancelled = current! with { State = DeliverySegmentState.Cancelled, CancelReason = cancelReason };
        _segments[key] = cancelled;
        return SegmentOutcome.Ok(cancelled);
    }

    private SegmentOutcome? PreTransition(DeliverySegmentKey key, long ownershipEpoch, string writerId,
        out DeliverySegment? current, out CharacterInstanceId instanceId)
    {
        current = null;
        instanceId = null!;
        if (!_commits.TryGetValue(key.CommitId, out var commit))
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentCommitUnknown,
                $"提交 {key.CommitId} 不在账本里，状态转换无所附属。");
        }

        instanceId = commit.InstanceId;
        if (!_ownership.IsAuthorizedWriter(instanceId, writerId, ownershipEpoch, out var ownershipReason, out _))
        {
            return SegmentOutcome.No(ownershipReason,
                $"写者 {writerId} 的代次 {ownershipEpoch} 已不具备写终态的资格。");
        }

        if (!_segments.TryGetValue(key, out current))
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentNotFound,
                $"账本里没有 ({key.CommitId}, {key.ActionId}, {key.SegmentIndex}) 这一段。");
        }

        if (current.IsTerminal)
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentTerminalImmutable,
                $"这一段已经是 {current.State} 终态，迟到结果不得翻转它。", current);
        }

        return null;
    }

    private List<DeliverySegment> ActionSequence(string commitId, string actionId) =>
        _indexes.TryGetValue((commitId, actionId), out var indexes)
            ? [.. indexes.Order().Select(i => _segments[new DeliverySegmentKey(commitId, actionId, i)])]
            : [];
}

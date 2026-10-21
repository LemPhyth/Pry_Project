namespace Pry.Core.Narrative;

/// <summary>实例的叙事侧持久状态投影：条件写要比较的就是这几项。</summary>
public sealed record NarrativeInstanceState(
    CharacterInstanceId InstanceId,
    long StoryVersion,
    ProcessedThroughSeq ProcessedThrough,
    long OwnershipEpoch,
    string? LastCommitId,
    int AppliedCommitCount,
    DateTimeOffset UpdatedAtUtc)
{
    /// <summary>把实例状态整体退回某个更早的水位：这条路径在本契约里不存在，
    /// 记录它是为了让"恢复/回退"必须走 HDS-001 的显式恢复而不是就地改写。</summary>
    public bool WatermarkRegressedTo(ProcessedThroughSeq candidate) => candidate.Seq < ProcessedThrough.Seq;
}

/// <summary>条件写的结果。被拒时 <see cref="State"/> 是账上未被改动的既有状态，供调用方核对"确实没覆盖"。</summary>
public sealed record ConditionalCommitOutcome(
    bool Accepted,
    NarrativeCommit? Applied,
    NarrativeInstanceState? State,
    NarrativeContractReason Reason,
    string? Detail)
{
    internal static ConditionalCommitOutcome No(NarrativeContractReason reason, string detail, NarrativeInstanceState? state)
        => new(false, null, state, reason, detail);

    internal static ConditionalCommitOutcome Yes(NarrativeCommit commit, NarrativeInstanceState state)
        => new(true, commit, state, NarrativeContractReason.None, null);
}

/// <summary>
/// 叙事提交的条件写入口（HDS-002）。
/// 一句话形状：<b>写者带来的身份、所有权代次、故事版本与消费水位必须同时等于账上的那一份</b>，
/// 少一项或多一项都拒，且拒绝路径上一条字节都不落。
/// 迟到提交（进程被接管后旧写者才回来）因此不可能"顺手覆盖新真相"。
/// </summary>
public sealed class NarrativeCommitWriter
{
    private readonly OutboxOwnershipRegistry _ownership;
    private readonly CharacterInstanceRegistry _instances;
    private readonly Dictionary<CharacterInstanceId, NarrativeInstanceState> _states = [];
    private readonly Dictionary<string, NarrativeCommit> _applied = [];
    private readonly Dictionary<string, long> _conversationDeletionEpoch = new(StringComparer.Ordinal);

    public NarrativeCommitWriter(OutboxOwnershipRegistry ownership, CharacterInstanceRegistry instances)
    {
        ArgumentNullException.ThrowIfNull(ownership);
        ArgumentNullException.ThrowIfNull(instances);
        _ownership = ownership;
        _instances = instances;
        Ledger = new DeliveryLedger(ownership);
    }

    public DeliveryLedger Ledger { get; }

    public NarrativeInstanceState? StateOf(CharacterInstanceId instanceId) =>
        _states.TryGetValue(instanceId, out var state) ? state : null;

    public NarrativeCommit? FindAppliedCommit(string commitId) =>
        _applied.TryGetValue(commitId, out var commit) ? commit : null;

    /// <summary>会话删除/清空：提升删除 epoch 并记下时间戳，之后所有较低 epoch 的提交永久无资格。</summary>
    public void InvalidateConversation(string conversationId, long deletionEpoch)
    {
        NarrativeGuard.RequireText(conversationId, nameof(conversationId), NarrativeContractReason.ConversationInvalidated);
        ArgumentOutOfRangeException.ThrowIfNegative(deletionEpoch);
        _conversationDeletionEpoch.TryGetValue(conversationId, out var current);
        _conversationDeletionEpoch[conversationId] = Math.Max(current, deletionEpoch);
    }

    public long DeletionEpochOf(string conversationId) =>
        _conversationDeletionEpoch.TryGetValue(conversationId, out var epoch) ? epoch : 0;

    /// <summary>
    /// 事务 A 的条件写：叙事原文、状态引用、候选意图、准确的输入处理范围与待发动作在同一次判定里成立或整体不成立。
    /// 判定顺序固定，因为"谁先拒"决定了运维看到的病因；所有权与版本一定在内容校验之前。
    /// </summary>
    public ConditionalCommitOutcome TryCommit(NarrativeRequest request, NarrativeCommit commit)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(commit);

        if (_instances.Registrations.All(r => !r.InstanceId.Equals(commit.InstanceId)))
        {
            return ConditionalCommitOutcome.No(NarrativeContractReason.InstanceUnknown,
                $"实例 {commit.InstanceId.Value} 没有在稳定身份登记表里，提交无处归属。", null);
        }

        if (!_ownership.IsAuthorizedWriter(commit.InstanceId, commit.CommittedByWriter, commit.OwnershipEpoch,
                out var ownershipReason, out var ticket))
        {
            return ConditionalCommitOutcome.No(ownershipReason,
                ownershipReason == NarrativeContractReason.OwnershipFenced
                    ? $"写者 {commit.CommittedByWriter} 带来的代次 {commit.OwnershipEpoch} 落后于当前 "
                      + $"{_ownership.CurrentEpoch(commit.InstanceId)}：所有权已交接，迟到提交只能被拒。"
                    : $"写者 {commit.CommittedByWriter} 不持有实例 {commit.InstanceId.Value} 的写入权。",
                StateOf(commit.InstanceId));
        }

        if (!NarrativeContractVersions.IsSupportedNarrativeVersion(commit.ContractVersion))
        {
            return ConditionalCommitOutcome.No(NarrativeContractReason.ContractVersionUnsupported,
                $"提交携带契约版本 {commit.ContractVersion}，本波只认 "
                + $"{NarrativeContractVersions.NarrativeDocument}。", StateOf(commit.InstanceId));
        }

        if (string.IsNullOrWhiteSpace(commit.ContentHash))
        {
            return ConditionalCommitOutcome.No(NarrativeContractReason.ContentHashMissing,
                "没有内容散列的提交在恢复时无法自证没被改写，一律拒绝。", StateOf(commit.InstanceId));
        }

        if (DeletionEpochOf(commit.ConversationId) > commit.DeletionEpoch)
        {
            return ConditionalCommitOutcome.No(NarrativeContractReason.DeletionEpochAdvanced,
                $"会话 {commit.ConversationId} 的删除 epoch 已提升到 {DeletionEpochOf(commit.ConversationId)}，"
                + $"这条提交停留在 {commit.DeletionEpoch}：迟到结果不得复活已删除的作用域。",
                StateOf(commit.InstanceId));
        }

        if (_applied.TryGetValue(commit.CommitId, out var previous))
        {
            // 幂等重放只允许"完全同一份内容"；内容不同就是第二条提交冒用了 ID。
            return string.Equals(previous.ContentHash, commit.ContentHash, StringComparison.Ordinal)
                ? ConditionalCommitOutcome.No(NarrativeContractReason.CommitAlreadyApplied,
                    "这条提交已经生效过，重放不再推进水位。", StateOf(commit.InstanceId))
                : ConditionalCommitOutcome.No(NarrativeContractReason.DuplicateCommitId,
                    $"提交 {commit.CommitId} 的 ID 被复用到不同内容上。", StateOf(commit.InstanceId));
        }

        var state = _states.TryGetValue(commit.InstanceId, out var existing)
            ? existing
            : new NarrativeInstanceState(commit.InstanceId, request.StoryVersion, request.ProcessedThroughAtIssue,
                commit.OwnershipEpoch, null, 0, request.IssuedAtUtc);

        if (commit.ExpectedStoryVersion != state.StoryVersion)
        {
            return ConditionalCommitOutcome.No(
                commit.ExpectedStoryVersion < state.StoryVersion
                    ? NarrativeContractReason.LateCommitRejected
                    : NarrativeContractReason.StoryVersionConflict,
                $"提交建立在故事版本 {commit.ExpectedStoryVersion} 上，而账上已经是 {state.StoryVersion}。"
                + (commit.ExpectedStoryVersion < state.StoryVersion ? "这是迟到提交。" : "这是版本超前，同样不落。"),
                state);
        }

        if (!commit.ProcessedThrough.AdvancesPast(state.ProcessedThrough))
        {
            return ConditionalCommitOutcome.No(NarrativeContractReason.WatermarkNotAdvancing,
                $"消费水位停留在 {state.ProcessedThrough.Seq}，提交声称 {commit.ProcessedThrough.Seq}："
                + "水位不前进的提交等于重复消费。", state);
        }

        if (!commit.ProcessedThrough.WithinBatch(request.BatchHighestSeq))
        {
            return ConditionalCommitOutcome.No(NarrativeContractReason.ProcessedBeyondBatch,
                $"提交把水位报到 {commit.ProcessedThrough.Seq}，而宿主签发的批次上界是 {request.BatchHighestSeq}。",
                state);
        }

        foreach (var seq in commit.ConsumedInputSeqs)
        {
            if (!request.InputSeqs.Contains(seq))
            {
                return ConditionalCommitOutcome.No(NarrativeContractReason.ProcessedInputUnreceived,
                    $"提交声称消费了序号 {seq}，宿主在这一批次里没有收到过它。", state);
            }
        }

        var bound = commit with
        {
            Actions = [.. commit.Actions.Select(a =>
                a with { Binding = a.Binding ?? NarrativeProseBinder.Bind(commit.NarrativeProse, a.ProseQuote) })],
        };

        var rejections = NarrativeCommitValidator.Validate(bound, request);
        if (rejections.Count > 0)
        {
            return ConditionalCommitOutcome.No(rejections[0].Reason,
                string.Join("；", rejections.Select(r => r.Detail)), state);
        }

        var applied = state with
        {
            StoryVersion = state.StoryVersion + 1,
            ProcessedThrough = commit.ProcessedThrough,
            OwnershipEpoch = ticket!.OwnershipEpoch,
            LastCommitId = commit.CommitId,
            AppliedCommitCount = state.AppliedCommitCount + 1,
            UpdatedAtUtc = commit.CommittedAtUtc,
        };
        _states[commit.InstanceId] = applied;
        _applied[commit.CommitId] = bound;
        Ledger.RegisterAppliedCommit(bound, request.MaxOutboundSegments);
        return ConditionalCommitOutcome.Yes(bound, applied);
    }
}

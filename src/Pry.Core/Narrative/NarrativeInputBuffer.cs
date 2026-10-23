namespace Pry.Core.Narrative;

/// <summary>
/// 攒批的封顶条件。全部由调用方传入，本类型自身不含任何数字门槛——
/// 仓库里既没有"一轮最多攒几条"也没有"最多攒多久"的权威值（旧链把这两个数藏在
/// <c>RuntimeSettings.DebounceMs</c>／<c>MaxBufferMs</c> 里，那是待退役的旧假设，不得当作新链权威），
/// 故两者都是必填入参，实际取值登记为"待标定（押到标定轮）"。
/// </summary>
public sealed record InputBufferCeiling
{
    /// <summary>条数封顶：一批攒到这么多条即被顶掉封批。必须为正，由入参给定。</summary>
    public required int MaxBatchItems { get; init; }

    /// <summary>时限封顶：自批次开启经过此时长即到期封批。必须为正，由入参给定。</summary>
    public required TimeSpan MaxBatchHold { get; init; }

    public InputBufferCeiling Validate()
    {
        if (MaxBatchItems < 1)
        {
            throw new NarrativeContractException("条数封顶必须是正整数（由入参传入，本类型不自造默认值）。",
                NarrativeContractReason.ActionSegmentCountInvalid);
        }

        if (MaxBatchHold <= TimeSpan.Zero)
        {
            throw new NarrativeContractException("时限封顶必须是正时长（由入参传入，本类型不自造默认值）。",
                NarrativeContractReason.ActionSegmentCountInvalid);
        }

        return this;
    }
}

/// <summary>批次如何收场。<see cref="Open"/> 之外的三档都可点名回放，与旧链"buffer_deadline/forced"同形但独立成词汇。</summary>
public enum InputBufferCloseReason
{
    /// <summary>仍在攒，尚未封批。</summary>
    Open = 0,
    /// <summary>条数封顶把这一批顶掉。</summary>
    DisplacedByItemCount = 1,
    /// <summary>时限到期自然封批。</summary>
    HoldElapsed = 2,
    /// <summary>调用方显式封批（强制立即）。</summary>
    ForcedByCaller = 3,
}

/// <summary>一次 <see cref="NarrativeInputBuffer.Append"/> 的判定结果。</summary>
public enum InputBufferAppendDecision
{
    /// <summary>照到达次序并入当前批次，批次仍开着。</summary>
    Appended = 0,
    /// <summary>并入后正好触达条数封顶，这一批当场封批。</summary>
    AppendedAndClosedByItemCount = 1,
    /// <summary>重复序号：同一序号不能第二次入批。</summary>
    RejectedDuplicateSeq = 2,
    /// <summary>乱序：宿主签发的序号必须严格递增（到达次序）。</summary>
    RejectedOutOfOrder = 3,
}

/// <summary>
/// 一封好的人话批次。<see cref="BatchId"/> 在批次开启那一刻就定死，封批只读取、不改写，
/// 因而"到期或被顶掉时批次标识稳定可回放"；<see cref="OrderedSeqs"/> 记录到达次序。
/// 每封好的批次自带一枚唯一可发送真相令牌（见 <see cref="NarrativeInputBuffer.TryTakeSendableTruth"/>）。
/// </summary>
public sealed record NarratedInputBatch(
    string ConversationId,
    string BatchId,
    IReadOnlyList<long> OrderedSeqs,
    InputBufferCloseReason CloseReason);

/// <summary>
/// 输入攒批（HDS-010 缺口 1 的等价格，隔离原型）。它复刻旧链"连着发几条、攒成一轮再答"的用户可见语义，
/// 但只依赖内存对象：同一会话的连续输入按到达次序归入<b>一枚</b>批次；封顶条件（条数／时限）全部走入参；
/// 到期或被顶掉时批次标识稳定可回放；同一批次不产生两条可发送真相。
/// 不接生产、不落库、不引用旧主链任何装配或评分件。
/// </summary>
public sealed class NarrativeInputBuffer
{
    private readonly string _conversationId;
    private readonly InputBufferCeiling _ceiling;
    private readonly Dictionary<string, NarratedInputBatch> _sealed = [];
    private readonly HashSet<string> _claimedSendable = [];
    private List<long>? _currentSeqs;
    private string? _currentBatchId;
    private long _lastSeq;
    private DateTimeOffset _openedAt;

    public NarrativeInputBuffer(string conversationId, InputBufferCeiling ceiling)
    {
        NarrativeGuard.RequireText(conversationId, nameof(conversationId), NarrativeContractReason.IdentityValueBlank);
        _conversationId = conversationId;
        _ceiling = ceiling ?? throw new NarrativeContractException("封顶条件必须由入参传入。",
            NarrativeContractReason.ActionSegmentCountInvalid);
        _ceiling.Validate();
    }

    /// <summary>当前是否有开着的批次。</summary>
    public bool HasOpenBatch => _currentSeqs is not null;

    /// <summary>
    /// 按到达次序并入一条输入。序号必须由宿主严格递增签发：重复或倒退各点名拒绝，
    /// 绝不静默并入——否则"同一会话连续输入归入一枚批次"会被乱序破坏。
    /// </summary>
    public InputBufferAppendDecision Append(long seq, DateTimeOffset arrivedAt)
    {
        if (_currentSeqs is not null && seq <= _lastSeq)
        {
            return seq == _lastSeq ? InputBufferAppendDecision.RejectedDuplicateSeq
                : InputBufferAppendDecision.RejectedOutOfOrder;
        }

        _currentSeqs ??= [];
        _currentBatchId ??= DeriveBatchId(seq);
        _openedAt = _currentSeqs.Count == 0 ? arrivedAt : _openedAt;
        _currentSeqs.Add(seq);
        _lastSeq = seq;

        if (_currentSeqs.Count >= _ceiling.MaxBatchItems)
        {
            Seal(InputBufferCloseReason.DisplacedByItemCount);
            return InputBufferAppendDecision.AppendedAndClosedByItemCount;
        }

        return InputBufferAppendDecision.Appended;
    }

    /// <summary>时限到期检查：开着的批次自开启经过 <see cref="InputBufferCeiling.MaxBatchHold"/> 即封批。</summary>
    public bool TryCloseDueAt(DateTimeOffset now)
    {
        if (_currentSeqs is null)
        {
            return false;
        }

        if (now - _openedAt < _ceiling.MaxBatchHold)
        {
            return false;
        }

        Seal(InputBufferCloseReason.HoldElapsed);
        return true;
    }

    /// <summary>调用方强制封当前批（例如"这条必须立刻答"）。</summary>
    public bool ForceClose()
    {
        if (_currentSeqs is null)
        {
            return false;
        }

        Seal(InputBufferCloseReason.ForcedByCaller);
        return true;
    }

    /// <summary>取回一封好的批次（按 <see cref="NarratedInputBatch.BatchId"/> 稳定回放）。</summary>
    public NarratedInputBatch? FindSealed(string batchId) =>
        _sealed.TryGetValue(batchId, out var batch) ? batch : null;

    /// <summary>
    /// 领取某批的唯一可发送真相令牌：同一 <paramref name="batchId"/> 第二次领取即失败——
    /// 同批次绝不产生两条可发送真相。领取只对已封好的批次成立。
    /// </summary>
    public bool TryTakeSendableTruth(string batchId)
    {
        if (!_sealed.ContainsKey(batchId))
        {
            return false;
        }

        return _claimedSendable.Add(batchId);
    }

    private void Seal(InputBufferCloseReason reason)
    {
        var batch = new NarratedInputBatch(_conversationId, _currentBatchId!, [.. _currentSeqs!], reason);
        _sealed[batch.BatchId] = batch;
        _currentSeqs = null;
        _currentBatchId = null;
    }

    /// <summary>
    /// 批次标识＝会话 + 该批第一条输入序号，确定性推导、可回放：重放同一条首发输入必得同一标识。
    /// 不引入任何计数器或随机源，因此没有可被改坏却无人响的隐藏状态。
    /// </summary>
    private string DeriveBatchId(long firstSeq) => $"{_conversationId}#b{firstSeq}";
}

// HDS-006 第一片·隔离原型：证据存储的本体与撤销 CAS（工单 §一.5，v4:302 AUD-03 的那一课）。
//
// AUD-03 在盘上的实形（当场复算，锚点未漂移）＝src/Pry.Core/Memory/SqliteMemoryRepository.cs:433：
// RestoreDetachedMemoriesAsync 里那条 UPDATE … WHERE id AND scope_key AND state_version=$expected
// 的 await Execute(...) **返回值当场不核**，紧随其后的证据 DELETE＋INSERT 无条件继续 ⇒
// "恢复状态的版本更新结果未检查，后续证据仍可覆盖"。同一文件 :397 的脱链方向反倒是核结果的
// （if (await Execute(...) == 1) rewritten.Add(...)）。
// 本原型照 :397 那一侧做，不照 :433 那一侧做：任何一处版本条件不成立就**一个字段都不写**，
// 失败以数据（outcome 枚举）交回而不是异常，且失败不得先把唯一的恢复记录弹掉（v4:281 后半句）。
//
// 全内存、参数全走入参、零生产调用方；数字门槛零新增（版本只做等值条件，不与任何门槛比较）。
namespace Pry.Core.Memory;

/// <summary>撤销尝试的结果。四种都是"当场可判"的数据，不是异常，也不是布尔。</summary>
public enum Hds006UndoOutcome
{
    /// <summary>两处版本条件同时成立，快照字段逐字写回。</summary>
    Restored,

    /// <summary>行的状态版本已经走过快照所依据的那一格：不写任何字段。</summary>
    StateVersionMismatch,

    /// <summary>来源账版本已经走过快照所依据的那一格：不写任何字段。</summary>
    SourceVersionMismatch,

    /// <summary>行已经不在这个原型里（显式遗忘之后的形状）：不写任何字段。</summary>
    RowGone
}

/// <summary>
/// 一次脱链产生的恢复记录：带着**脱链前的逐字字段**＋两处版本条件（行状态版本、来源账版本）。
/// 形状对齐 <see cref="DetachedMemory"/>（整行预删字段＋证据三元组），但本片全内存、不落表。
/// </summary>
public sealed record Hds006UndoRecord(long EvidenceId, string SourceId, string PriorContent,
    double PriorConfidence, long PriorStateVersion, long ExpectedStateVersion, long ExpectedSourceVersion,
    string DetachReason);

/// <summary>
/// 全内存证据本体。行的每一次改写都要过状态版本这道等值条件；
/// 恢复记录只在**撤销成功**那一条路径上被弹掉，失败路径一条都不弹。
/// </summary>
public sealed class Hds006InMemoryEvidenceStore(Hds006SourceLedger ledger)
{
    private readonly Dictionary<long, RowState> _rows = new();
    private readonly List<Hds006UndoRecord> _recoveryRecords = new();

    /// <summary>快照按记录序号升序、同序号按证据标识升序返回；检索自己决定读取方向。</summary>
    public IReadOnlyList<Hds006EvidenceRow> Rows
    {
        get
        {
            var rows = new List<Hds006EvidenceRow>(_rows.Count);
            foreach (var row in _rows.Values.OrderBy(x => x.RecordedSequence).ThenBy(x => x.EvidenceId))
                rows.Add(row.Snapshot());
            return rows;
        }
    }

    /// <summary>当前仍然可用的恢复记录（可观察面：失败不弹掉，成功才弹掉）。</summary>
    public IReadOnlyList<Hds006UndoRecord> RecoveryRecords => _recoveryRecords.ToArray();

    /// <summary>入册一条证据。标识已被占用是编程错误，直接抛出，不静默替换已有身份。</summary>
    public Hds006EvidenceRow Record(Hds006EvidenceDraft draft)
    {
        var state = new RowState
        {
            EvidenceId = draft.EvidenceId,
            Content = draft.Content,
            Identity = draft.Identity,
            SourceId = draft.SourceId,
            Confidence = draft.Confidence,
            StateVersion = draft.StateVersion,
            RecordedSequence = draft.RecordedSequence
        };
        if (!_rows.TryAdd(draft.EvidenceId, state))
            throw new ArgumentException($"Evidence id {draft.EvidenceId} is already recorded.", nameof(draft));
        return state.Snapshot();
    }

    /// <summary>按标识取一条快照；不存在返回 null（调用方自己决定这是不是错误）。</summary>
    public Hds006EvidenceRow? Find(long evidenceId) =>
        _rows.TryGetValue(evidenceId, out var row) ? row.Snapshot() : null;

    /// <summary>
    /// 改写正文（人工改写／显式抽取都走这一条）。状态版本不匹配 ⇒ false，且**一个字节都不写**：
    /// 这就是 AUD-03 的反面——写者不能靠"我先拿到行、再无条件盖下去"覆盖掉已经变过的行。
    /// </summary>
    public bool TryRewrite(long evidenceId, string content, long expectedStateVersion)
    {
        if (!_rows.TryGetValue(evidenceId, out var row)) return false;
        if (row.StateVersion != expectedStateVersion) return false;
        row.Content = content;
        row.StateVersion += 1;
        return true;
    }

    /// <summary>
    /// 脱链一条证据（来源被删／被撤回时的形状）。状态版本不匹配 ⇒ null，行与来源账都不动。
    /// 成功时：行标记脱链并让来源账前进一步，随后返回的恢复记录带着**这两处新版本的等值条件**，
    /// 于是同一次脱链的第二把撤销必然在版本上撞墙，而不是把别人的改动盖回去。
    /// </summary>
    public Hds006UndoRecord? TryDetach(long evidenceId, long expectedStateVersion, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A detachment must name its reason.", nameof(reason));
        if (!_rows.TryGetValue(evidenceId, out var row)) return null;
        if (row.StateVersion != expectedStateVersion) return null;
        var priorContent = row.Content;
        var priorConfidence = row.Confidence;
        var priorStateVersion = row.StateVersion;
        row.Detached = true;
        row.DetachReason = reason;
        row.StateVersion += 1;
        var sourceReadout = ledger.Invalidate(row.SourceId, reason);
        var record = new Hds006UndoRecord(row.EvidenceId, row.SourceId, priorContent, priorConfidence,
            priorStateVersion, row.StateVersion, sourceReadout.SourceVersion, reason);
        _recoveryRecords.Add(record);
        return record;
    }

    /// <summary>
    /// 撤销＝以状态版本与来源版本双条件恢复（v4:281 那句）。任一条件不成立就返回明确结果、
    /// 不写任何字段、并且**保留这条唯一的恢复记录**；只有成功路径才把它从待恢复清单里弹掉。
    /// </summary>
    public Hds006UndoOutcome TryRestore(Hds006UndoRecord record)
    {
        if (!_rows.TryGetValue(record.EvidenceId, out var row)) return Hds006UndoOutcome.RowGone;
        if (row.StateVersion != record.ExpectedStateVersion) return Hds006UndoOutcome.StateVersionMismatch;
        if (ledger.Readout(record.SourceId).SourceVersion != record.ExpectedSourceVersion)
            return Hds006UndoOutcome.SourceVersionMismatch;
        row.Content = record.PriorContent;
        row.Confidence = record.PriorConfidence;
        row.Detached = false;
        row.DetachReason = string.Empty;
        row.StateVersion += 1;
        _recoveryRecords.Remove(record);
        return Hds006UndoOutcome.Restored;
    }

    private sealed class RowState
    {
        public long EvidenceId;
        public string Content = string.Empty;
        public Hds006EvidenceKind Identity;
        public string SourceId = string.Empty;
        public double Confidence;
        public long StateVersion;
        public long RecordedSequence;
        public bool Detached;
        public string DetachReason = string.Empty;

        public Hds006EvidenceRow Snapshot() => new(EvidenceId, Content, Identity, SourceId, Confidence,
            StateVersion, RecordedSequence, Detached, DetachReason);
    }
}

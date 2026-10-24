// HDS-006「记忆与删除全链」第一片·隔离原型（工单 HDS-WAVE28-1024 §一.1／§一.2）。
//
// 本文件与同族 Hds006* 文件是一个**全内存、参数全走入参、零生产调用方**的原型：
// 它不接 SQLite、不接端点、不接界面，也不得被任何生产入口调用；本片的唯一凭据＝判据文件
// tests/Pry.Core.Tests/Hds006EvidenceIdentityRetrievalPrototypeTests.cs。
//
// 数字门槛：本原型**不新增、不复制任何数字门槛**。既有检索地板 .5 的唯一落点是
// src/Pry.Core/Memory/MemoryQueryService.cs:22（x.Confidence >= .5），它是行内字面量而不是命名常量，
// 因此无法被"原样引用"；本原型于是**不设地板**，"新链今天没有地板"这件事由判据格
// The_prototype_applies_no_retrieval_floor_the_existing_confidence_gate_would_have_applied 钉成明写事实。
// 接线那一把必须让这条格改绿为"地板守住了"，不许静默删掉它。
//
// 状态词与结案词不在本文出现；"原型跑通"不等于"记忆与删除全链已完成"，更不等于新链接线。
namespace Pry.Core.Memory;

/// <summary>
/// 三类证据身份（v4:325 第一格）。同一句话在现实／虚构／认知三个身份下是**三条不同证据**；
/// 检索一条不得顺带把另一条带出来（守它的格＝Hds006 判据文件 §一.1 那四枚）。
/// </summary>
public enum Hds006EvidenceKind
{
    /// <summary>现实：真实用户来源支撑的用户事实。</summary>
    Real,

    /// <summary>虚构：角色自己编造的经历／设定，不得当作现实事实召回。</summary>
    Fiction,

    /// <summary>认知：角色对同一材料的信念／理解，既不是现实也不是对外叙述。</summary>
    Cognitive
}

/// <summary>来源的三种可判定状态。缺失与已失效是两个不同原因，不是一个"都不在"的沉默分支。</summary>
public enum Hds006SourceState
{
    Live,
    Invalidated,
    Missing
}

/// <summary>
/// 一次来源问账的读数：状态＋明确原因＋来源版本。§一.2 要的是"可比较的来源标识"与
/// "可判定的来源是否已失效"两件事，SourceId 按 Ordinal 比较，版本供撤销 CAS（§一.5）条件化。
/// </summary>
/// <param name="SourceId">可比较的来源标识；Ordinal 相等即同一来源。</param>
/// <param name="State">来源状态；从未登记过的来源读数一律是 <see cref="Hds006SourceState.Missing"/>。</param>
/// <param name="Reason">明确原因文本；Missing 固定为 <c>source_never_registered</c>，不是空串也不是 null。</param>
/// <param name="SourceVersion">来源账的版本；每次登记或失效都前进一步，撤销快照把它当作条件之一。</param>
public sealed record Hds006SourceReadout(string SourceId, Hds006SourceState State, string Reason, long SourceVersion);

/// <summary>
/// 来源账（§一.2）。它只回答三件事：这个来源登记过没有、现在活着还是已失效、账走到第几版。
/// 来源缺失在这里是**一个明确的读数**，调用方拿不到"查不到就当没有这行"的静默出口。
/// </summary>
public sealed class Hds006SourceLedger
{
    /// <summary>Missing 读数的固定原因文本。</summary>
    public const string NeverRegisteredReason = "source_never_registered";

    private readonly Dictionary<string, (Hds006SourceState State, string Reason, long Version)> _sources =
        new(StringComparer.Ordinal);

    /// <summary>登记一个来源：状态 Live，版本号前进（首次登记为第一版）。</summary>
    public Hds006SourceReadout Register(string sourceId, string reason)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            throw new ArgumentException("A non-empty source id is required.", nameof(sourceId));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A source readout must carry a named reason.", nameof(reason));
        var version = NextVersion(sourceId);
        _sources[sourceId] = (Hds006SourceState.Live, reason, version);
        return new Hds006SourceReadout(sourceId, Hds006SourceState.Live, reason, version);
    }

    /// <summary>
    /// 让来源失效：状态 Invalidated，版本号**每次调用都前进一步**。
    /// 这一步是撤销 CAS 的另一半条件——来源账一旦又走过一格，依据旧格签发的恢复记录就必须被拒
    /// （v4:281"以状态版本和来源版本作条件恢复"）；把它做成幂等不推进，等于把这条条件抹掉。
    /// </summary>
    public Hds006SourceReadout Invalidate(string sourceId, string reason)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            throw new ArgumentException("A non-empty source id is required.", nameof(sourceId));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A source readout must carry a named reason.", nameof(reason));
        var version = NextVersion(sourceId);
        _sources[sourceId] = (Hds006SourceState.Invalidated, reason, version);
        return new Hds006SourceReadout(sourceId, Hds006SourceState.Invalidated, reason, version);
    }

    /// <summary>来源问账。未登记过的来源返回 Missing ＋明确原因，绝不返回 null 让调用方自己猜。</summary>
    public Hds006SourceReadout Readout(string sourceId) =>
        _sources.TryGetValue(sourceId, out var entry)
            ? new Hds006SourceReadout(sourceId, entry.State, entry.Reason, entry.Version)
            : new Hds006SourceReadout(sourceId, Hds006SourceState.Missing, NeverRegisteredReason, 0);

    private long NextVersion(string sourceId) =>
        _sources.TryGetValue(sourceId, out var entry) ? entry.Version + 1 : 1;
}

/// <summary>
/// 一条证据的入册参数。身份、来源标识、置信度、状态版本与记录序号**全部由调用方给**（"参数全走入参"），
/// 本原型不自行发号、不设任何资格门。
/// </summary>
public sealed record Hds006EvidenceDraft(long EvidenceId, string Content, Hds006EvidenceKind Identity,
    string SourceId, double Confidence, long StateVersion, long RecordedSequence);

/// <summary>
/// 一条证据的不可变快照（§一.1／§一.2）。同一个 <see cref="Content"/> 换身份就是另一行；
/// <see cref="Confidence"/> 在这里只是**随身带着的材料**，本原型不按它筛掉任何东西（见文件头）。
/// </summary>
public sealed record Hds006EvidenceRow(long EvidenceId, string Content, Hds006EvidenceKind Identity,
    string SourceId, double Confidence, long StateVersion, long RecordedSequence, bool Detached,
    string DetachReason);

// HDS-006 第一片·隔离原型：统一检索的一个入口（工单 §一.3）。
//
// 形状＝一次调用、一个入参形状（Hds006RetrievalRequest），返回集合的排序与截断**当场可判**：
// 每一条没被返回的行都在 Exclusions 里带着自己的原因；被截掉的那几条带着它们各自的排名，
// 因此"哪几条被截、被截的为什么被截"不需要读实现才能回答（AUD-08 那句"入口分裂"的反面）。
//
// 数字门槛：本入口不新增、不复制任何门槛。它不按 Confidence 筛任何东西——
// 既有检索地板 .5 只住在 src/Pry.Core/Memory/MemoryQueryService.cs:22，
// "本原型没有那把地板"是明写事实，由判据格钉住，不许在这里抄第二个 .5。
//
// 全内存、参数全走入参、零生产调用方。
namespace Pry.Core.Memory;

/// <summary>没被返回的原因。四值皆有实体行可指认，不存在"消失但不说为什么"的出口。</summary>
public enum Hds006ExclusionReason
{
    /// <summary>正文不含本次查询串。</summary>
    ContentMismatch,

    /// <summary>身份与本次请求不同——同一句话的另外两个身份是被排除的，不是被顺带带出的。</summary>
    IdentityMismatch,

    /// <summary>来源从未登记过；明确说"缺来源"，不是静默丢弃。</summary>
    SourceMissing,

    /// <summary>来源已失效。</summary>
    SourceInvalidated,

    /// <summary>合格且已参与排序，但名次落在本次 ResultLimit 之外。</summary>
    BeyondLimit
}

/// <summary>
/// 唯一的检索入参形状。<see cref="ResultLimit"/> 是宿主的入参而不是原型里的常量；
/// 传 0 或负数不报错，其形状是"全部合格行都以 BeyondLimit 报出"，排序规则仍然成立。
/// </summary>
public sealed record Hds006RetrievalRequest(string Query, Hds006EvidenceKind Identity, int ResultLimit);

/// <summary>被返回的一行：Rank 是它在本次结果里的名次，从第一格开始按排序规则连续编号。</summary>
public sealed record Hds006RetrievalHit(long EvidenceId, Hds006EvidenceKind Identity, int Rank);

/// <summary>没被返回的一行：Reasoned 明确原因；只有参与过排序的行才带名次，其余名次为 0。</summary>
public sealed record Hds006RetrievalExclusion(long EvidenceId, Hds006ExclusionReason Reason, int ObservedRank);

/// <summary>一次检索的读数：命中的顺序就是排序规则的输出顺序，截断信息逐条点名。</summary>
public sealed record Hds006RetrievalResult(string Query, Hds006EvidenceKind Identity, int ResultLimit,
    IReadOnlyList<Hds006RetrievalHit> Hits, IReadOnlyList<Hds006RetrievalExclusion> Exclusions);

/// <summary>统一检索入口：一个方法、一个入参形状，别处不得再拼第二份检索。</summary>
public sealed class Hds006UnifiedMemoryRetrieval(Hds006InMemoryEvidenceStore store, Hds006SourceLedger ledger)
{
    /// <summary>
    /// 排序规则（当场可判，无权重、无门槛）：先按 RecordedSequence 新→旧，同序号按 EvidenceId 小→大；
    /// 名次连续编号，落在 <see cref="Hds006RetrievalRequest.ResultLimit"/> 之外的进 BeyondLimit。
    /// </summary>
    public Hds006RetrievalResult Retrieve(Hds006RetrievalRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var hits = new List<Hds006RetrievalHit>();
        var exclusions = new List<Hds006RetrievalExclusion>();
        var eligible = new List<Hds006EvidenceRow>();

        foreach (var row in store.Rows)
        {
            if (!row.Content.Contains(request.Query, StringComparison.Ordinal))
            {
                exclusions.Add(new Hds006RetrievalExclusion(row.EvidenceId,
                    Hds006ExclusionReason.ContentMismatch, 0));
                continue;
            }
            if (row.Identity != request.Identity)
            {
                exclusions.Add(new Hds006RetrievalExclusion(row.EvidenceId,
                    Hds006ExclusionReason.IdentityMismatch, 0));
                continue;
            }
            var source = ledger.Readout(row.SourceId);
            if (source.State == Hds006SourceState.Missing)
            {
                exclusions.Add(new Hds006RetrievalExclusion(row.EvidenceId,
                    Hds006ExclusionReason.SourceMissing, 0));
                continue;
            }
            if (source.State == Hds006SourceState.Invalidated)
            {
                exclusions.Add(new Hds006RetrievalExclusion(row.EvidenceId,
                    Hds006ExclusionReason.SourceInvalidated, 0));
                continue;
            }
            eligible.Add(row);
        }

        var rank = 0;
        foreach (var row in eligible.OrderByDescending(x => x.RecordedSequence).ThenBy(x => x.EvidenceId))
        {
            rank += 1;
            if (rank > request.ResultLimit)
            {
                exclusions.Add(new Hds006RetrievalExclusion(row.EvidenceId,
                    Hds006ExclusionReason.BeyondLimit, rank));
                continue;
            }
            hits.Add(new Hds006RetrievalHit(row.EvidenceId, row.Identity, rank));
        }

        return new Hds006RetrievalResult(request.Query, request.Identity, request.ResultLimit, hits, exclusions);
    }
}

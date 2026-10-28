// HDS-009「状态与显式学习迁移」第一片·隔离原型：Canon 与 Overlay 分层（工单 §一.1）。
//
// 形状口径：全内存、参数全走入参、零生产调用方、零数据库、零网络、零真实模型。
// Canon 是稳定层，一次叙事产出不得改写它；关系／心情／场景这类变化写成带来源引用与版本号的
// Overlay 或版本化状态。两道拒因各用不同名、可分辨——"叙事改写稳定层"与"覆盖层缺来源与版本"
// 不是同一句被拒。
//
// 命名口径：类型名用领域名（分层／覆盖／来源指认），不带验收编号；对"来源"只用不透明的
// "可指认来源引用＋版本号"形状表达（缺失即明确失败），本片不新建任何身份／来源枚举。
namespace Pry.Core.Narrative.StateAndLearning;

/// <summary>
/// 一次分层写入所必须携带的来源指认：可比较的来源标识＋版本号。版本由调用方给，
/// 本原型不自行发号；版本号缺失（null）就是"没带版本"，与"版本为某个数"是两种形状。
/// </summary>
public sealed record LayerSourceReference(string SourceId, long? SourceVersion);

/// <summary>
/// 一次针对稳定层的改写请求。<see cref="FromNarrativeOutput"/> 为真表示这一改来自角色叙事产出，
/// 那种改写被稳定层拒收；作者侧改写（false）仍须带可指认来源。
/// </summary>
public sealed record CanonRevisionRequest(
    string CanonKey,
    string Content,
    bool FromNarrativeOutput,
    LayerSourceReference? Source);

/// <summary>
/// 一条关系／心情／场景变化的覆盖层入册请求。<see cref="ChangeDomain"/> 只是随身的材料标签，
/// 本原型不按它设资格门；它必须带可指认来源与版本才入册。
/// </summary>
public sealed record OverlayDraft(
    string OverlayKey,
    string Content,
    string ChangeDomain,
    LayerSourceReference? Source);

/// <summary>一次分层写入的读数：结局、明确原因、覆盖层序号（被拒时序号为 null）。</summary>
public sealed record LayerReadout(string Key, string Outcome, string Reason, long? OverlaySequence);

/// <summary>一条已入册的覆盖层：随身带着它的来源标识与来源版本，另有一个本地递增序号。</summary>
public sealed record OverlayRecord(
    string OverlayKey,
    string Content,
    string ChangeDomain,
    string SourceId,
    long SourceVersion,
    long Sequence);

/// <summary>
/// 稳定层与覆盖层的分层原型：稳定层对叙事产出免疫，覆盖层必须带来源与版本才落。
/// 摘掉"稳定层免疫叙事改写"那一行、或摘掉"覆盖层必须带来源与版本"那一行，各自只会红它自己那一格。
/// </summary>
public sealed class StateLayering
{
    private readonly Dictionary<string, string> _canon = new(StringComparer.Ordinal);
    private readonly List<OverlayRecord> _overlays = [];

    /// <summary>已入册的覆盖层，按落册顺序。</summary>
    public IReadOnlyList<OverlayRecord> Overlays => _overlays;

    /// <summary>稳定层里当前是否有这个键（覆盖层落册不得改变它——那由"叙事免疫"那道闸守着）。</summary>
    public bool HasCanon(string canonKey) => _canon.ContainsKey(canonKey);

    /// <summary>稳定层当前内容；不存在则为 null。</summary>
    public string? CanonContent(string canonKey) => _canon.TryGetValue(canonKey, out var v) ? v : null;

    /// <summary>
    /// 改写稳定层。两道闸：叙事产出不得改写（拒因一），作者侧改写仍须可指认来源（拒因二）。
    /// </summary>
    public LayerReadout ReviseCanon(CanonRevisionRequest request)
    {
        // 拒因一·稳定层对叙事产出免疫。摘掉这一行 → 只有"叙事不得改写稳定层"那格会红。
        if (request.FromNarrativeOutput)
            return new LayerReadout(request.CanonKey, "Rejected", "canon_immune_to_narrative_output", null);

        // 作者侧改写也要能指认来源；来源缺失是明确被拒，不是静默放行。
        if (request.Source is null || string.IsNullOrWhiteSpace(request.Source.SourceId))
            return new LayerReadout(request.CanonKey, "Rejected", "canon_source_unattributable", null);

        _canon[request.CanonKey] = request.Content;
        return new LayerReadout(request.CanonKey, "Applied", "canon_revised_by_authority", null);
    }

    /// <summary>
    /// 写入一条覆盖层。摘掉"必须携带可指认来源与版本"那一行 → 只有"覆盖层缺来源与版本"那格会红。
    /// </summary>
    public LayerReadout WriteOverlay(OverlayDraft draft)
    {
        // 拒因二·覆盖层必须携带可指认来源与版本（来源标识非空 且 版本号在场）。
        if (draft.Source is null
            || string.IsNullOrWhiteSpace(draft.Source.SourceId)
            || draft.Source.SourceVersion is null)
            return new LayerReadout(draft.OverlayKey, "Rejected", "overlay_requires_pointable_source_and_version", null);

        var sequence = checked(_overlays.Count + 1L);
        _overlays.Add(new OverlayRecord(
            draft.OverlayKey, draft.Content, draft.ChangeDomain,
            draft.Source.SourceId, draft.Source.SourceVersion.Value, sequence));
        return new LayerReadout(draft.OverlayKey, "Applied", "overlay_recorded", sequence);
    }
}

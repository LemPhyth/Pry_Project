// HDS-009「状态与显式学习迁移」第一片·隔离原型：投递后与点击证据归因（工单 §一.3）。
//
// 一条经验候选只有在两枚证据齐时才入册：①该正文真实投递过；②有明确点击／反馈。
// 未投递的正文、被抑制／被去重／被拒绝的正文都不得成为归因证据——三道排除各有独立命名。
// 另立形状：一次"我明天告诉你"的发送只证明提出了承诺，不证明用户同意、也不证明任务已完成。
// 全内存、零生产调用方、零真实网络；"已投递／被抑制"等都是调用方带进来的事实位，不是本原型去查的。
namespace Pry.Core.Narrative.StateAndLearning;

/// <summary>
/// 一条经验候选的证据入参。三个排除位（抑制／去重／拒绝）与投递位、点击位各自独立，
/// 调用方给什么本原型就按什么判；本原型不去访问任何真实投递通道。
/// </summary>
public sealed record ExperienceCandidateEvidence(
    string BodyId,
    bool BodyDelivered,
    bool BodySuppressed,
    bool BodyDeduplicated,
    bool BodyRejected,
    bool HasExplicitClickOrFeedback);

/// <summary>一次"我明天告诉你"式发送的证据入参：只描述这一次发送本身。</summary>
public sealed record PromiseEvidence(
    string BodyId,
    bool PromiseSent,
    bool BodyDelivered,
    bool UserClicked);

/// <summary>
/// 归因读数。承诺／同意／完成三者分开点名：一次发送至多证明"提出承诺"，
/// <see cref="UserConsentProven"/> 与 <see cref="TaskCompletedProven"/> 不会因为一次发送就为真。
/// </summary>
public sealed record AttributionReadout(
    string BodyId,
    bool Admitted,
    string Reason,
    bool CommitmentProposed,
    bool UserConsentProven,
    bool TaskCompletedProven);

/// <summary>一条真正入册的经验：只在投递与点击两枚证据齐时才存在。</summary>
public sealed record AdmittedExperience(string BodyId, string Provenance);

/// <summary>
/// 投递后与点击归因原型。三排除、未投递、缺点击五道闸各有独立拒因；
/// 承诺归因单独一条方法，把"提出承诺"与"用户同意／任务完成"钉成互不等价的两件事。
/// </summary>
public sealed class DeliveryAttribution
{
    private readonly List<AdmittedExperience> _registry = [];

    /// <summary>已入册经验，按入册顺序。</summary>
    public IReadOnlyList<AdmittedExperience> Registry => _registry;

    /// <summary>
    /// 判定一条经验候选能否入册。摘掉其中任意一道闸的判行，只会红它对应那一格。
    /// </summary>
    public AttributionReadout Admit(ExperienceCandidateEvidence evidence)
    {
        // 闸①·被抑制／被去重／被拒绝的正文不得成为归因证据（哪怕正文投递过）。
        if (evidence.BodySuppressed || evidence.BodyDeduplicated || evidence.BodyRejected)
            return NotAdmitted(evidence.BodyId, "body_excluded_from_attribution");

        // 闸②·未投递的正文不得成为归因证据。摘掉这一行 → 只有"未投递不得归因"那格会红。
        if (!evidence.BodyDelivered)
            return NotAdmitted(evidence.BodyId, "body_never_delivered");

        // 闸③·没有明确点击／反馈不得入册。摘掉这一行 → 只有"缺点击不得归因"那格会红。
        if (!evidence.HasExplicitClickOrFeedback)
            return NotAdmitted(evidence.BodyId, "no_explicit_click_or_feedback");

        _registry.Add(new AdmittedExperience(evidence.BodyId, "delivered_and_clicked"));
        return new AttributionReadout(evidence.BodyId, true, "experience_attributed", false, false, false);
    }

    /// <summary>
    /// 一次"我明天告诉你"的发送的归因。至多提出承诺；下面两枚赋值行被翻向时，
    /// "发送不证明同意／不证明完成"那格会当场报警——这是那格能红的原因。
    /// </summary>
    public AttributionReadout AttributePromise(PromiseEvidence promise)
    {
        var proposed = promise.PromiseSent && promise.BodyDelivered;
        // 提出承诺的凭据只是这一次发送本身；用户同意与任务完成不由它支撑。
        return new AttributionReadout(
            promise.BodyId,
            proposed,
            proposed ? "commitment_proposed" : "promise_without_delivery",
            proposed,
            false,
            false);
    }

    private static AttributionReadout NotAdmitted(string bodyId, string reason) =>
        new(bodyId, false, reason, false, false, false);
}

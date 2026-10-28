// HDS-009「状态与显式学习迁移」第一片·隔离原型：经验只作软材料（工单 §一.4）。
//
// 入册经验只能作为后续叙事的**可选材料**被读取：它不得改写稳定层、不得成为硬门槛、
// 不得回流成"该不该说话"的裁决者，也不是任何权重训练。
// 新增数字门槛零枚：有效期、可用时刻全部由调用方随读请求带进来；参数缺失就是明确失败，
// 本文件不设任何阈值／窗口／有效期的源码侧默认值（把默认值写成常量就等于新增一道数字门槛）。
namespace Pry.Core.Narrative.StateAndLearning;

/// <summary>一条已存下来的经验材料：正文＋记录时刻；记录时刻由调用方给。</summary>
public sealed record StoredExperience(string BodyId, string Material, DateTime RecordedAtUtc);

/// <summary>
/// 材料读数。<see cref="OptionalMaterial"/> 在可用时给出文本，否则为 null；
/// <see cref="CarriesSpeakingAuthority"/> 恒为 false——经验从不带"该不该说话"的裁决权，
/// 那一行赋值被翻向时，"经验不是说话裁决者"那格会当场报警。
/// </summary>
public sealed record MaterialReadout(
    string BodyId,
    bool Available,
    string Reason,
    string? OptionalMaterial,
    bool CarriesSpeakingAuthority);

/// <summary>
/// 经验软材料台账：只提供可选材料，永不按它筛掉叙事、永不改写稳定层、永不裁决发言。
/// </summary>
public sealed class ExperienceMaterial
{
    private readonly List<StoredExperience> _stored = [];

    /// <summary>存下一条经验材料（入册权在 §一.3 的归因面，不在这里）。</summary>
    public void Store(StoredExperience experience) => _stored.Add(experience);

    /// <summary>
    /// 把一条经验当作后续叙事的可选材料读出来。有效期窗口是入参：缺失就是明确失败（不设默认），
    /// 超期只说"这段材料此刻不可用"，不作硬门槛也不裁决该不该说话。
    /// </summary>
    public MaterialReadout ReadOptional(string bodyId, DateTime nowUtc, TimeSpan? validityWindow)
    {
        // 零数字门槛·有效期必须作为入参给：参数缺失即明确失败。摘掉这一行 →
        // 后续 validityWindow.Value 会在"缺有效期"那格的 null 输入上抛出，本格当场报警。
        if (validityWindow is null)
            return NotOffered(bodyId, "validity_window_required_as_input");

        var match = _stored.Find(x => x.BodyId == bodyId);
        if (match is null)
            return NotOffered(bodyId, "experience_not_stored");

        if (nowUtc - match.RecordedAtUtc > validityWindow.Value)
            return NotOffered(bodyId, "experience_past_validity");

        return new MaterialReadout(bodyId, true, "experience_offered_as_material", match.Material, false);
    }

    private static MaterialReadout NotOffered(string bodyId, string reason) =>
        new(bodyId, false, reason, null, false);
}

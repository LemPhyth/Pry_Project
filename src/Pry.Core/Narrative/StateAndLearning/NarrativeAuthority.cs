// HDS-009「状态与显式学习迁移」第一片·隔离原型：叙事侧的三道授权闸（工单 §一.2）。
//
// 角色叙事（Narrator 侧的候选产出）不得：直接写亲密／关系类状态、修改作者边界、宣称用户情绪。
// 三道是三道不同的闸，不是同一句 if 的三种措辞：每道闸有独立可判的输入位与独立命名的拒因，
// 摘掉其中一道只会红它自己那一格。语义对齐存储契约 §7.2"来源决定它能支撑哪种断言"——
// 这里只引用该语义，不引用它的任何类型，也不新建身份／来源枚举。
namespace Pry.Core.Narrative.StateAndLearning;

/// <summary>
/// 一次叙事候选产出的写入尝试：三个布尔位各自表示"这一产出想触碰哪一面授权"。
/// 它们相互独立——这正是区分性探针的落点：单独置某一位，只有对应的闸拦下它并点名对应的拒因。
/// </summary>
public sealed record NarrativeWriteAttempt(
    string CandidateId,
    bool WritesIntimacyState,
    bool ModifiesAuthorBoundary,
    bool DeclaresUserEmotion);

/// <summary>授权判定读数：是否放行、明确原因。三道闸的拒因各不相同、可分辨。</summary>
public sealed record AuthorityReadout(string CandidateId, bool Allowed, string Reason);

/// <summary>
/// 叙事侧授权闸：候选产出只能作为叙事，不得越权改写这三面。判定顺序固定为
/// 亲密→作者边界→用户情绪，因此摘掉中间那道只会让"单独改作者边界"那格红，另外两格不动。
/// 三道拒因各不相同、可分辨，且都不来自任何枚举——它们只是明确命名后的字面量。
/// </summary>
public sealed class NarrativeAuthority
{
    /// <summary>唯一入口：判定一次叙事候选产出是否越权。</summary>
    public AuthorityReadout Assess(NarrativeWriteAttempt attempt)
    {
        // 闸①·亲密／关系类状态不得由叙事直接写。摘掉这一行 → 只有"叙事不得写亲密状态"那格会红。
        if (attempt.WritesIntimacyState)
            return new AuthorityReadout(attempt.CandidateId, false, "intimacy_state_is_not_narrator_writable");

        // 闸②·作者边界不得由叙事改写。摘掉这一行 → 只有"叙事不得改作者边界"那格会红。
        if (attempt.ModifiesAuthorBoundary)
            return new AuthorityReadout(attempt.CandidateId, false, "author_boundary_is_not_narrator_mutable");

        // 闸③·用户情绪不得由叙事宣称。摘掉这一行 → 只有"叙事不得宣称用户情绪"那格会红。
        if (attempt.DeclaresUserEmotion)
            return new AuthorityReadout(attempt.CandidateId, false, "user_emotion_is_not_narrator_assertable");

        return new AuthorityReadout(attempt.CandidateId, true, "narrative_within_authority");
    }
}

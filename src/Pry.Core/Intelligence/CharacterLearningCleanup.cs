using Pry.Core.Abstractions;

namespace Pry.Core.Intelligence;

/// <summary>
/// 角色卡被删掉时唯一该被记住的一句话：她学到的东西也要跟着走。四个仓储各有一条
/// <c>PurgeAsync</c>，但"哪一条按角色卡 id、哪三条按宿主给的实例 id"是领域知识，不该散落到调用方——
/// 所以只有这里知道作用域怎么对上。
///
/// <para>
/// 这里不写 SQL：每条删除都由各自的仓储执行，schema 编号与表结构仍然全归 <c>Pry.Core.Memory</c>
/// 与 PLAT-003。代价必须明写：四条删除是四个事务，不是一整个。中途抛错时前面几条已经落下去了，
/// 而这次调用方还能把卡片退回来——所以调用点必须选在"删除仍可撤销"的那一段里。
/// </para>
/// </summary>
public sealed class CharacterLearningCleanup(ICharacterStateRepository states,
    IBehaviorPatternRepository patterns, IBehaviorExperienceRepository experiences,
    IExpressionPatternRepository expressions)
{
    /// <summary>
    /// 清掉这张卡的关系与 Mood（该角色名下所有用户）、行为模式、经验日志与表达模式。
    /// 同一个字符串既当 <c>characterId</c> 又当 <c>characterInstanceId</c> 用：1.0 的宿主就是拿卡片 id
    /// 建运行时的（<c>ConversationSessionService</c> 传 <c>character.Id</c>）。宿主哪天给出一张卡对应
    /// 多个实例的形状，这里就必须改成收全部实例 id——那是 <c>docs/character-intelligence.md</c> §8
    /// 缺口 ③ 登记的已知脆弱点，不是这里的实现细节。
    /// </summary>
    public async Task PurgeAsync(string characterId, CancellationToken cancellationToken = default)
    {
        // 顺序是有意的：三张按实例计的表先走，那份"她跟这个人关系到第几步"的状态最后走。四条删除
        // 各自成事务，中间断掉时，宁可留下"学到的模式没了、进度还在"，也不要反过来——进度没了的
        // 角色卡看起来像刚认识，比少了几条经验难发现得多。
        await patterns.PurgeAsync(characterId, cancellationToken);
        await expressions.PurgeAsync(characterId, cancellationToken);
        await experiences.PurgeAsync(characterId, cancellationToken);
        await states.PurgeAsync(characterId, cancellationToken);
    }
}

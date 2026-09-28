using Pry.Core.Abstractions;
using Pry.Core.Expression;
using Pry.Core.Intelligence;

namespace Pry.Core.Memory;

/// <summary>
/// 学习状态的 SQLite 仓储（DEF-P3STATE-01，RT-003 落库侧缺口 ①）。四个类都是薄委托：
/// SQL 与条件写入全部留在 <see cref="MemoryDatabase"/> 的 Intelligence 那半片里，
/// 这里只把 <c>Pry.Core.Abstractions</c> 的接口形状对上，和 <c>DatabaseRuntimeStore</c> 同一个分层。
///
/// <para>
/// 为什么不合成一个类：三条 <c>PurgeAsync(string)</c> 签名相同、含义不同（角色卡 id ／实例 id ／实例 id），
/// 一个类同时实现四个接口就必须靠参数猜作用域，而那正是"删卡后学习状态留在库里"这个失效形状。
/// </para>
/// </summary>
public sealed class SqliteCharacterStateRepository(MemoryDatabase database) : ICharacterStateRepository
{
    public Task<CharacterStateRecord?> LoadAsync(string characterId, string userId,
        CancellationToken cancellationToken = default) =>
        database.LoadCharacterStateAsync(characterId, userId, cancellationToken);

    public Task<bool> SaveAsync(CharacterStateRecord record, CancellationToken cancellationToken = default) =>
        database.SaveCharacterStateAsync(record, cancellationToken);

    public Task<int> PurgeAsync(string characterId, CancellationToken cancellationToken = default) =>
        database.PurgeCharacterStateAsync(characterId, cancellationToken);
}

public sealed class SqliteBehaviorPatternRepository(MemoryDatabase database) : IBehaviorPatternRepository
{
    public Task<IReadOnlyList<BehaviorPattern>> ListForCharacterAsync(string characterInstanceId,
        CancellationToken cancellationToken = default) =>
        database.ListBehaviorPatternsAsync(characterInstanceId, cancellationToken);

    public Task<BehaviorPattern?> FindAsync(string behaviorPatternId,
        CancellationToken cancellationToken = default) =>
        database.FindBehaviorPatternAsync(behaviorPatternId, cancellationToken);

    public Task SaveAsync(BehaviorPattern pattern, CancellationToken cancellationToken = default) =>
        database.SaveBehaviorPatternAsync(pattern, cancellationToken);

    public Task<int> PurgeAsync(string characterInstanceId, CancellationToken cancellationToken = default) =>
        database.PurgeBehaviorPatternsAsync(characterInstanceId, cancellationToken);
}

public sealed class SqliteBehaviorExperienceRepository(MemoryDatabase database) : IBehaviorExperienceRepository
{
    public Task AppendAsync(BehaviorExperience experience, CancellationToken cancellationToken = default) =>
        database.AppendBehaviorExperienceAsync(experience, cancellationToken);

    public Task<IReadOnlyList<BehaviorExperience>> ListForCharacterAsync(string characterInstanceId, int limit,
        CancellationToken cancellationToken = default) =>
        database.ListBehaviorExperiencesAsync(characterInstanceId, limit, cancellationToken);

    public Task<int> PurgeAsync(string characterInstanceId, CancellationToken cancellationToken = default) =>
        database.PurgeBehaviorExperiencesAsync(characterInstanceId, cancellationToken);
}

public sealed class SqliteExpressionPatternRepository(MemoryDatabase database) : IExpressionPatternRepository
{
    public Task<IReadOnlyList<ExpressionPattern>> ListForCharacterAsync(string characterInstanceId,
        CancellationToken cancellationToken = default) =>
        database.ListExpressionPatternsAsync(characterInstanceId, cancellationToken);

    public Task SaveAsync(ExpressionPattern pattern, CancellationToken cancellationToken = default) =>
        database.SaveExpressionPatternAsync(pattern, cancellationToken);

    public Task<bool> DeleteAsync(string expressionPatternId, CancellationToken cancellationToken = default) =>
        database.DeleteExpressionPatternAsync(expressionPatternId, cancellationToken);

    public Task<int> PurgeAsync(string characterInstanceId, CancellationToken cancellationToken = default) =>
        database.PurgeExpressionPatternsAsync(characterInstanceId, cancellationToken);
}

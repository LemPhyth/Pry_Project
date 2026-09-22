using Pry.Core.Expression;
using Pry.Core.Intelligence;
using Pry.Core.Models;

namespace Pry.Core.Abstractions;

/// <summary>
/// Long-term state per User x Character. Implementations must reject a write whose
/// <see cref="CharacterStateRecord.StateVersion"/> is already behind the stored one.
/// </summary>
public interface ICharacterStateRepository
{
    Task<CharacterStateRecord?> LoadAsync(string characterId, string userId,
        CancellationToken cancellationToken = default);

    Task<bool> SaveAsync(CharacterStateRecord record, CancellationToken cancellationToken = default);
}

public interface IBehaviorPatternRepository
{
    Task<IReadOnlyList<BehaviorPattern>> ListForCharacterAsync(string characterInstanceId,
        CancellationToken cancellationToken = default);
    Task<BehaviorPattern?> FindAsync(string behaviorPatternId, CancellationToken cancellationToken = default);
    Task SaveAsync(BehaviorPattern pattern, CancellationToken cancellationToken = default);
}

/// <summary>Raw per-turn record. Patterns are derived from these; the log itself is the audit trail.</summary>
public interface IBehaviorExperienceRepository
{
    Task AppendAsync(BehaviorExperience experience, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BehaviorExperience>> ListForCharacterAsync(string characterInstanceId, int limit,
        CancellationToken cancellationToken = default);
}

public interface IExpressionPatternRepository
{
    Task<IReadOnlyList<ExpressionPattern>> ListForCharacterAsync(string characterInstanceId,
        CancellationToken cancellationToken = default);
    Task SaveAsync(ExpressionPattern pattern, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string expressionPatternId, CancellationToken cancellationToken = default);
}

/// <summary>
/// The only memory access the intelligence layer is allowed: a scoped, budgeted read model that
/// can distinguish "nothing relevant" from "could not look". MEM-005's IMemoryQuery/MemoryContext
/// is the outward contract; a one-line adapter is expected to satisfy this until then.
/// </summary>
public interface IRelevantMemorySource
{
    Task<RelevantMemoryView> RetrieveAsync(string characterId, string userId, string query,
        int limit, CancellationToken cancellationToken = default);
}

/// <summary>
/// Answers "if Pry were to speak now, what should she do?" and returns a plan suggestion only.
/// It never queues, delays, cancels or delivers a message.
/// </summary>
public interface ICharacterIntelligence
{
    Task<ReplyGenerationContext> DecideAsync(SituationSnapshot situation, CharacterStateSnapshot state,
        CancellationToken cancellationToken = default);
}

public sealed class InMemoryCharacterStateRepository : ICharacterStateRepository
{
    private readonly Dictionary<string, CharacterStateRecord> _records = [];

    private static string Key(string characterId, string userId) => $"{characterId}\n{userId}";

    public Task<CharacterStateRecord?> LoadAsync(string characterId, string userId,
        CancellationToken cancellationToken = default)
    {
        lock (_records)
            return Task.FromResult(_records.TryGetValue(Key(characterId, userId), out var found) ? found : null);
    }

    public Task<bool> SaveAsync(CharacterStateRecord record, CancellationToken cancellationToken = default)
    {
        lock (_records)
        {
            var key = Key(record.CharacterId, record.UserId);
            if (_records.TryGetValue(key, out var existing) && existing.StateVersion > record.StateVersion)
                return Task.FromResult(false);
            _records[key] = record;
            return Task.FromResult(true);
        }
    }
}

public sealed class InMemoryBehaviorPatternRepository : IBehaviorPatternRepository
{
    private readonly Dictionary<string, BehaviorPattern> _patterns = [];

    public Task<IReadOnlyList<BehaviorPattern>> ListForCharacterAsync(string characterInstanceId,
        CancellationToken cancellationToken = default)
    {
        lock (_patterns)
            return Task.FromResult<IReadOnlyList<BehaviorPattern>>(
                _patterns.Values.Where(x => x.CharacterInstanceId == characterInstanceId)
                    .OrderByDescending(x => x.UpdatedAt).ToArray());
    }

    public Task<BehaviorPattern?> FindAsync(string behaviorPatternId, CancellationToken cancellationToken = default)
    {
        lock (_patterns)
            return Task.FromResult(_patterns.TryGetValue(behaviorPatternId, out var found) ? found : null);
    }

    public Task SaveAsync(BehaviorPattern pattern, CancellationToken cancellationToken = default)
    {
        lock (_patterns) _patterns[pattern.BehaviorPatternId] = pattern;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryExpressionPatternRepository : IExpressionPatternRepository
{
    private readonly Dictionary<string, ExpressionPattern> _patterns = [];

    public Task<IReadOnlyList<ExpressionPattern>> ListForCharacterAsync(string characterInstanceId,
        CancellationToken cancellationToken = default)
    {
        lock (_patterns)
            return Task.FromResult<IReadOnlyList<ExpressionPattern>>(
                _patterns.Values.Where(x => x.CharacterInstanceId == characterInstanceId)
                    .OrderByDescending(x => x.UpdatedAt).ToArray());
    }

    public Task SaveAsync(ExpressionPattern pattern, CancellationToken cancellationToken = default)
    {
        lock (_patterns) _patterns[pattern.Id] = pattern;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string expressionPatternId, CancellationToken cancellationToken = default)
    {
        lock (_patterns) return Task.FromResult(_patterns.Remove(expressionPatternId));
    }
}

/// <summary>Append-only experience log, bounded so a long-lived instance cannot grow without limit.</summary>
public sealed class InMemoryBehaviorExperienceRepository : IBehaviorExperienceRepository
{
    private const int Retained = 500;
    private readonly List<BehaviorExperience> _experiences = [];

    public Task AppendAsync(BehaviorExperience experience, CancellationToken cancellationToken = default)
    {
        lock (_experiences)
        {
            _experiences.Add(experience);
            if (_experiences.Count > Retained) _experiences.RemoveRange(0, _experiences.Count - Retained);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BehaviorExperience>> ListForCharacterAsync(string characterInstanceId, int limit,
        CancellationToken cancellationToken = default)
    {
        lock (_experiences)
            return Task.FromResult<IReadOnlyList<BehaviorExperience>>(
                _experiences.Where(x => x.CharacterInstanceId == characterInstanceId)
                    .OrderByDescending(x => x.ObservedAt).Take(Math.Max(1, limit)).ToArray());
    }
}

/// <summary>
/// Lets a host feed the scoped memory view from whatever retrieval it already owns, without the
/// intelligence layer taking a dependency on a memory implementation. A failed lookup must be
/// signalled by throwing here so it can be told apart from "nothing relevant".
/// </summary>
public sealed class FuncMemorySource(
    Func<string, string, int, CancellationToken, Task<IReadOnlyList<MemoryRecord>>> search) : IRelevantMemorySource
{
    public async Task<RelevantMemoryView> RetrieveAsync(string characterId, string userId, string query,
        int limit, CancellationToken cancellationToken = default)
    {
        var found = await search(characterId, query, limit, cancellationToken);
        return RelevantMemoryView.Of(found.Select(MemoryObservation.From).ToArray());
    }
}

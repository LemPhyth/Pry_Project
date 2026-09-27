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

    /// <summary>Clears every user's state for a character that is being deleted, so learned state
    /// cannot outlive the card it belongs to. Returns the number of records cleared.</summary>
    Task<int> PurgeAsync(string characterId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Read side must be deterministic: <c>updated_at DESC</c> then the pattern id ascending, ordinal.
/// Returned objects are detached copies — mutating one must never change what the next read gives back.
/// </summary>
public interface IBehaviorPatternRepository
{
    Task<IReadOnlyList<BehaviorPattern>> ListForCharacterAsync(string characterInstanceId,
        CancellationToken cancellationToken = default);
    Task<BehaviorPattern?> FindAsync(string behaviorPatternId, CancellationToken cancellationToken = default);
    Task SaveAsync(BehaviorPattern pattern, CancellationToken cancellationToken = default);
    Task<int> PurgeAsync(string characterInstanceId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Raw per-turn record. Patterns are derived from these; the log itself is the audit trail.
/// Append-only and bounded per character instance, oldest evicted first, so one busy character
/// cannot erase another's trail. Reads come back most recently appended first, which is what an
/// <c>ORDER BY observed_at DESC, id DESC</c> yields.
/// </summary>
public interface IBehaviorExperienceRepository
{
    Task AppendAsync(BehaviorExperience experience, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BehaviorExperience>> ListForCharacterAsync(string characterInstanceId, int limit,
        CancellationToken cancellationToken = default);
    Task<int> PurgeAsync(string characterInstanceId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Same deterministic read order as <see cref="IBehaviorPatternRepository"/>. Suppression and the
/// two accepted-evidence replay guards are part of the stored state, not runtime extras: a store
/// that cannot return them unchanged has not implemented this interface.
/// </summary>
public interface IExpressionPatternRepository
{
    Task<IReadOnlyList<ExpressionPattern>> ListForCharacterAsync(string characterInstanceId,
        CancellationToken cancellationToken = default);
    Task SaveAsync(ExpressionPattern pattern, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string expressionPatternId, CancellationToken cancellationToken = default);
    Task<int> PurgeAsync(string characterInstanceId, CancellationToken cancellationToken = default);
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

    public Task<int> PurgeAsync(string characterId, CancellationToken cancellationToken = default)
    {
        lock (_records)
        {
            // Match on the record's own field, not on a split of the composite key: a character id
            // is host-supplied text and may contain the separator.
            var keys = _records.Where(x => x.Value.CharacterId == characterId).Select(x => x.Key).ToArray();
            foreach (var key in keys) _records.Remove(key);
            return Task.FromResult(keys.Length);
        }
    }
}

public sealed class InMemoryBehaviorPatternRepository : IBehaviorPatternRepository
{
    private readonly Dictionary<string, BehaviorPattern> _patterns = [];

    /// <summary>A SQL store returns a detached row per read. Handing out the live object here would
    /// let a caller change persisted state without saving, and the two stores would then disagree
    /// on the same assertions — the shape that makes an in-process fake report a false green.</summary>
    private static BehaviorPattern Detach(BehaviorPattern value) =>
        value with { EvaluatedTurnKeys = [.. value.EvaluatedTurnKeys] };

    public Task<IReadOnlyList<BehaviorPattern>> ListForCharacterAsync(string characterInstanceId,
        CancellationToken cancellationToken = default)
    {
        lock (_patterns)
            return Task.FromResult<IReadOnlyList<BehaviorPattern>>(
                [.. _patterns.Values.Where(x => x.CharacterInstanceId == characterInstanceId)
                    .OrderByDescending(x => x.UpdatedAt)
                    .ThenBy(x => x.BehaviorPatternId, StringComparer.Ordinal)
                    .Select(Detach)]);
    }

    public Task<BehaviorPattern?> FindAsync(string behaviorPatternId, CancellationToken cancellationToken = default)
    {
        lock (_patterns)
            return Task.FromResult(_patterns.TryGetValue(behaviorPatternId, out var found) ? Detach(found) : null);
    }

    public Task SaveAsync(BehaviorPattern pattern, CancellationToken cancellationToken = default)
    {
        lock (_patterns) _patterns[pattern.BehaviorPatternId] = Detach(pattern);
        return Task.CompletedTask;
    }

    public Task<int> PurgeAsync(string characterInstanceId, CancellationToken cancellationToken = default)
    {
        lock (_patterns)
        {
            var ids = _patterns.Values.Where(x => x.CharacterInstanceId == characterInstanceId)
                .Select(x => x.BehaviorPatternId).ToArray();
            foreach (var id in ids) _patterns.Remove(id);
            return Task.FromResult(ids.Length);
        }
    }
}

public sealed class InMemoryExpressionPatternRepository : IExpressionPatternRepository
{
    private readonly Dictionary<string, ExpressionPattern> _patterns = [];

    private static ExpressionPattern Detach(ExpressionPattern value) =>
        value with
        {
            AcceptedEvidenceIds = [.. value.AcceptedEvidenceIds],
            AcceptedTurnKeys = [.. value.AcceptedTurnKeys]
        };

    public Task<IReadOnlyList<ExpressionPattern>> ListForCharacterAsync(string characterInstanceId,
        CancellationToken cancellationToken = default)
    {
        lock (_patterns)
            return Task.FromResult<IReadOnlyList<ExpressionPattern>>(
                [.. _patterns.Values.Where(x => x.CharacterInstanceId == characterInstanceId)
                    .OrderByDescending(x => x.UpdatedAt)
                    .ThenBy(x => x.Id, StringComparer.Ordinal)
                    .Select(Detach)]);
    }

    public Task SaveAsync(ExpressionPattern pattern, CancellationToken cancellationToken = default)
    {
        lock (_patterns) _patterns[pattern.Id] = Detach(pattern);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string expressionPatternId, CancellationToken cancellationToken = default)
    {
        lock (_patterns) return Task.FromResult(_patterns.Remove(expressionPatternId));
    }

    public Task<int> PurgeAsync(string characterInstanceId, CancellationToken cancellationToken = default)
    {
        lock (_patterns)
        {
            var ids = _patterns.Values.Where(x => x.CharacterInstanceId == characterInstanceId)
                .Select(x => x.Id).ToArray();
            foreach (var id in ids) _patterns.Remove(id);
            return Task.FromResult(ids.Length);
        }
    }
}

/// <summary>Append-only experience log, bounded per character instance so a long-lived process
/// cannot grow without limit and one active character cannot evict another's audit trail.</summary>
public sealed class InMemoryBehaviorExperienceRepository : IBehaviorExperienceRepository
{
    public const int RetainedPerInstance = 500;
    private readonly List<BehaviorExperience> _experiences = [];

    public Task AppendAsync(BehaviorExperience experience, CancellationToken cancellationToken = default)
    {
        lock (_experiences)
        {
            _experiences.Add(experience);
            var excess = _experiences.Count(x => x.CharacterInstanceId == experience.CharacterInstanceId)
                - RetainedPerInstance;
            for (var i = 0; i < excess; i++)
                _experiences.RemoveAt(_experiences.FindIndex(x =>
                    x.CharacterInstanceId == experience.CharacterInstanceId));
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BehaviorExperience>> ListForCharacterAsync(string characterInstanceId, int limit,
        CancellationToken cancellationToken = default)
    {
        lock (_experiences)
            // Reversed first: the sort below is stable, so experiences sharing one timestamp keep
            // the store's own "latest append first" order instead of collapsing to insertion order.
            return Task.FromResult<IReadOnlyList<BehaviorExperience>>(
                [.. _experiences.AsEnumerable().Reverse()
                    .Where(x => x.CharacterInstanceId == characterInstanceId)
                    .OrderByDescending(x => x.ObservedAt)
                    .Take(Math.Max(1, limit))]);
    }

    public Task<int> PurgeAsync(string characterInstanceId, CancellationToken cancellationToken = default)
    {
        lock (_experiences)
        {
            var removed = _experiences.RemoveAll(x => x.CharacterInstanceId == characterInstanceId);
            return Task.FromResult(removed);
        }
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

using Pry.Core.Abstractions;
using Pry.Core.Intelligence;

namespace Pry.Core.Expression;

/// <summary>
/// The single entry point Runtime uses for the Expression domain: retrieve a few habits, select
/// at most one, record what was actually used, and feed delivered turns to the background
/// learner. Nothing here sends or delays a message.
/// </summary>
public sealed class ExpressionService
{
    private readonly IExpressionPatternRepository _store;
    private readonly ExpressionRetriever _retriever;
    private readonly ExpressionSelector _selector;
    private readonly string _instanceId;

    public ExpressionService(IExpressionPatternRepository store, string characterInstanceId,
        ExpressionRetriever? retriever = null, ExpressionSelector? selector = null)
    {
        _store = store;
        _instanceId = characterInstanceId;
        _retriever = retriever ?? new ExpressionRetriever();
        _selector = selector ?? new ExpressionSelector();
        Learner = new ExpressionLearner(store, characterInstanceId);
    }

    public ExpressionLearner Learner { get; }
    public string CharacterInstanceId => _instanceId;

    public async Task<ExpressionSelection> SelectForTurnAsync(SituationSnapshot situation,
        CharacterStateSnapshot state, BehaviorDecision decision, CancellationToken cancellationToken = default)
    {
        var store = await _store.ListForCharacterAsync(_instanceId, cancellationToken);
        var candidates = _retriever.Retrieve(store, decision.ScenarioTag, situation.UserInput, situation.Now);
        return _selector.Select(situation, state, decision, candidates);
    }

    /// <summary>Called only for text the user actually received.</summary>
    public async Task NoteUsedAsync(IReadOnlyList<ExpressionHint> hints, DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        if (hints.Count == 0) return;
        foreach (var hint in hints)
        {
            var pattern = (await _store.ListForCharacterAsync(_instanceId, cancellationToken))
                .FirstOrDefault(x => x.Id == hint.PatternId);
            if (pattern is null) continue;
            pattern.UsageCount++;
            pattern.LastUsedAt = now;
            pattern.UpdatedAt = now;
            await _store.SaveAsync(pattern, cancellationToken);
        }
    }

    public Task<IReadOnlyList<ExpressionPattern>> AllAsync(CancellationToken cancellationToken = default) =>
        _store.ListForCharacterAsync(_instanceId, cancellationToken);
}

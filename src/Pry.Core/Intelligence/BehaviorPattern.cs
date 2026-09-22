using Pry.Core.Abstractions;
using System.Runtime.CompilerServices;

namespace Pry.Core.Intelligence;

/// <summary>
/// What tended to work for this user and character in a scenario kind. Confidence is a ranking
/// hint for tie-breaking, never a permission: hard gates stay in the planner.
/// </summary>
public sealed record BehaviorPattern
{
    public required string BehaviorPatternId { get; init; }
    public required string CharacterInstanceId { get; init; }
    public required string Scenario { get; init; }
    public required ResponseIntent Strategy { get; init; }

    public double Confidence { get; set; } = 0.5;
    public long UsageCount { get; set; }
    public long SuccessCount { get; set; }
    public long FailureCount { get; set; }
    public double OutcomeAverage { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }

    public bool HasEvidence => SuccessCount + FailureCount >= MinimumObservationsToInfluence;

    /// <summary>Scoped turn keys already folded; persistence must retain these on reload.</summary>
    public IReadOnlyList<string> EvaluatedTurnKeys { get; init; } = [];

    /// <summary>Below this many observations a pattern is reference only, not influence.</summary>
    public const int MinimumObservationsToInfluence = 3;

    /// <summary>Only a replay guard: a turn older than the window cannot be delivered again, and an
    /// unbounded list would grow with every conversation the user ever had.</summary>
    public const int EvaluatedTurnKeyWindow = 64;

    public IReadOnlyList<string> WithEvaluatedTurnKey(string turnKey) =>
        [.. EvaluatedTurnKeys.Skip(Math.Max(0, EvaluatedTurnKeys.Count - EvaluatedTurnKeyWindow + 1)), turnKey];
}

/// <summary>One recorded interaction. Experiences are kept even when they are thin.</summary>
public sealed record BehaviorExperience(
    string CharacterInstanceId,
    string ConversationId,
    string TurnId,
    string Scenario,
    ResponseIntent Intent,
    InteractionOutcome Outcome,
    DateTimeOffset ObservedAt,
    string? FollowUpEvidence = null)
{
    /// <summary>True only for feedback explicitly about this behavior, never inferred engagement.</summary>
    public bool FeedbackIsExplicit { get; init; }
}

public enum InteractionOutcome
{
    Unknown,
    Continued,
    PositiveReaction,
    TopicShiftedAway,
    Corrected,
    Rejected,
    Silenced
}

public static class InteractionOutcomeSignals
{
    /// <summary>Absent or ambiguous feedback deliberately contributes nothing.</summary>
    public static double? Signal(this InteractionOutcome outcome) => outcome switch
    {
        InteractionOutcome.PositiveReaction => 1.0,
        InteractionOutcome.Corrected => -0.8,
        InteractionOutcome.Rejected => -1.0,
        _ => null
    };
}

public sealed class BehaviorPatternRetriever
{
    private readonly int _limit;

    public BehaviorPatternRetriever(int limit = 3) => _limit = Math.Clamp(limit, 1, 5);

    public IReadOnlyList<BehaviorPattern> Retrieve(IEnumerable<BehaviorPattern> candidates, string scenarioTag)
    {
        var key = NormalizeScenario(scenarioTag);
        return candidates
            .Where(x => NormalizeScenario(x.Scenario) == key)
            .OrderByDescending(x => x.Confidence)
            .ThenByDescending(x => x.UpdatedAt)
            .Take(_limit)
            .ToArray();
    }

    public static string NormalizeScenario(string scenario) => scenario.Trim().Replace(" ", "");
}

/// <summary>
/// Folds explicit feedback into patterns once per scoped turn. A small bounded step keeps
/// a single abnormal interaction from rewriting the character's behavior.
/// </summary>
public sealed class BehaviorPatternUpdater
{
    private const double StepPerObservation = 0.05;
    private const double ConfidenceFloor = 0.05;
    private const double ConfidenceCeiling = 0.85;
    private const double OutcomeSmoothing = 0.25;

    private readonly IBehaviorPatternRepository _repository;
    private readonly Func<DateTimeOffset> _clock;
    private static readonly ConditionalWeakTable<IBehaviorPatternRepository, SemaphoreSlim> Gates = new();

    public BehaviorPatternUpdater(IBehaviorPatternRepository repository, Func<DateTimeOffset>? clock = null)
    {
        _repository = repository;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public async Task<BehaviorPattern?> RecordAsync(BehaviorExperience experience,
        CancellationToken cancellationToken = default)
    {
        var gate = Gates.GetValue(_repository, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try { return await RecordCoreAsync(experience, cancellationToken); }
        finally { gate.Release(); }
    }

    private async Task<BehaviorPattern?> RecordCoreAsync(BehaviorExperience experience,
        CancellationToken cancellationToken)
    {
        var signal = experience.FeedbackIsExplicit && !string.IsNullOrWhiteSpace(experience.FollowUpEvidence)
            ? experience.Outcome.Signal() : null;
        var existing = await _repository.FindAsync(
            PatternId(experience.CharacterInstanceId, experience.Scenario, experience.Intent), cancellationToken);
        var now = _clock();
        // Ambiguous observations stay in the experience log, including when a pattern exists.
        if (signal is null) return existing;
        var turnKey = $"{experience.ConversationId.Length}:{experience.ConversationId}{experience.TurnId}";
        if (existing?.EvaluatedTurnKeys.Contains(turnKey, StringComparer.Ordinal) == true) return existing;

        if (existing is null)
        {
            var created = new BehaviorPattern
            {
                BehaviorPatternId = PatternId(experience.CharacterInstanceId, experience.Scenario, experience.Intent),
                CharacterInstanceId = experience.CharacterInstanceId,
                Scenario = BehaviorPatternRetriever.NormalizeScenario(experience.Scenario),
                Strategy = experience.Intent,
                Confidence = 0.5 + Math.Clamp(signal.Value, -1, 1) * StepPerObservation,
                UsageCount = 1,
                SuccessCount = signal.Value > 0 ? 1 : 0,
                FailureCount = signal.Value < 0 ? 1 : 0,
                OutcomeAverage = signal.Value,
                CreatedAt = now,
                UpdatedAt = now,
                LastUsedAt = now,
                EvaluatedTurnKeys = [turnKey]
            };
            await _repository.SaveAsync(created, cancellationToken);
            return created;
        }

        existing = existing with { EvaluatedTurnKeys = existing.WithEvaluatedTurnKey(turnKey) };
        existing.UsageCount++;
        existing.LastUsedAt = now;
        existing.UpdatedAt = now;
        var value = signal.Value;
        existing.OutcomeAverage += (value - existing.OutcomeAverage) * OutcomeSmoothing;
        existing.Confidence = Math.Clamp(existing.Confidence + value * StepPerObservation,
            ConfidenceFloor, ConfidenceCeiling);
        if (value > 0) existing.SuccessCount++;
        else if (value < 0) existing.FailureCount++;
        await _repository.SaveAsync(existing, cancellationToken);
        return existing;
    }

    public static string PatternId(string characterInstanceId, string scenario, ResponseIntent intent) =>
        $"bp:{characterInstanceId}:{BehaviorPatternRetriever.NormalizeScenario(scenario)}:{intent}";
}

using Pry.Core.Expression;

namespace Pry.Core.Intelligence;

/// <summary>What to do. Wording is never decided here; see the Expression layer.</summary>
public enum ResponseIntent
{
    Acknowledge,
    ContinueTopic,
    Ask,
    Tease,
    PushBack,
    Comfort,
    Clarify,
    Refuse,
    Share,
    EndTopic,
    DoNothing,
    UseCapability
}

/// <summary>Overall tone the Expression layer may work in.</summary>
public enum ExpressionRegister { Neutral, Warm, Playful, Serious, Guarded }

/// <summary>
/// Whether learned verbal habits may be offered at all this turn. GroundedOnly keeps calm
/// habits available while joke catchphrases stay out of sad or serious moments.
/// </summary>
public enum HabitToneAllowed { None, GroundedOnly, Any }

/// <summary>
/// A proposal, not a send. Rationale and Gates exist so Developer Mode can explain the choice
/// without anyone re-deriving it from the prompt.
/// </summary>
public sealed record BehaviorDecision
{
    public required ResponseIntent Intent { get; init; }
    public required string ScenarioTag { get; init; }
    public double Confidence { get; init; } = 0.5;

    public ExpressionRegister Register { get; init; } = ExpressionRegister.Neutral;
    public HabitToneAllowed HabitToneAllowed { get; init; } = HabitToneAllowed.Any;

    /// <summary>How many questions the reply may contain. 0 is the common case, not the exception.</summary>
    public int QuestionBudget { get; init; } = 1;

    /// <summary>Multiplies the host's own length limits; the host keeps final control.</summary>
    public double RelativeLengthBudget { get; init; } = 1.0;

    public bool ParaphraseAllowed { get; init; } = true;

    /// <summary>Set when the turn should propose a relationship-state update; applying it is the state service's job.</summary>
    public RelationshipCorrection? RequestedRelationshipCorrection { get; init; }
    public bool RequestedMemoryCorrection { get; init; }
    public string? CapabilityHint { get; init; }

    public IReadOnlyList<string> Rationale { get; init; } = [];
    public IReadOnlyList<string> Gates { get; init; } = [];
}

public sealed record RelationshipCorrection(bool Nearer, string Evidence);

/// <summary>
/// Everything this layer hands back for one turn: the intent, the habits that survived the
/// selector, and what must be avoided. Generation consumes this; it does not consume raw state.
/// </summary>
public sealed record ReplyGenerationContext
{
    public required string TurnId { get; init; }
    public required BehaviorDecision Decision { get; init; }
    public long StateVersion { get; init; }
    public IReadOnlyList<ExpressionHint> ExpressionHints { get; init; } = [];
    public IReadOnlyList<MemoryObservation> Memory { get; init; } = [];
    public bool MemoryDegraded { get; init; }
    public MoodSummary Mood { get; init; } = new();
    public IReadOnlyList<string> Avoidances { get; init; } = [];
    public ExpressionFamiliarityGate FamiliarityGate { get; init; } = ExpressionFamiliarityGate.Reserved;
    public bool AllowsAffectionateAddress { get; init; }
}

public sealed record MoodSummary(string Label = "平静", bool SupportsPlayfulRegister = true, double LengthBudget = 1.0);

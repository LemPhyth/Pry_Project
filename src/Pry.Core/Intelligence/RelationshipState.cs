namespace Pry.Core.Intelligence;

/// <summary>
/// Internal gate buckets. These are not a game stage system: reaching a band unlocks no
/// scripted relationship state, it only widens which behaviours stay plausible.
/// </summary>
public enum FamiliarityBand { Unfamiliar, Acquainted, Familiar, Close }

public enum ExpressionFamiliarityGate { Reserved, Open, Playful }

/// <summary>
/// Relationship between one user and one character instance. Four signals move on their own
/// clocks on purpose; a single averaged "affinity" number would let one loud night stand in
/// for months of knowing someone.
/// </summary>
public sealed record RelationshipState
{
    public double Familiarity { get; set; } = 0.12;
    public double Trust { get; set; } = 0.35;
    public double Closeness { get; set; } = 0.10;
    public double SharedContextStrength { get; set; }

    public long ExchangeCount { get; set; }
    public DateTimeOffset? LastExchangeAt { get; set; }
    public int RepairsAccepted { get; set; }
    public double BoundaryFriction { get; set; }
    public long StateVersion { get; set; }

    public RelationshipState Copy() => this with { };

    public FamiliarityBand Band =>
        Familiarity >= 0.75 && (Closeness >= 0.45 || RepairsAccepted >= 2) ? FamiliarityBand.Close :
        Familiarity >= 0.45 && Trust >= 0.45 ? FamiliarityBand.Familiar :
        Familiarity >= 0.22 ? FamiliarityBand.Acquainted : FamiliarityBand.Unfamiliar;

    /// <summary>Teasing needs both a history of being familiar enough and no fresh friction.</summary>
    public bool AllowsTeasing => Band >= FamiliarityBand.Familiar && BoundaryFriction < 0.4;

    public bool AllowsAffectionateAddress => Band >= FamiliarityBand.Familiar && Closeness >= 0.4;

    public ExpressionFamiliarityGate ExpressionGate => Band switch
    {
        FamiliarityBand.Close => ExpressionFamiliarityGate.Playful,
        FamiliarityBand.Familiar => ExpressionFamiliarityGate.Open,
        _ => ExpressionFamiliarityGate.Reserved
    };

    /// <summary>Multiplies how often the character may speak first; never a stand-alone trigger.</summary>
    public double ProactiveWillingness =>
        Math.Clamp(0.25 + Closeness * 0.5 + SharedContextStrength * 0.25 - BoundaryFriction * 0.3, 0, 1);

    public void NoteExchange(DateTimeOffset now, double sharedContextDelta = 0)
    {
        DriftOnAbsence(now);
        ExchangeCount++;
        LastExchangeAt = now;
        Familiarity = Move(Familiarity, 0.012);
        SharedContextStrength = Clamp(SharedContextStrength + sharedContextDelta, 0, 1);
        StateVersion++;
    }

    public void NoteVulnerabilityShared(DateTimeOffset now)
    {
        NoteExchange(now, 0.03);
        Closeness = Move(Closeness, 0.05);
        Trust = Move(Trust, 0.02);
    }

    public void NoteRepairAccepted(DateTimeOffset now)
    {
        NoteExchange(now);
        RepairsAccepted++;
        Trust = Move(Trust, 0.04);
        BoundaryFriction = Clamp(BoundaryFriction - 0.25, 0, 1);
    }

    public void NoteBoundaryPushed(DateTimeOffset now)
    {
        NoteExchange(now);
        BoundaryFriction = Clamp(BoundaryFriction + 0.34, 0, 1);
        Trust = Move(Trust, -0.05, asymmetric: true);
        Closeness = Move(Closeness, -0.04, asymmetric: true);
    }

    public void NoteMutualJoke(DateTimeOffset now)
    {
        NoteExchange(now, 0.02);
        Closeness = Move(Closeness, 0.03);
        BoundaryFriction = Clamp(BoundaryFriction - 0.08, 0, 1);
    }

    /// <summary>
    /// An explicit correction of how the relationship is framed overrides accumulated guesswork,
    /// in both directions, but still only moves the signals by a bounded step.
    /// </summary>
    public void NoteRelationshipCorrected(DateTimeOffset now, bool closer)
    {
        NoteExchange(now);
        var step = closer ? 0.12 : -0.12;
        Familiarity = Clamp(Familiarity + step, 0, 1);
        Closeness = Clamp(Closeness + step * 1.5, 0, 1);
        if (closer) Trust = Move(Trust, 0.05);
        BoundaryFriction = closer ? Clamp(BoundaryFriction - 0.4, 0, 1) : BoundaryFriction;
    }

    /// <summary>Being met halfway after a mistake counts; the record is not erased in one turn.</summary>
    private const double TrustGainRate = 0.6;
    private const double LossRate = 1.0;
    private static readonly TimeSpan AbsenceDriftWindow = TimeSpan.FromDays(21);

    private void DriftOnAbsence(DateTimeOffset now)
    {
        if (LastExchangeAt is not { } last) return;
        var gap = now - last;
        if (gap <= TimeSpan.FromDays(3)) return;
        var factor = Math.Clamp(gap.TotalDays / AbsenceDriftWindow.TotalDays, 0, 0.35);
        SharedContextStrength = Clamp(SharedContextStrength * (1 - factor), 0, 1);
        Closeness = Clamp(Closeness - 0.04 * factor / 0.35, 0, 1);
    }

    /// <summary>Trust and closeness recover slowly and give ground faster.</summary>
    private static double Move(double current, double delta, bool asymmetric = false)
    {
        var applied = delta >= 0 && asymmetric ? delta * TrustGainRate : delta;
        return Clamp(current + applied, 0, 1);
    }

    private static double Clamp(double value, double min, double max) => Math.Clamp(value, min, max);
}

/// <summary>
/// A judgment about the user's affect. Deliberately not part of MoodState: it is input to an
/// explicit empathy step, never a value written straight into the character's own mood.
/// </summary>
public enum PerceivedAffect { Neutral, Positive, Flat, Down, Agitated, Unsure }

public sealed record UserAffectEstimate(PerceivedAffect Affect, double Confidence, DateTimeOffset ObservedAt)
{
    public bool Reliable => Confidence >= 0.5;
}

namespace Pry.Core.Intelligence;

public sealed record MoodBaseline
{
    public double Valence { get; init; } = 0.15;
    public double Arousal { get; init; } = 0.45;

    private static readonly (string Marker, double Valence, double Arousal)[] Hints =
    [
        ("元气", 0.35, 0.70), ("活泼", 0.30, 0.65), ("开朗", 0.30, 0.62), ("热情", 0.28, 0.66),
        ("安静", -0.02, 0.28), ("冷淡", -0.15, 0.26), ("毒舌", -0.05, 0.48), ("忧郁", -0.25, 0.32),
        ("温柔", 0.22, 0.38), ("慵懒", 0.05, 0.24)
    ];

    public static MoodBaseline From(string personality)
    {
        var text = personality ?? string.Empty;
        foreach (var hint in Hints)
            if (text.Contains(hint.Marker, StringComparison.Ordinal))
                return new MoodBaseline { Valence = hint.Valence, Arousal = hint.Arousal };
        return new MoodBaseline();
    }
}

/// <summary>
/// The character's own lightweight continuous state. It has no field for what the user is
/// feeling: another module must convert that judgment into a bounded character-side event.
/// </summary>
public sealed record MoodState
{
    /// <summary>-1 (bad) .. 1 (good).</summary>
    public double Valence { get; set; } = 0.15;

    /// <summary>0 (calm) .. 1 (activated).</summary>
    public double Arousal { get; set; } = 0.45;

    public double Interest { get; set; } = 0.20;
    public double Fatigue { get; set; } = 0.10;
    public double Irritation { get; set; }

    public MoodBaseline Baseline { get; init; } = new();
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public MoodState Copy() => this with { };

    /// <summary>Half-life of the pull back to baseline; mood should not persist for days.</summary>
    public static readonly TimeSpan DecayHalfLife = TimeSpan.FromMinutes(45);

    public void DriftTowardBaseline(TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero) return;
        var kept = Math.Pow(0.5, elapsed.TotalMinutes / DecayHalfLife.TotalMinutes);
        Valence = Lerp(Baseline.Valence, Valence, kept);
        Arousal = Lerp(Baseline.Arousal, Arousal, kept);
        Interest = Interest * kept;
        Irritation = Irritation * kept;
        Fatigue = Fatigue * kept;
        UpdatedAt = UpdatedAt + elapsed;
    }

    public void Apply(MoodSignal signal, DateTimeOffset now)
    {
        DriftTowardBaseline(now - UpdatedAt);
        Valence = Clamp(Valence + signal.Valence, -1, 1);
        Arousal = Clamp(Arousal + signal.Arousal, 0, 1);
        Interest = Clamp(Interest + signal.Interest, 0, 1);
        Fatigue = Clamp(Fatigue + signal.Fatigue, 0, 1);
        Irritation = Clamp(Irritation + signal.Irritation, 0, 1);
        UpdatedAt = now;
    }

    /// <summary>Low mood plus fatigue is where playful habits stop being appropriate.</summary>
    public bool SupportsPlayfulRegister => Valence > -0.15 && Irritation < 0.55 && Fatigue < 0.85;

    public bool PrefersDisengagement => Fatigue > 0.75 || (Irritation > 0.6 && Valence < 0);

    public double LengthBudget => Math.Clamp(0.7 + Interest * 0.6 - Fatigue * 0.35, 0.45, 1.35);

    public string DescriptiveLabel =>
        Valence > 0.3 && Irritation < 0.3 ? "心情不错" :
        Irritation >= 0.5 ? "有点烦" :
        Valence <= -0.3 ? "情绪偏低" :
        Fatigue >= 0.7 ? "有点累" :
        Interest >= 0.6 ? "对当前话题有兴趣" : "平静";

    private static double Lerp(double from, double to, double kept) => from + (to - from) * kept;

    private static double Clamp(double value, double min, double max) => Math.Clamp(value, min, max);
}

/// <summary>Bounded character-side reaction. Callers pass explicit amounts so tests stay readable.</summary>
public readonly record struct MoodSignal(double Valence = 0, double Arousal = 0, double Interest = 0,
    double Fatigue = 0, double Irritation = 0)
{
    public static MoodSignal InterestingTopic => new(Interest: 0.22, Arousal: 0.08);
    public static MoodSignal WarmExchange => new(Valence: 0.10, Interest: 0.06);
    public static MoodSignal LightJokeLanded => new(Valence: 0.14, Arousal: 0.10, Irritation: -0.08);
    public static MoodSignal RepeatedPingPong => new(Fatigue: 0.10, Irritation: 0.06, Interest: -0.08);
    public static MoodSignal IgnoredBoundary => new(Irritation: 0.28, Valence: -0.14);
    public static MoodSignal EmpathyWithLowMoodUser => new(Valence: -0.06, Arousal: -0.05);
    public static MoodSignal SustainedConversation => new(Fatigue: 0.06);
}

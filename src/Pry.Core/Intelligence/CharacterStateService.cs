using Pry.Core.Abstractions;

namespace Pry.Core.Intelligence;

/// <summary>Read-only aggregate handed to the planner. The live state stays in the service.</summary>
public sealed record CharacterStateSnapshot(IdentitySnapshot Identity, RelationshipState Relationship,
    MoodState Mood, long StateVersion, DateTimeOffset ObservedAt)
{
    public static CharacterStateSnapshot Freeze(IdentitySnapshot identity, RelationshipState relationship,
        MoodState mood, DateTimeOffset now) =>
        new(identity, relationship.Copy(), mood.Copy(), relationship.StateVersion, now);
}

/// <summary>Flat persistence form so a store never has to know about gate semantics.</summary>
public sealed record CharacterStateRecord
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public required string CharacterId { get; init; }
    public required string UserId { get; init; }

    public double Familiarity { get; init; } = 0.12;
    public double Trust { get; init; } = 0.35;
    public double Closeness { get; init; } = 0.10;
    public double SharedContextStrength { get; init; }
    public long ExchangeCount { get; init; }
    public DateTimeOffset? LastExchangeAt { get; init; }
    public int RepairsAccepted { get; init; }
    public double BoundaryFriction { get; init; }
    public long StateVersion { get; init; }

    public double Valence { get; init; } = 0.15;
    public double Arousal { get; init; } = 0.45;
    public double Interest { get; init; } = 0.20;
    public double Fatigue { get; init; } = 0.10;
    public double Irritation { get; init; }
    public DateTimeOffset MoodUpdatedAt { get; init; }
    public double BaselineValence { get; init; } = 0.15;
    public double BaselineArousal { get; init; } = 0.45;
}

/// <summary>
/// One instance per User x Character. Conversation-scoped buffers are not held here, so
/// switching sessions cannot carry short-term state into the long-term record.
/// </summary>
public sealed class CharacterStateService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ICharacterStateRepository _repository;
    private readonly IdentitySnapshot _identity;
    private readonly string _userId;

    public CharacterStateService(IdentitySnapshot identity, string userId, ICharacterStateRepository repository)
    {
        _identity = identity;
        _userId = userId;
        _repository = repository;
        Relationship = new RelationshipState();
        Mood = new MoodState { Baseline = identity.Baseline };
    }

    public RelationshipState Relationship { get; private set; }
    public MoodState Mood { get; private set; }
    public IdentitySnapshot Identity => _identity;

    public async Task<bool> RestoreAsync(CancellationToken cancellationToken = default)
    {
        var stored = await _repository.LoadAsync(_identity.CharacterId, _userId, cancellationToken);
        if (stored is null) return false;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Relationship = new RelationshipState
            {
                Familiarity = stored.Familiarity, Trust = stored.Trust, Closeness = stored.Closeness,
                SharedContextStrength = stored.SharedContextStrength, ExchangeCount = stored.ExchangeCount,
                LastExchangeAt = stored.LastExchangeAt, RepairsAccepted = stored.RepairsAccepted,
                BoundaryFriction = stored.BoundaryFriction, StateVersion = stored.StateVersion
            };
            Mood = new MoodState
            {
                Valence = stored.Valence, Arousal = stored.Arousal, Interest = stored.Interest,
                Fatigue = stored.Fatigue, Irritation = stored.Irritation,
                UpdatedAt = stored.MoodUpdatedAt == default ? DateTimeOffset.UtcNow : stored.MoodUpdatedAt,
                Baseline = new MoodBaseline { Valence = stored.BaselineValence, Arousal = stored.BaselineArousal }
            };
            return true;
        }
        finally { _gate.Release(); }
    }

    public CharacterStateSnapshot Snapshot(DateTimeOffset now)
    {
        var mood = Mood.Copy();
        mood.DriftTowardBaseline(now - mood.UpdatedAt);
        return CharacterStateSnapshot.Freeze(_identity, Relationship, mood, now);
    }

    public Task<CharacterStateRecord> CurrentRecordAsync()
    {
        return Task.FromResult(new CharacterStateRecord
        {
            CharacterId = _identity.CharacterId, UserId = _userId,
            Familiarity = Relationship.Familiarity, Trust = Relationship.Trust, Closeness = Relationship.Closeness,
            SharedContextStrength = Relationship.SharedContextStrength, ExchangeCount = Relationship.ExchangeCount,
            LastExchangeAt = Relationship.LastExchangeAt, RepairsAccepted = Relationship.RepairsAccepted,
            BoundaryFriction = Relationship.BoundaryFriction, StateVersion = Relationship.StateVersion,
            Valence = Mood.Valence, Arousal = Mood.Arousal, Interest = Mood.Interest, Fatigue = Mood.Fatigue,
            Irritation = Mood.Irritation, MoodUpdatedAt = Mood.UpdatedAt,
            BaselineValence = Mood.Baseline.Valence, BaselineArousal = Mood.Baseline.Arousal
        });
    }

    /// <summary>Returns false when a concurrent writer already advanced this instance's state.</summary>
    public async Task<bool> PersistAsync(CancellationToken cancellationToken = default)
    {
        var record = await CurrentRecordAsync();
        return await _repository.SaveAsync(record, cancellationToken);
    }

    public async Task NoteExchangeAsync(DateTimeOffset now, MoodSignal? mood = null,
        double sharedContextDelta = 0, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Relationship.NoteExchange(now, sharedContextDelta);
            if (mood is { } signal) Mood.Apply(signal, now);
            else Mood.DriftTowardBaseline(now - Mood.UpdatedAt);
        }
        finally { _gate.Release(); }
        await PersistAsync(cancellationToken);
    }

    public async Task NoteRelationshipCorrectedAsync(DateTimeOffset now, bool closer,
        MoodSignal? mood = null, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Relationship.NoteRelationshipCorrected(now, closer);
            if (mood is { } signal) Mood.Apply(signal, now);
        }
        finally { _gate.Release(); }
        await PersistAsync(cancellationToken);
    }

    public async Task NoteBoundaryPushedAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Relationship.NoteBoundaryPushed(now);
            Mood.Apply(MoodSignal.IgnoredBoundary, now);
        }
        finally { _gate.Release(); }
        await PersistAsync(cancellationToken);
    }

    public async Task NoteRepairAcceptedAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Relationship.NoteRepairAccepted(now);
            Mood.Apply(MoodSignal.WarmExchange, now);
        }
        finally { _gate.Release(); }
        await PersistAsync(cancellationToken);
    }

    /// <summary>
    /// The only supported path from "how the user seems" into the character's mood: a bounded
    /// empathic reaction, dropped entirely when the estimate is unreliable. Unreliable readings
    /// must not be able to steer the character.
    /// </summary>
    public async Task<bool> ReactToUserAffectAsync(UserAffectEstimate estimate,
        CancellationToken cancellationToken = default)
    {
        if (!estimate.Reliable) return false;
        var signal = estimate.Affect switch
        {
            PerceivedAffect.Down => MoodSignal.EmpathyWithLowMoodUser with { Interest = 0.10 },
            PerceivedAffect.Positive => new MoodSignal(0.06, 0.04),
            PerceivedAffect.Agitated => new MoodSignal(-0.02, 0.06),
            _ => (MoodSignal?)null
        };
        if (signal is null) return false;
        await _gate.WaitAsync(cancellationToken);
        try { Mood.Apply(signal.Value, estimate.ObservedAt); }
        finally { _gate.Release(); }
        await PersistAsync(cancellationToken);
        return true;
    }
}

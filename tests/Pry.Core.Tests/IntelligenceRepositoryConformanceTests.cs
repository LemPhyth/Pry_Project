using Pry.Core.Abstractions;
using Pry.Core.Expression;
using Pry.Core.Intelligence;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// The persistence contract for the intelligence layer, as an executable rather than a paragraph.
/// Everything the character has learned lives behind these four interfaces; the SQLite store is a
/// second implementation of the same contract, so it must pass this class unchanged.
///
/// <para>
/// The Persistence Owner adds one derived class whose <see cref="NewStores"/> opens a real database
/// and whose <see cref="FlushAsync"/> reopens it. Nothing here needs to be edited to do that, and no
/// assertion here is specific to dictionaries: each one is a claim about what a store must return.
/// </para>
///
/// <para>
/// Why the assertions are field-by-field instead of <c>Assert.Equal(wholeRecord, ...)</c>: these are
/// records whose evidence lists are <see cref="IReadOnlyList{T}"/>, and list equality is reference
/// equality, so a whole-record compare would pass or fail for reasons unrelated to the store.
/// </para>
/// </summary>
public abstract class IntelligenceRepositoryConformanceTests
{
    protected sealed record Stores
    {
        public required ICharacterStateRepository States { get; init; }
        public required IBehaviorPatternRepository Patterns { get; init; }
        public required IBehaviorExperienceRepository Experiences { get; init; }
        public required IExpressionPatternRepository Expressions { get; init; }
    }

    protected abstract Stores NewStores();

    /// <summary>
    /// Forces whatever the store holds in memory down into its durable form so that the next read
    /// comes back from there. The in-process reference stores have no second copy, so they inherit
    /// this no-op; a database store overrides it by closing and reopening the file.
    /// </summary>
    protected virtual Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static readonly DateTimeOffset Base = new(2026, 9, 20, 10, 0, 0, TimeSpan.FromHours(8));
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static CharacterStateRecord State(string characterId = "card-a", string userId = "user-1",
        long stateVersion = 5) => new()
    {
        CharacterId = characterId, UserId = userId, StateVersion = stateVersion,
        Familiarity = 0.417, Trust = 0.632, Closeness = 0.271, SharedContextStrength = 0.389,
        ExchangeCount = 91, LastExchangeAt = Base, RepairsAccepted = 3, BoundaryFriction = 0.154,
        Valence = 0.236, Arousal = 0.517, Interest = 0.628, Fatigue = 0.143, Irritation = 0.091,
        MoodUpdatedAt = Base.AddMinutes(7), BaselineValence = 0.111, BaselineArousal = 0.444
    };

    private static BehaviorPattern Pattern(string id = "bp:instance-a:topic:Comfort",
        string instanceId = "instance-a", DateTimeOffset? updatedAt = null,
        IReadOnlyList<string>? turnKeys = null) => new()
    {
        BehaviorPatternId = id, CharacterInstanceId = instanceId, Scenario = " 安慰 ",
        Strategy = ResponseIntent.Comfort, Confidence = 0.61, UsageCount = 12, SuccessCount = 9,
        FailureCount = 2, OutcomeAverage = 0.35, CreatedAt = Base,
        UpdatedAt = updatedAt ?? Base.AddHours(1), LastUsedAt = Base.AddMinutes(20),
        EvaluatedTurnKeys = turnKeys ?? ["conv-1turn-1", "conv-1turn-2", "conv-2turn-7"]
    };

    private static ExpressionPattern Expression(string id = "ep:instance-a:aaa111",
        string instanceId = "instance-a", DateTimeOffset? updatedAt = null,
        IReadOnlyList<string>? evidenceIds = null, IReadOnlyList<string>? turnKeys = null) => new()
    {
        Id = id, CharacterInstanceId = instanceId, Situation = "轻微吐槽", Style = "笑不活了",
        Tone = ExpressionTone.Playful, Source = ExpressionPatternSource.Learned,
        Confidence = 0.53, UsageCount = 6, EvidenceCount = 4,
        CreatedAt = Base, UpdatedAt = updatedAt ?? Base.AddHours(1), LastUsedAt = Base.AddMinutes(9),
        AcceptedEvidenceIds = evidenceIds ?? ["fb-1", "fb-2", "fb-3"],
        AcceptedTurnKeys = turnKeys ?? ["conv-1turn-1", "conv-1turn-2"]
    };

    private static BehaviorExperience Experience(string turnId, string instanceId = "instance-a",
        InteractionOutcome outcome = InteractionOutcome.PositiveReaction, string? evidence = "fb-9",
        bool explicitFeedback = true, DateTimeOffset? at = null) => new(
        instanceId, "conv-1", turnId, "topic", ResponseIntent.Comfort, outcome,
        at ?? Base, evidence)
    { FeedbackIsExplicit = explicitFeedback };

    private static void AssertStateEqual(CharacterStateRecord expected, CharacterStateRecord actual)
    {
        Assert.Equal(expected.SchemaVersion, actual.SchemaVersion);
        Assert.Equal(expected.CharacterId, actual.CharacterId);
        Assert.Equal(expected.UserId, actual.UserId);
        Assert.Equal(expected.Familiarity, actual.Familiarity);
        Assert.Equal(expected.Trust, actual.Trust);
        Assert.Equal(expected.Closeness, actual.Closeness);
        Assert.Equal(expected.SharedContextStrength, actual.SharedContextStrength);
        Assert.Equal(expected.ExchangeCount, actual.ExchangeCount);
        AssertInstant(expected.LastExchangeAt, actual.LastExchangeAt);
        Assert.Equal(expected.RepairsAccepted, actual.RepairsAccepted);
        Assert.Equal(expected.BoundaryFriction, actual.BoundaryFriction);
        Assert.Equal(expected.StateVersion, actual.StateVersion);
        Assert.Equal(expected.Valence, actual.Valence);
        Assert.Equal(expected.Arousal, actual.Arousal);
        Assert.Equal(expected.Interest, actual.Interest);
        Assert.Equal(expected.Fatigue, actual.Fatigue);
        Assert.Equal(expected.Irritation, actual.Irritation);
        AssertInstant(expected.MoodUpdatedAt, actual.MoodUpdatedAt);
        Assert.Equal(expected.BaselineValence, actual.BaselineValence);
        Assert.Equal(expected.BaselineArousal, actual.BaselineArousal);
    }

    private static void AssertPatternEqual(BehaviorPattern expected, BehaviorPattern actual)
    {
        Assert.Equal(expected.BehaviorPatternId, actual.BehaviorPatternId);
        Assert.Equal(expected.CharacterInstanceId, actual.CharacterInstanceId);
        Assert.Equal(expected.Scenario, actual.Scenario);
        Assert.Equal(expected.Strategy, actual.Strategy);
        Assert.Equal(expected.Confidence, actual.Confidence);
        Assert.Equal(expected.UsageCount, actual.UsageCount);
        Assert.Equal(expected.SuccessCount, actual.SuccessCount);
        Assert.Equal(expected.FailureCount, actual.FailureCount);
        Assert.Equal(expected.OutcomeAverage, actual.OutcomeAverage);
        Assert.Equal(expected.HasEvidence, actual.HasEvidence);
        AssertInstant(expected.CreatedAt, actual.CreatedAt);
        AssertInstant(expected.UpdatedAt, actual.UpdatedAt);
        AssertInstant(expected.LastUsedAt, actual.LastUsedAt);
        Assert.Equal(expected.EvaluatedTurnKeys, actual.EvaluatedTurnKeys, StringComparer.Ordinal);
    }

    private static void AssertExpressionEqual(ExpressionPattern expected, ExpressionPattern actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.CharacterInstanceId, actual.CharacterInstanceId);
        Assert.Equal(expected.Situation, actual.Situation);
        Assert.Equal(expected.Style, actual.Style);
        Assert.Equal(expected.Tone, actual.Tone);
        Assert.Equal(expected.Confidence, actual.Confidence);
        Assert.Equal(expected.UsageCount, actual.UsageCount);
        Assert.Equal(expected.EvidenceCount, actual.EvidenceCount);
        Assert.Equal(expected.Source, actual.Source);
        Assert.Equal(expected.Suppressed, actual.Suppressed);
        AssertInstant(expected.CreatedAt, actual.CreatedAt);
        AssertInstant(expected.UpdatedAt, actual.UpdatedAt);
        AssertInstant(expected.LastUsedAt, actual.LastUsedAt);
        Assert.Equal(expected.AcceptedEvidenceIds, actual.AcceptedEvidenceIds, StringComparer.Ordinal);
        Assert.Equal(expected.AcceptedTurnKeys, actual.AcceptedTurnKeys, StringComparer.Ordinal);
        Assert.Equal(expected.DedupKey, actual.DedupKey);
    }

    /// <summary>The instant has to survive; whether the store keeps the original UTC offset is its own business.</summary>
    private static void AssertInstant(DateTimeOffset? expected, DateTimeOffset? actual)
    {
        Assert.Equal(expected?.ToUniversalTime(), actual?.ToUniversalTime());
    }

    private static void AssertInstant(DateTimeOffset expected, DateTimeOffset actual)
    {
        Assert.Equal(expected.ToUniversalTime(), actual.ToUniversalTime());
    }

    [Fact]
    public async Task Every_state_field_survives_a_reload()
    {
        var stores = NewStores();
        var written = State();
        Assert.True(await stores.States.SaveAsync(written, Token));
        await FlushAsync(Token);

        var loaded = await stores.States.LoadAsync(written.CharacterId, written.UserId, Token);
        Assert.NotNull(loaded);
        AssertStateEqual(written, loaded);
    }

    [Fact]
    public async Task Absent_last_exchange_stays_absent()
    {
        var stores = NewStores();
        Assert.True(await stores.States.SaveAsync(State() with { LastExchangeAt = null }, Token));
        await FlushAsync(Token);

        var loaded = await stores.States.LoadAsync("card-a", "user-1", Token);
        Assert.NotNull(loaded);
        Assert.Null(loaded.LastExchangeAt);
    }

    [Fact]
    public async Task State_is_scoped_per_user_and_unknown_pairs_read_as_null()
    {
        var stores = NewStores();
        Assert.True(await stores.States.SaveAsync(State(userId: "user-1"), Token));
        Assert.True(await stores.States.SaveAsync(State(userId: "user-2", stateVersion: 9), Token));
        await FlushAsync(Token);

        Assert.Equal(9L, (await stores.States.LoadAsync("card-a", "user-2", Token))?.StateVersion);
        Assert.Equal(5L, (await stores.States.LoadAsync("card-a", "user-1", Token))?.StateVersion);
        Assert.Null(await stores.States.LoadAsync("card-a", "user-3", Token));
        Assert.Null(await stores.States.LoadAsync("card-b", "user-1", Token));
    }

    [Fact]
    public async Task A_write_behind_the_stored_state_version_is_rejected()
    {
        var stores = NewStores();
        Assert.True(await stores.States.SaveAsync(State(stateVersion: 5), Token));

        var stale = State(stateVersion: 4) with { Familiarity = 0.999 };
        Assert.False(await stores.States.SaveAsync(stale, Token));
        await FlushAsync(Token);

        var loaded = await stores.States.LoadAsync("card-a", "user-1", Token);
        Assert.Equal(5L, loaded?.StateVersion);
        Assert.Equal(0.417, loaded?.Familiarity);
    }

    [Fact]
    public async Task Writing_the_same_state_version_again_is_allowed()
    {
        var stores = NewStores();
        Assert.True(await stores.States.SaveAsync(State(stateVersion: 5), Token));
        Assert.True(await stores.States.SaveAsync(State(stateVersion: 5) with { Trust = 0.8 }, Token));
        Assert.Equal(0.8, (await stores.States.LoadAsync("card-a", "user-1", Token))?.Trust);
    }

    [Fact]
    public async Task Purging_a_character_clears_every_user_of_that_card_only()
    {
        var stores = NewStores();
        await stores.States.SaveAsync(State(userId: "user-1"), Token);
        await stores.States.SaveAsync(State(userId: "user-2"), Token);
        await stores.States.SaveAsync(State(characterId: "card-b"), Token);

        Assert.Equal(2, await stores.States.PurgeAsync("card-a", Token));
        await FlushAsync(Token);

        Assert.Null(await stores.States.LoadAsync("card-a", "user-1", Token));
        Assert.Null(await stores.States.LoadAsync("card-a", "user-2", Token));
        Assert.NotNull(await stores.States.LoadAsync("card-b", "user-1", Token));
        Assert.Equal(0, await stores.States.PurgeAsync("card-gone", Token));
    }

    [Fact]
    public async Task Every_pattern_field_survives_a_reload()
    {
        var stores = NewStores();
        var written = Pattern();
        await stores.Patterns.SaveAsync(written, Token);
        await FlushAsync(Token);

        AssertPatternEqual(written, await stores.Patterns.FindAsync(written.BehaviorPatternId, Token)
            ?? throw new InvalidOperationException("pattern vanished from the store"));
    }

    [Fact]
    public async Task Replay_guards_survive_a_reload_in_order()
    {
        var stores = NewStores();
        // The window is the model's job; the store must hand back exactly what it was given,
        // because a re-ordered or de-duplicated guard silently re-enables double folding.
        string[] keys = [.. Enumerable.Range(0, 70).Select(x => $"conv-7turn-{x}")];
        await stores.Patterns.SaveAsync(Pattern(turnKeys: keys), Token);
        await FlushAsync(Token);

        var loaded = await stores.Patterns.FindAsync("bp:instance-a:topic:Comfort", Token);
        Assert.Equal(keys, loaded?.EvaluatedTurnKeys, StringComparer.Ordinal);
    }

    [Fact]
    public async Task Patterns_are_listed_most_recent_first_then_by_id_and_scoped_per_instance()
    {
        var stores = NewStores();
        await stores.Patterns.SaveAsync(Pattern(id: "bp:b", updatedAt: Base), Token);
        await stores.Patterns.SaveAsync(Pattern(id: "bp:a", updatedAt: Base.AddHours(2)), Token);
        await stores.Patterns.SaveAsync(Pattern(id: "bp:c", updatedAt: Base.AddHours(2)), Token);
        await stores.Patterns.SaveAsync(Pattern(id: "bp:other", instanceId: "instance-b"), Token);
        await FlushAsync(Token);

        var listed = await stores.Patterns.ListForCharacterAsync("instance-a", Token);
        Assert.Equal(new[] { "bp:a", "bp:c", "bp:b" }, listed.Select(x => x.BehaviorPatternId).ToArray());
        Assert.Equal("bp:other", (await stores.Patterns.ListForCharacterAsync("instance-b", Token))
            .Single().BehaviorPatternId);
        Assert.Empty(await stores.Patterns.ListForCharacterAsync("instance-gone", Token));
    }

    [Fact]
    public async Task An_object_handed_out_by_a_pattern_read_cannot_rewrite_the_store()
    {
        var stores = NewStores();
        await stores.Patterns.SaveAsync(Pattern(), Token);

        var handed = (await stores.Patterns.ListForCharacterAsync("instance-a", Token)).Single();
        handed.Confidence = 0.95;
        handed.UsageCount = 9999;
        handed.SuccessCount = 9999;

        await FlushAsync(Token);
        AssertPatternEqual(Pattern(), await stores.Patterns.FindAsync("bp:instance-a:topic:Comfort", Token)
            ?? throw new InvalidOperationException("pattern vanished from the store"));

        // Two reads must not share one evidence-list instance: the guard lists are init-only, so the
        // way a caller corrupts them is in place, through a list the store also holds.
        var again = await stores.Patterns.FindAsync("bp:instance-a:topic:Comfort", Token);
        Assert.NotSame(handed.EvaluatedTurnKeys, again!.EvaluatedTurnKeys);
    }

    [Fact]
    public async Task Purging_an_instance_clears_its_patterns_only()
    {
        var stores = NewStores();
        await stores.Patterns.SaveAsync(Pattern(id: "bp:a"), Token);
        await stores.Patterns.SaveAsync(Pattern(id: "bp:other", instanceId: "instance-b"), Token);

        Assert.Equal(1, await stores.Patterns.PurgeAsync("instance-a", Token));
        await FlushAsync(Token);

        Assert.Null(await stores.Patterns.FindAsync("bp:a", Token));
        Assert.NotNull(await stores.Patterns.FindAsync("bp:other", Token));
        Assert.Equal(0, await stores.Patterns.PurgeAsync("instance-gone", Token));
    }

    [Fact]
    public async Task Suppression_survives_a_reload()
    {
        var stores = NewStores();
        // The zero-confidence placeholder is how an explicit "don't talk like that" outlives the
        // learner that wrote it; losing Suppressed or the 0 re-promotes the habit after a restart.
        var placeholder = Expression(id: "ep:instance-a:zzz000", evidenceIds: [], turnKeys: []) with
        {
            Suppressed = true, Confidence = 0, EvidenceCount = 0
        };
        await stores.Expressions.SaveAsync(placeholder, Token);
        await FlushAsync(Token);

        var loaded = (await stores.Expressions.ListForCharacterAsync("instance-a", Token)).Single();
        Assert.True(loaded.Suppressed);
        Assert.Equal(0d, loaded.Confidence);
        Assert.Equal(ExpressionPatternSource.Learned, loaded.Source);
    }

    [Fact]
    public async Task Every_expression_field_survives_a_reload()
    {
        var stores = NewStores();
        var written = Expression();
        await stores.Expressions.SaveAsync(written, Token);
        await FlushAsync(Token);

        AssertExpressionEqual(written, (await stores.Expressions
            .ListForCharacterAsync("instance-a", Token)).Single());
    }

    [Fact]
    public async Task Accepted_evidence_references_survive_a_reload_in_order()
    {
        var stores = NewStores();
        string[] ids = [.. Enumerable.Range(0, 70).Select(x => $"fb-{x}")];
        string[] turns = [.. Enumerable.Range(0, 70).Select(x => $"conv-3turn-{x}")];
        await stores.Expressions.SaveAsync(Expression(evidenceIds: ids, turnKeys: turns), Token);
        await FlushAsync(Token);

        var loaded = (await stores.Expressions.ListForCharacterAsync("instance-a", Token)).Single();
        Assert.Equal(ids, loaded.AcceptedEvidenceIds, StringComparer.Ordinal);
        Assert.Equal(turns, loaded.AcceptedTurnKeys, StringComparer.Ordinal);
    }

    [Fact]
    public async Task Expressions_are_listed_most_recent_first_then_by_id_and_scoped_per_instance()
    {
        var stores = NewStores();
        await stores.Expressions.SaveAsync(Expression(id: "ep:z", updatedAt: Base), Token);
        await stores.Expressions.SaveAsync(Expression(id: "ep:a", updatedAt: Base.AddHours(3)), Token);
        await stores.Expressions.SaveAsync(Expression(id: "ep:b", updatedAt: Base.AddHours(3)), Token);
        await stores.Expressions.SaveAsync(Expression(id: "ep:other", instanceId: "instance-b"), Token);
        await FlushAsync(Token);

        Assert.Equal(new[] { "ep:a", "ep:b", "ep:z" },
            (await stores.Expressions.ListForCharacterAsync("instance-a", Token))
            .Select(x => x.Id).ToArray());
        Assert.Equal("ep:other", (await stores.Expressions.ListForCharacterAsync("instance-b", Token))
            .Single().Id);
    }

    [Fact]
    public async Task An_object_handed_out_by_an_expression_read_cannot_rewrite_the_store()
    {
        var stores = NewStores();
        await stores.Expressions.SaveAsync(Expression(), Token);

        var handed = (await stores.Expressions.ListForCharacterAsync("instance-a", Token)).Single();
        handed.Suppressed = true;
        handed.Confidence = 0.9;

        await FlushAsync(Token);
        AssertExpressionEqual(Expression(), (await stores.Expressions
            .ListForCharacterAsync("instance-a", Token)).Single());

        var again = await stores.Expressions.ListForCharacterAsync("instance-a", Token);
        Assert.NotSame(handed.AcceptedEvidenceIds, again.Single().AcceptedEvidenceIds);
        Assert.NotSame(handed.AcceptedTurnKeys, again.Single().AcceptedTurnKeys);
    }

    [Fact]
    public async Task Deleting_an_expression_reports_whether_it_was_there()
    {
        var stores = NewStores();
        await stores.Expressions.SaveAsync(Expression(id: "ep:gone"), Token);

        Assert.True(await stores.Expressions.DeleteAsync("ep:gone", Token));
        Assert.False(await stores.Expressions.DeleteAsync("ep:gone", Token));
        Assert.Empty(await stores.Expressions.ListForCharacterAsync("instance-a", Token));
    }

    [Fact]
    public async Task Purging_an_instance_clears_its_expressions_only()
    {
        var stores = NewStores();
        await stores.Expressions.SaveAsync(Expression(id: "ep:a"), Token);
        await stores.Expressions.SaveAsync(Expression(id: "ep:other", instanceId: "instance-b"), Token);

        Assert.Equal(1, await stores.Expressions.PurgeAsync("instance-a", Token));
        await FlushAsync(Token);

        Assert.Empty(await stores.Expressions.ListForCharacterAsync("instance-a", Token));
        Assert.Single(await stores.Expressions.ListForCharacterAsync("instance-b", Token));
        Assert.Equal(0, await stores.Expressions.PurgeAsync("instance-gone", Token));
    }

    [Fact]
    public async Task Explicit_and_ambiguous_feedback_stay_distinguishable_after_a_reload()
    {
        var stores = NewStores();
        await stores.Experiences.AppendAsync(Experience("t-1"), Token);
        await stores.Experiences.AppendAsync(
            Experience("t-2", outcome: InteractionOutcome.Continued, evidence: null,
                explicitFeedback: false), Token);
        await FlushAsync(Token);

        var listed = await stores.Experiences.ListForCharacterAsync("instance-a", 10, Token);
        Assert.Equal(2, listed.Count);
        Assert.All(listed, x => Assert.Equal("topic", x.Scenario));
        Assert.True(listed.Single(x => x.TurnId == "t-1").FeedbackIsExplicit);
        Assert.Equal("fb-9", listed.Single(x => x.TurnId == "t-1").FollowUpEvidence);
        Assert.False(listed.Single(x => x.TurnId == "t-2").FeedbackIsExplicit);
        Assert.Null(listed.Single(x => x.TurnId == "t-2").FollowUpEvidence);
        Assert.Equal(InteractionOutcome.Continued, listed.Single(x => x.TurnId == "t-2").Outcome);
        AssertInstant(Base, listed.Single(x => x.TurnId == "t-2").ObservedAt);
    }

    [Fact]
    public async Task Experiences_come_back_most_recently_appended_first()
    {
        var stores = NewStores();
        // A fixed clock is the normal case in the learning tests and in any store that keeps whole
        // seconds, so ties must fall back on append order rather than on hash or insert luck.
        var at = Base.AddHours(5);
        for (var i = 0; i < 5; i++) await stores.Experiences.AppendAsync(Experience($"t-{i}", at: at), Token);
        await FlushAsync(Token);

        Assert.Equal(new[] { "t-4", "t-3", "t-2", "t-1", "t-0" },
            (await stores.Experiences.ListForCharacterAsync("instance-a", 10, Token))
            .Select(x => x.TurnId).ToArray());
        Assert.Equal(new[] { "t-4", "t-3" }, (await stores.Experiences.ListForCharacterAsync("instance-a", 2, Token))
            .Select(x => x.TurnId).ToArray());
    }

    [Fact]
    public async Task One_busy_character_cannot_evict_another_characters_audit_trail()
    {
        var stores = NewStores();
        var bound = InMemoryBehaviorExperienceRepository.RetainedPerInstance;
        for (var i = 0; i < bound + 5; i++) await stores.Experiences.AppendAsync(Experience($"t-{i}"), Token);
        await stores.Experiences.AppendAsync(Experience("keep", instanceId: "instance-b"), Token);
        await FlushAsync(Token);

        var kept = await stores.Experiences.ListForCharacterAsync("instance-a", bound + 10, Token);
        Assert.Equal(bound, kept.Count);
        // Eviction takes the oldest, so the tail that survives is the most recent window.
        Assert.Equal($"t-{bound + 4}", kept[0].TurnId);
        Assert.Equal($"t-5", kept[^1].TurnId);
        Assert.Equal("keep", (await stores.Experiences.ListForCharacterAsync("instance-b", 10, Token))
            .Single().TurnId);
    }

    [Fact]
    public async Task Purging_an_instance_clears_its_experiences_only()
    {
        var stores = NewStores();
        await stores.Experiences.AppendAsync(Experience("t-1"), Token);
        await stores.Experiences.AppendAsync(Experience("t-2", instanceId: "instance-b"), Token);

        Assert.Equal(1, await stores.Experiences.PurgeAsync("instance-a", Token));
        await FlushAsync(Token);

        Assert.Empty(await stores.Experiences.ListForCharacterAsync("instance-a", 10, Token));
        Assert.Single(await stores.Experiences.ListForCharacterAsync("instance-b", 10, Token));
        Assert.Equal(0, await stores.Experiences.PurgeAsync("instance-gone", Token));
    }
}

/// <summary>
/// The in-process reference implementations are the ones being held to the contract today. This is
/// also the carrier the Persistence Owner inherits from: adding
/// <c>SqliteIntelligenceRepositoryConformanceTests</c> over a real database file is the whole of
/// that side's obligation, and no assertion here may be weakened to make it pass.
/// </summary>
public sealed class InMemoryIntelligenceRepositoryConformanceTests : IntelligenceRepositoryConformanceTests
{
    protected override Stores NewStores() => new()
    {
        States = new InMemoryCharacterStateRepository(),
        Patterns = new InMemoryBehaviorPatternRepository(),
        Experiences = new InMemoryBehaviorExperienceRepository(),
        Expressions = new InMemoryExpressionPatternRepository()
    };
}

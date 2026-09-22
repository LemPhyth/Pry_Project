using Pry.Core.Abstractions;
using Pry.Core.Expression;
using Pry.Core.Intelligence;
using Xunit;

namespace Pry.Core.Tests;

public sealed class LearningSafetyTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 20, 0, 0, 0, TimeSpan.Zero);
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static DeliveredTurn Turn(string id, bool accepted = false) =>
        new("conversation", id, Now, "聊阳台的植物", "我嘞个，居然活过来了")
        {
            FeedbackEvidenceId = accepted ? $"feedback-{id}" : null,
            AcceptedStyles = accepted ? ["我嘞个"] : []
        };

    private static BehaviorExperience Experience(string id, InteractionOutcome outcome, bool explicitFeedback = true) =>
        new("instance", "conversation", id, "topic", ResponseIntent.ContinueTopic, outcome, Now, $"feedback-{id}")
        { FeedbackIsExplicit = explicitFeedback };

    [Theory]
    [InlineData(InteractionOutcome.Continued)]
    [InlineData(InteractionOutcome.TopicShiftedAway)]
    [InlineData(InteractionOutcome.Silenced)]
    [InlineData(InteractionOutcome.Unknown)]
    public async Task Ambiguous_outcomes_do_not_create_or_reinforce_patterns(InteractionOutcome outcome)
    {
        var store = new InMemoryBehaviorPatternRepository();
        var updater = new BehaviorPatternUpdater(store, () => Now);
        Assert.Null(await updater.RecordAsync(Experience("empty", outcome), Token));
        var initial = await updater.RecordAsync(Experience("first", InteractionOutcome.PositiveReaction), Token);
        for (var i = 0; i < 20; i++)
            await updater.RecordAsync(Experience($"t-{i}", outcome), Token);
        var result = Assert.Single(await store.ListForCharacterAsync("instance", Token));
        Assert.Equal(initial!.Confidence, result.Confidence);
        Assert.Equal(1, result.SuccessCount);
        Assert.False(result.HasEvidence);
    }

    [Fact]
    public async Task A_label_without_explicit_follow_up_does_not_teach_behavior()
    {
        var updater = new BehaviorPatternUpdater(new InMemoryBehaviorPatternRepository());
        Assert.Null(await updater.RecordAsync(Experience("t", InteractionOutcome.PositiveReaction, false), Token));
        Assert.Null(await updater.RecordAsync(Experience("t", InteractionOutcome.Rejected) with
            { FollowUpEvidence = null }, Token));
    }

    [Fact]
    public async Task Replayed_feedback_is_folded_once_even_with_concurrent_updaters()
    {
        var store = new InMemoryBehaviorPatternRepository();
        var feedback = Experience("t", InteractionOutcome.PositiveReaction);
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ =>
            new BehaviorPatternUpdater(store).RecordAsync(feedback, Token)));
        var result = Assert.Single(await store.ListForCharacterAsync("instance", Token));
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.UsageCount);
        Assert.Single(result.EvaluatedTurnKeys);
        Assert.Equal(0.55, result.Confidence, 5);
    }

    [Fact]
    public async Task Three_explicit_independent_outcomes_can_inform_the_planner()
    {
        var store = new InMemoryBehaviorPatternRepository();
        var updater = new BehaviorPatternUpdater(store);
        for (var i = 0; i < 3; i++)
            await updater.RecordAsync(Experience($"t-{i}", InteractionOutcome.PositiveReaction), Token);
        Assert.True(Assert.Single(await store.ListForCharacterAsync("instance", Token)).HasEvidence);
    }

    [Fact]
    public async Task Model_repetition_alone_never_becomes_a_habit()
    {
        var store = new InMemoryExpressionPatternRepository();
        var learner = new ExpressionLearner(store, "instance");
        for (var i = 0; i < 40; i++) learner.TryEnqueue(Turn($"t-{i}"));
        Assert.Contains(await learner.DrainAsync(Token), x => x.Outcome == LearningOutcome.RejectedUnconfirmed);
        Assert.Empty(await store.ListForCharacterAsync("instance", Token));
    }

    [Fact]
    public async Task One_feedback_reference_cannot_confirm_multiple_turns()
    {
        var store = new InMemoryExpressionPatternRepository();
        var learner = new ExpressionLearner(store, "instance");
        learner.TryEnqueue(Turn("t1", true));
        learner.TryEnqueue(Turn("t2", true) with { FeedbackEvidenceId = "feedback-t1" });
        await learner.DrainAsync(Token);
        Assert.Empty(await store.ListForCharacterAsync("instance", Token));
    }

    [Fact]
    public async Task Multiple_feedback_events_for_one_turn_are_not_independent_turns()
    {
        var store = new InMemoryExpressionPatternRepository();
        var learner = new ExpressionLearner(store, "instance");
        learner.TryEnqueue(Turn("t1", true));
        learner.TryEnqueue(Turn("t1", true) with { FeedbackEvidenceId = "other-feedback" });
        learner.TryEnqueue(Turn("unconfirmed"));
        await learner.DrainAsync(Token);
        Assert.Empty(await store.ListForCharacterAsync("instance", Token));
    }

    [Fact]
    public async Task Explicit_feedback_is_idempotent_across_learner_recreation()
    {
        var store = new InMemoryExpressionPatternRepository();
        var learner = new ExpressionLearner(store, "instance");
        learner.TryEnqueue(Turn("t1", true));
        learner.TryEnqueue(Turn("t2", true));
        await learner.DrainAsync(Token);
        var before = Assert.Single(await store.ListForCharacterAsync("instance", Token));
        learner = new ExpressionLearner(store, "instance");
        learner.TryEnqueue(Turn("t1", true));
        learner.TryEnqueue(Turn("t2", true));
        Assert.Equal(LearningOutcome.Unchanged, Assert.Single(await learner.DrainAsync(Token)).Outcome);
        var after = Assert.Single(await store.ListForCharacterAsync("instance", Token));
        Assert.Equal(before.Confidence, after.Confidence);
        Assert.Equal(2, after.EvidenceCount);
    }

    [Fact]
    public async Task Correction_suppresses_existing_pattern_without_repeating_it_and_survives_recreation()
    {
        var store = new InMemoryExpressionPatternRepository();
        var learner = new ExpressionLearner(store, "instance");
        learner.TryEnqueue(Turn("t1", true));
        learner.TryEnqueue(Turn("t2", true));
        await learner.DrainAsync(Token);
        learner.TryEnqueue(Turn("correction") with
        {
            DeliveredAssistantText = "好。", RejectedStyles = ["我嘞个"]
        });
        await learner.DrainAsync(Token);
        Assert.True(Assert.Single(await store.ListForCharacterAsync("instance", Token)).Suppressed);

        learner = new ExpressionLearner(store, "instance");
        learner.TryEnqueue(Turn("t3", true));
        learner.TryEnqueue(Turn("t4", true));
        await learner.DrainAsync(Token);
        var patterns = await store.ListForCharacterAsync("instance", Token);
        Assert.True(Assert.Single(patterns).Suppressed);
        Assert.Empty(new ExpressionRetriever().Retrieve(patterns, "轻微吐槽", "我嘞个", Now));
    }

    [Fact]
    public async Task Late_correction_on_an_already_observed_turn_takes_precedence()
    {
        var store = new InMemoryExpressionPatternRepository();
        var learner = new ExpressionLearner(store, "instance");
        learner.TryEnqueue(Turn("t1", true));
        learner.TryEnqueue(Turn("t2", true));
        await learner.DrainAsync(Token);
        learner.TryEnqueue(Turn("t1", true) with { UserAskedToStopTalkingLikeThat = true });
        await learner.DrainAsync(Token);
        Assert.True(Assert.Single(await store.ListForCharacterAsync("instance", Token)).Suppressed);
    }

    [Fact]
    public async Task A_failed_store_write_is_retried_without_another_turn()
    {
        var store = new FailingStore();
        var learner = new ExpressionLearner(store, "instance");
        learner.TryEnqueue(Turn("t1", true));
        learner.TryEnqueue(Turn("t2", true));
        await Assert.ThrowsAsync<IOException>(() => learner.DrainAsync(Token));
        Assert.Equal(0, learner.PendingCount);
        Assert.Equal(LearningOutcome.Created, Assert.Single(await learner.DrainAsync(Token)).Outcome);
        Assert.Equal(2, Assert.Single(await store.ListForCharacterAsync("instance", Token)).EvidenceCount);
    }

    [Fact]
    public async Task A_cancelled_drain_preserves_queued_work()
    {
        var store = new InMemoryExpressionPatternRepository();
        var learner = new ExpressionLearner(store, "instance");
        learner.TryEnqueue(Turn("t1", true));
        learner.TryEnqueue(Turn("t2", true));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => learner.DrainAsync(cancelled.Token));
        Assert.Equal(2, learner.PendingCount);
        await learner.DrainAsync(Token);
        Assert.Single(await store.ListForCharacterAsync("instance", Token));
    }

    [Fact]
    public async Task Concurrent_enqueue_is_bounded_and_concurrent_drains_do_not_duplicate_evidence()
    {
        var store = new InMemoryExpressionPatternRepository();
        var learner = new ExpressionLearner(store, "instance");
        await Parallel.ForEachAsync(Enumerable.Range(0, 512), Token, (i, _) =>
        {
            learner.TryEnqueue(Turn($"t-{i}", true));
            return ValueTask.CompletedTask;
        });
        Assert.Equal(ExpressionLearner.QueueCapacity, learner.PendingCount);
        Assert.Equal(512 - ExpressionLearner.QueueCapacity, learner.DroppedCount);
        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => learner.DrainAsync(Token)));
        Assert.Equal(ExpressionLearner.QueueCapacity,
            Assert.Single(await store.ListForCharacterAsync("instance", Token)).EvidenceCount);
    }

    [Fact]
    public async Task Later_feedback_does_not_count_another_exchange_or_use_assistant_text_as_evidence()
    {
        var state = new CharacterStateService(new IdentitySnapshot
        {
            CharacterId = "character", Name = "普莱", CoreIdentity = "本地角色",
            PersonalityOutline = "安静", SpeechStyleSeed = "短句", UserName = "你"
        }, "user", new InMemoryCharacterStateRepository());
        var patterns = new InMemoryBehaviorPatternRepository();
        var experiences = new InMemoryBehaviorExperienceRepository();
        var expressions = new ExpressionService(new InMemoryExpressionPatternRepository(), "instance");
        var service = new CharacterIntelligenceService(state, expressions, patterns, experiences: experiences);
        var situation = new SituationSnapshot
        {
            CharacterInstanceId = "instance", ConversationId = "conversation", TurnId = "t",
            UserInput = "今天养的植物活过来了", Now = Now
        };
        var decision = new BehaviorDecision { Intent = ResponseIntent.ContinueTopic, ScenarioTag = "topic" };
        var outcome = new TurnOutcome(situation, state.Snapshot(Now), decision, [],
            InteractionOutcome.Unknown, "我嘞个，居然活过来了", Now);
        await service.CompleteTurnAsync(outcome, Token);
        Assert.Null(Assert.Single(await experiences.ListForCharacterAsync("instance", 10, Token)).FollowUpEvidence);
        var before = await state.CurrentRecordAsync();

        Assert.True(await service.ObserveFeedbackAsync(outcome with
        {
            Outcome = InteractionOutcome.PositiveReaction, FeedbackIsExplicit = true,
            FollowUpEvidence = "feedback-message-id", ExpressionFeedbackId = "feedback-message-id",
            AcceptedExpressionStyles = ["我嘞个"]
        }, Token));
        Assert.Equal(before, await state.CurrentRecordAsync());
        Assert.Equal(1, Assert.Single(await patterns.ListForCharacterAsync("instance", Token)).SuccessCount);
        Assert.Contains(await experiences.ListForCharacterAsync("instance", 10, Token),
            x => x.FollowUpEvidence == "feedback-message-id");
    }

    [Fact]
    public async Task Correction_blocks_authored_variants_with_a_different_tone()
    {
        var store = new InMemoryExpressionPatternRepository();
        await store.SaveAsync(new ExpressionPattern
        {
            Id = "authored", CharacterInstanceId = "instance", Situation = "轻微吐槽", Style = "我嘞个",
            Source = ExpressionPatternSource.Authored, Tone = ExpressionTone.Neutral, Confidence = 0.7
        }, Token);
        var learner = new ExpressionLearner(store, "instance");
        learner.TryEnqueue(Turn("correction") with { DeliveredAssistantText = "好。", RejectedStyles = ["我嘞个"] });
        await learner.DrainAsync(Token);
        Assert.All(await store.ListForCharacterAsync("instance", Token), x => Assert.True(x.Suppressed));
    }

    [Fact]
    public async Task Replay_guard_stays_bounded_over_a_long_history()
    {
        var store = new InMemoryBehaviorPatternRepository();
        var updater = new BehaviorPatternUpdater(store, () => Now);
        for (var i = 0; i < 200; i++)
            await updater.RecordAsync(Experience($"t-{i}", InteractionOutcome.PositiveReaction), Token);
        var result = Assert.Single(await store.ListForCharacterAsync("instance", Token));
        Assert.Equal(200, result.SuccessCount);
        Assert.Equal(0.85, result.Confidence, 5);
        Assert.Equal(BehaviorPattern.EvaluatedTurnKeyWindow, result.EvaluatedTurnKeys.Count);
    }

    [Fact]
    public async Task A_late_approval_still_counts_after_the_occurrence_tally_saturates()
    {
        var store = new InMemoryExpressionPatternRepository();
        var learner = new ExpressionLearner(store, "instance");
        for (var i = 0; i < 60; i++) learner.TryEnqueue(Turn($"t-{i}"));
        await learner.DrainAsync(Token);
        Assert.Empty(await store.ListForCharacterAsync("instance", Token));

        learner.TryEnqueue(Turn("late", accepted: true));
        learner.TryEnqueue(Turn("later", accepted: true));
        await learner.DrainAsync(Token);
        Assert.Equal(2, Assert.Single(await store.ListForCharacterAsync("instance", Token)).EvidenceCount);
    }

    [Fact]
    public async Task Feedback_without_any_new_evidence_does_not_duplicate_the_delivery_record()
    {
        var state = new CharacterStateService(new IdentitySnapshot
        {
            CharacterId = "character", Name = "普莱", CoreIdentity = "本地角色",
            PersonalityOutline = "安静", SpeechStyleSeed = "短句", UserName = "你"
        }, "user", new InMemoryCharacterStateRepository());
        var experiences = new InMemoryBehaviorExperienceRepository();
        var service = new CharacterIntelligenceService(state,
            new ExpressionService(new InMemoryExpressionPatternRepository(), "instance"),
            new InMemoryBehaviorPatternRepository(), experiences: experiences);
        var outcome = new TurnOutcome(new SituationSnapshot
        {
            CharacterInstanceId = "instance", ConversationId = "conversation", TurnId = "t",
            UserInput = "今天养的植物活过来了", Now = Now
        }, state.Snapshot(Now),
            new BehaviorDecision { Intent = ResponseIntent.ContinueTopic, ScenarioTag = "topic" }, [],
            InteractionOutcome.Unknown, "我嘞个，居然活过来了", Now);
        await service.CompleteTurnAsync(outcome, Token);

        Assert.True(await service.ObserveFeedbackAsync(outcome with { Outcome = InteractionOutcome.Continued }, Token));
        Assert.Single(await experiences.ListForCharacterAsync("instance", 10, Token));
    }

    private sealed class FailingStore : IExpressionPatternRepository
    {
        private readonly InMemoryExpressionPatternRepository _inner = new();
        private bool _fail = true;
        public Task<IReadOnlyList<ExpressionPattern>> ListForCharacterAsync(string characterInstanceId,
            CancellationToken cancellationToken = default) => _inner.ListForCharacterAsync(characterInstanceId, cancellationToken);
        public Task SaveAsync(ExpressionPattern pattern, CancellationToken cancellationToken = default)
        {
            if (_fail) { _fail = false; throw new IOException("simulated store failure"); }
            return _inner.SaveAsync(pattern, cancellationToken);
        }
        public Task<bool> DeleteAsync(string expressionPatternId, CancellationToken cancellationToken = default) =>
            _inner.DeleteAsync(expressionPatternId, cancellationToken);
    }
}

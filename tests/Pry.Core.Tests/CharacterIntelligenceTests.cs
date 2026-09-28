using System.Text.Json;
using Pry.Contracts;
using Pry.Core.Abstractions;
using Pry.Core.Configuration;
using Pry.Core.Expression;
using Pry.Core.Intelligence;
using Pry.Core.Models;
using Pry.Core.Prompting;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// Fixed Behavior eval cases from PROJECT_PLAN 7.1 plus the reference-project failure modes.
/// These assert decisions and gates, not wording, so they stay model-independent.
/// </summary>
public sealed class CharacterIntelligenceTests
{
    private static readonly DateTimeOffset T = new(2026, 9, 20, 10, 0, 0, TimeSpan.FromHours(8));
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static readonly IdentitySnapshot Identity = new()
    {
        CharacterId = "author-character", Name = "普莱", CoreIdentity = "本地角色",
        PersonalityOutline = "安静但话多", SpeechStyleSeed = "短句", UserName = "你"
    };

    private static SituationSnapshot At(string input, Action<Build>? adjust = null)
    {
        var build = new Build();
        adjust?.Invoke(build);
        return new SituationSnapshot
        {
            CharacterInstanceId = "author-character", ConversationId = "conv-1", TurnId = "turn-1",
            UserInput = input, Now = T, Memory = build.Memory, RecentMessages = build.Messages,
            RequiresReply = build.RequiresReply, ConsecutiveLowContentInputs = build.LowContentStreak,
            AvailableCapabilityNames = build.Capabilities, SinceLastAssistantReply = build.SinceLastAssistantReply
        };
    }

    private static CharacterStateSnapshot In(RelationshipState? relationship = null, MoodState? mood = null) =>
        CharacterStateSnapshot.Freeze(Identity, relationship ?? new RelationshipState(), mood ?? new MoodState(), T);

    private static RelationshipState Familiar() => new() { Familiarity = 0.6, Trust = 0.6 };
    private static RelationshipState Close() => new() { Familiarity = 0.8, Trust = 0.75, Closeness = 0.6 };

    private static ChatMessage Said(string text, ChatRole role = ChatRole.Assistant, int minutesAgo = 0) =>
        new(1, "conv-1", role, text, T.AddMinutes(-minutesAgo));

    private static void CloseTo(double expected, double actual) =>
        Assert.True(Math.Abs(expected - actual) < 1e-6, $"expected {expected}, got {actual}");

    private sealed class Build
    {
        public RelevantMemoryView Memory = RelevantMemoryView.Empty();
        public IReadOnlyList<ChatMessage> Messages = [];
        public bool RequiresReply;
        public int LowContentStreak { get; set; }
        public IReadOnlyList<string> Capabilities = [];
        public TimeSpan? SinceLastAssistantReply { get; set; }
    }

    // ------------------------------------------------------------ 沉默也是决策

    [Fact]
    public void Low_content_right_after_a_reply_is_silence()
    {
        var decision = new BehaviorPlanner().Plan(
            At("草", b => b.SinceLastAssistantReply = TimeSpan.FromSeconds(5)), In());
        Assert.Equal(ResponseIntent.DoNothing, decision.Intent);
        Assert.Equal(0, decision.RelativeLengthBudget);
    }

    [Fact]
    public void Low_content_from_someone_she_hardly_knows_is_silence()
    {
        Assert.Equal(ResponseIntent.DoNothing, new BehaviorPlanner().Plan(At("嗯"), In()).Intent);
    }

    [Fact]
    public void Runtime_can_require_a_reply_but_keeps_it_short()
    {
        var decision = new BehaviorPlanner().Plan(
            At("嗯", b =>
            {
                b.RequiresReply = true;
                b.SinceLastAssistantReply = TimeSpan.FromSeconds(5);
            }), In());
        Assert.Equal(ResponseIntent.Acknowledge, decision.Intent);
        Assert.Contains("runtime:reply_required", decision.Gates);
        Assert.True(decision.RelativeLengthBudget <= 0.5);
    }

    // ------------------------------------------------------------ 不过度提问

    [Fact]
    public void Two_asking_turns_in_a_row_end_the_question_run()
    {
        var decision = new BehaviorPlanner().Plan(At("今天在公司忙了一整天", b => b.Messages =
            [Said("你吃饭了没？", minutesAgo: 6), Said("今天累不累？", minutesAgo: 3)]), In(Familiar()));
        Assert.Equal(0, decision.QuestionBudget);
        Assert.NotEqual(ResponseIntent.Ask, decision.Intent);
        Assert.Contains("behavior:question_streak", decision.Gates);
    }

    [Fact]
    public void Answering_a_question_does_not_bounce_one_back()
    {
        var decision = new BehaviorPlanner().Plan(At("你一般几点睡觉？"), In(Familiar()));
        Assert.Equal(ResponseIntent.ContinueTopic, decision.Intent);
        Assert.Equal(0, decision.QuestionBudget);
    }

    [Fact]
    public void Curiosity_is_allowed_once_when_nothing_is_outstanding()
    {
        var decision = new BehaviorPlanner().Plan(At("周末我把阳台那盆快死的薄荷救回来了"), In(Familiar()));
        Assert.Equal(ResponseIntent.Ask, decision.Intent);
        Assert.Equal(1, decision.QuestionBudget);
    }

    [Fact]
    public void Ambiguity_is_clarified_once_and_then_stops()
    {
        Assert.Equal(ResponseIntent.Clarify, new BehaviorPlanner().Plan(At("这个不太一样"), In(Familiar())).Intent);

        var afterQuestion = new BehaviorPlanner().Plan(At("这个不太一样", b => b.Messages =
            [Said("哪个不太一样？", minutesAgo: 1)]), In(Familiar()));
        Assert.NotEqual(ResponseIntent.Clarify, afterQuestion.Intent);
    }

    // ------------------------------------------------------------ 不客服化 / 不过度安慰

    [Fact]
    public void Low_mood_without_a_request_is_met_not_managed()
    {
        var decision = new BehaviorPlanner().Plan(At("今天开了一天会，现在好累"), In(Familiar()));
        Assert.Equal(ResponseIntent.Acknowledge, decision.Intent);
        Assert.Equal(ExpressionRegister.Warm, decision.Register);
        Assert.False(decision.ParaphraseAllowed);
    }

    [Fact]
    public void An_explicit_request_for_company_gets_comfort()
    {
        var decision = new BehaviorPlanner().Plan(At("今天真的好累，你能不能陪陪我"), In(Familiar()));
        Assert.Equal(ResponseIntent.Comfort, decision.Intent);
        Assert.Equal(ExpressionRegister.Warm, decision.Register);
    }

    [Fact]
    public async Task Service_tone_risk_is_named_as_an_avoidance()
    {
        var (service, _, _, _) = Compose();
        var context = await service.DecideAsync(At("今天开了一天会，现在好累"), In(Familiar()), Token);
        Assert.Contains(context.Avoidances, x => x.Contains("客服式套话"));
    }

    // ------------------------------------------------------------ 关系边界

    [Fact]
    public void Tease_stays_locked_until_they_are_familiar()
    {
        var decision = new BehaviorPlanner().Plan(At("就这？你行不行啊"), In());
        Assert.Equal(ResponseIntent.Acknowledge, decision.Intent);
        Assert.Contains("relationship:tease_locked", decision.Gates);
    }

    [Fact]
    public void Familiarity_unlocks_natural_banter_without_a_scorewall()
    {
        var decision = new BehaviorPlanner().Plan(At("就这？你行不行啊"), In(Familiar()));
        Assert.Equal(ResponseIntent.Tease, decision.Intent);
        Assert.Equal(HabitToneAllowed.Any, decision.HabitToneAllowed);
    }

    [Fact]
    public void Hostility_scales_from_pushback_to_refusal()
    {
        Assert.Equal(ResponseIntent.PushBack, new BehaviorPlanner().Plan(At("滚"), In(Familiar())).Intent);
        Assert.Equal(ResponseIntent.Refuse,
            new BehaviorPlanner().Plan(At("滚"), In(new RelationshipState
            { Familiarity = 0.6, Trust = 0.6, BoundaryFriction = 0.5 })).Intent);
    }

    [Fact]
    public void A_framing_correction_is_accepted_without_a_second_explanation()
    {
        var decision = new BehaviorPlanner().Plan(At("不是这个意思，我说的是昨天"), In(Familiar()));
        Assert.Equal(ResponseIntent.Acknowledge, decision.Intent);
        Assert.Equal(ExpressionRegister.Serious, decision.Register);
        Assert.Equal(HabitToneAllowed.None, decision.HabitToneAllowed);
    }

    [Fact]
    public void Correcting_the_relationship_is_reported_as_a_state_request()
    {
        var decision = new BehaviorPlanner().Plan(At("我们只是朋友，别叫我宝贝"), In(Close()));
        Assert.NotNull(decision.RequestedRelationshipCorrection);
        Assert.False(decision.RequestedRelationshipCorrection!.Nearer);
    }

    [Fact]
    public async Task A_relationship_correction_event_moves_the_signals_in_both_directions()
    {
        var state = new CharacterStateService(Identity, "user-1", new InMemoryCharacterStateRepository());
        await state.NoteExchangeAsync(T.AddMinutes(-30), cancellationToken: Token);
        var before = (await state.CurrentRecordAsync()).Closeness;

        await state.NoteRelationshipCorrectedAsync(T, closer: false, cancellationToken: Token);
        var afterFar = (await state.CurrentRecordAsync()).Closeness;
        Assert.True(afterFar < before);

        await state.NoteRelationshipCorrectedAsync(T.AddMinutes(1), closer: true, cancellationToken: Token);
        Assert.True((await state.CurrentRecordAsync()).Closeness > afterFar);
    }

    [Fact]
    public async Task Relationship_state_survives_a_restart_and_rejects_a_stale_write()
    {
        var repository = new InMemoryCharacterStateRepository();
        var state = new CharacterStateService(Identity, "user-1", repository);
        await state.NoteExchangeAsync(T, MoodSignal.InterestingTopic, 0.1, Token);
        var saved = await repository.LoadAsync("author-character", "user-1", Token);
        Assert.NotNull(saved);

        var restored = new CharacterStateService(Identity, "user-1", repository);
        Assert.True(await restored.RestoreAsync(Token));
        var again = await restored.CurrentRecordAsync();
        Assert.Equal(saved!.Interest, again.Interest);
        Assert.Equal(saved.SharedContextStrength, again.SharedContextStrength);
        Assert.Equal(saved.ExchangeCount, again.ExchangeCount);

        Assert.False(await repository.SaveAsync(saved with { StateVersion = saved.StateVersion - 1 }, Token));
    }

    // ------------------------------------------------------------ Mood 只能代表她自己

    [Fact]
    public void Mood_has_no_place_to_store_a_user_emotion()
    {
        Assert.All(typeof(MoodState).GetProperties(),
            property => Assert.DoesNotContain("User", property.Name, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Unreliable_affect_readings_are_dropped_and_reliable_ones_stay_bounded()
    {
        var state = new CharacterStateService(Identity, "user-1", new InMemoryCharacterStateRepository());
        var before = await state.CurrentRecordAsync();

        Assert.False(await state.ReactToUserAffectAsync(new UserAffectEstimate(PerceivedAffect.Down, 0.2, T), Token));
        CloseTo(before.Valence, (await state.CurrentRecordAsync()).Valence);

        Assert.True(await state.ReactToUserAffectAsync(new UserAffectEstimate(PerceivedAffect.Down, 0.9, T), Token));
        var after = await state.CurrentRecordAsync();
        Assert.True(before.Valence - after.Valence <= 0.08);
        CloseTo(before.Trust, after.Trust);
        CloseTo(before.Familiarity, after.Familiarity);
    }

    [Fact]
    public void Mood_drifts_back_to_baseline_instead_of_persisting()
    {
        var mood = new MoodState { Interest = 1, Valence = 0.9, UpdatedAt = T };
        mood.DriftTowardBaseline(TimeSpan.FromHours(4));
        Assert.True(mood.Interest < 0.35);
        Assert.True(Math.Abs(mood.Valence - Identity.Baseline.Valence) < 0.3);
    }

    [Fact]
    public void Tired_and_irritated_stops_banter()
    {
        var decision = new BehaviorPlanner().Plan(At("就这？你行不行啊"),
            In(Familiar(), new MoodState { Fatigue = 0.9, Irritation = 0.7, Valence = -0.4 }));
        Assert.NotEqual(ResponseIntent.Tease, decision.Intent);
        Assert.NotEqual(HabitToneAllowed.Any, decision.HabitToneAllowed);
    }

    // ------------------------------------------------------------ 学习的稳定性

    [Fact]
    public async Task One_bad_night_cannot_rewrite_how_she_behaves()
    {
        var pattern = await new BehaviorPatternUpdater(new InMemoryBehaviorPatternRepository(), () => T)
            .RecordAsync(new BehaviorExperience("author-character", "conv-1", "t1", "TopicContinuation",
                ResponseIntent.Tease, InteractionOutcome.Rejected, T, "feedback-1") { FeedbackIsExplicit = true }, Token);

        Assert.NotNull(pattern);
        CloseTo(0.45, pattern!.Confidence);
        Assert.Equal(1, pattern.UsageCount);
    }

    [Fact]
    public async Task Repeated_failure_floors_instead_of_collapsing()
    {
        var store = new InMemoryBehaviorPatternRepository();
        var updater = new BehaviorPatternUpdater(store, () => T);
        BehaviorPattern? pattern = null;
        for (var i = 0; i < 40; i++)
            pattern = await updater.RecordAsync(new BehaviorExperience("author-character", "conv-1", $"t{i}",
                "TopicContinuation", ResponseIntent.Tease, InteractionOutcome.Rejected, T.AddMinutes(i), $"feedback-{i}")
                { FeedbackIsExplicit = true }, Token);

        Assert.NotNull(pattern);
        Assert.True(pattern!.Confidence >= 0.05);
        Assert.Equal(40, pattern.UsageCount);
        Assert.Equal(40, pattern.FailureCount);
    }

    [Fact]
    public async Task An_ambiguous_outcome_is_never_invented_into_a_signal()
    {
        var store = new InMemoryBehaviorPatternRepository();
        Assert.Null(await new BehaviorPatternUpdater(store, () => T).RecordAsync(
            new BehaviorExperience("author-character", "conv-1", "t1", "LowContent",
                ResponseIntent.DoNothing, InteractionOutcome.Unknown, T), Token));
        Assert.Empty(await store.ListForCharacterAsync("author-character", Token));
    }

    [Fact]
    public void Thin_history_is_reference_only_and_never_overrides_a_gate()
    {
        var thin = new BehaviorPattern
        {
            BehaviorPatternId = "bp-1", CharacterInstanceId = "author-character", Scenario = "PlayfulProvoke",
            Strategy = ResponseIntent.Tease, UsageCount = 2, Confidence = 0.8
        };
        var decision = new BehaviorPlanner().Plan(At("就这？你行不行啊"), In(), [thin]);
        Assert.Equal(ResponseIntent.Acknowledge, decision.Intent);
        Assert.Contains("relationship:tease_locked", decision.Gates);
        Assert.False(thin.HasEvidence);
        Assert.True(thin with { SuccessCount = BehaviorPattern.MinimumObservationsToInfluence } is { HasEvidence: true });
        Assert.Empty(new BehaviorPatternRetriever().Retrieve([thin], "LowContent"));
        Assert.Single(new BehaviorPatternRetriever().Retrieve([thin], "PlayfulProvoke"));
    }

    [Fact]
    public void A_proven_pattern_can_break_a_low_confidence_tie()
    {
        var pattern = new BehaviorPattern
        {
            BehaviorPatternId = "bp-2", CharacterInstanceId = "author-character", Scenario = "Unknown",
            Strategy = ResponseIntent.Share, UsageCount = 6, SuccessCount = 6, Confidence = 0.7
        };
        var decision = new BehaviorPlanner().Plan(At("就那样"), In(Familiar()), [pattern]);
        Assert.Equal(ResponseIntent.Share, decision.Intent);
        Assert.Contains(decision.Rationale, x => x.Contains("历史经验"));
    }

    [Fact]
    public async Task Cancelled_output_teaches_nothing_at_all()
    {
        var (service, expressions, patterns, experiences) = Compose();
        var situation = At("周末我把阳台那盆快死的薄荷救回来了");
        var state = In(Familiar());
        var context = await service.DecideAsync(situation, state, Token);

        await service.CompleteTurnAsync(new TurnOutcome(situation, state, context.Decision, [],
            InteractionOutcome.PositiveReaction, "我嘞个，居然活过来了", T) { DeliveredToUser = false }, Token);

        Assert.Equal(0, expressions.Learner.PendingCount);
        Assert.Empty(await patterns.ListForCharacterAsync("author-character", Token));
        Assert.Empty(await experiences.ListForCharacterAsync("author-character", 10, Token));
        Assert.Equal(0, service.State.Relationship.ExchangeCount);
    }

    [Fact]
    public async Task A_delivered_turn_feeds_experience_usage_and_the_background_learner()
    {
        var (service, expressions, patterns, experiences) = Compose();
        var situation = At("周末我把阳台那盆快死的薄荷救回来了");
        var state = In(Familiar());
        var context = await service.DecideAsync(situation, state, Token);

        await service.CompleteTurnAsync(new TurnOutcome(situation, state, context.Decision,
            [new ExpressionHint("ep-x", "轻微吐槽", "我嘞个", ExpressionTone.Playful, 0.5)],
            InteractionOutcome.PositiveReaction, "我嘞个，居然活过来了", T)
            { FeedbackIsExplicit = true, FollowUpEvidence = "explicit-feedback-1" }, Token);

        Assert.Equal(1, expressions.Learner.PendingCount);
        Assert.Single(await experiences.ListForCharacterAsync("author-character", 10, Token));
        Assert.Equal(1, (await patterns.ListForCharacterAsync("author-character", Token)).Single().UsageCount);
        Assert.Equal(1, service.State.Relationship.ExchangeCount);
    }

    // ------------------------------------------------------------ Capability 占位

    [Fact]
    public void An_action_request_becomes_a_placeholder_only_when_something_is_registered()
    {
        var registered = new BehaviorPlanner().Plan(
            At("帮我打开应用，放点音乐", b => b.Capabilities = ["打开应用"]), In(Familiar()));
        Assert.Equal(ResponseIntent.UseCapability, registered.Intent);
        Assert.Equal("打开应用", registered.CapabilityHint);

        var nothing = new BehaviorPlanner().Plan(At("帮我打开应用，放点音乐"), In(Familiar()));
        Assert.Equal(ResponseIntent.ContinueTopic, nothing.Intent);
        Assert.Contains("capability:none_registered", nothing.Gates);
    }

    // ------------------------------------------------------------ 记忆契约的降级语义

    [Fact]
    public void Empty_and_failed_lookups_stay_different()
    {
        Assert.True(RelevantMemoryView.Empty().MayClaimNoHistory);
        Assert.False(RelevantMemoryView.Unavailable().MayClaimNoHistory);
        Assert.True(RelevantMemoryView.Unavailable().IsDegraded);
        Assert.DoesNotContain("memory:unavailable_do_not_claim_no_history",
            new BehaviorPlanner().Plan(At("我上次说的那家店"), In(Familiar())).Gates);
    }

    [Fact]
    public void Vector_degradation_is_still_usable_but_is_tracked()
    {
        var degraded = new RelevantMemoryView
        {
            Outcome = MemoryQueryOutcome.DegradedKeywordOnly,
            Observations = [new MemoryObservation(7, "用户养了一只白猫叫雪球", "fact", 0.8, T, 12)]
        };
        Assert.False(degraded.IsDegraded);
        Assert.False(degraded.MayClaimNoHistory);

        var decision = new BehaviorPlanner().Plan(
            At("雪球今天特别闹", b => b.Memory = degraded), In(Familiar()));
        Assert.Contains("memory:keyword_only", decision.Gates);
        Assert.DoesNotContain("memory:unavailable_do_not_claim_no_history", decision.Gates);
    }

    [Fact]
    public async Task A_degraded_lookup_becomes_an_avoidance_not_a_confident_denial()
    {
        var (service, _, _, _) = Compose();
        var situation = At("上次那家店叫什么来着", b => b.Memory = RelevantMemoryView.Unavailable());
        var context = await service.DecideAsync(situation, In(Familiar()), Token);

        Assert.True(context.MemoryDegraded);
        Assert.Contains("memory:unavailable_do_not_claim_no_history", context.Decision.Gates);
        Assert.Contains(context.Avoidances, x => x.Contains("不要声称用户从未提过"));
    }

    [Fact]
    public async Task A_failing_memory_source_is_not_silently_swallowed_as_empty()
    {
        var source = new FuncMemorySource((_, _, _, _) =>
            Task.FromException<IReadOnlyList<MemoryRecord>>(new InvalidOperationException("db busy")));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            source.RetrieveAsync("author-character", "user-1", "猫", 4, Token));
    }

    // ------------------------------------------------------------ Developer Mode 可见性

    [Fact]
    public void Every_decision_explains_itself_and_leaks_no_numbers_into_the_prompt()
    {
        var state = In(new RelationshipState { Familiarity = 0.73, Trust = 0.68, Closeness = 0.44 });
        var decision = new BehaviorPlanner().Plan(At("就这？你行不行啊"), state);
        Assert.NotEmpty(decision.Rationale);
        Assert.NotEmpty(new BehaviorPlanner().Plan(At("就这？你行不行啊"), In()).Gates);

        var rendered = ReplyGenerationRenderer.Render(new ReplyGenerationContext
        {
            TurnId = "turn-1", Decision = decision, StateVersion = 3, Mood = new MoodSummary("平静")
        }, Identity);
        Assert.DoesNotContain("0.73", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("Familiarity", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("信任", rendered, StringComparison.Ordinal);
        Assert.Contains("这一轮：", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void The_shipped_card_prompt_never_hands_the_model_a_numeric_affinity()
    {
        var prompt = new PromptBuilder().Build(new PromptContext(
            new CharacterDefinition { Id = "c", Name = "星", Identity = "陪伴者", Personality = "温柔", SpeechStyle = "简洁" },
            new RuntimeState { Mood = "开心" }, [], [], [], "你好", null));
        Assert.Contains("开心", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("信任", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("熟悉度", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("/100", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void The_card_format_carries_no_numeric_affinity_field_to_leak()
    {
        // 上一条守的是"不进提示词"，这条守的是源头：卡片格式里没有可供打分的亲密刻度。
        // 关系深浅只在 RelationshipState 内部按各自的速率变化，渲染成中文，不回流成卡片上的分数。
        Assert.Null(typeof(RuntimeState).GetProperty("Trust"));
        Assert.Null(typeof(RuntimeState).GetProperty("Familiarity"));
        Assert.DoesNotContain(typeof(RuntimeState).GetProperties(), p =>
            p.PropertyType == typeof(int) || p.PropertyType == typeof(double) || p.PropertyType == typeof(float));
    }

    [Fact]
    public async Task A_card_saved_while_the_affinity_fields_existed_still_loads_and_still_shows_no_numbers()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pry-legacy-card-{Guid.NewGuid():N}.json");
        var character = new CharacterDefinition
        {
            Id = "c", Name = "星", Identity = "陪伴者", Personality = "温柔", SpeechStyle = "简洁"
        };
        var json = JsonSerializer.Serialize(character, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            .Replace("\"initialState\":{", "\"initialState\":{\"trust\":91,\"familiarity\":88,");
        try
        {
            await File.WriteAllTextAsync(path, json, Token);
            var loaded = await JsonConfiguration.LoadAsync<CharacterDefinition>(path, Token);
            JsonConfiguration.Validate(loaded);
            Assert.Equal("日常空间", loaded.InitialState.Location);
            Assert.DoesNotContain("91",
                new PromptBuilder().Build(new PromptContext(loaded, loaded.InitialState, [], [], [], "你好", null)),
                StringComparison.Ordinal);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void The_renderer_drops_optional_colour_before_it_breaks_its_budget()
    {
        var decision = new BehaviorPlanner().Plan(At("就这？你行不行啊"), In(Familiar()));
        var context = new ReplyGenerationContext
        {
            TurnId = "turn-1", Decision = decision, StateVersion = 1, Mood = new MoodSummary("心情不错"),
            ExpressionHints = [new ExpressionHint("ep-1", "轻微吐槽", "我嘞个", ExpressionTone.Playful, 0.6)]
        };
        var full = ReplyGenerationRenderer.Render(context, Identity);
        var tight = ReplyGenerationRenderer.Render(context, Identity, 40);

        Assert.Contains("我嘞个", full, StringComparison.Ordinal);
        Assert.DoesNotContain("我嘞个", tight, StringComparison.Ordinal);
        Assert.Contains("这一轮：", tight, StringComparison.Ordinal);
        Assert.True(tight.Length <= full.Length);
    }

    private static (CharacterIntelligenceService, ExpressionService, IBehaviorPatternRepository,
        IBehaviorExperienceRepository) Compose()
    {
        var state = new CharacterStateService(Identity, "user-1", new InMemoryCharacterStateRepository());
        var expressions = new ExpressionService(new InMemoryExpressionPatternRepository(), "author-character");
        var patterns = new InMemoryBehaviorPatternRepository();
        var experiences = new InMemoryBehaviorExperienceRepository();
        return (new CharacterIntelligenceService(state, expressions, patterns, experiences: experiences),
            expressions, patterns, experiences);
    }
}

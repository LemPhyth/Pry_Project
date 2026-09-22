using Pry.Core.Abstractions;
using Pry.Core.Expression;
using Pry.Core.Intelligence;
using Pry.Core.Models;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// Expression eval cases: a habit may be recalled, declined, or simply not used this time.
/// Retrieval is capped, the selector must be able to return nothing, and learning only accepts
/// repeated character-side evidence.
/// </summary>
public sealed class ExpressionSystemTests
{
    private static readonly DateTimeOffset T = new(2026, 9, 20, 10, 0, 0, TimeSpan.FromHours(8));
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static readonly IdentitySnapshot Identity = new()
    {
        CharacterId = "author-character", Name = "普莱", CoreIdentity = "本地角色",
        PersonalityOutline = "安静", SpeechStyleSeed = "短句", UserName = "你"
    };

    private static CharacterStateSnapshot In(RelationshipState? relationship = null, MoodState? mood = null) =>
        CharacterStateSnapshot.Freeze(Identity, relationship ?? new RelationshipState(), mood ?? new MoodState(), T);

    private static RelationshipState Familiar() => new() { Familiarity = 0.6, Trust = 0.6 };

    private static SituationSnapshot At(string input, IReadOnlyList<ChatMessage>? messages = null) =>
        new()
        {
            CharacterInstanceId = "author-character", ConversationId = "conv-1", TurnId = "turn-1",
            UserInput = input, Now = T, RecentMessages = messages ?? []
        };

    private static BehaviorDecision Decide(HabitToneAllowed tone, string scenario = "轻微吐槽",
        ExpressionRegister register = ExpressionRegister.Neutral) => new()
    {
        Intent = ResponseIntent.ContinueTopic, ScenarioTag = scenario, HabitToneAllowed = tone, Register = register
    };

    private static ExpressionPattern Habit(string style, ExpressionTone tone = ExpressionTone.Playful,
        double confidence = 0.6, DateTimeOffset? lastUsed = null, long evidence = 4, bool suppressed = false) => new()
    {
        Id = $"ep-{style}-{tone}", CharacterInstanceId = "author-character", Situation = "轻微吐槽",
        Style = style, Tone = tone, Confidence = confidence, EvidenceCount = evidence,
        LastUsedAt = lastUsed, Suppressed = suppressed, CreatedAt = T.AddDays(-9), UpdatedAt = T.AddDays(-9)
    };

    private static ScoredExpression Candidate(ExpressionPattern pattern, double score = 0.5) =>
        new(pattern, score, "test");

    // ------------------------------------------------------------ Selector 可以全部拒绝

    [Fact]
    public void The_selector_is_allowed_to_use_none_of_them()
    {
        var selection = new ExpressionSelector().Select(At("草"), In(Familiar()),
            Decide(HabitToneAllowed.Any), [Candidate(Habit("我嘞个", confidence: 0.2))]);
        Assert.True(selection.ChoseNothing);
        Assert.Contains(selection.Rejections, x => x.StartsWith("low_confidence"));
    }

    [Fact]
    public void A_serious_scene_blocks_every_catchphrase()
    {
        var selection = new ExpressionSelector().Select(At("不是这个意思"), In(Familiar()),
            Decide(HabitToneAllowed.None, register: ExpressionRegister.Serious),
            [Candidate(Habit("我嘞个")), Candidate(Habit("笑不活"))]);
        Assert.True(selection.ChoseNothing);
        Assert.Contains("selector:register_Serious_blocks_habits", selection.Rejections);
    }

    [Fact]
    public void Warm_scenes_keep_plain_habits_but_drop_the_joke_ones()
    {
        var candidates = new[] { Candidate(Habit("我嘞个")), Candidate(Habit("好嘞", ExpressionTone.Neutral)) };
        var selection = new ExpressionSelector().Select(At("今天好累"), In(Familiar()),
            Decide(HabitToneAllowed.GroundedOnly), candidates);

        Assert.Single(selection.Hints);
        Assert.Equal("好嘞", selection.Hints[0].Style);
        Assert.Contains("tone_blocked:ep-我嘞个-Playful", selection.Rejections);
    }

    [Fact]
    public void At_most_one_habit_is_offered_per_reply()
    {
        var selection = new ExpressionSelector().Select(At("草"), In(Familiar()), Decide(HabitToneAllowed.Any),
            [Candidate(Habit("我嘞个"), 0.9), Candidate(Habit("笑不活"), 0.8), Candidate(Habit("啊这"), 0.7)]);
        Assert.Single(selection.Hints);
        Assert.Contains("selector:budget_1", selection.Rejections);
    }

    // ------------------------------------------------------------ 不会每句话都用

    [Fact]
    public void A_freshly_used_habit_is_cooling_down()
    {
        var selection = new ExpressionSelector().Select(At("草"), In(Familiar()), Decide(HabitToneAllowed.Any),
            [Candidate(Habit("我嘞个", lastUsed: T.AddMinutes(-3)))]);
        Assert.True(selection.ChoseNothing);
        Assert.Contains(selection.Rejections, x => x.StartsWith("cooldown:"));
    }

    [Fact]
    public void An_old_habit_can_be_recalled_without_being_forced()
    {
        var selection = new ExpressionSelector().Select(At("草"), In(Familiar()), Decide(HabitToneAllowed.Any),
            [Candidate(Habit("我嘞个", lastUsed: T.AddHours(-6)))]);
        Assert.Single(selection.Hints);
    }

    [Fact]
    public void A_habit_she_just_used_in_her_own_reply_is_not_repeated()
    {
        var recent = new[] { Said("我嘞个，居然活过来了", minutesAgo: 2), Said("真的假的", ChatRole.User, 3) };
        var selection = new ExpressionSelector().Select(At("后面又养了一盆", recent), In(Familiar()),
            Decide(HabitToneAllowed.Any), [Candidate(Habit("我嘞个"))]);

        Assert.True(selection.ChoseNothing);
        Assert.Contains(selection.Rejections, x => x.StartsWith("already_recent:"));
    }

    [Fact]
    public void Low_energy_suppresses_playful_habits()
    {
        var selection = new ExpressionSelector().Select(At("草"),
            In(Familiar(), new MoodState { Valence = -0.6, Irritation = 0.8 }),
            Decide(HabitToneAllowed.Any), [Candidate(Habit("我嘞个"))]);
        Assert.True(selection.ChoseNothing);
        Assert.Contains("mood_blocked:ep-我嘞个-Playful", selection.Rejections);
    }

    [Fact]
    public void Shallow_relationship_does_not_borrow_intimate_or_joking_forms()
    {
        var selection = new ExpressionSelector().Select(At("在忙吗"), In(), Decide(HabitToneAllowed.GroundedOnly),
            [Candidate(Habit("来啦", ExpressionTone.Affectionate)), Candidate(Habit("我嘞个"))]);
        Assert.True(selection.ChoseNothing);
        Assert.Equal(2, selection.Rejections.Count);
    }

    // ------------------------------------------------------------ Retrieval 上限

    [Fact]
    public void Retrieval_returns_a_handful_never_the_whole_store()
    {
        var store = Enumerable.Range(0, 60).Select(i => Habit($"习惯{i}")).ToArray();
        var found = new ExpressionRetriever().Retrieve(store, "轻微吐槽", "草", T);
        Assert.Equal(3, found.Count);
    }

    [Fact]
    public void Suppressed_habits_are_not_recalled_at_all()
    {
        var found = new ExpressionRetriever().Retrieve(
            [Habit("我嘞个", suppressed: true), Habit("笑不活")], "轻微吐槽", "草", T);
        Assert.DoesNotContain(found, x => x.Pattern.Style == "我嘞个");
    }

    [Fact]
    public async Task Service_wires_retrieval_to_selection_and_records_usage()
    {
        var store = new InMemoryExpressionPatternRepository();
        await store.SaveAsync(Habit("我嘞个"), Token);
        var service = new ExpressionService(store, "author-character");

        var selection = await service.SelectForTurnAsync(At("草"), In(Familiar()),
            Decide(HabitToneAllowed.Any), Token);
        Assert.Single(selection.Hints);
        Assert.Null((await store.ListForCharacterAsync("author-character", Token)).Single().LastUsedAt);

        await service.NoteUsedAsync(selection.Hints, T, Token);
        var stored = (await store.ListForCharacterAsync("author-character", Token)).Single();
        Assert.Equal(1, stored.UsageCount);
        Assert.Equal(T, stored.LastUsedAt!.Value);
    }

    // ------------------------------------------------------------ 后台学习的准入

    [Fact]
    public async Task A_single_use_of_a_phrase_is_not_a_personality()
    {
        var learner = Learner(out _);
        learner.TryEnqueue(Turn("t1", "user-1", "我嘞个，居然活过来了"));

        Assert.Equal(LearningOutcome.RejectedInsufficientEvidence, (await learner.DrainAsync(Token)).Single().Outcome);
    }

    [Fact]
    public async Task A_shape_repeated_across_turns_becomes_one_pattern()
    {
        var learner = Learner(out var store);
        learner.TryEnqueue(Turn("t1", "u1", "我嘞个，居然活过来了"));
        learner.TryEnqueue(Turn("t2", "u2", "我嘞个，你这个点还没睡"));

        Assert.Equal(LearningOutcome.Created, (await learner.DrainAsync(Token)).Single(r => r.Style == "我嘞个").Outcome);
        var patterns = await store.ListForCharacterAsync("author-character", Token);
        Assert.Contains(patterns, x => x.Style == "我嘞个" && x.EvidenceCount == 2);
    }

    [Fact]
    public async Task Repeated_evidence_merges_instead_of_multiplying()
    {
        var learner = Learner(out var store);
        learner.TryEnqueue(Turn("t1", "u1", "我嘞个，居然活过来了"));
        learner.TryEnqueue(Turn("t2", "u2", "我嘞个，你这个点还没睡"));
        await learner.DrainAsync(Token);

        learner.TryEnqueue(Turn("t3", "u3", "我嘞个，外面下雨了"));
        learner.TryEnqueue(Turn("t4", "u4", "我嘞个，你又忘了"));
        var results = await learner.DrainAsync(Token);

        Assert.Equal(LearningOutcome.Merged, results.Single(r => r.Style == "我嘞个").Outcome);
        Assert.Single(await store.ListForCharacterAsync("author-character", Token));
    }

    [Fact]
    public async Task Something_the_user_keeps_saying_stays_theirs()
    {
        var learner = Learner(out var store);
        learner.TryEnqueue(Turn("t1", "我嘞个草", "我嘞个，居然活过来了"));
        learner.TryEnqueue(Turn("t2", "我嘞个", "我嘞个，你真会讲"));
        learner.TryEnqueue(Turn("t3", "别的", "我嘞个，睡吧"));
        learner.TryEnqueue(Turn("t4", "嗯", "我嘞个，晚安"));

        var result = (await learner.DrainAsync(Token)).Single(r => r.Style == "我嘞个");
        Assert.Equal(LearningOutcome.RejectedUserEcho, result.Outcome);
        Assert.DoesNotContain(await store.ListForCharacterAsync("author-character", Token), x => x.Style == "我嘞个");
    }

    [Theory]
    [InlineData("请问，有什么可以帮你的", "请问", LearningOutcome.RejectedBoilerplate)]
    [InlineData("你个傻逼，笑死", "你个傻逼", LearningOutcome.RejectedCrude)]
    [InlineData("我是AI，嗯", "我是AI", LearningOutcome.RejectedPersonaConflict)]
    [InlineData("1234，来了", "1234", LearningOutcome.RejectedFactual)]
    public async Task Contaminated_candidates_are_never_learned(string assistantText, string shape,
        LearningOutcome expected)
    {
        var learner = Learner(out var store);
        learner.TryEnqueue(Turn("t1", "u1", assistantText));
        learner.TryEnqueue(Turn("t2", "u2", assistantText));

        var result = (await learner.DrainAsync(Token)).SingleOrDefault(x => x.Style == shape);
        Assert.NotNull(result);
        Assert.Equal(expected, result!.Outcome);
        Assert.DoesNotContain(await store.ListForCharacterAsync("author-character", Token), x => x.Style == shape);
    }

    [Fact]
    public async Task Asking_her_to_stop_talking_like_that_removes_the_candidate()
    {
        var learner = Learner(out var store);
        learner.TryEnqueue(Turn("t1", "你别老我嘞个我嘞个的", "我嘞个，居然活过来了", corrected: true));
        learner.TryEnqueue(Turn("t2", "就是", "我嘞个，你真会讲", corrected: true));

        Assert.Equal(LearningOutcome.RejectedExplicitCorrection,
            (await learner.DrainAsync(Token)).Single(r => r.Style == "我嘞个").Outcome);
        Assert.All(await store.ListForCharacterAsync("author-character", Token), x => Assert.True(x.Suppressed));
    }

    [Fact]
    public void Learning_is_queue_based_and_drops_instead_of_blocking_the_reply()
    {
        var learner = Learner(out _);
        var enqueued = 0;
        for (var i = 0; i < ExpressionLearner.QueueCapacity + 5; i++)
            if (learner.TryEnqueue(Turn($"t{i}", "u", "我嘞个，又来了"))) enqueued++;

        Assert.Equal(ExpressionLearner.QueueCapacity, enqueued);
        Assert.Equal(ExpressionLearner.QueueCapacity, learner.PendingCount);
        Assert.Equal(5, learner.DroppedCount);
    }

    [Fact]
    public async Task Nothing_is_looked_up_until_the_host_drains_the_queue()
    {
        var learner = Learner(out var store);
        learner.TryEnqueue(Turn("t1", "u1", "我嘞个，居然活过来了"));
        learner.TryEnqueue(Turn("t2", "u2", "我嘞个，你又忘了"));
        Assert.Equal(2, learner.PendingCount);
        Assert.Empty(await store.ListForCharacterAsync("author-character", Token));

        Assert.Equal(LearningOutcome.Created,
            (await learner.DrainAsync(Token)).Single(r => r.Style == "我嘞个").Outcome);
    }

    private static ExpressionLearner Learner(out InMemoryExpressionPatternRepository store)
    {
        store = new InMemoryExpressionPatternRepository();
        return new ExpressionLearner(store, "author-character", () => T);
    }

    private static DeliveredTurn Turn(string turnId, string userInput, string assistantText, bool corrected = false) =>
        new("conv-1", turnId, T, userInput, assistantText, corrected)
        {
            // These fixtures model independent, explicit user acceptance; delivery alone does not teach.
            FeedbackEvidenceId = $"feedback-{turnId}", AcceptedStyles = ["我嘞个"]
        };

    private static ChatMessage Said(string text, ChatRole role = ChatRole.Assistant, int minutesAgo = 0) =>
        new(1, "conv-1", role, text, T.AddMinutes(-minutesAgo));
}

using Pry.Core.Abstractions;
using Pry.Core.Expression;
using Pry.Core.Intelligence;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// `CharacterLearningCleanup` 是四张学习表唯一的作用域翻译处：状态按角色卡 id 存，行为／表达／经验按
/// 宿主给的实例 id 存。这个类只验"一次调用把四边都清干净、且只清这一份"，落库侧的等价判据在
/// `SqliteCharacterLearningDeletionTests`（那边跑真库与真删卡链路）。
/// </summary>
public sealed class CharacterLearningCleanupTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private sealed class Stores
    {
        public required InMemoryCharacterStateRepository States { get; init; }
        public required InMemoryBehaviorPatternRepository Patterns { get; init; }
        public required InMemoryBehaviorExperienceRepository Experiences { get; init; }
        public required InMemoryExpressionPatternRepository Expressions { get; init; }

        public CharacterLearningCleanup Cleanup() => new(States, Patterns, Experiences, Expressions);
    }

    private static Stores Learned() => new()
    {
        States = new InMemoryCharacterStateRepository(),
        Patterns = new InMemoryBehaviorPatternRepository(),
        Experiences = new InMemoryBehaviorExperienceRepository(),
        Expressions = new InMemoryExpressionPatternRepository()
    };

    private static async Task SeedAsync(Stores stores, string cardId)
    {
        await stores.States.SaveAsync(new CharacterStateRecord
        {
            CharacterId = cardId, UserId = "user-1", StateVersion = 7, Familiarity = 0.4
        }, Token);
        await stores.States.SaveAsync(new CharacterStateRecord
        {
            CharacterId = cardId, UserId = "user-2", StateVersion = 3, Familiarity = 0.9
        }, Token);
        await stores.Patterns.SaveAsync(new BehaviorPattern
        {
            BehaviorPatternId = $"bp:{cardId}:topic:Comfort", CharacterInstanceId = cardId,
            Scenario = "安慰", Strategy = ResponseIntent.Comfort, SuccessCount = 4, UsageCount = 6,
            EvaluatedTurnKeys = [$"{cardId}turn-1"]
        }, Token);
        await stores.Expressions.SaveAsync(new ExpressionPattern
        {
            Id = $"ep:{cardId}:aaa111", CharacterInstanceId = cardId, Situation = "轻微吐槽",
            Style = "笑不活了", AcceptedEvidenceIds = ["fb-1"], AcceptedTurnKeys = ["t-1"]
        }, Token);
        await stores.Experiences.AppendAsync(new BehaviorExperience(cardId, "conv-1", "t-1", "topic",
            ResponseIntent.Comfort, InteractionOutcome.PositiveReaction,
            new DateTimeOffset(2026, 9, 20, 10, 0, 0, TimeSpan.FromHours(8)), "fb-1")
        { FeedbackIsExplicit = true }, Token);
    }

    [Fact]
    public async Task Purging_a_card_clears_all_four_stores_for_every_user()
    {
        var stores = Learned();
        await SeedAsync(stores, "card-a");

        await stores.Cleanup().PurgeAsync("card-a", Token);

        Assert.Null(await stores.States.LoadAsync("card-a", "user-1", Token));
        Assert.Null(await stores.States.LoadAsync("card-a", "user-2", Token));
        Assert.Empty(await stores.Patterns.ListForCharacterAsync("card-a", Token));
        Assert.Empty(await stores.Expressions.ListForCharacterAsync("card-a", Token));
        Assert.Empty(await stores.Experiences.ListForCharacterAsync("card-a", 10, Token));
    }

    [Fact]
    public async Task Purging_one_card_leaves_another_cards_whole_learning_state_alone()
    {
        var stores = Learned();
        await SeedAsync(stores, "card-a");
        await SeedAsync(stores, "card-b");

        await stores.Cleanup().PurgeAsync("card-a", Token);

        Assert.Equal(0.9, (await stores.States.LoadAsync("card-b", "user-2", Token))?.Familiarity);
        Assert.Single(await stores.Patterns.ListForCharacterAsync("card-b", Token));
        var expression = Assert.Single(await stores.Expressions.ListForCharacterAsync("card-b", Token));
        Assert.Equal(new[] { "fb-1" }, expression.AcceptedEvidenceIds, StringComparer.Ordinal);
        Assert.Single(await stores.Experiences.ListForCharacterAsync("card-b", 10, Token));
    }

    [Fact]
    public async Task Purging_a_card_that_taught_her_nothing_is_not_an_error()
    {
        var stores = Learned();
        await SeedAsync(stores, "card-a");

        // 删卡是可以重试的：清空一张没学过的卡（或再来一次）都不该抛，否则重试通道本身就断了。
        await stores.Cleanup().PurgeAsync("card-gone", Token);
        await stores.Cleanup().PurgeAsync("card-a", Token);
        await stores.Cleanup().PurgeAsync("card-a", Token);

        Assert.Empty(await stores.Patterns.ListForCharacterAsync("card-a", Token));
    }
}

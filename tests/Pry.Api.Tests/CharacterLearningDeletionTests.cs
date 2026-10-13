using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Pry.Api.Services;
using Pry.Contracts;
using Pry.Core.Abstractions;
using Pry.Core.Expression;
using Pry.Core.Intelligence;
using Pry.Core.Memory;
using Pry.Core.Models;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// RT-003 缺口 ② 的调用点判据：删掉一张角色卡，她学到的东西必须跟着走，而且只在删除真的成立时走。
/// 下面三条都跑真 SQLite、真宿主服务链（`ConfigurationApplicationService` → `CharacterLearningCleanup`
/// → 四个落库适配器），因为这条链上唯一可能出错的就是"接线"本身——仓储语义那边由
/// `SqliteIntelligenceRepositoryConformanceTests` 的 25 例守着，两边不能互顶读数。
///
/// <para>
/// 第 2、3 条才是这一刀的实质：清空动作落在"卡片还能退回去"的那一段里，所以被引用挡掉的删除
/// 一个字都不该动库，而失败的清空会把卡片原样放回去、留下一条可重试的删除。
/// 反过来排（先删卡再清库）的失效形状就是 `QA-P3STATE-03` 记的那条——用户已经删过的角色，
/// 她的关系与经验永远留在库里，而界面上再没有任何入口能碰到它们。
/// </para>
/// </summary>
public sealed class CharacterLearningDeletionTests : IAsyncLifetime
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"pry-learning-deletion-{Guid.NewGuid():N}");
    private MemoryDatabase _database = null!;
    private string _databasePath = null!;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        _databasePath = Path.Combine(_root, "memory.db");
        _database = new MemoryDatabase(_databasePath);
        await _database.InitializeAsync(Token);
    }

    public ValueTask DisposeAsync()
    {
        // 只清自己这条库产生的池（DEF-P1WALK-04：进程级 ClearAllPools 会拆掉并行用例的句柄）。
        // MemoryDatabase 现在自带长期记忆仓储那条串，只清宿主形状会漏掉它。
        SqliteTestPools.Clear(_databasePath);
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        return ValueTask.CompletedTask;
    }

    private SqliteConnection Open() => new(new SqliteConnectionStringBuilder
    {
        DataSource = _databasePath, Mode = SqliteOpenMode.ReadWriteCreate, Cache = SqliteCacheMode.Shared
    }.ToString());

    private CharacterLearningCleanup Cleanup(IBehaviorExperienceRepository? experiences = null) => new(
        new SqliteCharacterStateRepository(_database), new SqliteBehaviorPatternRepository(_database),
        experiences ?? new SqliteBehaviorExperienceRepository(_database),
        new SqliteExpressionPatternRepository(_database));

    private sealed record Host(ConfigurationManager Configuration, ModelProcessRegistry Registry,
        BackendRuntime Runtime, MediaAssetStore Media, ConversationSessionService Sessions) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Sessions.DisposeAsync();
            await Runtime.StopAsync(CancellationToken.None);
            await Registry.DisposeAsync();
        }
    }

    private async Task<Host> StartHostAsync()
    {
        var configuration = new ConfigurationManager();
        configuration["Pry:DataDirectory"] = _root;
        var media = new MediaAssetStore(configuration);
        var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var runtime = new BackendRuntime(configuration, registry, NullLogger<BackendRuntime>.Instance);
        await runtime.StartAsync(Token);
        var sessions = new ConversationSessionService(_database, runtime, media,
            NullLogger<ConversationSessionService>.Instance);
        return new Host(configuration, registry, runtime, media, sessions);
    }

    private async Task<string> CreateCardAsync(ConfigurationApplicationService service, string name) =>
        (await service.CreateCharacterAsync(new SaveCharacterRequest(name, $"{name}-v1", "你", "陪伴者", "沉稳",
            "简洁", ["尊重用户"], [], new RuntimeState(), "你好。", CharacterPromptMode.Structured, "", null,
            false, new ImageDisplayPreferences()), Token)).Id;

    private async Task SeedLearnedRowsAsync(string cardId)
    {
        await new SqliteCharacterStateRepository(_database).SaveAsync(new CharacterStateRecord
        {
            CharacterId = cardId, UserId = "user-1", StateVersion = 7, Familiarity = 0.61, Trust = 0.44,
            ExchangeCount = 33, MoodUpdatedAt = new DateTimeOffset(2026, 9, 20, 10, 0, 0, TimeSpan.FromHours(8))
        }, Token);
        await new SqliteBehaviorPatternRepository(_database).SaveAsync(new BehaviorPattern
        {
            BehaviorPatternId = $"bp:{cardId}:topic:Comfort", CharacterInstanceId = cardId, Scenario = "安慰",
            Strategy = ResponseIntent.Comfort, UsageCount = 6, SuccessCount = 4,
            CreatedAt = new DateTimeOffset(2026, 9, 20, 10, 0, 0, TimeSpan.FromHours(8)),
            UpdatedAt = new DateTimeOffset(2026, 9, 20, 11, 0, 0, TimeSpan.FromHours(8)),
            EvaluatedTurnKeys = [$"{cardId}conv-1turn-1", $"{cardId}conv-1turn-2"]
        }, Token);
        await new SqliteExpressionPatternRepository(_database).SaveAsync(new ExpressionPattern
        {
            Id = $"ep:{cardId}:aaa111", CharacterInstanceId = cardId, Situation = "轻微吐槽", Style = "笑不活了",
            Tone = ExpressionTone.Playful, EvidenceCount = 2,
            CreatedAt = new DateTimeOffset(2026, 9, 20, 10, 0, 0, TimeSpan.FromHours(8)),
            UpdatedAt = new DateTimeOffset(2026, 9, 20, 11, 0, 0, TimeSpan.FromHours(8)),
            AcceptedEvidenceIds = ["fb-1", "fb-2"], AcceptedTurnKeys = [$"{cardId}turn-1"]
        }, Token);
        await new SqliteBehaviorExperienceRepository(_database).AppendAsync(new BehaviorExperience(cardId,
            "conv-1", $"{cardId}turn-1", "topic", ResponseIntent.Comfort, InteractionOutcome.PositiveReaction,
            new DateTimeOffset(2026, 9, 20, 10, 30, 0, TimeSpan.FromHours(8)), "fb-1")
        { FeedbackIsExplicit = true }, Token);
    }

    private async Task<bool> AnythingLearnedAsync(string cardId)
    {
        return await new SqliteCharacterStateRepository(_database).LoadAsync(cardId, "user-1", Token) is not null
            || (await new SqliteBehaviorPatternRepository(_database).ListForCharacterAsync(cardId, Token)).Count > 0
            || (await new SqliteExpressionPatternRepository(_database).ListForCharacterAsync(cardId, Token)).Count > 0
            || (await new SqliteBehaviorExperienceRepository(_database)
                .ListForCharacterAsync(cardId, 10, Token)).Count > 0;
    }

    [Fact]
    public async Task Deleting_a_card_clears_every_row_she_learned_and_leaves_the_next_card_intact()
    {
        await using var host = await StartHostAsync();
        var service = new ConfigurationApplicationService(host.Configuration, host.Runtime, host.Sessions,
            host.Media, _database, Cleanup());
        var gone = await CreateCardAsync(service, "待删除角色");
        var kept = await CreateCardAsync(service, "留下的角色");
        await SeedLearnedRowsAsync(gone);
        await SeedLearnedRowsAsync(kept);
        Assert.True(await AnythingLearnedAsync(gone));

        await service.DeleteCharacterAsync(gone, Token);

        Assert.False(await AnythingLearnedAsync(gone));
        Assert.Null(await new SqliteCharacterStateRepository(_database).LoadAsync(gone, "user-1", Token));
        // 守卫子表跟着父行走：留下那张的模式与表达必须连回放守卫一起是原来的。
        var pattern = Assert.Single(await new SqliteBehaviorPatternRepository(_database)
            .ListForCharacterAsync(kept, Token));
        Assert.Equal(new[] { $"{kept}conv-1turn-1", $"{kept}conv-1turn-2" }, pattern.EvaluatedTurnKeys,
            StringComparer.Ordinal);
        var expression = Assert.Single(await new SqliteExpressionPatternRepository(_database)
            .ListForCharacterAsync(kept, Token));
        Assert.Equal(new[] { "fb-1", "fb-2" }, expression.AcceptedEvidenceIds, StringComparer.Ordinal);
        Assert.Single(await new SqliteBehaviorExperienceRepository(_database).ListForCharacterAsync(kept, 10, Token));
    }

    [Fact]
    public async Task A_deletion_that_is_refused_touches_no_learning_row()
    {
        await using var host = await StartHostAsync();
        var service = new ConfigurationApplicationService(host.Configuration, host.Runtime, host.Sessions,
            host.Media, _database, Cleanup());
        var cardId = await CreateCardAsync(service, "被引用的角色");
        await SeedLearnedRowsAsync(cardId);
        await _database.EnsureConversationAsync("uses-character", cardId, Token);

        await Assert.ThrowsAsync<ResourceConflictException>(() => service.DeleteCharacterAsync(cardId, Token));

        // 挡在前面就得什么都没发生：这条断言是"清空只发生在可撤销那一段里"的反证点。
        Assert.NotNull(await new SqliteCharacterStateRepository(_database).LoadAsync(cardId, "user-1", Token));
        Assert.Single(await new SqliteBehaviorPatternRepository(_database).ListForCharacterAsync(cardId, Token));
        Assert.Single(await new SqliteExpressionPatternRepository(_database).ListForCharacterAsync(cardId, Token));
        Assert.Single(await new SqliteBehaviorExperienceRepository(_database)
            .ListForCharacterAsync(cardId, 10, Token));
    }

    [Fact]
    public async Task A_failed_cleanup_gives_the_card_back_and_leaves_the_deletion_retryable()
    {
        await using var host = await StartHostAsync();
        var failing = new ExperiencePurgeFails(new SqliteBehaviorExperienceRepository(_database));
        var service = new ConfigurationApplicationService(host.Configuration, host.Runtime, host.Sessions,
            host.Media, _database, Cleanup(failing));
        var cardId = await CreateCardAsync(service, "清空失败的角色");
        await SeedLearnedRowsAsync(cardId);

        failing.Fail = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteCharacterAsync(cardId, Token));

        // 卡片必须还在原处，否则这次失败就变成"删掉了但库里还有她的关系"——那正是永远修不好的那条。
        Assert.Contains(service.ListCharacters(), x => x.Id == cardId);
        Assert.True(File.Exists(Path.Combine(_root, "characters", cardId + ".json")));

        failing.Fail = false;
        await service.DeleteCharacterAsync(cardId, Token);

        Assert.DoesNotContain(service.ListCharacters(), x => x.Id == cardId);
        Assert.False(await AnythingLearnedAsync(cardId));
    }

    private sealed class ExperiencePurgeFails(IBehaviorExperienceRepository inner) : IBehaviorExperienceRepository
    {
        public bool Fail { get; set; }

        public Task AppendAsync(BehaviorExperience experience, CancellationToken cancellationToken = default) =>
            inner.AppendAsync(experience, cancellationToken);

        public Task<IReadOnlyList<BehaviorExperience>> ListForCharacterAsync(string characterInstanceId, int limit,
            CancellationToken cancellationToken = default) =>
            inner.ListForCharacterAsync(characterInstanceId, limit, cancellationToken);

        public Task<int> PurgeAsync(string characterInstanceId, CancellationToken cancellationToken = default) =>
            Fail ? Task.FromException<int>(new InvalidOperationException("模拟存储层在清空经验日志时失败"))
                : inner.PurgeAsync(characterInstanceId, cancellationToken);
    }
}

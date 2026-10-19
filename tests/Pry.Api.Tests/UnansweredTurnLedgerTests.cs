using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Pry.Api.Runtime;
using Pry.Core.Abstractions;
using Pry.Core.Expression;
using Pry.Core.Intelligence;
using Pry.Core.Memory;
using Pry.Core.Models;
using Pry.Core.Runtime;
using Pry.Core.TurnTaking;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// 占位符那一轮的账本判据（<c>DEF-P3SEAM-04</c>）：模型没兑现出正文时，界面上看到的是两个点，
/// 而这一轮在账本上今天被记成了"她说了话、而且说成功了"。三条各守一处：
/// 互动与关系那一格不许动，她原本打算用的那个口癖不许算成用过一次，
/// 而她真说了话的那些轮一个字都不许少记（哪怕同一轮里还附了一条占位）。
///
/// <para>
/// 跑真 SQLite、真网关、真投递装饰器：这三条唯一可能出错的地方是记账入口本身，
/// 领域语义有 <c>CharacterIntelligenceTests</c> 那批守着，接线有没有断有
/// <c>CharacterIntelligenceSeamTests</c> 那批守着，三边读数不互顶。
/// <c>An_undelivered_turn_teaches_nothing</c> 守的是"一笔都没投出去"，
/// 而这里的形状是"投出去了一笔，但那不是话"——那条判据今天抓不到它，所以才需要这一份。
/// </para>
/// </summary>
public sealed class UnansweredTurnLedgerTests : IAsyncLifetime
{
    /// <summary>真机读到的那一格正文（<c>planned_messages.content</c>，state=delivered）。测试面允许写字面量。</summary>
    private const string Unanswered = "……";

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"pry-unanswered-ledger-{Guid.NewGuid():N}");
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
        SqliteTestPools.Clear(_databasePath);
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        return ValueTask.CompletedTask;
    }

    private static CharacterDefinition Card() => new()
    {
        Id = "card-unanswered", Name = "小潮", CardName = "小潮", UserName = "你", Identity = "陪伴者",
        Personality = "沉稳", SpeechStyle = "简洁", BehavioralRules = ["不要敷衍用户"], Greeting = "你好。"
    };

    private CharacterIntelligenceGateway Gateway() => new(_database,
        new SqliteCharacterStateRepository(_database), new SqliteBehaviorPatternRepository(_database),
        new SqliteExpressionPatternRepository(_database), new SqliteBehaviorExperienceRepository(_database),
        NullLogger<CharacterIntelligenceGateway>.Instance);

    private SqliteCharacterStateRepository States() => new(_database);

    private SqliteBehaviorExperienceRepository Experiences() => new(_database);

    private SqliteExpressionPatternRepository Habits() => new(_database);

    /// <summary>Runtime 先追加用户输入、再成轮、再过闸门，所以这里也先写库。</summary>
    private async Task<string> SeedAsync(params (ChatRole Role, string Text)[] transcript)
    {
        var conversationId = $"conv-{Guid.NewGuid():N}";
        await _database.EnsureConversationAsync(conversationId, "card-unanswered", Token);
        foreach (var (role, text) in transcript)
            await _database.AddMessageAsync(conversationId, role, text, null, Token);
        return conversationId;
    }

    private static ReplyGateContext Gate(TurnIdentity turn, string input) =>
        new(turn, input, false, true, false, RuntimePhase.Planning, false, 1, 0, null, TimeSpan.Zero, 0, false);

    /// <summary>
    /// 完成一轮并交回状态格：投递走真的装饰器，终局走真的 <c>TurnTransitionAsync</c>。
    /// </summary>
    private async Task DeliverAndCloseAsync(CharacterIntelligenceGateway gateway, string conversationId,
        TurnIdentity turn, string userText, params string[] bubbles)
    {
        var sink = new CharacterIntelligenceOutputSink(new CollectingSink(), gateway);
        await gateway.CreateAdvisor(conversationId, Card(), new TurnTakingSettings())
            .AdviseAsync(Gate(turn, userText), Token);
        var messageId = 100L + turn.TurnId;
        for (var sequence = 0; sequence < bubbles.Length; sequence++)
            await sink.MessageDeliveredAsync(conversationId, messageId + sequence,
                new RuntimePlannedMessage(sequence, bubbles[sequence], null), MessageSources.ModelReply, Token);

        await sink.TurnTransitionAsync(conversationId, turn, RuntimeTurnOutcome.Completed, "delivered", Token);
    }

    [Fact]
    public async Task A_turn_that_only_delivered_the_placeholder_teaches_nothing()
    {
        var conversationId = await SeedAsync((ChatRole.User, "我今天加班到十点，整个人都不好了"));
        var gateway = Gateway();

        // 先让她真说上一轮：空账本证不了"+1 没发生"，只有先有一格，才谈得上"一字不动"。
        await DeliverAndCloseAsync(gateway, conversationId, new TurnIdentity(1, 1, 1),
            "我今天加班到十点，整个人都不好了", "辛苦你了");
        var before = await States().LoadAsync("card-unanswered", CharacterIntelligenceGateway.LocalUserId, Token);
        Assert.NotNull(before);
        Assert.Equal(1, before.ExchangeCount);

        // 第二轮模型一个字没兑现出来，投出去的就是那条占位符——界面上她"发了两个点"。
        await DeliverAndCloseAsync(gateway, conversationId, new TurnIdentity(2, 1, 1), "周末有什么打算吗", Unanswered);

        var after = await States().LoadAsync("card-unanswered", CharacterIntelligenceGateway.LocalUserId, Token);
        // 整格比对（record 值相等）：exchange_count、熟悉度、心情各列一起钉住，不靠逐列挑。
        Assert.Equal(before, after);
        Assert.Single(await Experiences().ListForCharacterAsync("card-unanswered", 10, Token));
    }

    [Fact]
    public async Task The_habit_she_meant_to_use_is_not_charged_by_a_placeholder_turn()
    {
        var conversationId = await SeedAsync((ChatRole.User, "周末有什么打算吗"));
        await Habits().SaveAsync(new ExpressionPattern
        {
            Id = "habit-neutral-1", CharacterInstanceId = "card-unanswered", Situation = "周末安排",
            Style = "先应一声", Tone = ExpressionTone.Neutral, Confidence = 0.5,
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        }, Token);
        var gateway = Gateway();

        // 占位那一轮：她打算用的口癖不许算成"用过一次"。
        await DeliverAndCloseAsync(gateway, conversationId, new TurnIdentity(3, 1, 1), "周末有什么打算吗", Unanswered);
        var afterPlaceholder = await Habits().ListForCharacterAsync("card-unanswered", Token);
        var habit = Assert.Single(afterPlaceholder);
        Assert.Equal(0, habit.UsageCount);
        Assert.Null(habit.LastUsedAt);

        // 同一本习惯、同一句话，下一轮她真的说了话：这一笔必须照记。
        // 少了这半条，上一条就只是"没人选上口癖"的常绿，而不是"占位轮不收费"。
        await DeliverAndCloseAsync(gateway, conversationId, new TurnIdentity(4, 1, 1), "周末有什么打算吗", "想去哪儿");
        var afterReal = await Habits().ListForCharacterAsync("card-unanswered", Token);
        var charged = Assert.Single(afterReal);
        Assert.Equal(1, charged.UsageCount);
        Assert.NotNull(charged.LastUsedAt);
    }

    [Fact]
    public async Task A_real_bubble_alongside_a_placeholder_is_still_one_successful_exchange()
    {
        var conversationId = await SeedAsync((ChatRole.User, "我今天加班到十点，整个人都不好了"));
        var gateway = Gateway();

        // 不许过度修复：本轮有真正文时记账照旧，哪怕同轮还附了一条占位。
        await DeliverAndCloseAsync(gateway, conversationId, new TurnIdentity(5, 1, 1),
            "我今天加班到十点，整个人都不好了", "辛苦你了", Unanswered);

        var state = await States().LoadAsync("card-unanswered", CharacterIntelligenceGateway.LocalUserId, Token);
        Assert.NotNull(state);
        Assert.Equal(1, state.ExchangeCount);
        Assert.Single(await Experiences().ListForCharacterAsync("card-unanswered", 10, Token));
    }

    private sealed class CollectingSink : IRuntimeOutputSink
    {
        public Task MessageDeliveredAsync(string conversationId, long messageId, RuntimePlannedMessage message,
            string source, CancellationToken token) => Task.CompletedTask;

        public Task TypingIndicatorAsync(string conversationId, bool active, CancellationToken token) =>
            Task.CompletedTask;

        public Task PhaseChangedAsync(string conversationId, RuntimePhase phase, RuntimePhase previous,
            CancellationToken token) => Task.CompletedTask;

        public Task TurnTransitionAsync(string conversationId, TurnIdentity turn, RuntimeTurnOutcome outcome,
            string reason, CancellationToken token) => Task.CompletedTask;

        public Task CapabilityRequestedAsync(string conversationId, CapabilityRequest request,
            CancellationToken token) => Task.CompletedTask;

        public Task FailureAsync(string conversationId, TurnIdentity turn, string errorCode,
            CancellationToken token) => Task.CompletedTask;
    }
}

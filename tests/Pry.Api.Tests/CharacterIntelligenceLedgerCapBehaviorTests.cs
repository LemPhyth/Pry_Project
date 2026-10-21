using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Pry.Api.Runtime;
using Pry.Core.Abstractions;
using Pry.Core.Memory;
using Pry.Core.Models;
using Pry.Core.Runtime;
using Pry.Core.TurnTaking;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// HDS-011c-E2｜资源限额总盘里两格零判据的行为判据（本刀零产品代码）。
///
/// <see cref="CharacterIntelligenceGateway"/> 手里有两本没人收的账：<c>PendingTurnLimit</c>（说话前那一问的
/// 账本，8 格）与 <c>ReceiptLimit</c>（"这条气泡出自哪一轮"的投递凭据，64 格）。总盘登记时这两格是零判据
/// ——后者只有一道 grep <c>ReceiptLimit</c> 的文本门，把那句 <c>while</c> 摘掉也不会红。
/// 摘掉的后果分两层：Pry 的常态是后端开着几天不关，无上限的账本用户看见的是越用越卡；
/// 而凭据本一旦长到不封顶，"找不到就归给最近一轮"那条 <c>DEF-P3SEAM-02</c> 拆掉的写法就随时能悄悄回来，
/// 她会把自己没说的话学成说过。
///
/// <para>
/// 两条都是行为级：真的把上限越过一次，看**被挤掉的是不是最老那一格、界内那格还学不学得到**。
/// 只判"挤掉了"的那一半，把整本账删空也是绿；只判"还学得到"的那一半，把上限调到一千万也是绿。
/// 上限的实际数值由反射从声明点读（本刀不许改数值，也不许在测试里抄第二份 8／64 当尺——
/// 抄了就变成测试自己定规则），所以改小改大都会由这两条当场读出形状变化。
/// 跑真 SQLite、真网关、真投递装饰器，与 <c>UnansweredTurnLedgerTests</c>、<c>ExplicitExpressionFeedbackTests</c>
/// 同一套手法；临时目录、只清自己那一份池。
/// </para>
/// </summary>
public sealed class CharacterIntelligenceLedgerCapBehaviorTests : IAsyncLifetime
{
    private const string CardId = "card-ledger-cap";

    /// <summary>取自 <c>ExpressionLearner</c> 的感叹开头词表：这条气泡必须有可学形状，否则"学得到"那半条永远立不住。</summary>
    private const string Bubble = "我嘞个，居然活过来了";

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"pry-ledger-cap-{Guid.NewGuid():N}");
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

    /// <summary>
    /// 账本上限（8）的行为判据。形状：一次性攒出 <c>cap + 1</c> 笔没收尾的轮次 ⇒ 最老那笔被挤掉 ⇒
    /// 它投递后再回报终局，一个字都不许教；而界内的第二老那笔与最新那笔必须照学。
    /// </summary>
    [Fact]
    public async Task A_turn_past_the_pending_cap_drops_the_oldest_entry_and_keeps_the_newest()
    {
        var cap = PrivateConst("PendingTurnLimit");
        var gateway = Gateway();
        var sink = new CharacterIntelligenceOutputSink(new SilentSink(), gateway);

        var turns = new List<(string Conversation, long MessageId, TurnIdentity Turn)>();
        for (var index = 1; index <= cap + 1; index++)
        {
            var conversationId = await SeedConversationAsync($"第 {index} 句");
            // 只问不投：这一笔现在挂在账本上，等的是投递与终局。
            var turn = new TurnIdentity(index, 1, 1);
            await gateway.CreateAdvisor(conversationId, Card(), new TurnTakingSettings())
                .AdviseAsync(Gate(turn, $"第 {index} 句"), Token);
            var messageId = await _database.AddMessageAsync(conversationId, ChatRole.Assistant, Bubble, null, Token);
            turns.Add((conversationId, messageId, turn));
        }

        // 每一笔都真的投出去过——否则"没学到"只是因为没投递，与上限无关。
        foreach (var (conversationId, messageId, _) in turns)
            await sink.MessageDeliveredAsync(conversationId, messageId, new RuntimePlannedMessage(0, Bubble, null),
                MessageSources.ModelReply, Token);

        Assert.Equal(0, await ExchangeCountAsync());
        Assert.Empty(await Experiences().ListForCharacterAsync(CardId, 10, Token));

        // ① 最老那一笔已经被 cap+1 这一笔挤出去了：它说什么都不该长成一次学习。
        await sink.TurnTransitionAsync(turns[0].Conversation, turns[0].Turn, RuntimeTurnOutcome.Completed,
            "delivered", Token);
        Assert.Equal(0, await ExchangeCountAsync());
        Assert.Empty(await Experiences().ListForCharacterAsync(CardId, 10, Token));

        // ② 界内的两笔（第二老与最新）必须照学：只判 ① 的话，把整本账删空也是绿。
        await sink.TurnTransitionAsync(turns[1].Conversation, turns[1].Turn, RuntimeTurnOutcome.Completed,
            "delivered", Token);
        Assert.Equal(1, await ExchangeCountAsync());

        await sink.TurnTransitionAsync(turns[^1].Conversation, turns[^1].Turn, RuntimeTurnOutcome.Completed,
            "delivered", Token);
        Assert.Equal(2, await ExchangeCountAsync());
        Assert.NotEmpty(await Experiences().ListForCharacterAsync(CardId, 10, Token));
    }

    /// <summary>
    /// 凭据窗口上限（64）的行为判据。<c>cap + 1</c> 轮真实投递之后：第 1 轮的凭据被挤掉 ⇒ 用户点它时必须
    /// **明确拒收**（<c>NotAttributable</c>，消息在库里、本进程没有它的账），不许静默挂到最近那一轮上；
    /// 第 2 轮（刚好还在窗口里的最老一格）与最后一轮必须仍然记成一次表态。
    /// </summary>
    [Fact]
    public async Task A_receipt_pushed_out_of_the_window_is_refused_not_guessed()
    {
        var cap = PrivateConst("ReceiptLimit");
        var gateway = Gateway();
        var sink = new CharacterIntelligenceOutputSink(new SilentSink(), gateway);
        var conversationId = await SeedConversationAsync("聊阳台的植物");

        var messageIds = new List<long>();
        for (var index = 1; index <= cap + 1; index++)
        {
            var turn = new TurnIdentity(index, 1, index);
            await gateway.CreateAdvisor(conversationId, Card(), new TurnTakingSettings())
                .AdviseAsync(Gate(turn, "聊阳台的植物"), Token);
            var messageId = await _database.AddMessageAsync(conversationId, ChatRole.Assistant, Bubble, null, Token);
            await sink.MessageDeliveredAsync(conversationId, messageId, new RuntimePlannedMessage(0, Bubble, null),
                MessageSources.ModelReply, Token);
            await sink.TurnTransitionAsync(conversationId, turn, RuntimeTurnOutcome.Completed, "delivered", Token);
            messageIds.Add(messageId);
            // 走真的后台排空：不排的话 65 轮会把 Learner 那条 64 格的队列先塞满，
            // 本判据要判的是凭据窗口，不是队列容量（那是另一格）。
            foreach (var active in gateway.ActiveLearners()) await active.Learner.DrainAsync(Token);
        }

        // ① 被挤掉的那一格：库里确有这条消息，本进程却没有它的账 ⇒ 明确拒收，不猜。
        Assert.Equal(ExpressionFeedbackOutcome.NotAttributable,
            await gateway.ReportExplicitFeedbackAsync(conversationId, messageIds[0], approved: true, Token));

        // ② 窗口里的最老一格与最新一格：照旧认得出来。少了这一半，① 只是"什么都不认"的常绿。
        Assert.Equal(ExpressionFeedbackOutcome.Recorded,
            await gateway.ReportExplicitFeedbackAsync(conversationId, messageIds[1], approved: true, Token));
        Assert.Equal(ExpressionFeedbackOutcome.Recorded,
            await gateway.ReportExplicitFeedbackAsync(conversationId, messageIds[^1], approved: true, Token));
        // ① 的读数本身就不许是 Recorded：一旦有人把"找不到凭据"退化成"归给最近一轮"（DEF-P3SEAM-02 拆掉的写法），
        // 这里立刻从 NotAttributable 变成 Recorded，红得指名道姓。
    }

    /// <summary>上限从声明点当场读：本刀不许改数值，也不许在测试里抄第二份字面量当尺。</summary>
    private static int PrivateConst(string name)
    {
        var field = typeof(CharacterIntelligenceGateway).GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.True(field is not null, $"读不到 CharacterIntelligenceGateway.{name}——" +
                                      "docs/resource-limit-ledger.md 第三节那一行的声明点已经不在，判据要跟着重读。");
        return Convert.ToInt32(field!.GetRawConstantValue());
    }

    private static CharacterDefinition Card() => new()
    {
        Id = CardId, Name = "小潮", CardName = "小潮", UserName = "你", Identity = "陪伴者", Personality = "沉稳",
        SpeechStyle = "简洁", BehavioralRules = ["不要敷衍用户"], Greeting = "你好。"
    };

    private static ReplyGateContext Gate(TurnIdentity turn, string input) =>
        new(turn, input, true, true, false, RuntimePhase.Planning, false, 1, 0, null, TimeSpan.Zero, 0, false);

    private CharacterIntelligenceGateway Gateway() => new(_database,
        new SqliteCharacterStateRepository(_database), new SqliteBehaviorPatternRepository(_database),
        new SqliteExpressionPatternRepository(_database), new SqliteBehaviorExperienceRepository(_database),
        NullLogger<CharacterIntelligenceGateway>.Instance);

    private SqliteBehaviorExperienceRepository Experiences() => new(_database);

    private async Task<string> SeedConversationAsync(string userText)
    {
        var conversationId = $"conv-{Guid.NewGuid():N}";
        await _database.EnsureConversationAsync(conversationId, CardId, Token);
        await _database.AddMessageAsync(conversationId, ChatRole.User, userText, null, Token);
        return conversationId;
    }

    private async Task<int> ExchangeCountAsync()
    {
        var state = await new SqliteCharacterStateRepository(_database)
            .LoadAsync(CardId, CharacterIntelligenceGateway.LocalUserId, Token);
        return checked((int)(state?.ExchangeCount ?? 0L));
    }

    private sealed class SilentSink : IRuntimeOutputSink
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

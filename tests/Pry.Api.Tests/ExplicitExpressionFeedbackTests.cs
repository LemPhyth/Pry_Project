using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Pry.Api.Runtime;
using Pry.Api.Services;
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
/// <c>DEF-P4LEARN-01</c> 的判据：她学不学得到一句说话方式，从今天起只由用户在界面上明确按下的
/// 那一下决定。跑真 SQLite（<c>SqliteExpressionPatternRepository</c>）、真
/// <see cref="CharacterIntelligenceGateway"/> 与真的 <see cref="CharacterIntelligenceOutputSink"/>，
/// 表态从真的 <see cref="ExpressionFeedbackService"/> 进来——工单 §2.1 明令"不许手调 handler"，
/// 而这一刀唯一可能出错的地方正是"点的那一条气泡算哪一轮"。
///
/// <para>
/// 各条守的形状：两次不同轮次的明确认可真的长成一行 <c>Learned</c> 并活过重启；一次不点就永远学不到；
/// 同一条气泡连点三次只算一次；"别这样说"压下去之后点不回来；开关关了就把一次本来成立的分析也拒掉、
/// 当场打开就立即可用；归属对不上时明确拒而不是挂到最近那一轮上；她没有真的说话的那些气泡教不出东西；
/// 一次点击不等慢仓储、慢仓储卡住时下一条回复照旧走得完。
/// </para>
///
/// <para>
/// 与 <see cref="ExpressionDrainChannelTests"/> 的分工：那份判的是"队列有没有人取"，它用 <c>Approve</c>
/// 替用户点头；本份判的是"点头这件事今天有没有真的入口、入口只认那一下"。两边的排空都走真后台通道，
/// 读数不互顶。
/// </para>
/// </summary>
public sealed class ExplicitExpressionFeedbackTests : IAsyncLifetime
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"pry-explicit-feedback-{Guid.NewGuid():N}");
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

    private static CharacterDefinition Card(string id = "card-feedback") => new()
    {
        Id = id, Name = "小潮", CardName = "小潮", UserName = "你", Identity = "陪伴者", Personality = "沉稳",
        SpeechStyle = "简洁", BehavioralRules = ["不要敷衍用户"], Greeting = "你好。"
    };

    private CharacterIntelligenceGateway Gateway(IExpressionPatternRepository? expressions = null) => new(_database,
        new SqliteCharacterStateRepository(_database), new SqliteBehaviorPatternRepository(_database),
        expressions ?? new SqliteExpressionPatternRepository(_database),
        new SqliteBehaviorExperienceRepository(_database),
        NullLogger<CharacterIntelligenceGateway>.Instance);

    private static ExpressionDrainService Channel(CharacterIntelligenceGateway gateway) =>
        new(gateway, new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Pry:ExpressionDrainMs"] = "25"
        }).Build(), NullLogger<ExpressionDrainService>.Instance);

    private static ReplyGateContext Gate(TurnIdentity turn, string input) =>
        new(turn, input, true, true, false, RuntimePhase.Planning, false, 1, 0, null, TimeSpan.Zero, 0, false);

    /// <summary>
    /// 一轮真实投递，顺序与 Runtime 一致：说话前那一问 → 气泡落库并把落库拿到的行 id 交给 sink → 终局回报。
    /// 返回那个行 id，因为客户端手里唯一持有的就是它（<c>ChatMessage.Id</c>）。
    /// </summary>
    private async Task<long> SpeakAsync(CharacterIntelligenceGateway gateway,
        CharacterIntelligenceOutputSink sink, string conversationId, long turnId, string input, string reply,
        CharacterDefinition? card = null)
    {
        var turn = new TurnIdentity(turnId, 1, turnId);
        await gateway.CreateAdvisor(conversationId, card ?? Card(), new TurnTakingSettings())
            .AdviseAsync(Gate(turn, input), Token);
        var messageId = await _database.AddMessageAsync(conversationId, ChatRole.Assistant, reply, null, Token);
        await sink.MessageDeliveredAsync(conversationId, messageId,
            new RuntimePlannedMessage(0, reply, null), MessageSources.ModelReply, Token);
        await sink.TurnTransitionAsync(conversationId, turn, RuntimeTurnOutcome.Completed, "delivered", Token);
        return messageId;
    }

    private async Task<string> SeedConversationAsync(string cardId = "card-feedback")
    {
        var conversationId = $"conv-{Guid.NewGuid():N}";
        await _database.EnsureConversationAsync(conversationId, cardId, Token);
        await _database.AddMessageAsync(conversationId, ChatRole.User, "聊阳台的植物", null, Token);
        return conversationId;
    }

    private async Task<BackendRuntime> RuntimeAsync(ModelProcessRegistry registry, bool enabled)
    {
        var configuration = new ConfigurationManager();
        configuration["Pry:DataDirectory"] = _root;
        var runtime = new BackendRuntime(configuration, registry, NullLogger<BackendRuntime>.Instance);
        await runtime.ApplyPreferencesSnapshotAsync(new UserPreferences { ExplicitExpressionFeedback = enabled }, Token);
        return runtime;
    }

    private static async Task<ExpressionFeedbackResponse> ReactAsync(ExpressionFeedbackService service,
        string conversationId, long messageId, bool approved) =>
        await service.ReactAsync(conversationId, messageId, new ExpressionFeedbackRequest(approved), Token);

    /// <summary>
    /// §2.7 的读法：表态这条请求自己必须在预算内回来。三次点击都按同一个预算等——只给最后一次设预算，
    /// 会让"点击自己排在慢仓储后面"那种写法在第一次点击上就把整条判据吊死（反证 M6 实测：判据不红，
    /// 测试进程一直挂着），而挂住的门禁等于没有门禁。
    /// </summary>
    private static async Task<ExpressionFeedbackResponse> ClickWithinBudgetAsync(ExpressionFeedbackService service,
        string conversationId, long messageId, bool approved)
    {
        var click = ReactAsync(service, conversationId, messageId, approved);
        var winner = await Task.WhenAny(click, Task.Delay(TimeSpan.FromSeconds(2), Token));
        Assert.Same(click, winner);
        return await click;
    }

    private static async Task<string> CodeOfAsync(Func<Task> action)
    {
        var exception = await Assert.ThrowsAsync<ExpressionFeedbackRejectedException>(action);
        return exception.Code;
    }

    private SqliteExpressionPatternRepository Patterns() => new(_database);

    private static ExpressionLearner LiveLearner(CharacterIntelligenceGateway gateway) =>
        gateway.ActiveLearners().Single(x => x.CharacterInstanceId == "card-feedback").Learner;

    private async Task<ExpressionPattern> WaitForPatternAsync(IExpressionPatternRepository store,
        string style, string what, Func<ExpressionPattern, bool>? accept = null)
    {
        // 等的是"后台通道终归会把这一行落库"，不是快慢；快慢由 A_click_never_waits_for_the_pattern_store 判。
        // 预算给到 30 秒：同一台机器上别的 Agent 会同时跑整解决方案门禁，8 秒会在负载下先红在仪器上。
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var row = (await store.ListForCharacterAsync("card-feedback", Token))
                .SingleOrDefault(x => x.Style == style);
            // accept 是必需的：等"这一行存在"会把上一阶段的形状当成本阶段结果（压制落库前就返回）。
            if (row is not null && (accept is null || accept(row))) return row;
            await Task.Delay(25, Token);
        }

        throw new TimeoutException($"后台通道始终没做到：{what}");
    }

    private static async Task WaitForPendingToClearAsync(CharacterIntelligenceGateway gateway)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(30);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (gateway.ActiveLearners().All(x => x.Learner.PendingCount == 0)) return;
            await Task.Delay(25, Token);
        }

        throw new TimeoutException("静置一个排空周期之后队列仍然非空，说明取走动作没有发生");
    }

    private const string Habit = "我嘞个";
    private const string Bubble = "我嘞个，居然活过来了";

    /// <summary>§2.1：两次不同轮次的明确点头真的长成一行 Learned，并且活过重启。</summary>
    [Fact]
    public async Task Two_explicit_approvals_on_two_turns_become_a_learned_row_that_survives_a_restart()
    {
        var conversationId = await SeedConversationAsync();
        await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var gateway = Gateway();
        var sink = new CharacterIntelligenceOutputSink(new SilentSink(), gateway);
        var service = new ExpressionFeedbackService(gateway, await RuntimeAsync(registry, true));

        var first = await SpeakAsync(gateway, sink, conversationId, 1, "聊阳台的植物", Bubble);
        var second = await SpeakAsync(gateway, sink, conversationId, 2, "聊阳台的植物", Bubble);

        Assert.Equal(ExpressionFeedbackOutcomes.Recorded,
            (await ReactAsync(service, conversationId, first, true)).Outcome);
        Assert.Equal(ExpressionFeedbackOutcomes.Recorded,
            (await ReactAsync(service, conversationId, second, true)).Outcome);

        var channel = Channel(gateway);
        await channel.StartAsync(Token);
        ExpressionPattern learned;
        try
        {
            learned = await WaitForPatternAsync(Patterns(), Habit, "两次明确认可没有长成一行 Learned");
            Assert.Equal(ExpressionPatternSource.Learned, learned.Source);
            Assert.Equal(2, learned.EvidenceCount);
            Assert.Equal(2, learned.AcceptedEvidenceIds.Count);
            Assert.Equal(2, learned.AcceptedTurnKeys.Distinct(StringComparer.Ordinal).Count());
            Assert.Equal(0.35, learned.Confidence, 3);
            Assert.False(learned.Suppressed);
            // 学到之后检索一侧拿得到它；Selector 自己的许可（这次可以一个都不用）不归本刀。
            var candidates = new ExpressionRetriever().Retrieve(
                await Patterns().ListForCharacterAsync("card-feedback", Token), "轻微吐槽", "谁啊",
                DateTimeOffset.UtcNow);
            Assert.Contains(candidates, x => x.Pattern.Id == learned.Id);
        }
        finally
        {
            await channel.StopAsync(Token);
        }

        // 重启形状：新网关（新状态域、新学习器、投递账本清空），同一条库。
        var reopened = Gateway();
        // 新网关一个字的内存证据都没有——活着的那一行只可能来自库里，不是进程状态没清干净。
        Assert.Empty(reopened.ActiveLearners());
        var afterRestart = Assert.Single(await new SqliteExpressionPatternRepository(_database)
            .ListForCharacterAsync("card-feedback", Token));
        Assert.Equal(learned.Id, afterRestart.Id);
        Assert.Equal(ExpressionPatternSource.Learned, afterRestart.Source);
        Assert.Equal(learned.AcceptedEvidenceIds, afterRestart.AcceptedEvidenceIds);
        Assert.Equal(learned.AcceptedTurnKeys, afterRestart.AcceptedTurnKeys);
    }

    /// <summary>§2.2 + §3.1：不点就永远学不到。投递本身、模型重复说同一句都不是认可来源。</summary>
    [Fact]
    public async Task Deliveries_without_a_single_click_never_teach_a_habit()
    {
        var conversationId = await SeedConversationAsync();
        var gateway = Gateway();
        var sink = new CharacterIntelligenceOutputSink(new SilentSink(), gateway);

        for (var turn = 1; turn <= 5; turn++)
            await SpeakAsync(gateway, sink, conversationId, turn, "聊阳台的植物", Bubble);

        var channel = Channel(gateway);
        await channel.StartAsync(Token);
        try
        {
            await WaitForPendingToClearAsync(gateway);
            await Task.Delay(120, Token);
            Assert.Empty(await Patterns().ListForCharacterAsync("card-feedback", Token));
            Assert.Equal(0, gateway.ActiveLearners().Single(x => x.CharacterInstanceId == "card-feedback")
                .Learner.DroppedCount);
        }
        finally
        {
            await channel.StopAsync(Token);
        }
    }

    /// <summary>§2.3：同一条气泡连点三次只算一次认可，仍然够不着那道两次的门槛，不晋升。</summary>
    [Fact]
    public async Task Three_clicks_on_one_bubble_are_one_approval_and_promote_nothing()
    {
        var conversationId = await SeedConversationAsync();
        await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var gateway = Gateway();
        var sink = new CharacterIntelligenceOutputSink(new SilentSink(), gateway);
        var service = new ExpressionFeedbackService(gateway, await RuntimeAsync(registry, true));

        var bubble = await SpeakAsync(gateway, sink, conversationId, 1, "聊阳台的植物", Bubble);
        for (var click = 0; click < 3; click++)
            Assert.Equal(ExpressionFeedbackOutcomes.Recorded,
                (await ReactAsync(service, conversationId, bubble, true)).Outcome);

        var channel = Channel(gateway);
        await channel.StartAsync(Token);
        try
        {
            await WaitForPendingToClearAsync(gateway);
            await Task.Delay(120, Token);
            // 一行都不该有：三次点击在 Core 那道按 turnKey 的去重之后仍是一次认可。
            Assert.Empty(await Patterns().ListForCharacterAsync("card-feedback", Token));
        }
        finally
        {
            await channel.StopAsync(Token);
        }
    }

    /// <summary>§2.4："别这样说"真的压住，之后连自己的点头都救不回来。</summary>
    [Fact]
    public async Task An_explicit_rejection_silences_the_shape_and_no_later_click_revives_it()
    {
        var conversationId = await SeedConversationAsync();
        await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var gateway = Gateway();
        var sink = new CharacterIntelligenceOutputSink(new SilentSink(), gateway);
        var service = new ExpressionFeedbackService(gateway, await RuntimeAsync(registry, true));

        var first = await SpeakAsync(gateway, sink, conversationId, 1, "聊阳台的植物", Bubble);
        var second = await SpeakAsync(gateway, sink, conversationId, 2, "聊阳台的植物", Bubble);
        await ReactAsync(service, conversationId, first, true);
        await ReactAsync(service, conversationId, second, true);

        var channel = Channel(gateway);
        await channel.StartAsync(Token);
        try
        {
            await WaitForPatternAsync(Patterns(), Habit, "先学起来这一步没发生，压制无从判起");

            var third = await SpeakAsync(gateway, sink, conversationId, 3, "聊阳台的植物", Bubble);
            await ReactAsync(service, conversationId, third, false);
            await WaitForPatternAsync(Patterns(), Habit, "明确拒绝没有把这一句压下去", x => x.Suppressed);

            // 压下去之后再用两次点头去救：她说话仍然不带这个味道。
            var fourth = await SpeakAsync(gateway, sink, conversationId, 4, "聊阳台的植物", Bubble);
            var fifth = await SpeakAsync(gateway, sink, conversationId, 5, "聊阳台的植物", Bubble);
            await ReactAsync(service, conversationId, fourth, true);
            await ReactAsync(service, conversationId, fifth, true);
            await Task.Delay(250, Token);

            var rows = await Patterns().ListForCharacterAsync("card-feedback", Token);
            var row = Assert.Single(rows);
            Assert.True(row.Suppressed);
            Assert.Empty(new ExpressionRetriever().Retrieve(rows, "轻微吐槽", "我嘞个", DateTimeOffset.UtcNow));
        }
        finally
        {
            await channel.StopAsync(Token);
        }
    }

    /// <summary>
    /// §2.6 后端那一半：开关是真的开关。关掉时连一次本来完全成立的分析也拒掉（403 抢在归属解析之前），
    /// 在同一个运行时实例上打开就立即可用——不需要重启，也不需要新网关。
    /// </summary>
    [Fact]
    public async Task The_switch_gates_a_valid_approval_and_reopening_it_needs_no_restart()
    {
        var conversationId = await SeedConversationAsync();
        await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var gateway = Gateway();
        var sink = new CharacterIntelligenceOutputSink(new SilentSink(), gateway);
        var runtime = await RuntimeAsync(registry, false);
        var service = new ExpressionFeedbackService(gateway, runtime);

        var bubble = await SpeakAsync(gateway, sink, conversationId, 1, "聊阳台的植物", Bubble);
        // 投递本身已经记下一笔互动；判的是这次被拒的表态有没有额外添一笔证据。
        var pendingFromDeliveryOnly = LiveLearner(gateway).PendingCount;
        Assert.Equal(ExpressionFeedbackErrorCodes.Disabled,
            await CodeOfAsync(async () => await ReactAsync(service, conversationId, bubble, true)));
        // 拒得很干净：一次没打开的表态不该在后台留下任何一笔额外的东西。
        Assert.Equal(pendingFromDeliveryOnly, LiveLearner(gateway).PendingCount);

        await runtime.ApplyPreferencesSnapshotAsync(new UserPreferences { ExplicitExpressionFeedback = true }, Token);
        Assert.Equal(ExpressionFeedbackOutcomes.Recorded,
            (await ReactAsync(service, conversationId, bubble, true)).Outcome);
    }

    /// <summary>
    /// §3.1 + §4.4 的承重墙：对不上就不认，而且绝不挂到"最近那一轮"上。这条消息确实存在于这个会话里
    /// （直接落库、本进程没有它的投递凭据），而此刻会话里正有一轮在飞——按最近一轮归属的写法会把表态
    /// 记到那一轮头上，这正是 <c>DEF-P3SEAM-02</c> 花一整刀才拆掉的形状。
    /// </summary>
    [Fact]
    public async Task An_unattributable_bubble_is_refused_instead_of_hanging_on_the_latest_turn()
    {
        var conversationId = await SeedConversationAsync();
        await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var gateway = Gateway();
        var sink = new CharacterIntelligenceOutputSink(new SilentSink(), gateway);
        var service = new ExpressionFeedbackService(gateway, await RuntimeAsync(registry, true));

        // 重启前旧气泡的形状：库里有这一行，投递凭据没有。
        var stale = await _database.AddMessageAsync(conversationId, ChatRole.Assistant, Bubble, null, Token);
        // 此刻同一会话真的有一轮在飞（还没走到终局）。
        var turn = new TurnIdentity(7, 1, 7);
        await gateway.CreateAdvisor(conversationId, Card(), new TurnTakingSettings())
            .AdviseAsync(Gate(turn, "聊阳台的植物"), Token);
        await sink.MessageDeliveredAsync(conversationId,
            await _database.AddMessageAsync(conversationId, ChatRole.Assistant, Bubble, null, Token),
            new RuntimePlannedMessage(0, Bubble, null), MessageSources.ModelReply, Token);

        Assert.Equal(ExpressionFeedbackErrorCodes.NotAttributable,
            await CodeOfAsync(async () => await ReactAsync(service, conversationId, stale, true)));
        // 关键一半：在飞那一轮的学习器一个字都没收到，而不是"顺便替旧气泡作证"。
        Assert.Equal(0, gateway.ActiveLearners().Single(x => x.CharacterInstanceId == "card-feedback")
            .Learner.PendingCount);
    }

    /// <summary>§4.4：气泡属于另一个会话时单独成一种拒法，不能记进这里的某一轮。</summary>
    [Fact]
    public async Task A_bubble_from_another_conversation_is_not_owed_to_this_one()
    {
        var hers = await SeedConversationAsync();
        var others = await SeedConversationAsync();
        await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var gateway = Gateway();
        var sink = new CharacterIntelligenceOutputSink(new SilentSink(), gateway);
        var service = new ExpressionFeedbackService(gateway, await RuntimeAsync(registry, true));

        var hersBubble = await SpeakAsync(gateway, sink, hers, 1, "聊阳台的植物", Bubble);
        await SpeakAsync(gateway, sink, others, 1, "聊阳台的植物", Bubble);

        Assert.Equal(ExpressionFeedbackErrorCodes.WrongConversation,
            await CodeOfAsync(async () => await ReactAsync(service, others, hersBubble, true)));
    }

    /// <summary>§3.6 的口径：占位符那一类"她其实没说话"的气泡教不出东西，也不重放。</summary>
    [Fact]
    public async Task A_bubble_where_she_did_not_really_speak_teaches_nothing()
    {
        var conversationId = await SeedConversationAsync();
        await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var gateway = Gateway();
        var sink = new CharacterIntelligenceOutputSink(new SilentSink(), gateway);
        var service = new ExpressionFeedbackService(gateway, await RuntimeAsync(registry, true));

        var placeholder = await SpeakAsync(gateway, sink, conversationId, 1, "聊阳台的植物", "……");
        var pendingFromDeliveryOnly = LiveLearner(gateway).PendingCount;
        Assert.Equal(ExpressionFeedbackOutcomes.NothingToLearn,
            (await ReactAsync(service, conversationId, placeholder, true)).Outcome);
        // 点击是真的，但那一轮她没有真的说话——不重放，也就不会把占位符记成一次可学的互动。
        Assert.Equal(pendingFromDeliveryOnly, LiveLearner(gateway).PendingCount);
    }

    /// <summary>§2.5：这条消息编号根本不是编号时明确拒，而不是把它当成一次找不到。</summary>
    [Fact]
    public async Task A_message_id_that_is_not_one_is_a_request_error_not_a_miss()
    {
        var conversationId = await SeedConversationAsync();
        await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var gateway = Gateway();
        var service = new ExpressionFeedbackService(gateway, await RuntimeAsync(registry, true));

        await Assert.ThrowsAsync<ApiValidationException>(async () =>
            await service.ReactAsync(conversationId, 0, new ExpressionFeedbackRequest(true), Token));
    }

    /// <summary>库里有、这个会话里没有：那也是一种独立的拒法，不许与"归属对不上"混成一种。</summary>
    [Fact]
    public async Task A_message_that_is_not_in_this_conversation_is_reported_as_missing()
    {
        var conversationId = await SeedConversationAsync();
        await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var gateway = Gateway();
        var service = new ExpressionFeedbackService(gateway, await RuntimeAsync(registry, true));

        Assert.Equal(ExpressionFeedbackErrorCodes.MessageMissing,
            await CodeOfAsync(async () => await ReactAsync(service, conversationId, 999_999, true)));
    }

    /// <summary>
    /// §2.7：一次点击只往队列里放。写库卡在后台出不来时，表态照样当场返回，下一条回复照样走得完。
    /// </summary>
    [Fact]
    public async Task A_click_never_waits_for_the_pattern_store()
    {
        var conversationId = await SeedConversationAsync();
        await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var store = new BlockingStore(_database);
        var gateway = Gateway(store);
        var sink = new CharacterIntelligenceOutputSink(new SilentSink(), gateway);
        var service = new ExpressionFeedbackService(gateway, await RuntimeAsync(registry, true));

        var first = await SpeakAsync(gateway, sink, conversationId, 1, "聊阳台的植物", Bubble);
        var second = await SpeakAsync(gateway, sink, conversationId, 2, "聊阳台的植物", Bubble);
        await ClickWithinBudgetAsync(service, conversationId, first, true);
        await ClickWithinBudgetAsync(service, conversationId, second, true);

        var channel = Channel(gateway);
        try
        {
            // 两次认可凑够一次真正的写库，后台通道一起来就卡在库里出不来（人为拖慢）。
            await channel.StartAsync(Token);
            await store.EnteredBlockedSave.WaitAsync(TimeSpan.FromSeconds(30), Token);

            // 后台堵在写库里，此刻点下的表态只往队列里放，不等那张库。
            var third = await SpeakAsync(gateway, sink, conversationId, 3, "聊阳台的植物", Bubble);
            Assert.Equal(ExpressionFeedbackOutcomes.Recorded,
                (await ClickWithinBudgetAsync(service, conversationId, third, true)).Outcome);

            // 后台还堵在写库里，一整条可见路径必须走得完（工单 §2.3 那个方向）。
            var next = SpeakAsync(gateway, sink, conversationId, 4, "我今天加班到十点", "辛苦你了");
            var winner = await Task.WhenAny(next, Task.Delay(TimeSpan.FromSeconds(3), Token));
            Assert.Same(next, winner);
            await next;
        }
        finally
        {
            store.Release();
            await channel.StopAsync(Token);
        }
    }

    /// <summary>
    /// §2.6 的另一半：这个开关走的就是仓库里那条既有设置通路，不是新搭的一条。写进去之后运行时读到的
    /// 就是这一份（服务每次表态都实时读它），没带这个字段的另一次保存也不会把它悄悄关掉——
    /// 别的设置页保存时不该顺手关掉别人的入口。
    /// </summary>
    [Fact]
    public async Task The_advanced_switch_travels_the_existing_settings_path_and_survives_an_omitted_write()
    {
        await using var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var configuration = new ConfigurationManager();
        configuration["Pry:DataDirectory"] = _root;
        var runtime = new BackendRuntime(configuration, registry, NullLogger<BackendRuntime>.Instance);
        await runtime.StartAsync(Token);
        await using var sessions = new ConversationSessionService(_database, runtime,
            new MediaAssetStore(configuration), NullLogger<ConversationSessionService>.Instance);
        var service = new ConfigurationApplicationService(configuration, runtime, sessions,
            new MediaAssetStore(configuration), _database,
            new CharacterLearningCleanup(new SqliteCharacterStateRepository(_database),
                new SqliteBehaviorPatternRepository(_database), new SqliteBehaviorExperienceRepository(_database),
                new SqliteExpressionPatternRepository(_database)));

        var initial = await service.UpdatePreferencesAsync(
            new UpdateClientPreferencesRequest(null, null, null, null, null, null, null), Token);
        Assert.False(initial.ExplicitExpressionFeedback);
        Assert.False(runtime.Preferences.ExplicitExpressionFeedback);

        var opened = await service.UpdatePreferencesAsync(
            new UpdateClientPreferencesRequest(null, null, null, null, null, null, null, true), Token);
        Assert.True(opened.ExplicitExpressionFeedback);
        Assert.True(runtime.Preferences.ExplicitExpressionFeedback);
        Assert.Contains("\"explicitExpressionFeedback\": true",
            await File.ReadAllTextAsync(Path.Combine(_root, "preferences.json"), Token));

        var untouched = await service.UpdatePreferencesAsync(
            new UpdateClientPreferencesRequest(null, null, null, null, null, null, null), Token);
        Assert.True(untouched.ExplicitExpressionFeedback);

        var closed = await service.UpdatePreferencesAsync(
            new UpdateClientPreferencesRequest(null, null, null, null, null, null, null, false), Token);
        Assert.False(closed.ExplicitExpressionFeedback);
        Assert.False(runtime.Preferences.ExplicitExpressionFeedback);
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

    /// <summary>只挡第一次写库，其余一律原样透传——读的一侧不透传就会把被测对象换成自己。</summary>
    private sealed class BlockingStore : IExpressionPatternRepository
    {
        private readonly SqliteExpressionPatternRepository _inner;
        private readonly TaskCompletionSource _enteredBlockedSave =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseBlockedSave =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _saveCalls;

        public BlockingStore(MemoryDatabase database) => _inner = new SqliteExpressionPatternRepository(database);

        public Task EnteredBlockedSave => _enteredBlockedSave.Task;
        public void Release() => _releaseBlockedSave.TrySetResult();

        public Task<IReadOnlyList<ExpressionPattern>> ListForCharacterAsync(string characterInstanceId,
            CancellationToken cancellationToken = default) =>
            _inner.ListForCharacterAsync(characterInstanceId, cancellationToken);

        public async Task SaveAsync(ExpressionPattern pattern, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _saveCalls) == 1)
            {
                _enteredBlockedSave.TrySetResult();
                await _releaseBlockedSave.Task.WaitAsync(cancellationToken);
            }

            await _inner.SaveAsync(pattern, cancellationToken);
        }

        public Task<bool> DeleteAsync(string expressionPatternId, CancellationToken cancellationToken = default) =>
            _inner.DeleteAsync(expressionPatternId, cancellationToken);

        public Task<int> PurgeAsync(string characterInstanceId, CancellationToken cancellationToken = default) =>
            _inner.PurgeAsync(characterInstanceId, cancellationToken);
    }
}

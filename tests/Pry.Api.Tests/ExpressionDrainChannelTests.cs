using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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
/// <c>RT-004-DRAIN</c> 的判据：表达学习那条队列第一次有了取走它的人。
/// 跑真 SQLite（<c>SqliteExpressionPatternRepository</c>）、真 <see cref="CharacterIntelligenceGateway"/>
/// 与真的 <see cref="ExpressionDrainService"/>（宿主形状：自己 <c>StartAsync</c>，让
/// <c>BackgroundService.ExecuteAsync</c> 那条 tick 循环真的转起来），因为这一刀唯一可能出错的地方
/// 就是"谁在什么时候排空"这件事本身。
///
/// <para>
/// 各条守的形状：明确认可的口癖经后台通道写进模式账本并被检索得到；投递量超过队列容量而一条不丢；
/// 存储卡在排空中间时可见回复照旧走得完整轮；写库失败与关机截断都不需要用户重说一遍；
/// 一张卡的存储坏了不终止通道也不连累别的卡；明确纠正不被随后的每一次 tick 复活；
/// 学到的东西跨重启还在且不被重建的学习器重复加权。最后两条是源码门：注册本身没有行为断言能替它守着
/// （行为判据是自己 new 出服务来的），而"别把排空搬上轮次路径"正是这条通道的实质。
/// </para>
///
/// <para>
/// 一处必须点明的替身：本文件用 <see cref="Approve"/> 把"用户明确认可了这句说法"直接送进那张卡当前
/// 活着的学习器。<c>ObserveFeedbackAsync</c> 是宿主里唯一携带认可的入口，而它在生产路径上仍然零调用点
/// （工单 <c>docs/dispatch-rt004-drain.md</c> §3.7 明令本刀不接，那属第 3 层 <c>RT-006</c>）。
/// 所以本刀判的是"队列有没有人取、取了对不对、会不会丢、会不会拖慢回复"，不判"界面能不能点头"。
/// 真机今天仍不会长出 <c>Learned</c> 行——这条随刀写进台账，不留在这里当默契。
/// </para>
/// </summary>
public sealed class ExpressionDrainChannelTests : IAsyncLifetime
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"pry-expression-drain-{Guid.NewGuid():N}");
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

    private static CharacterDefinition Card(string id = "card-drain") => new()
    {
        Id = id, Name = "小潮", CardName = "小潮", UserName = "你", Identity = "陪伴者", Personality = "沉稳",
        SpeechStyle = "简洁", BehavioralRules = ["不要敷衍用户"], Greeting = "你好。"
    };

    private CharacterIntelligenceGateway Gateway(IExpressionPatternRepository? expressions = null) => new(_database,
        new SqliteCharacterStateRepository(_database), new SqliteBehaviorPatternRepository(_database),
        expressions ?? new SqliteExpressionPatternRepository(_database),
        new SqliteBehaviorExperienceRepository(_database),
        NullLogger<CharacterIntelligenceGateway>.Instance);

    /// <summary>间隔从配置读（发行那条路径含夹取），测试把 15 秒压到 25 毫秒。</summary>
    private static ExpressionDrainService Channel(CharacterIntelligenceGateway gateway) =>
        new(gateway, new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Pry:ExpressionDrainMs"] = "25"
        }).Build(), NullLogger<ExpressionDrainService>.Instance);

    private async Task<string> SeedAsync(string cardId = "card-drain",
        params (ChatRole Role, string Text)[] transcript)
    {
        var conversationId = $"conv-{Guid.NewGuid():N}";
        await _database.EnsureConversationAsync(conversationId, cardId, Token);
        foreach (var (role, text) in transcript)
            await _database.AddMessageAsync(conversationId, role, text, null, Token);
        return conversationId;
    }

    private static ReplyGateContext Gate(TurnIdentity turn, string input) =>
        new(turn, input, true, true, false, RuntimePhase.Planning, false, 1, 0, null, TimeSpan.Zero, 0, false);

    /// <summary>一轮真实投递：说话前那一问、气泡、终局回报，顺序与 Runtime 一致。</summary>
    private static async Task SpeakAsync(CharacterIntelligenceGateway gateway,
        CharacterIntelligenceOutputSink sink, string conversationId, long turnId, string input, string reply,
        CharacterDefinition? card = null)
    {
        var turn = new TurnIdentity(turnId, 1, turnId);
        await gateway.CreateAdvisor(conversationId, card ?? Card(), new TurnTakingSettings())
            .AdviseAsync(Gate(turn, input), Token);
        await sink.MessageDeliveredAsync(conversationId, turnId * 100,
            new RuntimePlannedMessage(0, reply, null), MessageSources.ModelReply, Token);
        await sink.TurnTransitionAsync(conversationId, turn, RuntimeTurnOutcome.Completed, "delivered", Token);
    }

    private static ExpressionLearner LiveLearner(CharacterIntelligenceGateway gateway, string characterId) =>
        gateway.ActiveLearners().Single(x => x.CharacterInstanceId == characterId).Learner;

    /// <summary>
    /// 本文件唯一的替身（见类注释）：把"用户明确点头"送进那张卡的学习器。
    /// 每条认可带自己的 <c>FeedbackEvidenceId</c>，落在不同会话的不同轮次上——一次反馈不能替两轮作证
    /// （<c>One_feedback_reference_cannot_confirm_multiple_turns</c> 的方向）。
    /// </summary>
    private static void Approve(CharacterIntelligenceGateway gateway, string characterId, string assistantText,
        string style, params (string ConversationId, string TurnId)[] approvals)
    {
        var learner = LiveLearner(gateway, characterId);
        foreach (var (conversationId, turnId) in approvals)
            learner.TryEnqueue(new DeliveredTurn(conversationId, turnId, DateTimeOffset.UtcNow,
                "聊阳台的植物", assistantText)
            {
                FeedbackEvidenceId = $"feedback-{conversationId}-{turnId}",
                AcceptedStyles = [style]
            });
    }

    private SqliteExpressionPatternRepository Patterns() => new(_database);

    private async Task<ExpressionPattern> WaitForPatternAsync(IExpressionPatternRepository store,
        string characterId, string style, string what, Func<ExpressionPattern, bool>? accept = null)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(8);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var row = (await store.ListForCharacterAsync(characterId, Token))
                .SingleOrDefault(x => x.Style == style);
            // accept 是必需的：等"这一行存在"会让上一阶段的旧形状被当成这一步的结果（压制落库前就返回）。
            if (row is not null && (accept is null || accept(row))) return row;
            await Task.Delay(25, Token);
        }

        throw new TimeoutException($"后台排空通道始终没做到：{what}");
    }

    private static async Task WaitForPendingToClearAsync(CharacterIntelligenceGateway gateway)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(8);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (gateway.ActiveLearners().All(x => x.Learner.PendingCount == 0)) return;
            await Task.Delay(25, Token);
        }

        throw new TimeoutException("静置一个排空周期之后队列仍然非空，说明取走动作没有发生");
    }

    [Fact]
    public async Task Approved_habits_reach_the_pattern_ledger_through_the_background_channel()
    {
        var first = await SeedAsync();
        var second = await SeedAsync();
        var gateway = Gateway();
        var sink = new CharacterIntelligenceOutputSink(new SilentSink(), gateway);

        // 先让这张卡真的说上一句话：状态域是那时才建起来的，通道取用的就是这一个学习器。
        await SpeakAsync(gateway, sink, first, 1, "聊阳台的植物", "浇水就好");
        Assert.Single(gateway.ActiveLearners());

        // 同一张卡、两个会话、两次独立明确认可（工单 §2.1 要求的形状）。
        Approve(gateway, "card-drain", "我嘞个，居然活过来了", "我嘞个", (first, "1"), (second, "2"));

        var channel = Channel(gateway);
        await channel.StartAsync(Token);
        try
        {
            var pattern = await WaitForPatternAsync(Patterns(), "card-drain", "我嘞个",
                "明确认可的口癖没有写进 expression_pattern");

            Assert.Equal(ExpressionPatternSource.Learned, pattern.Source);
            Assert.Equal(2, pattern.EvidenceCount);
            Assert.Equal(2, pattern.AcceptedEvidenceIds.Count);
            Assert.Equal(2, pattern.AcceptedTurnKeys.Count);
            Assert.Equal(0.35, pattern.Confidence, 3);
            Assert.False(pattern.Suppressed);

            // 检索一侧拿得到它：这就是"下一轮有机会被她用上"的那道门。Selector 自己的许可（可以一个
            // 都不用）不在本刀面上，反向约束 3 由 Core 那些判据守着，三边读数不互顶。
            var candidates = new ExpressionRetriever()
                .Retrieve(await Patterns().ListForCharacterAsync("card-drain", Token), "轻微吐槽", "谁啊",
                    DateTimeOffset.UtcNow);
            Assert.Contains(candidates, x => x.Pattern.Id == pattern.Id);

            Assert.Equal(0, LiveLearner(gateway, "card-drain").DroppedCount);
            await channel.StopAsync(Token);
            Assert.Equal(0, LiveLearner(gateway, "card-drain").PendingCount);
        }
        finally
        {
            await channel.StopAsync(Token);
        }
    }

    [Fact]
    public async Task The_channel_keeps_up_so_no_delivered_turn_is_dropped()
    {
        var conversationId = await SeedAsync();
        var gateway = Gateway();
        var sink = new CharacterIntelligenceOutputSink(new SilentSink(), gateway);
        var channel = Channel(gateway);
        await channel.StartAsync(Token);
        try
        {
            // 累计投递量刻意超过 QueueCapacity = 64；每 16 轮静置一个排空周期。判的是"一条没丢"，
            // 不是"排得动"——没有通道时这里会是 PendingCount 顶到 64 且 DroppedCount 一路涨。
            for (var turn = 1; turn <= 80; turn++)
            {
                await SpeakAsync(gateway, sink, conversationId, turn, "我今天加班到十点，整个人都不好了",
                    $"辛苦你了，第 {turn} 次");
                if (turn % 16 == 0) await WaitForPendingToClearAsync(gateway);
            }

            Assert.Equal(0, LiveLearner(gateway, "card-drain").DroppedCount);
        }
        finally
        {
            await channel.StopAsync(Token);
        }

        await WaitForPendingToClearAsync(gateway);
    }

    [Fact]
    public async Task A_store_that_stops_mid_drain_cannot_delay_a_visible_reply()
    {
        var conversationId = await SeedAsync();
        var store = new ControllableStore(_database) { BlockFirstSave = true };
        var gateway = Gateway(store);
        var sink = new CharacterIntelligenceOutputSink(new SilentSink(), gateway);

        await SpeakAsync(gateway, sink, conversationId, 1, "聊阳台的植物", "浇水就好");
        Approve(gateway, "card-drain", "我嘞个，居然活过来了", "我嘞个", (conversationId, "1"),
            ("other-conversation", "2"));

        var channel = Channel(gateway);
        await channel.StartAsync(Token);
        try
        {
            // 让后台排空停在写库里出不来（工单 §2.3 要求的人为拖慢）。
            await store.EnteredBlockedSave.WaitAsync(TimeSpan.FromSeconds(8), Token);

            // 此时一整轮可见路径必须走得完：那一问、气泡、终局回报，没有一步等这个排空。
            var visible = SpeakAsync(gateway, sink, conversationId, 2, "我今天加班到十点", "辛苦你了");
            var winner = await Task.WhenAny(visible, Task.Delay(TimeSpan.FromSeconds(3), Token));
            Assert.Same(visible, winner);
            await visible;
        }
        finally
        {
            store.Release();
            await channel.StopAsync(Token);
        }
    }

    [Fact]
    public async Task A_write_that_fails_is_picked_up_by_the_next_tick_without_the_user_repeating_it()
    {
        var conversationId = await SeedAsync();
        var store = new ControllableStore(_database) { FailFirstSave = true, BlockFirstSave = false };
        var gateway = Gateway(store);
        var sink = new CharacterIntelligenceOutputSink(new SilentSink(), gateway);

        await SpeakAsync(gateway, sink, conversationId, 1, "聊阳台的植物", "浇水就好");
        Approve(gateway, "card-drain", "我嘞个，居然活过来了", "我嘞个", (conversationId, "1"),
            ("other-conversation", "2"));

        var channel = Channel(gateway);
        await channel.StartAsync(Token);
        try
        {
            var pattern = await WaitForPatternAsync(store, "card-drain", "我嘞个",
                "写库失败之后下一次 tick 没把同一句口癖补上");

            // 补一次，不多加权：一次失败的写入不该变成两条证据。
            Assert.Equal(2, pattern.EvidenceCount);
            await Task.Delay(150, Token);
            Assert.Equal(2, Assert.Single(await store.ListForCharacterAsync("card-drain", Token)).EvidenceCount);
        }
        finally
        {
            await channel.StopAsync(Token);
        }
    }

    [Fact]
    public async Task A_drain_cut_short_by_shutdown_loses_nothing()
    {
        var conversationId = await SeedAsync();
        var store = new ControllableStore(_database) { BlockFirstSave = true };
        var gateway = Gateway(store);
        var sink = new CharacterIntelligenceOutputSink(new SilentSink(), gateway);

        await SpeakAsync(gateway, sink, conversationId, 1, "聊阳台的植物", "浇水就好");
        Approve(gateway, "card-drain", "我嘞个，居然活过来了", "我嘞个", (conversationId, "1"),
            ("other-conversation", "2"));

        var first = Channel(gateway);
        await first.StartAsync(Token);
        await store.EnteredBlockedSave.WaitAsync(TimeSpan.FromSeconds(8), Token);
        // 宿主退出把这一次排空从中间截断；网关还在，学习器与它的脏标记也还在。
        await first.StopAsync(Token);
        store.Release();

        var restarted = Channel(gateway);
        await restarted.StartAsync(Token);
        try
        {
            var pattern = await WaitForPatternAsync(store, "card-drain", "我嘞个",
                "被关机截断的排空没有在下一次启动时把活儿补完");
            Assert.Equal(2, pattern.EvidenceCount);
        }
        finally
        {
            await restarted.StopAsync(Token);
        }
    }

    [Fact]
    public async Task One_card_whose_store_throws_cannot_stop_the_channel_or_the_next_card()
    {
        var brokenConversation = await SeedAsync("card-broken");
        var healthyConversation = await SeedAsync("card-healthy");
        var store = new ControllableStore(_database) { ThrowsFor = "card-broken", BlockFirstSave = false };
        var gateway = Gateway(store);
        var sink = new CharacterIntelligenceOutputSink(new SilentSink(), gateway);

        await SpeakAsync(gateway, sink, brokenConversation, 1, "聊阳台的植物", "浇水就好", Card("card-broken"));
        await SpeakAsync(gateway, sink, healthyConversation, 1, "聊阳台的植物", "浇水就好", Card("card-healthy"));
        Assert.Equal(2, gateway.ActiveLearners().Count);

        Approve(gateway, "card-broken", "我嘞个，居然活过来了", "我嘞个", (brokenConversation, "1"),
            ("other-conversation", "2"));
        Approve(gateway, "card-healthy", "我嘞个，居然活过来了", "我嘞个", (healthyConversation, "1"),
            ("other-conversation", "2"));

        var channel = Channel(gateway);
        await channel.StartAsync(Token);
        try
        {
            var healthy = await WaitForPatternAsync(store, "card-healthy", "我嘞个",
                "坏掉的卡把后台通道带停，健康的那张再也排不到");
            Assert.Equal(2, healthy.EvidenceCount);

            // 通道还活着：健康卡随后学的第二句照样落库。这一半与枚举顺序无关——异常一旦逃出单次排空，
            // tick 循环就死了，之后再教什么都不会落库。
            Approve(gateway, "card-healthy", "不是，它居然活了", "不是", ("third-conversation", "5"),
                ("third-conversation", "6"));
            await WaitForPatternAsync(store, "card-healthy", "不是", "坏卡之后通道已经不再排空任何卡");
        }
        finally
        {
            await channel.StopAsync(Token);
        }
    }

    [Fact]
    public async Task An_explicit_correction_survives_every_tick_that_follows()
    {
        var conversationId = await SeedAsync();
        var gateway = Gateway();
        var sink = new CharacterIntelligenceOutputSink(new SilentSink(), gateway);
        await SpeakAsync(gateway, sink, conversationId, 1, "聊阳台的植物", "浇水就好");

        var channel = Channel(gateway);
        await channel.StartAsync(Token);
        try
        {
            Approve(gateway, "card-drain", "我嘞个，居然活过来了", "我嘞个", (conversationId, "1"),
                ("other-conversation", "2"));
            await WaitForPatternAsync(Patterns(), "card-drain", "我嘞个", "口癖没先被学到，压制无从判起");

            // 用户明确说别再这么说话（工单 §1 里那条纠正分支的宿主形状）。
            LiveLearner(gateway, "card-drain").TryEnqueue(new DeliveredTurn(conversationId, "9",
                DateTimeOffset.UtcNow, "别老那么说话", "好。") { RejectedStyles = ["我嘞个"] });

            var suppressed = await WaitForPatternAsync(Patterns(), "card-drain", "我嘞个",
                "明确纠正之后压制没有落库", x => x.Suppressed);

            // 落库之后，再多的 tick 与再多的"她又说了这句"都不许把它复活。
            var learner = LiveLearner(gateway, "card-drain");
            for (var round = 0; round < 3; round++)
            {
                learner.TryEnqueue(new DeliveredTurn($"late-{round}", $"{round}", DateTimeOffset.UtcNow,
                    "聊阳台的植物", "我嘞个，又活过来了"));
                await Task.Delay(120, Token);
            }

            var rows = await Patterns().ListForCharacterAsync("card-drain", Token);
            Assert.Single(rows);
            Assert.True(Assert.Single(rows).Suppressed);
            Assert.Equal(suppressed.EvidenceCount, Assert.Single(rows).EvidenceCount);
            Assert.Empty(new ExpressionRetriever().Retrieve(rows, "轻微吐槽", "我嘞个", DateTimeOffset.UtcNow));
        }
        finally
        {
            await channel.StopAsync(Token);
        }
    }

    [Fact]
    public async Task A_learned_habit_survives_a_restart_without_being_counted_twice()
    {
        var conversationId = await SeedAsync();
        var other = await SeedAsync();
        var gateway = Gateway();
        var sink = new CharacterIntelligenceOutputSink(new SilentSink(), gateway);
        await SpeakAsync(gateway, sink, conversationId, 1, "聊阳台的植物", "浇水就好");

        var first = Channel(gateway);
        await first.StartAsync(Token);
        Approve(gateway, "card-drain", "我嘞个，居然活过来了", "我嘞个", (conversationId, "1"), (other, "2"));
        var before = await WaitForPatternAsync(Patterns(), "card-drain", "我嘞个", "重启前就没学到，跨重启无从判起");
        await first.StopAsync(Token);

        // 重启形状：新网关（新状态域、新学习器），同一条库。
        var reopened = Gateway();
        var reopenedSink = new CharacterIntelligenceOutputSink(new SilentSink(), reopened);
        await SpeakAsync(reopened, reopenedSink, conversationId, 3, "聊阳台的植物", "嗯");

        var second = Channel(reopened);
        await second.StartAsync(Token);
        try
        {
            Approve(reopened, "card-drain", "我嘞个，居然活过来了", "我嘞个", (conversationId, "1"), (other, "2"));
            await Task.Delay(250, Token);

            var after = Assert.Single(await Patterns().ListForCharacterAsync("card-drain", Token));
            Assert.Equal(2, after.EvidenceCount);
            Assert.Equal(before.Confidence, after.Confidence, 3);
            Assert.Equal(before.AcceptedEvidenceIds, after.AcceptedEvidenceIds);
        }
        finally
        {
            await second.StopAsync(Token);
        }
    }

    [Fact]
    public void The_host_registers_the_channel_and_nothing_on_a_turn_path_drains_the_queue()
    {
        // 源码门：行为判据是自己 new 出服务来的，只有这一条能发现"发行链上没人启动它"。
        var root = RepositoryRoot();
        var program = File.ReadAllText(Path.Combine(root, "src", "Pry.Api", "Program.cs"));
        // 只数没被注释掉的那一行：`// builder.Services.AddHostedService<...>()` 在文件里留着同样的
        // token，纯出现次数会对"把注册注释掉"这种最安静的假绿放行。
        int Live(string text, string token) => text.Replace('\r', '\n').Split('\n').Count(line =>
            line.Contains(token, StringComparison.Ordinal)
            && !line.TrimStart().StartsWith("//", StringComparison.Ordinal));
        Assert.Equal(1, Live(program, "AddHostedService<ExpressionDrainService>()"));

        // 注册成 BackgroundService 才会被宿主启动；只 new 不用是一个更安静的失败。
        Assert.True(typeof(BackgroundService).IsAssignableFrom(typeof(ExpressionDrainService)));

        // 反向约束 2：表达队列的排空只能发生在后台通道里。这三个文件是"一轮说话"的必经之路，
        // 任何一处出现排空调用就是把后台活搬上可见回复。（`ConversationSessionService.cs` 不在扫描面里：
        // 它那句 `session.Runtime.DrainAsync(...)` 排的是 Runtime 自己的待投递队列，同名不同物。）
        var turnPathFiles = new[]
        {
            Path.Combine(root, "src", "Pry.Api", "Runtime", "CharacterIntelligenceGateway.cs"),
            Path.Combine(root, "src", "Pry.Core", "Intelligence", "CharacterIntelligenceService.cs"),
            Path.Combine(root, "src", "Pry.Core", "Expression", "ExpressionService.cs")
        };
        foreach (var file in turnPathFiles)
        {
            Assert.True(File.Exists(file), $"轮次路径的扫描面收缩了：{file} 不在原位");
            Assert.DoesNotContain("DrainAsync(", File.ReadAllText(file), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void The_expression_domain_still_promises_it_never_delays_a_message()
    {
        // 工单 §3.2 点名那句注释：改掉它等于给"把排空搬上轮次"留了一条自我说服的路。
        var source = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Pry.Core", "Expression",
            "ExpressionService.cs"));
        Assert.Contains("Nothing here sends or delays a message", source, StringComparison.Ordinal);
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null;
             directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Pry.slnx"))) return directory.FullName;
        throw new InvalidOperationException("从测试程序集出发找不到仓库根，无法核对源码约束。");
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

    /// <summary>
    /// 真 SQLite 仓储外面的三个旋钮：卡在写库里、第一次写就抛、只有某一张卡抛。
    /// 读的一侧一律原样透传，否则"扫描器看不见被测对象"那一类假绿就又回来了。
    /// </summary>
    private sealed class ControllableStore : IExpressionPatternRepository
    {
        private readonly SqliteExpressionPatternRepository _inner;
        private readonly TaskCompletionSource _enteredBlockedSave =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseBlockedSave =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _saveCalls;

        public ControllableStore(MemoryDatabase database) =>
            _inner = new SqliteExpressionPatternRepository(database);

        public bool BlockFirstSave { get; init; }
        public bool FailFirstSave { get; init; }
        public string? ThrowsFor { get; init; }

        public Task EnteredBlockedSave => _enteredBlockedSave.Task;
        public void Release() => _releaseBlockedSave.TrySetResult();

        public Task<IReadOnlyList<ExpressionPattern>> ListForCharacterAsync(string characterInstanceId,
            CancellationToken cancellationToken = default) =>
            _inner.ListForCharacterAsync(characterInstanceId, cancellationToken);

        public async Task SaveAsync(ExpressionPattern pattern, CancellationToken cancellationToken = default)
        {
            var attempt = Interlocked.Increment(ref _saveCalls);
            if (ThrowsFor is not null && pattern.CharacterInstanceId == ThrowsFor)
                throw new IOException($"模拟角色 {ThrowsFor} 的表达写库失败");
            if (FailFirstSave && attempt == 1) throw new IOException("模拟第一次写库失败");
            if (BlockFirstSave && attempt == 1)
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

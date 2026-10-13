using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Pry.Api.Runtime;
using Pry.Core.Abstractions;
using Pry.Core.Expression;
using Pry.Core.Inference;
using Pry.Core.Intelligence;
using Pry.Core.Memory;
using Pry.Core.Models;
using Pry.Core.Prompting;
using Pry.Core.Runtime;
using Pry.Core.TurnTaking;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// 生产者接缝的判据：Runtime 说话前那一问与投递后那一报，今天在宿主里真的有人发了。
/// 下面每一条都跑真 SQLite（`Sqlite*Repository` 四个适配器）与真的 <see cref="CharacterIntelligenceGateway"/>，
/// 因为这一刀唯一可能出错的地方就是接线本身——领域语义有
/// <c>tests/Pry.Core.Tests/CharacterIntelligenceTests.cs</c> 那 79 例守着，存储语义有
/// <c>SqliteIntelligenceRepositoryConformanceTests</c> 的 25 例守着，三边读数不互顶。
///
/// <para>
/// 各条守的形状：说出去的话要留下状态与经验；没投递、主动消息、决策失败都不许教一个字；
/// 低信息量时沉默是一等结果而被点名/被提问时不是；决策渲染出的那段话必须真的走到模型跟前，
/// 而这段里一个数字都不能有；同一张卡在两个会话里共用一本账（TurnId 是会话内序号，故意撞一次）。
/// 另有三条钉住网关自己的归属判断：一轮要过两到三次闸，账本记的是模型真读过的那一份而非复校那一份；
/// 同会话两轮并飞时气泡一个字都不记（而不是挑最近问过的那轮挂上去）；首轮 restore 被取消不许把这张卡
/// 永久切成过渡形状。最后一条是源码门：谁把这两处接线悄悄摘掉，绿灯不会跟着他走——接线只在宿主那一处发生，
/// 没有任何一条行为断言能替它守着。
/// </para>
/// </summary>
public sealed class CharacterIntelligenceSeamTests : IAsyncLifetime
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"pry-intelligence-seam-{Guid.NewGuid():N}");
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
        // 这里必须走 SqliteTestPools.Clear：MemoryDatabase 现在自带长期记忆仓储那条串，只清宿主形状会漏掉它。
        SqliteTestPools.Clear(_databasePath);
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        return ValueTask.CompletedTask;
    }

    private SqliteConnection Open() => new(new SqliteConnectionStringBuilder
    {
        DataSource = _databasePath, Mode = SqliteOpenMode.ReadWriteCreate, Cache = SqliteCacheMode.Shared
    }.ToString());

    private static CharacterDefinition Card(string id = "card-seam") => new()
    {
        Id = id, Name = "小潮", CardName = "小潮", UserName = "你", Identity = "陪伴者", Personality = "沉稳",
        SpeechStyle = "简洁", BehavioralRules = ["不要敷衍用户"], Greeting = "你好。"
    };

    private CharacterIntelligenceGateway Gateway(IBehaviorPatternRepository? patterns = null,
        ICharacterStateRepository? stateRepository = null) => new(_database,
        stateRepository ?? new SqliteCharacterStateRepository(_database),
        patterns ?? new SqliteBehaviorPatternRepository(_database),
        new SqliteExpressionPatternRepository(_database), new SqliteBehaviorExperienceRepository(_database),
        NullLogger<CharacterIntelligenceGateway>.Instance);

    /// <summary>把一轮对话按真实顺序落库：Runtime 是先追加用户输入、再成轮、再过闸门，所以这里也先写库。</summary>
    private async Task<string> SeedAsync(params (ChatRole Role, string Text)[] transcript)
    {
        var conversationId = $"conv-{Guid.NewGuid():N}";
        await _database.EnsureConversationAsync(conversationId, "card-seam", Token);
        foreach (var (role, text) in transcript)
            await _database.AddMessageAsync(conversationId, role, text, null, Token);
        return conversationId;
    }

    [Fact]
    public async Task A_delivered_turn_leaves_state_and_experience_where_the_product_left_nothing_before()
    {
        var conversationId = await SeedAsync((ChatRole.User, "我今天加班到十点，整个人都不好了"));
        var gateway = Gateway();
        var sink = new CharacterIntelligenceOutputSink(new CollectingSink(), gateway);
        var turn = new TurnIdentity(1, 1, 1);

        var advice = await gateway.CreateAdvisor(conversationId, Card(), new TurnTakingSettings())
            .AdviseAsync(Gate(turn, "我今天加班到十点，整个人都不好了"), Token);
        Assert.True(advice.ShouldReply);
        await sink.MessageDeliveredAsync(conversationId, 101, new RuntimePlannedMessage(0, "辛苦你了", null),
            MessageSources.ModelReply, Token);
        await sink.TurnTransitionAsync(conversationId, turn, RuntimeTurnOutcome.Completed, "delivered", Token);

        var state = await new SqliteCharacterStateRepository(_database)
            .LoadAsync("card-seam", CharacterIntelligenceGateway.LocalUserId, Token);
        Assert.NotNull(state);
        Assert.Equal(1, state.ExchangeCount);
        Assert.Single(await new SqliteBehaviorExperienceRepository(_database)
            .ListForCharacterAsync("card-seam", 10, Token));
    }

    [Fact]
    public async Task An_undelivered_turn_teaches_nothing()
    {
        var conversationId = await SeedAsync((ChatRole.User, "我今天加班到十点，整个人都不好了"));
        var gateway = Gateway();
        var advisor = gateway.CreateAdvisor(conversationId, Card(), new TurnTakingSettings());
        var sink = new CharacterIntelligenceOutputSink(new CollectingSink(), gateway);
        var turn = new TurnIdentity(2, 1, 1);

        await advisor.AdviseAsync(Gate(turn, "我今天加班到十点，整个人都不好了"), Token);
        await sink.MessageDeliveredAsync(conversationId, 101, new RuntimePlannedMessage(0, "辛苦你了", null),
            MessageSources.ModelReply, Token);
        // 气泡已经落库但整轮被判中断（用户插话、围栏淘汰都是这个形状）：这一轮不算一次完成的互动。
        await sink.TurnTransitionAsync(conversationId, turn, RuntimeTurnOutcome.Interrupted, "interrupted", Token);

        Assert.Null(await new SqliteCharacterStateRepository(_database)
            .LoadAsync("card-seam", CharacterIntelligenceGateway.LocalUserId, Token));
        Assert.Empty(await new SqliteBehaviorExperienceRepository(_database)
            .ListForCharacterAsync("card-seam", 10, Token));
    }

    [Fact]
    public async Task A_low_content_moment_she_chooses_to_skip_says_nothing_and_records_nothing()
    {
        // 她刚说完话，用户回一个"嗯"：这时沉默是一等结果，不是缺陷（docs/character-intelligence.md §3）。
        var conversationId = await SeedAsync((ChatRole.Assistant, "你先忙，我在这儿"), (ChatRole.User, "嗯"));
        var gateway = Gateway();
        var advisor = gateway.CreateAdvisor(conversationId, Card(), new TurnTakingSettings());

        var advice = await advisor.AdviseAsync(Gate(new TurnIdentity(3, 1, 1), "嗯",
            sinceLastCharacterActivity: TimeSpan.FromSeconds(4)), Token);

        Assert.False(advice.ShouldReply);
        Assert.Equal(ResponseIntent.DoNothing, advice.Intent);
        Assert.Null(advice.PromptBlock);
        await new CharacterIntelligenceOutputSink(new CollectingSink(), gateway)
            .TurnTransitionAsync(conversationId, new TurnIdentity(3, 1, 1), RuntimeTurnOutcome.DoNothing,
                "do_nothing", Token);
        Assert.Null(await new SqliteCharacterStateRepository(_database)
            .LoadAsync("card-seam", CharacterIntelligenceGateway.LocalUserId, Token));
    }

    [Fact]
    public async Task Being_addressed_or_asked_never_lets_her_choose_silence()
    {
        // 输入与上一条逐字相同（她刚说完话、用户只回了一个"嗯"），唯一差别是这轮的闸门事实：
        // 用户点了她的名，或者追了个问号。守的是 ReplyGateContext → SituationSnapshot.RequiresReply
        // 这一次翻译本身——摘掉它，被点名的人会安静地站在那里，而界面上看不出任何原因。
        var conversationId = await SeedAsync((ChatRole.Assistant, "你先忙，我在这儿"), (ChatRole.User, "嗯"));
        var gateway = Gateway();

        var addressed = await gateway.CreateAdvisor(conversationId, Card(), new TurnTakingSettings())
            .AdviseAsync(Gate(new TurnIdentity(4, 1, 1), "嗯", directAddress: true,
                sinceLastCharacterActivity: TimeSpan.FromSeconds(4)), Token);
        var asked = await gateway.CreateAdvisor(conversationId, Card(), new TurnTakingSettings())
            .AdviseAsync(Gate(new TurnIdentity(5, 1, 1), "嗯", asksQuestion: true,
                sinceLastCharacterActivity: TimeSpan.FromSeconds(4)), Token);

        Assert.True(addressed.ShouldReply);
        Assert.Equal(ResponseIntent.Acknowledge, addressed.Intent);
        Assert.True(asked.ShouldReply);
        Assert.Equal(ResponseIntent.Acknowledge, asked.Intent);
    }

    [Fact]
    public async Task The_block_she_hands_the_model_explains_the_turn_without_handing_over_any_number()
    {
        var conversationId = await SeedAsync((ChatRole.User, "我今天加班到十点，整个人都不好了"));
        var gateway = Gateway();

        var advice = await gateway.CreateAdvisor(conversationId, Card(), new TurnTakingSettings())
            .AdviseAsync(Gate(new TurnIdentity(5, 1, 1), "我今天加班到十点，整个人都不好了"), Token);

        Assert.NotNull(advice.PromptBlock);
        Assert.Contains("这一轮：", advice.PromptBlock, StringComparison.Ordinal);
        // 记忆没接（MEM-005 未收敛），这一轮必须是"没查"而不是"查了没有"——那句话要真的走到模型跟前。
        Assert.Contains("不要说对方从没提过", advice.PromptBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("信任", advice.PromptBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("熟悉度", advice.PromptBlock, StringComparison.Ordinal);
        // 不暴露数值这条不变量要跨过 Core 边界：块里连一个数字都不该有，比例和小数都不行。
        Assert.DoesNotMatch(new Regex(@"\d"), advice.PromptBlock!);
    }

    [Fact]
    public async Task The_block_she_decided_with_is_the_block_the_model_reads_before_writing_the_reply()
    {
        // 接上"说话前那一问"却把渲染结果丢掉，她只会变哑或照旧说话——学了也不改行为，比不接更坏。
        // 所以这一条走完网关到模型的整段：建议 → TurnRequest.Advice → 生成端口 → 提示词。
        var conversationId = await SeedAsync((ChatRole.User, "我今天加班到十点，整个人都不好了"));
        var turn = new TurnIdentity(5, 1, 1);
        var advice = await Gateway().CreateAdvisor(conversationId, Card(), new TurnTakingSettings())
            .AdviseAsync(Gate(turn, "我今天加班到十点，整个人都不好了"), Token);
        Assert.NotNull(advice.PromptBlock);

        var model = new RecordingModel(new ModelProfile { Id = "text", DisplayName = "Text" },
            """{"messages":[{"type":"text","content":"辛苦你了","stickerId":null}]}""");
        var prompt = await ComposeAsync(conversationId, turn, advice, model);

        Assert.Contains("这一轮：", prompt, StringComparison.Ordinal);
        Assert.Contains("不要说对方从没提过", prompt, StringComparison.Ordinal);

        // 没有行为决策的那一类轮次（主动消息、回落）必须与接线前逐字一致，否则这一刀改了所有人的提示词。
        Assert.DoesNotContain("这一轮：", await ComposeAsync(conversationId, turn, null, model),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_proactive_candidate_keeps_the_shape_the_product_shipped_with()
    {
        // 主动消息送审时 CombinedInput 是她自己打算说的话，不是用户输入；这一刀不给它加生产者。
        var conversationId = await SeedAsync((ChatRole.User, "在忙吗"));
        var gateway = Gateway();
        var sink = new CharacterIntelligenceOutputSink(new CollectingSink(), gateway);
        var turn = new TurnIdentity(6, 1, 1);

        var advice = await gateway.CreateAdvisor(conversationId, Card(), new TurnTakingSettings())
            .AdviseAsync(Gate(turn, "想起你还没睡，就过来看看", proactive: true), Token);
        Assert.Equal("transitional_no_social_judgement", advice.Reason);
        await sink.MessageDeliveredAsync(conversationId, 102, new RuntimePlannedMessage(0, "想起你还没睡，就过来看看", null),
            MessageSources.Proactive, Token);
        await sink.TurnTransitionAsync(conversationId, turn, RuntimeTurnOutcome.Completed, "delivered", Token);

        Assert.Null(await new SqliteCharacterStateRepository(_database)
            .LoadAsync("card-seam", CharacterIntelligenceGateway.LocalUserId, Token));
    }

    [Fact]
    public async Task Two_conversations_on_one_card_share_one_ledger_but_never_take_each_other_s_bubble()
    {
        var first = await SeedAsync((ChatRole.User, "我今天加班到十点，整个人都不好了"));
        var second = await SeedAsync((ChatRole.User, "周末有什么打算吗"));
        var gateway = Gateway();
        var sink = new CharacterIntelligenceOutputSink(new CollectingSink(), gateway);
        // 两边的 TurnId 故意相同：它是会话内序号，账本 key 或气泡归属少了 conversationId 就会互相顶掉。
        var turn = new TurnIdentity(1, 1, 1);

        // 两轮同时在飞，再各自投递、各自收终局——串行跑不出这个形状，所以两条都先问完。
        await gateway.CreateAdvisor(first, Card(), new TurnTakingSettings())
            .AdviseAsync(Gate(turn, "我今天加班到十点，整个人都不好了"), Token);
        await gateway.CreateAdvisor(second, Card(), new TurnTakingSettings())
            .AdviseAsync(Gate(turn, "周末有什么打算吗"), Token);
        await sink.MessageDeliveredAsync(first, 201, new RuntimePlannedMessage(0, "辛苦你了", null),
            MessageSources.ModelReply, Token);
        await sink.TurnTransitionAsync(first, turn, RuntimeTurnOutcome.Completed, "", Token);
        await sink.MessageDeliveredAsync(second, 202, new RuntimePlannedMessage(0, "想想去哪儿", null),
            MessageSources.ModelReply, Token);
        await sink.TurnTransitionAsync(second, turn, RuntimeTurnOutcome.Completed, "", Token);

        var state = await new SqliteCharacterStateRepository(_database)
            .LoadAsync("card-seam", CharacterIntelligenceGateway.LocalUserId, Token);
        Assert.NotNull(state);
        Assert.Equal(2, state.ExchangeCount);
        var experiences = await new SqliteBehaviorExperienceRepository(_database)
            .ListForCharacterAsync("card-seam", 10, Token);
        Assert.Equal(2, experiences.Count);
        // 各会话记的是自己那一轮：串了会话的经验会把"周末"学到"加班"那一轮上去。
        Assert.Equal(new HashSet<string>(StringComparer.Ordinal) { first, second },
            experiences.Select(x => x.ConversationId).ToHashSet(StringComparer.Ordinal));
    }

    [Fact]
    public async Task She_learns_the_decision_the_model_read_not_the_one_the_gate_recheck_handed_back()
    {
        // 一轮要过两到三次闸，每次都问一遍建议器；模型读到的只有第一次那份（DEF-P3SEAM-01）。
        // 账本若改写成"最后问过的那一份"，她学的就是一次根本没起作用的决策——今天不改变她说什么，
        // 改变的是她学到什么，所以这一条必须钉在真库上的经验行，而不是钉在返回值上。
        var conversationId = await SeedAsync((ChatRole.User, "我今天加班到十点，整个人都不好了"));
        var gateway = Gateway();
        var advisor = gateway.CreateAdvisor(conversationId, Card(), new TurnTakingSettings());
        var sink = new CharacterIntelligenceOutputSink(new CollectingSink(), gateway);
        var turn = new TurnIdentity(12, 1, 1);

        var consumedByModel = await advisor.AdviseAsync(Gate(turn, "我今天加班到十点，整个人都不好了"), Token);
        var gateRecheck = await advisor.AdviseAsync(Gate(turn, "周末有什么打算吗"), Token);
        // 两次咨询给出不同的决策，这条判据才有判别力；哪天规划器把两者收敛成同一份，本条要显式作废而不是静默常绿。
        Assert.NotEqual(consumedByModel.Reason, gateRecheck.Reason);

        await sink.MessageDeliveredAsync(conversationId, 301, new RuntimePlannedMessage(0, "辛苦你了", null),
            MessageSources.ModelReply, Token);
        await sink.TurnTransitionAsync(conversationId, turn, RuntimeTurnOutcome.Completed, "delivered", Token);

        var only = Assert.Single(await new SqliteBehaviorExperienceRepository(_database)
            .ListForCharacterAsync("card-seam", 10, Token));
        Assert.Equal(consumedByModel.Reason, $"intelligence:{only.Scenario}:{only.Intent}");
    }

    [Fact]
    public async Task A_bubble_is_only_recorded_when_exactly_one_turn_of_that_conversation_is_in_flight()
    {
        // 外发端口那句气泡回调不带轮身份，签名是契约 §7.1 结论 1 冻住的（DEF-P3SEAM-02）。
        // 同会话两轮并飞在生产里到不了（SingleInFlightTurnBoundaryTests 钉着 ReplyGate 那两道闸的顺序），
        // 这里仍然把它造出来，为的是钉住网关自己那一半：归属说不清时一个字都不记，
        // 而不是"挑最近被问过的那一轮"——按最近猜，正是把上一轮的正文记到下一轮账上的那个形状。
        var conversationId = await SeedAsync((ChatRole.User, "我今天加班到十点，整个人都不好了"));
        var gateway = Gateway();
        var advisor = gateway.CreateAdvisor(conversationId, Card(), new TurnTakingSettings());
        var sink = new CharacterIntelligenceOutputSink(new CollectingSink(), gateway);

        await advisor.AdviseAsync(Gate(new TurnIdentity(13, 1, 1), "我今天加班到十点，整个人都不好了"), Token);
        await advisor.AdviseAsync(Gate(new TurnIdentity(14, 1, 1), "周末有什么打算吗"), Token);
        await sink.MessageDeliveredAsync(conversationId, 302, new RuntimePlannedMessage(0, "辛苦你了", null),
            MessageSources.ModelReply, Token);
        await sink.TurnTransitionAsync(conversationId, new TurnIdentity(13, 1, 1), RuntimeTurnOutcome.Completed,
            "delivered", Token);
        await sink.TurnTransitionAsync(conversationId, new TurnIdentity(14, 1, 1), RuntimeTurnOutcome.Completed,
            "delivered", Token);

        Assert.Empty(await new SqliteBehaviorExperienceRepository(_database)
            .ListForCharacterAsync("card-seam", 10, Token));
        Assert.Null(await new SqliteCharacterStateRepository(_database)
            .LoadAsync("card-seam", CharacterIntelligenceGateway.LocalUserId, Token));
    }

    [Fact]
    public async Task A_failed_first_restore_does_not_switch_this_card_off_for_good()
    {
        // 状态域是按卡片缓存的，而首轮 restore 恰好是客户端最容易断开的那一刻（DEF-P3SEAM-03）。
        // 那笔失败的 Task 若留在字典里，这张卡此后每一轮都拿到同一个坏结果：静默退回过渡形状，
        // 界面看不出任何异常，只能等进程重启自愈。所以第二轮必须自己恢复。
        var conversationId = await SeedAsync((ChatRole.User, "我今天加班到十点，整个人都不好了"));
        var state = new CancelFirstRestore(_database);
        var gateway = Gateway(stateRepository: state);
        var advisor = gateway.CreateAdvisor(conversationId, Card(), new TurnTakingSettings());
        var sink = new CharacterIntelligenceOutputSink(new CollectingSink(), gateway);

        var first = await advisor.AdviseAsync(Gate(new TurnIdentity(15, 1, 1), "我今天加班到十点，整个人都不好了"),
            Token);
        Assert.Equal("transitional_no_social_judgement", first.Reason);

        var second = await advisor.AdviseAsync(Gate(new TurnIdentity(16, 1, 1), "我今天加班到十点，整个人都不好了"),
            Token);
        Assert.StartsWith("intelligence:", second.Reason, StringComparison.Ordinal);
        Assert.NotNull(second.PromptBlock);

        // 恢复的不只是返回值：轮末那本账走的是另一条取用状态域的路径，它也必须重新有货。
        await sink.MessageDeliveredAsync(conversationId, 303, new RuntimePlannedMessage(0, "辛苦你了", null),
            MessageSources.ModelReply, Token);
        await sink.TurnTransitionAsync(conversationId, new TurnIdentity(16, 1, 1), RuntimeTurnOutcome.Completed,
            "delivered", Token);
        var ledger = await new SqliteCharacterStateRepository(_database)
            .LoadAsync("card-seam", CharacterIntelligenceGateway.LocalUserId, Token);
        Assert.NotNull(ledger);
        Assert.Equal(1, ledger.ExchangeCount);
    }

    [Fact]
    public async Task When_the_decision_cannot_be_made_she_still_answers_instead_of_going_silent()
    {
        var conversationId = await SeedAsync((ChatRole.User, "我今天加班到十点，整个人都不好了"));
        var gateway = Gateway(new UnreadablePatterns());
        var turn = new TurnIdentity(9, 1, 1);

        var advice = await gateway.CreateAdvisor(conversationId, Card(), new TurnTakingSettings())
            .AdviseAsync(Gate(turn, "我今天加班到十点，整个人都不好了"), Token);

        // 回落形状与接线前的过渡建议器逐字一致：故障不许把她变成不说话的那个人。
        Assert.True(advice.ShouldReply);
        Assert.Equal("transitional_no_social_judgement", advice.Reason);
        Assert.Null(advice.PromptBlock);
        await new CharacterIntelligenceOutputSink(new CollectingSink(), gateway)
            .TurnTransitionAsync(conversationId, turn, RuntimeTurnOutcome.Completed, "delivered", Token);
        Assert.Null(await new SqliteCharacterStateRepository(_database)
            .LoadAsync("card-seam", CharacterIntelligenceGateway.LocalUserId, Token));
    }

    [Fact]
    public void The_session_wires_both_ends_of_the_seam_and_not_just_the_one_that_shows_up_in_a_trace()
    {
        // 源码门：只接"说话前那一问"会留下一本没人收的账；只接终局则回报的是没决策过的轮次。
        // 扫的是源文件本身，因为这两处都在同一个方法里，没有任何一条行为断言能替它守着。
        var source = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Pry.Api", "Services",
            "ConversationSessionService.cs"));
        Assert.Contains("intelligence?.CreateAdvisor(conversationId, character, runtime.TurnSettings)", source,
            StringComparison.Ordinal);
        Assert.Contains("new CharacterIntelligenceOutputSink(sink, intelligence)", source, StringComparison.Ordinal);
        // 过渡建议器只能作为"没有接线"的回落出现一次。数出现次数而不是数换行后的形状，
        // 因为换行符一旦被编辑器写成 CRLF，字符串断言就会静默常绿——那正是一条假守卫。
        Assert.Equal(1, source.Split("new TransitionalReplyAdvisor()", StringSplitOptions.None).Length - 1);
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null;
             directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Pry.slnx"))) return directory.FullName;
        throw new InvalidOperationException("从测试程序集出发找不到仓库根，无法核对源码约束。");
    }

    /// <summary>Runtime 递给她的那一眼：闸门全过之后才有建议器被调用，所以这里直接构造"已经放行"的上下文。</summary>
    private static ReplyGateContext Gate(TurnIdentity turn, string input,
        bool directAddress = false, bool asksQuestion = false, bool proactive = false,
        TimeSpan? sinceLastCharacterActivity = null) =>
        new(turn, input, directAddress, asksQuestion, false, RuntimePhase.Planning, false, 1, 0,
            sinceLastCharacterActivity is null ? null : DateTimeOffset.UtcNow - sinceLastCharacterActivity.Value,
            sinceLastCharacterActivity ?? TimeSpan.Zero, 0, proactive);

    /// <summary>把一轮的建议送进真正的生成端口，返回模型实际读到的那段系统提示词。</summary>
    private async Task<string> ComposeAsync(string conversationId, TurnIdentity turn, ReplyAdvice? advice,
        RecordingModel model)
    {
        var catalog = new StickerCatalog(Path.Combine(_root, "stickers-missing.json"),
            Path.Combine(_root, "stickers"));
        await catalog.LoadAsync(Token);
        var planner = new ReplyPlanner(_database, new PromptBuilder(), new ModelRouter([model], "text"),
            Card(), new RuntimeState(), catalog);
        var request = new TurnRequest(conversationId, turn, DateTimeOffset.UtcNow,
            [TurnInputPart.NewInput(new RuntimeUserInput("我今天加班到十点，整个人都不好了"))],
            ["我今天加班到十点，整个人都不好了"], [], [], 1,
            new DeliveryPolicyOptions("像平时一样说话", 1, 3, 60), advice);
        await new ReplyPlannerComposer(planner, new TurnTakingSettings()).ComposeAsync(request, Token);
        return model.LastSystemPrompt;
    }

    private sealed class RecordingModel(ModelProfile profile, string response) : IChatModel
    {
        public ModelProfile Profile { get; } = profile;
        public string LastSystemPrompt { get; private set; } = "";

        public async IAsyncEnumerable<string> StreamAsync(string systemPrompt, IReadOnlyList<ChatMessage> messages,
            IReadOnlyList<string>? imagePaths, ChatRequestOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            LastSystemPrompt = systemPrompt;
            await Task.Yield();
            yield return response;
        }
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

    /// <summary>
    /// 首轮 restore 被取消的状态读口（DEF-P3SEAM-03 的触发形状：客户端在断开窗口里问第一句）。
    /// 只掐第一次读，其余全部转给真正的 SQLite 仓库——恢复那半边必须跑在真库上，否则 fake 与真库的
    /// 判定分歧（DEF-P2PLAN-01）会让这条判据只在假人身上成立。
    /// </summary>
    private sealed class CancelFirstRestore(MemoryDatabase database) : ICharacterStateRepository
    {
        private readonly SqliteCharacterStateRepository _inner = new(database);
        private int _loads;

        public Task<CharacterStateRecord?> LoadAsync(string characterId, string userId,
            CancellationToken cancellationToken = default) =>
            Interlocked.Increment(ref _loads) == 1
                ? Task.FromException<CharacterStateRecord?>(new OperationCanceledException("模拟首轮 restore 被取消"))
                : _inner.LoadAsync(characterId, userId, cancellationToken);

        public Task<bool> SaveAsync(CharacterStateRecord record, CancellationToken cancellationToken = default) =>
            _inner.SaveAsync(record, cancellationToken);

        public Task<int> PurgeAsync(string characterId, CancellationToken cancellationToken = default) =>
            _inner.PurgeAsync(characterId, cancellationToken);
    }

    private sealed class UnreadablePatterns : IBehaviorPatternRepository
    {
        public Task<IReadOnlyList<BehaviorPattern>> ListForCharacterAsync(string characterInstanceId,
            CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<BehaviorPattern>>(new InvalidOperationException("模拟读表失败"));

        public Task<BehaviorPattern?> FindAsync(string behaviorPatternId, CancellationToken cancellationToken = default) =>
            Task.FromResult<BehaviorPattern?>(null);

        public Task SaveAsync(BehaviorPattern pattern, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<int> PurgeAsync(string characterInstanceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}

using System.Collections.Concurrent;
using Pry.Core.Abstractions;
using Pry.Core.Expression;
using Pry.Core.Intelligence;
using Pry.Core.Memory;
using Pry.Core.Models;
using Pry.Core.Runtime;
using Pry.Core.TurnTaking;

namespace Pry.Api.Runtime;

/// <summary>
/// 角色智能在宿主侧的唯一接线处：Runtime 说话前那一问（<c>DecideAsync</c>）与消息真正投递后那一报
/// （<c>CompleteTurnAsync</c>）都由这里发出，其余类不再直接驱动 <see cref="CharacterIntelligenceService"/>。
///
/// 开工前对的归属线（<c>docs/phase2-runtime-contract.md</c> §7.1 四条冻结结论）：
/// 一，不新增对外通道——轮次终局是从 <see cref="IRuntimeOutputSink"/> 的既有调用上旁听来的，
/// 本类不发 SSE、不持有事件日志，也不改 <c>ConversationRuntimeEventSink</c> 的映射；
/// 二，不碰失效权威——建议器只在 ReplyGate 的状态闸门全过之后才被调用，陈旧轮次到不了这里；
/// 三，不动 Capability 的住处——<c>AvailableCapabilityNames</c> 保持为空，
/// 行为规划器自带的 <c>capability:none_registered</c> 闸门会把该意图降级；
/// 四，不引入第二个 Context，也不把 Memory 接进评分——<c>SituationSnapshot.Memory</c> 一律填
/// "这一轮真的没查"（<see cref="RelevantMemoryView.Unavailable"/>）而不是"查了没有"；
/// MEM-005 把 <see cref="IRelevantMemorySource"/> 与 <c>IMemoryRetriever</c> 收敛成一个适配器之前，
/// 在这里多接一条检索旁路就是把别人的冻结结论改掉。
///
/// 作用域前提（<c>docs/character-intelligence.md</c> §8 缺口 ③，需要被维持）：行为模式／表达模式／
/// 经验日志按 <c>characterInstanceId</c> 存，宿主在这里传的是 <c>character.Id</c>——与
/// <c>ConversationRuntimeOptions.CharacterInstanceId</c> 同一个字符串。哪天出现一卡多实例，两处必须一起改，
/// 否则删卡时的 <c>PurgeAsync</c> 会静默漏掉后建的实例。
/// </summary>
public sealed class CharacterIntelligenceGateway(MemoryDatabase database,
    ICharacterStateRepository stateRepository, IBehaviorPatternRepository patternRepository,
    IExpressionPatternRepository expressionRepository, IBehaviorExperienceRepository experienceRepository,
    ILogger<CharacterIntelligenceGateway> logger)
{
    /// <summary>1.0 一台机器一个用户。多用户落地时改这一处，不要让调用方各写一份字面量。</summary>
    public const string LocalUserId = "local";

    /// <summary>
    /// 在飞轮次的账本上限。一个会话同一刻只可能有一轮在投递，留 8 份是给多会话与
    /// "建议过但终局没回来"的余量；超了丢最老的——宁可少学一次，也不让一本没人收的账无上限长。
    /// </summary>
    private const int PendingTurnLimit = 8;

    /// <summary>
    /// 投递凭据的上限，按气泡数计。客户端手里只有消息行 id，"用户点的是哪一条 ⇒ 出自哪一轮"只能靠
    /// 投递当场记下的这一本；超了丢最老的——拿不到凭据就明确拒收这次表态，
    /// 绝不退化成"按最近一轮归属"（那是 <c>DEF-P3SEAM-02</c> 花一整刀才拆掉的写法）。
    /// </summary>
    private const int ReceiptLimit = 64;

    private readonly ConcurrentDictionary<string, Task<Scope>> _scopes = new(StringComparer.Ordinal);

    /// <summary>说话前那一问要读对话记录，所以历史消息的读口在这里显式转交给建议器。</summary>
    private readonly MemoryDatabase _database = database;

    private readonly object _ledgerGate = new();
    private readonly Dictionary<TurnKey, PendingTurn> _pending = [];
    private readonly List<TurnKey> _ledgerOrder = [];
    private readonly Dictionary<long, DeliveredReceipt> _receipts = [];
    private readonly List<long> _receiptOrder = [];

    /// <summary>为某个会话构造"说话前那一问"。每个会话一个实例，因为它绑定 conversationId。</summary>
    public IReplyAdvisor CreateAdvisor(string conversationId, CharacterDefinition character,
        TurnTakingSettings turnSettings) =>
        new Advisor(this, conversationId, character, turnSettings);

    /// <summary>
    /// 一个角色实例一份长期状态：同一张卡在两个会话里必须共用同一个 <see cref="CharacterStateService"/>，
    /// 否则两边各记半本账，谁写出去的那份都不完整。
    ///
    /// 已结算却坏掉的那一笔不许留在字典里：首轮 restore 期间客户端断开是常态，而缓存按卡片生效，
    /// 一笔坏 Task 会让这张卡此后每一轮都拿到同一个坏结果，静默退回过渡形状、只能等重启自愈。
    /// 这里只逐出、不就地重建：断开不会因为再读一次就过去，本轮照旧由建议器那层的 fail-open 把话说完，
    /// 下一轮自然重建。
    /// </summary>
    private async Task<Scope> ScopeForAsync(CharacterDefinition character, CancellationToken token)
    {
        var built = _scopes.GetOrAdd(character.Id, key => BuildScopeAsync(key, character, token));
        if (built.IsCompleted && built.Status != TaskStatus.RanToCompletion)
            _scopes.TryRemove(new KeyValuePair<string, Task<Scope>>(character.Id, built));

        return await built;
    }

    private async Task<Scope> BuildScopeAsync(string characterInstanceId, CharacterDefinition character,
        CancellationToken token)
    {
        var identity = IdentitySnapshot.From(character);
        var state = new CharacterStateService(identity, LocalUserId, stateRepository);
        await state.RestoreAsync(token);
        // 表达服务在这里只建一次并留在作用域里：后台排空取的就是这同一个 Learner，
        // 另建一个就等于把同一张卡的学习队列分成两本，谁都被排空一半。
        var expressions = new ExpressionService(expressionRepository, characterInstanceId);
        return new Scope(state, new CharacterIntelligenceService(state, expressions, patternRepository,
            experiences: experienceRepository), identity, expressions);
    }

    /// <summary>
    /// 后台排空通道取用的"当前活着的 Learner"。三个刻意的限定：
    /// 一，只交已经建好的状态域——排空绝不为一张还没说过话的卡建域，那会把首轮 restore 的开销搬到后台；
    /// 二，不等待还在建的那一笔——它这一轮还没走到投递，账本里没货，下一次 tick 排同样排得到；
    /// 三，建失败的那一笔在这里被跳过而不是抛出：坏 Task 的逐出归 <see cref="ScopeForAsync"/>，
    /// 后台循环不该因为一次已经处理过的失败再多做一次决定。
    /// </summary>
    public IReadOnlyList<ActiveLearner> ActiveLearners()
    {
        var learners = new List<ActiveLearner>();
        foreach (var built in _scopes.ToArray())
            if (built.Value is { IsCompletedSuccessfully: true } scope)
                learners.Add(new ActiveLearner(built.Key, scope.Result.Expressions.Learner));

        return learners;
    }

    /// <summary>排空通道要的那一眼：哪张卡、哪个学习器。卡 id 一并交出，只为失败时日志能指名是哪一张。</summary>
    public sealed record ActiveLearner(string CharacterInstanceId, ExpressionLearner Learner);

    /// <summary>
    /// 把一轮的决策记进账本，等投递结果回来再学。key 含 conversationId：TurnId 是会话内序号，跨会话会撞。
    ///
    /// 只认第一次：一轮要过两到三次闸（<c>ConversationRuntime.cs:698</c> 初次、compose 之后投递之前的复校
    /// <c>:755</c>、Capability 续轮 <c>:896</c>），模型读到的提示块来自第一次那份建议（<c>:721</c>），
    /// 后面几次照旧要问（复校不能摘，迟到结果防护依赖它），但账本改写成"复校那一份"就等于教她一件事没发生过的决策。
    /// 每次咨询的条数本来就在 trace 里（<c>DecideGateAsync</c> 每过一道闸写一条 ReplyGate），不必在这里另记一本。
    /// </summary>
    private void Remember(string conversationId, TurnIdentity turn, SituationSnapshot situation,
        CharacterStateSnapshot state, ReplyGenerationContext generation)
    {
        var key = new TurnKey(conversationId, turn.TurnId, turn.GenerationId);
        lock (_ledgerGate)
        {
            if (_pending.ContainsKey(key)) return;
            _pending[key] = new PendingTurn(situation, state, generation);
            _ledgerOrder.Remove(key);
            _ledgerOrder.Add(key);
            while (_ledgerOrder.Count > PendingTurnLimit)
            {
                var oldest = _ledgerOrder[0];
                _ledgerOrder.RemoveAt(0);
                _pending.Remove(oldest, out _);
            }
        }
    }

    /// <summary>这一轮不再有机会走到"投递成功"，账本里就不该留着它。</summary>
    private void Forget(string conversationId, TurnIdentity turn)
    {
        var key = new TurnKey(conversationId, turn.TurnId, turn.GenerationId);
        lock (_ledgerGate)
        {
            _pending.Remove(key, out _);
            _ledgerOrder.Remove(key);
        }
    }

    /// <summary>
    /// 记下一个已投递气泡的正文，并同时记下"这一条气泡出自哪一轮"的投递凭据。气泡回调不带轮身份，
    /// 而端口签名是冻结的（契约 §7.1 结论 1），所以这里只认"该会话恰好有一轮在飞"这一种情形：
    /// 认出唯一才记，认不出唯一就一个字都不记、一条凭据都不记。
    ///
    /// 平时确实只有一轮在飞，靠的是三道闸而不是运气：<c>ReplyGate</c> 在 <c>UserTyping</c> 或
    /// <c>PendingOutputCount > 0</c> 时先 <c>Defer</c>（在建议器之前），<c>TryFormTurnLocked</c> 在
    /// <c>_active is not null</c> 时直接返回，主动候选在同样两个条件成立时被拒。
    /// 原来这里写的理由是"围栏只认最新一轮"，那句不成立——围栏比的是 epoch，而成轮并不抬 epoch。
    /// </summary>
    internal void RecordDeliveredBubble(string conversationId, long messageId, string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        lock (_ledgerGate)
        {
            PendingTurn? sole = null;
            var inFlight = 0;
            for (var i = 0; i < _ledgerOrder.Count; i++)
                if (_ledgerOrder[i].ConversationId == conversationId && _pending.TryGetValue(_ledgerOrder[i], out var entry))
                {
                    inFlight++;
                    sole = entry;
                }

            // 同会话两条以上 = 归属已经说不清了；宁可少学一次，也不把这句话挂到某一轮上去猜。
            if (inFlight != 1) return;
            sole!.Delivered.Add(text);
            RememberReceipt(conversationId, messageId, sole, text);
        }
    }

    /// <summary>
    /// 投递凭据按消息行 id 记：那正是客户端手里唯一持有的东西（<c>ChatMessage.Id</c>），
    /// 也是这里能拿到的同一个 id（<c>Delivery.cs:325</c> 提交气泡后交给 sink 的那一个）。
    /// </summary>
    private void RememberReceipt(string conversationId, long messageId, PendingTurn turn, string text)
    {
        _receipts[messageId] = new DeliveredReceipt(conversationId, turn, text, DateTimeOffset.UtcNow);
        _receiptOrder.Remove(messageId);
        _receiptOrder.Add(messageId);
        while (_receiptOrder.Count > ReceiptLimit)
        {
            var oldest = _receiptOrder[0];
            _receiptOrder.RemoveAt(0);
            _receipts.Remove(oldest, out _);
        }
    }

    /// <summary>
    /// 用户对一条已投递气泡的显式表态：把"就是这样／别这样说"落到她那一句真的说过的形状上。
    ///
    /// 三处刻意的保守：一，凭据不在场就分两种拒法（这个会话里没这条消息／有但这一轮没被记住），
    /// 绝不猜；二，认错会话（消息行 id 是全局唯一的，可能属于别的会话）单独成一种；
    /// 三，那一轮没有可学形状时不重放——占位符那一类"她其实没说话"的轮次不该因为一次点击被记成表态。
    ///
    /// 走的是 Core 早就备好的那条幂等形状（<c>ExpressionLearner.cs:150</c>：重放只补证据、
    /// 不制造另一次出现），所以同一轮连点三次仍然只算一次认可。这里不带 <c>FollowUpEvidence</c>：
    /// 行为经验那一侧归第 3 层 <c>RT-006</c>，本刀不越过去。
    /// </summary>
    public async Task<ExpressionFeedbackOutcome> ReportExplicitFeedbackAsync(string conversationId, long messageId,
        bool approved, CancellationToken token)
    {
        DeliveredReceipt? receipt;
        lock (_ledgerGate) _receipts.TryGetValue(messageId, out receipt);
        if (receipt is null)
            return await _database.GetMessageAsync(conversationId, messageId, token) is null
                ? ExpressionFeedbackOutcome.MessageMissing
                : ExpressionFeedbackOutcome.NotAttributable;

        if (!string.Equals(receipt.ConversationId, conversationId, StringComparison.Ordinal))
            return ExpressionFeedbackOutcome.WrongConversation;

        var shapes = ExpressionLearner.ShapesIn(receipt.Text);
        if (shapes.Count == 0) return ExpressionFeedbackOutcome.NothingToLearn;

        var scope = await RequiredScopeAsync(receipt.Turn.Situation.CharacterInstanceId, token);
        // 每一次点击是一笔独立的证据；重复点击由 Core 那道按 turnKey 的去重挡住，不在这里另造一本。
        var enqueued = await scope.Intelligence.ObserveFeedbackAsync(new TurnOutcome(
            receipt.Turn.Situation with { RecentMessages = [] }, receipt.Turn.State,
            receipt.Turn.Generation.Decision, [], InteractionOutcome.Unknown, receipt.Text, receipt.DeliveredAt)
        {
            FeedbackIsExplicit = true,
            ExpressionFeedbackId = Guid.NewGuid().ToString("N"),
            AcceptedExpressionStyles = approved ? shapes : [],
            RejectedExpressionStyles = approved ? [] : shapes
        }, token);

        return enqueued ? ExpressionFeedbackOutcome.Recorded : ExpressionFeedbackOutcome.QueueFull;
    }

    /// <summary>
    /// 轮次终局。Completed／PartiallyDelivered 且有实际正文才构造成一次可学的互动；其余终局只把账本条目
    /// 划掉——取消、被替换、失败、沉默的轮次一个字都不许教（<c>docs/character-intelligence.md</c> §4）。
    /// </summary>
    internal async Task ReportTurnEndAsync(string conversationId, TurnIdentity turn, RuntimeTurnOutcome outcome,
        CancellationToken token)
    {
        PendingTurn? entry;
        lock (_ledgerGate)
        {
            var key = new TurnKey(conversationId, turn.TurnId, turn.GenerationId);
            _pending.Remove(key, out entry);
            _ledgerOrder.Remove(key);
        }
        if (entry is null) return;

        // 长度不等于"她说了话"：模型没兑现出正文时投出去的那一条是占位符，两个点照样过得了纯长度那道闸。
        // 判定与可见侧同源，只在 Core 那一处定义（DEF-P3SEAM-04），这里只调用、不另比一次。
        if (outcome is not (RuntimeTurnOutcome.Completed or RuntimeTurnOutcome.PartiallyDelivered) ||
            !VisibleReply.HasVisibleContent(entry.Delivered)) return;

        var delivered = string.Join("\n", entry.Delivered);

        var scope = await RequiredScopeAsync(entry.Situation.CharacterInstanceId, token);
        await scope.Intelligence.CompleteTurnAsync(new TurnOutcome(entry.Situation, entry.State,
            entry.Generation.Decision, entry.Generation.ExpressionHints,
            // 这一轮只拿到"她说完了"这一个事实，用户怎么反应还没接进来（第三个调用点
            // ObserveFeedbackAsync 不在本刀范围）。Unknown 的 Signal() 是 null：经验照记，置信度一步不加。
            InteractionOutcome.Unknown, delivered, DateTimeOffset.UtcNow)
        { DeliveredToUser = true }, token);
    }

    internal void LogWarning(Exception error, string message, params object?[] args) =>
        logger.LogWarning(error, message, args);

    internal void LogTurnReportFailure(Exception error, string conversationId, TurnIdentity turn) =>
        logger.LogWarning(error, "会话 {Conversation} 的第 {Turn} 轮学习回报失败，本轮不教学", conversationId,
            turn.TurnId);

    /// <summary>终局回来时状态域一定已经建好了（说话前那一问就是它建的），所以这里只查不建。</summary>
    private async Task<Scope> RequiredScopeAsync(string characterInstanceId, CancellationToken token) =>
        _scopes.TryGetValue(characterInstanceId, out var built) ? await built
            : throw new InvalidOperationException($"角色 {characterInstanceId} 的状态域尚未建立，宿主接线顺序有误。");

    private sealed record Scope(CharacterStateService State, CharacterIntelligenceService Intelligence,
        IdentitySnapshot Identity, ExpressionService Expressions);

    private sealed record TurnKey(string ConversationId, long TurnId, long GenerationId);

    private sealed class PendingTurn(SituationSnapshot situation, CharacterStateSnapshot state,
        ReplyGenerationContext generation)
    {
        public SituationSnapshot Situation { get; } = situation;
        public CharacterStateSnapshot State { get; } = state;
        public ReplyGenerationContext Generation { get; } = generation;
        public List<string> Delivered { get; } = [];
    }

    /// <summary>一条已投递气泡的归属凭据：哪个会话、出自哪一轮的账、她这一条说了什么、什么时候投递的。</summary>
    private sealed record DeliveredReceipt(string ConversationId, PendingTurn Turn, string Text,
        DateTimeOffset DeliveredAt);

    /// <summary>
    /// 说话前那一问。"这次要不要说话"的状态闸门仍由 Runtime 的 ReplyGate 把，本类只回答
    /// "如果要说话，这一轮做什么、怎么说"；<c>DoNothing</c> 是一等结果，不是失败路径。
    /// </summary>
    private sealed class Advisor(CharacterIntelligenceGateway gateway, string conversationId,
        CharacterDefinition character, TurnTakingSettings turnSettings) : IReplyAdvisor
    {
        /// <summary>回落只有一处定义：接不上行为决策时，用过渡建议器那套机械映射把话说完。</summary>
        private static readonly TransitionalReplyAdvisor Fallback = new();

        public string Name => "character_intelligence";

        public async Task<ReplyAdvice> AdviseAsync(ReplyGateContext context, CancellationToken token)
        {
            // 主动消息送审时 CombinedInput 装的是她自己打算说的话，不是用户输入
            // （ConversationRuntime.cs:1010）。拿它当"用户这句话"去做行为决策是编一个不存在的输入，
            // 所以这条分支原样放行，也不记账——本刀不给主动消息新增生产者。
            if (context.ProactiveOrigin) return await Fallback.AdviseAsync(context, token);

            try
            {
                var scope = await gateway.ScopeForAsync(character, token);
                var now = DateTimeOffset.UtcNow;
                var recent = await gateway._database.GetRecentMessagesAsync(conversationId, 20, token);
                var situation = new SituationSnapshot
                {
                    CharacterInstanceId = character.Id,
                    ConversationId = conversationId,
                    TurnId = context.Turn.TurnId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    UserInput = context.CombinedInput,
                    Now = now,
                    Memory = RelevantMemoryView.Unavailable(),
                    RecentMessages = recent,
                    // 被点名或被提问时沉默才是异常；其余场景让她可以不接这句。
                    RequiresReply = context.DirectAddress || context.AsksQuestion,
                    SinceLastAssistantReply = context.LastCharacterActivity is null
                        ? null : context.SinceLastCharacterActivity,
                    ConsecutiveLowContentInputs = SituationReader.TrailingLowContentUserInputs(recent)
                };
                var state = scope.State.Snapshot(now);
                var generation = await scope.Intelligence.DecideAsync(situation, state, token);
                gateway.Remember(conversationId, context.Turn, situation, state, generation);

                var intent = generation.Decision.Intent;
                return new ReplyAdvice(intent != ResponseIntent.DoNothing, intent,
                    $"intelligence:{generation.Decision.ScenarioTag}:{intent}",
                    PromptBlock: intent == ResponseIntent.DoNothing ? null
                        : ReplyGenerationRenderer.Render(generation, scope.Identity, PromptBudget()));
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // 取消必须仍然是取消，不能被降级成一次普通失败；但这一轮不留账。
                gateway.Forget(conversationId, context.Turn);
                throw;
            }
            catch (Exception error)
            {
                // 行为决策不可用时宁可把话说完也不要静默变哑：回落形状与发行前的过渡建议器逐字一致。
                gateway.Forget(conversationId, context.Turn);
                gateway.LogWarning(error, "角色 {Character} 在会话 {Conversation} 的行为决策失败，本轮按过渡形状放行",
                    character.Id, conversationId);
                return await Fallback.AdviseAsync(context, token);
            }
        }

        /// <summary>提示块不该比她这一轮能说的话还长，所以预算跟着气泡长度与条数走，再封顶在渲染器默认值。</summary>
        private int PromptBudget() => Math.Clamp(
            Math.Clamp(turnSettings.MaxMessageCharacters, 20, 500) * Math.Clamp(turnSettings.MaxReplyMessages, 1, 6),
            160, ReplyGenerationRenderer.DefaultBudgetCharacters);
    }
}

/// <summary>
/// 一次显式表态在宿主侧的处理结果。这里的种类与 HTTP 状态码不是一回事：
/// 网关只说"这件事我能不能归给某一轮、归得到之后有没有真的进后台队列"，
/// 把它翻成错误语义是 <c>ExpressionFeedbackService</c> 的活。
/// </summary>
public enum ExpressionFeedbackOutcome
{
    Recorded,
    NothingToLearn,
    MessageMissing,
    WrongConversation,

    /// <summary>这条消息确实在这个会话里，但本进程没有它的投递凭据（重启前的旧气泡，或被账本上限挤掉）。</summary>
    NotAttributable,
    QueueFull
}

/// <summary>
/// 投递终局的旁听器：六个方法原样转给真正的宿主 sink，只在 <c>MessageDeliveredAsync</c> 上抄一份正文、
/// 在 <c>TurnTransitionAsync</c> 上补一次学习回报。它是同一个输出通道的内侧装饰器，不是第二条外发路径。
/// </summary>
public sealed class CharacterIntelligenceOutputSink(IRuntimeOutputSink inner,
    CharacterIntelligenceGateway gateway) : IRuntimeOutputSink
{
    public Task MessageDeliveredAsync(string conversationId, long messageId, RuntimePlannedMessage message,
        string source, CancellationToken token)
    {
        gateway.RecordDeliveredBubble(conversationId, messageId, message.Text);
        return inner.MessageDeliveredAsync(conversationId, messageId, message, source, token);
    }

    public Task TypingIndicatorAsync(string conversationId, bool active, CancellationToken token) =>
        inner.TypingIndicatorAsync(conversationId, active, token);

    public Task PhaseChangedAsync(string conversationId, RuntimePhase phase, RuntimePhase previous,
        CancellationToken token) => inner.PhaseChangedAsync(conversationId, phase, previous, token);

    public async Task TurnTransitionAsync(string conversationId, TurnIdentity turn, RuntimeTurnOutcome outcome,
        string reason, CancellationToken token)
    {
        await inner.TurnTransitionAsync(conversationId, turn, outcome, reason, token);
        try
        {
            await gateway.ReportTurnEndAsync(conversationId, turn, outcome, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error)
        {
            // 学习回报失败不得把一次已经投递成功的轮次变成 turn.failed：客户端已经看见那句话了。
            gateway.LogTurnReportFailure(error, conversationId, turn);
        }
    }

    public Task CapabilityRequestedAsync(string conversationId, CapabilityRequest request, CancellationToken token) =>
        inner.CapabilityRequestedAsync(conversationId, request, token);

    public Task FailureAsync(string conversationId, TurnIdentity turn, string errorCode, CancellationToken token) =>
        inner.FailureAsync(conversationId, turn, errorCode, token);
}

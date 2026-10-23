using System.Collections.Concurrent;
using Pry.Core.Memory;
using Pry.Core.Narrative;
using Pry.Core.Prompting;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// HDS-004 叙事生命周期协调层判据组（第十六波·隔离实现）。判据一一对应工单 §二.1–§二.7：
/// 作用域幂等（同键不二次生成、异键不互吞）、实例单写者（忙则排队、缺权点名拒绝）、
/// 公平调度（会话轮转、无饿死）、取消传播（新输入顶替、宿主取消、无视取消的迟到结果经既有代次门永久无资格）、
/// 删除与重配置作废（在飞＋排队全部作废且不重新入队）、三种失败可区分（超额度／provider 失败含坏响应／超时，
/// 绝不映射成合法沉默）、以及"新协调器源码零旧主链引用"的门（带阳性探针）。
/// provider 全部是注入的确定性假实现：真实模型对照不在本波，一律登记为未验证。
/// 所有超时与预算都从测试入参传入——本波不新造任何数字门槛。
/// </summary>
public sealed class NarrativeLifecycleCoordinatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 9, 0, 0, TimeSpan.Zero);
    private const string WriterId = "writer-narrative-1";

    private const string Prose = "She set the brush down, answered him, then kept painting.";
    private const string BoundQuote = "answered him";

    private const string IdentityText = "Pry canon";
    private const string OutputRules = "json rules";
    private const string CurrentEventText = "hello there";

    /// <summary>有界自旋上限：等不到在飞标记就让测试红，绝不无界挂死。</summary>
    private const int SpinCap = 1000;

    private static readonly CharacterInstanceId Alpha = CharacterInstanceId.Create("ci-instance-alpha");
    private static readonly CharacterInstanceId Beta = CharacterInstanceId.Create("ci-instance-beta");
    private static readonly CharacterInstanceKey AlphaKey = new()
    {
        LocalUserId = "local-user-1",
        CharacterId = "pry",
    };
    private static readonly CharacterInstanceKey BetaKey = new()
    {
        LocalUserId = "local-user-1",
        CharacterId = "companion-beta",
    };
    private static readonly ConservativeTokenEstimator Estimator = new();

    /// <summary>闸门单元的宽裕超时（真取消／真放行都不靠它判定）；超时判据单元用显式短档。</summary>
    private static readonly TimeSpan GatedTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan TimeoutProbe = TimeSpan.FromMilliseconds(80);

    // —— 夹具 ——

    private static NarrativeRequest Req(CharacterInstanceId instance, string conversation, string batch,
        long ownershipEpoch = 1) => new()
    {
        InstanceId = instance,
        ConversationId = conversation,
        BatchId = batch,
        Inputs = [new InputRef(101, "msg-101", conversation)],
        Trigger = NarrativeTriggerKind.UserInput,
        StoryVersion = 7,
        DeletionEpoch = 0,
        OwnershipEpoch = ownershipEpoch,
        IssuedByWriter = WriterId,
        MaxOutboundSegments = 4,
        IssuedAtUtc = Now,
        ProcessedThroughAtIssue = new ProcessedThroughSeq(100),
    };

    private static NarrativeContextPayloads Pay(string conversation) => new()
    {
        Budget = new ContextBudget(),
        Identity = IdentityText,
        OutputRules = OutputRules,
        CurrentEvent = new NarrativeContextUnit("input:101", conversation, CurrentEventText),
        Advisory = "",
        State = "",
        RecentMessages = [],
        MemoryExcerpts = [],
        BehaviorReferences = "",
        ExpressionPatterns = "",
        CapabilityResult = "",
    };

    private static NarrativeScopeKey KeyOf(CharacterInstanceId instance, string conversation, string batch) =>
        new(instance, conversation, batch);

    private static CommunicationAction SilenceAction(string conversation) => new()
    {
        ActionId = "action-silence", Mode = CommunicationMode.Silence, TargetConversationId = conversation,
        PlannedSegmentCount = 0,
    };

    private static CommunicationAction ImmediateAction(string conversation) => new()
    {
        ActionId = "action-now", Mode = CommunicationMode.Immediate, TargetConversationId = conversation,
        ProseQuote = BoundQuote, PlannedSegmentCount = 3,
    };

    /// <summary>草稿按调用请求的三项作用域＋快照字段镜像生成，保证快照新鲜度门只在取消路径上说话。</summary>
    private static NarrativeDraft DraftFor(NarratorCall call, IReadOnlyList<CommunicationAction> actions)
    {
        var r = call.Request;
        return new NarrativeDraft
        {
            InstanceId = r.InstanceId, ConversationId = r.ConversationId, BatchId = r.BatchId,
            NarrativeProse = Prose,
            ProcessedThrough = new ProcessedThroughSeq(101),
            PerceivedInputRefs = new PerceivedInputRefs { Refs = [new InputRef(101, "msg-101", r.ConversationId)] },
            Actions = actions, Intents = [],
            StoryVersionBase = r.StoryVersion, OwnershipEpoch = r.OwnershipEpoch, DeletionEpoch = r.DeletionEpoch,
            ReceivedThroughSeqAtIssue = r.ProcessedThroughAtIssue.Seq,
            ProposalId = "proposal-" + r.BatchId, ProposedAtUtc = Now,
        };
    }

    private enum Mode
    {
        Silence, GatedSilence, GatedImmediate, IgnoresTokenImmediate, Malformed, Throws, Hangs,
    }

    /// <summary>
    /// 确定性假 provider：按批次给形状、按批次闸门、按批次计数并记录被分派次序。
    /// 不含真实模型、网络或运行库；闸门一律等测试自己放行，判定不靠睡眠竞速。
    /// </summary>
    private sealed class CoordinatorNarrator : INarrator
    {
        private readonly ConcurrentDictionary<string, Mode> _modes = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, TaskCompletionSource> _gates = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, int> _calls = new(StringComparer.Ordinal);
        private readonly object _orderGate = new();
        private readonly List<string> _order = [];

        public void UseMode(string batch, Mode mode) => _modes[batch] = mode;

        public TaskCompletionSource Gate(string batch) =>
            _gates.GetOrAdd(batch, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));

        public void Release(string batch) => Gate(batch).TrySetResult();

        public int CallsFor(string batch) => _calls.GetValueOrDefault(batch, 0);

        public IReadOnlyList<string> DispatchOrder
        {
            get { lock (_orderGate) return _order.ToArray(); }
        }

        public async Task<NarratorOutcome> ProposeAsync(NarratorCall call, CancellationToken cancellationToken)
        {
            var batch = call.Request.BatchId;
            _calls.AddOrUpdate(batch, 1, (_, previous) => previous + 1);
            lock (_orderGate) _order.Add(batch);
            var mode = _modes.GetValueOrDefault(batch, Mode.Silence);
            switch (mode)
            {
                case Mode.GatedSilence:
                    await Gate(batch).Task.WaitAsync(cancellationToken);
                    return NarratorOutcome.Produced(DraftFor(call, [SilenceAction(call.Request.ConversationId)]));
                case Mode.GatedImmediate:
                    await Gate(batch).Task.WaitAsync(cancellationToken);
                    return NarratorOutcome.Produced(DraftFor(call, [ImmediateAction(call.Request.ConversationId)]));
                case Mode.IgnoresTokenImmediate:
                    await Gate(batch).Task; // 令牌取消后仍然返回：专门用来喂"无视取消的迟到结果"。
                    return NarratorOutcome.Produced(DraftFor(call, [ImmediateAction(call.Request.ConversationId)]));
                case Mode.Malformed:
                    return NarratorOutcome.Malformed("{ \"prose\": ", "响应缺右括号，不是合法结构响应");
                case Mode.Throws:
                    throw new InvalidOperationException("provider 内部故障");
                case Mode.Hangs:
                    await Task.Delay(Timeout.InfiniteTimeSpan, CancellationToken.None);
                    return NarratorOutcome.Produced(DraftFor(call, [SilenceAction(call.Request.ConversationId)]));
                default:
                    return NarratorOutcome.Produced(DraftFor(call, [SilenceAction(call.Request.ConversationId)]));
            }
        }
    }

    private sealed class Fixture
    {
        public required OutboxOwnershipRegistry Ownership { get; init; }
        public required DeliveryLedger Ledger { get; init; }
        public required NarrativeCommitWriter CommitWriter { get; init; }
        public required CoordinatorNarrator Provider { get; init; }
        public required NarrativeLifecycleCoordinator Coordinator { get; init; }
    }

    /// <summary>装配一台协调器：写票由测试自己签（本波不接生产宿主），两实例各自持票。</summary>
    private static Fixture NewFixture()
    {
        var ownership = new OutboxOwnershipRegistry();
        Assert.True(ownership.TryAcquire(Alpha, EngineMode.NarrativeV1, WriterId, Now).Accepted);
        Assert.True(ownership.TryAcquire(Beta, EngineMode.NarrativeV1, WriterId, Now).Accepted);

        var instances = new CharacterInstanceRegistry();
        instances.Resolve(AlphaKey, Alpha);
        instances.Resolve(BetaKey, Beta);

        var ledger = new DeliveryLedger(ownership);
        var commitWriter = new NarrativeCommitWriter(ownership, instances);
        var provider = new CoordinatorNarrator();
        var runner = new NarrativeRoundRunner(provider, ledger, WriterId, Estimator);
        var coordinator = new NarrativeLifecycleCoordinator(runner, ownership, commitWriter, WriterId);
        return new Fixture
        {
            Ownership = ownership, Ledger = ledger, CommitWriter = commitWriter,
            Provider = provider, Coordinator = coordinator,
        };
    }

    private static void WaitRunning(Fixture fx, NarrativeScopeKey key)
    {
        var spins = 0;
        while (spins < SpinCap && fx.Coordinator.RunningScope(key.InstanceId) != key)
        {
            Thread.Sleep(2);
            spins++;
        }

        Assert.True(fx.Coordinator.RunningScope(key.InstanceId) == key,
            $"作用域 {key} 从未出现在在飞位（自旋 {spins} 次后仍未出现）——单写者的在飞标记没有挂上。");
    }

    private static async Task<T> AwaitAsync<T>(Task<T> task) =>
        await task.WaitAsync(TestContext.Current.CancellationToken);

    // —— §二.1 作用域幂等：同键回放（排队／在飞／已结算三相）——

    [Fact]
    public async Task Same_scope_key_replays_the_same_handle_and_never_generates_twice()
    {
        var fx = NewFixture();
        fx.Provider.UseMode("batch-1", Mode.GatedSilence);
        fx.Provider.UseMode("batch-2", Mode.GatedSilence);

        var first = fx.Coordinator.SubmitAsync(Req(Alpha, "room-1", "batch-1"), Pay("room-1"),
            GatedTimeout, Now, TestContext.Current.CancellationToken);
        var key1 = KeyOf(Alpha, "room-1", "batch-1");
        WaitRunning(fx, key1);

        // 在飞相位：同键第二次索要取回同一枚任务，不产生第二次生成。
        var secondWhileRunning = fx.Coordinator.SubmitAsync(Req(Alpha, "room-1", "batch-1"), Pay("room-1"),
            GatedTimeout, Now, TestContext.Current.CancellationToken);
        Assert.Same(first, secondWhileRunning);

        // 排队相位：另一键先排队，再同键索要，同样取回同一枚任务。
        var other = fx.Coordinator.SubmitAsync(Req(Alpha, "room-2", "batch-2"), Pay("room-2"),
            GatedTimeout, Now, TestContext.Current.CancellationToken);
        var secondWhileQueued = fx.Coordinator.SubmitAsync(Req(Alpha, "room-2", "batch-2"), Pay("room-2"),
            GatedTimeout, Now, TestContext.Current.CancellationToken);
        Assert.Same(other, secondWhileQueued);

        fx.Provider.Release("batch-1");
        fx.Provider.Release("batch-2");
        var settled = await AwaitAsync(first);
        var replayed = await AwaitAsync(secondWhileRunning);
        Assert.Same(settled, replayed);
        Assert.Equal(NarrativeScopeDisposition.Completed, settled.Disposition);
        Assert.True(settled.Accepted);
        Assert.Equal(key1, settled.Scope);

        // 已结算相位：第三次索要取回同一份结果，provider 计数纹丝不动。
        var third = await AwaitAsync(fx.Coordinator.SubmitAsync(Req(Alpha, "room-1", "batch-1"), Pay("room-1"),
            GatedTimeout, Now, TestContext.Current.CancellationToken));
        Assert.Equal(settled, third);
        Assert.Equal(1, fx.Provider.CallsFor("batch-1"));
        Assert.Equal(1, fx.Provider.CallsFor("batch-2"));
        Assert.Equal(settled, fx.Coordinator.ResultOf(key1));
    }

    // —— §二.1 作用域幂等：异键不互吞 ——

    [Fact]
    public async Task Different_keys_keep_their_own_results_and_never_swallow_each_other()
    {
        var fx = NewFixture();

        var first = await AwaitAsync(fx.Coordinator.SubmitAsync(Req(Alpha, "room-1", "batch-1"), Pay("room-1"),
            GatedTimeout, Now, TestContext.Current.CancellationToken));
        var second = await AwaitAsync(fx.Coordinator.SubmitAsync(Req(Alpha, "room-1", "batch-2"), Pay("room-1"),
            GatedTimeout, Now, TestContext.Current.CancellationToken));
        var beta = await AwaitAsync(fx.Coordinator.SubmitAsync(Req(Beta, "room-1", "batch-3"), Pay("room-1"),
            GatedTimeout, Now, TestContext.Current.CancellationToken));

        Assert.Equal(KeyOf(Alpha, "room-1", "batch-1"), first.Scope);
        Assert.Equal(KeyOf(Alpha, "room-1", "batch-2"), second.Scope);
        Assert.Equal(KeyOf(Beta, "room-1", "batch-3"), beta.Scope);
        Assert.NotEqual(first, second);
        Assert.NotEqual(first, beta);
        Assert.Equal(NarrativeScopeDisposition.Completed, second.Disposition);
        Assert.Equal(1, fx.Provider.CallsFor("batch-1")); // 批次计数互不串：beta 用独立批次号。
        Assert.Equal(1, fx.Provider.CallsFor("batch-3"));
        Assert.NotNull(fx.Coordinator.ResultOf(KeyOf(Alpha, "room-1", "batch-2")));
        Assert.NotNull(fx.Coordinator.ResultOf(KeyOf(Beta, "room-1", "batch-3")));
    }

    // —— §二.2 实例单写者：忙则排队，跨实例互不相干 ——

    [Fact]
    public async Task Busy_instance_queues_the_second_scope_and_another_instance_still_runs()
    {
        var fx = NewFixture();
        fx.Provider.UseMode("batch-1", Mode.GatedSilence);
        fx.Provider.UseMode("batch-2", Mode.GatedSilence);
        fx.Provider.UseMode("batch-9", Mode.GatedSilence);

        var alphaFirst = fx.Coordinator.SubmitAsync(Req(Alpha, "room-1", "batch-1"), Pay("room-1"),
            GatedTimeout, Now, TestContext.Current.CancellationToken);
        var key1 = KeyOf(Alpha, "room-1", "batch-1");
        WaitRunning(fx, key1);

        var alphaSecond = fx.Coordinator.SubmitAsync(Req(Alpha, "room-2", "batch-2"), Pay("room-2"),
            GatedTimeout, Now, TestContext.Current.CancellationToken);
        fx.Provider.Release("batch-2"); // 先放行闸门：分派时刻由泵决定，不由本行决定。
        Assert.Equal(key1, fx.Coordinator.RunningScope(Alpha));
        Assert.Equal(1, fx.Coordinator.QueuedScopeCount(Alpha));
        Assert.Equal(0, fx.Provider.CallsFor("batch-2"));

        // 单写者是按实例的：另一个实例此刻可以并行在飞。
        var betaTask = fx.Coordinator.SubmitAsync(Req(Beta, "room-3", "batch-9"), Pay("room-3"),
            GatedTimeout, Now, TestContext.Current.CancellationToken);
        WaitRunning(fx, KeyOf(Beta, "room-3", "batch-9"));
        Assert.Equal(1, fx.Provider.CallsFor("batch-9"));
        Assert.Equal(1, fx.Coordinator.QueuedScopeCount(Alpha));

        fx.Provider.Release("batch-1");
        var firstResult = await AwaitAsync(alphaFirst);
        fx.Provider.Release("batch-9");
        await AwaitAsync(betaTask);
        Assert.Equal(NarrativeScopeDisposition.Completed, firstResult.Disposition);

        var secondResult = await AwaitAsync(alphaSecond);
        Assert.Equal(NarrativeScopeDisposition.Completed, secondResult.Disposition);
        Assert.Equal(1, fx.Provider.CallsFor("batch-2"));
        Assert.Null(fx.Coordinator.RunningScope(Alpha));
        Assert.Equal(0, fx.Coordinator.QueuedScopeCount(Alpha));
    }

    // —— §二.2 单写者的拒绝面：落后代次的请求点名拒绝、绝不进队 ——

    [Fact]
    public async Task Request_below_the_current_epoch_is_named_and_never_queued()
    {
        var fx = NewFixture();
        var ticket = fx.Ownership.CurrentTicket(Alpha)!;
        Assert.True(fx.Ownership.Release(Alpha, WriterId, ticket.OwnershipEpoch).Accepted);
        Assert.True(fx.Ownership.TryAcquire(Alpha, ticket.Mode, WriterId, Now).Accepted);
        Assert.Equal(2, fx.Ownership.CurrentEpoch(Alpha));

        var result = await AwaitAsync(fx.Coordinator.SubmitAsync(Req(Alpha, "room-1", "batch-1", ownershipEpoch: 1),
            Pay("room-1"), GatedTimeout, Now, TestContext.Current.CancellationToken));

        Assert.Equal(NarrativeScopeDisposition.RejectedByAuthority, result.Disposition);
        Assert.Equal(NarrativeContractReason.OwnershipFenced, result.Reason);
        Assert.Null(result.Round);
        Assert.False(result.Accepted);
        Assert.Equal(0, fx.Coordinator.QueuedScopeCount(Alpha));
        Assert.Equal(0, fx.Provider.CallsFor("batch-1"));
    }

    // —— §二.3 公平调度：会话轮转，不饿死 ——

    [Fact]
    public async Task Fair_rotation_dispatches_by_conversation_round_robin_not_arrival_order()
    {
        var fx = NewFixture();
        foreach (var batch in new[] { "batch-0", "batch-1", "batch-2", "batch-3", "batch-4" })
            fx.Provider.UseMode(batch, Mode.GatedSilence);

        var tasks = new Dictionary<string, Task<NarrativeScopeResult>>(StringComparer.Ordinal);
        tasks["batch-0"] = fx.Coordinator.SubmitAsync(Req(Alpha, "room-0", "batch-0"), Pay("room-0"),
            GatedTimeout, Now, TestContext.Current.CancellationToken);
        WaitRunning(fx, KeyOf(Alpha, "room-0", "batch-0"));

        tasks["batch-1"] = fx.Coordinator.SubmitAsync(Req(Alpha, "room-1", "batch-1"), Pay("room-1"),
            GatedTimeout, Now, TestContext.Current.CancellationToken);
        tasks["batch-2"] = fx.Coordinator.SubmitAsync(Req(Alpha, "room-1", "batch-2"), Pay("room-1"),
            GatedTimeout, Now, TestContext.Current.CancellationToken);
        tasks["batch-3"] = fx.Coordinator.SubmitAsync(Req(Alpha, "room-1", "batch-3"), Pay("room-1"),
            GatedTimeout, Now, TestContext.Current.CancellationToken);
        tasks["batch-4"] = fx.Coordinator.SubmitAsync(Req(Alpha, "room-2", "batch-4"), Pay("room-2"),
            GatedTimeout, Now, TestContext.Current.CancellationToken);
        Assert.Equal(4, fx.Coordinator.QueuedScopeCount(Alpha));

        // 规则：会话按登记次序轮转、同会话按入队次序，每轮每会话只取一枚——
        // 预期分派次序为 batch-0, batch-1, batch-4, batch-2, batch-3（room-1 连投三枚也压不住 room-2）。
        var expected = new[] { "batch-0", "batch-1", "batch-4", "batch-2", "batch-3" };
        foreach (var batch in expected)
        {
            WaitRunning(fx, KeyOf(Alpha, batch == "batch-0" ? "room-0" : batch == "batch-4" ? "room-2" : "room-1",
                batch));
            fx.Provider.Release(batch);
            var result = await AwaitAsync(tasks[batch]);
            Assert.Equal(NarrativeScopeDisposition.Completed, result.Disposition);
        }

        Assert.Equal(expected, fx.Provider.DispatchOrder);
        Assert.Equal(0, fx.Coordinator.QueuedScopeCount(Alpha));
    }

    // —— §二.4 取消传播：同会话新批次顶掉在飞草稿 ——

    [Fact]
    public async Task New_batch_in_the_same_conversation_supersedes_and_voids_the_running_draft()
    {
        var fx = NewFixture();
        fx.Provider.UseMode("batch-1", Mode.GatedImmediate);
        fx.Provider.UseMode("batch-2", Mode.GatedImmediate);

        var first = fx.Coordinator.SubmitAsync(Req(Alpha, "room-1", "batch-1"), Pay("room-1"),
            GatedTimeout, Now, TestContext.Current.CancellationToken);
        var key1 = KeyOf(Alpha, "room-1", "batch-1");
        WaitRunning(fx, key1);

        var second = fx.Coordinator.SubmitAsync(Req(Alpha, "room-1", "batch-2"), Pay("room-1"),
            GatedTimeout, Now, TestContext.Current.CancellationToken);
        fx.Provider.Release("batch-1");
        var firstResult = await AwaitAsync(first);
        Assert.Equal(NarrativeScopeDisposition.CancelledByNewInput, firstResult.Disposition);
        Assert.Null(firstResult.Round);
        Assert.False(firstResult.Accepted);
        Assert.Equal(2, fx.Ownership.CurrentEpoch(Alpha));

        WaitRunning(fx, KeyOf(Alpha, "room-1", "batch-2"));
        fx.Provider.Release("batch-2");
        var secondResult = await AwaitAsync(second);
        Assert.Equal(NarrativeScopeDisposition.Completed, secondResult.Disposition);
        Assert.Equal(NarrativeRoundDisposition.Immediate, secondResult.Round!.Disposition);

        Assert.DoesNotContain(fx.Ledger.Snapshot(), s => s.CommitId == "commit-proposal-batch-1");
        Assert.Equal(3, fx.Ledger.Snapshot().Count(s => s.CommitId == "commit-proposal-batch-2"));
        Assert.Equal(1, fx.Provider.CallsFor("batch-1"));
        Assert.Equal(1, fx.Provider.CallsFor("batch-2"));
    }

    // —— §二.4 取消传播：宿主取消＋无视取消的迟到结果经既有代次门永久无资格 ——

    [Fact]
    public async Task Late_result_from_a_provider_ignoring_cancellation_never_lands_in_history()
    {
        var fx = NewFixture();
        fx.Provider.UseMode("batch-1", Mode.IgnoresTokenImmediate);
        using var hostCancellation = new CancellationTokenSource();

        var task = fx.Coordinator.SubmitAsync(Req(Alpha, "room-1", "batch-1"), Pay("room-1"),
            GatedTimeout, Now, hostCancellation.Token);
        WaitRunning(fx, KeyOf(Alpha, "room-1", "batch-1"));

        hostCancellation.Cancel();
        fx.Provider.Release("batch-1"); // provider 无视取消，照样把即时草稿交回来。
        var result = await AwaitAsync(task);

        Assert.Equal(NarrativeScopeDisposition.CancelledByHost, result.Disposition);
        Assert.Null(result.Round);
        Assert.False(result.Accepted);
        Assert.Equal(2, fx.Ownership.CurrentEpoch(Alpha));
        Assert.Empty(fx.Ledger.Snapshot());
        Assert.Equal(1, fx.Provider.CallsFor("batch-1"));

        // 既有代次门自己说话：落后代次的段在投递账本永久无资格，协调器没有另造失权判定。
        var lateSegment = new DeliverySegment
        {
            InstanceId = Alpha, ConversationId = "room-1", CommitId = "commit-probe", ActionId = "act-probe",
            SegmentIndex = 0, State = DeliverySegmentState.Pending, Text = "probe", OwnershipEpoch = 1,
        };
        var fenced = fx.Ledger.TryAppend(lateSegment, WriterId);
        Assert.False(fenced.Accepted);
        Assert.Equal(NarrativeContractReason.OwnershipFenced, fenced.Reason);
    }

    // —— §二.5 会话删除：在飞＋排队全部作废、不重新入队；:403 的协调层等价判据 ——

    [Fact]
    public async Task Conversation_deletion_voids_inflight_and_queued_without_requeue()
    {
        var fx = NewFixture();
        fx.Provider.UseMode("batch-1", Mode.GatedImmediate);
        fx.Provider.UseMode("batch-2", Mode.GatedSilence);
        fx.Provider.UseMode("batch-3", Mode.GatedSilence);
        fx.Provider.UseMode("batch-4", Mode.GatedSilence);

        var inflight = fx.Coordinator.SubmitAsync(Req(Alpha, "room-1", "batch-1"), Pay("room-1"),
            GatedTimeout, Now, TestContext.Current.CancellationToken);
        WaitRunning(fx, KeyOf(Alpha, "room-1", "batch-1"));

        var queued = new List<Task<NarrativeScopeResult>>
        {
            fx.Coordinator.SubmitAsync(Req(Alpha, "room-2", "batch-2"), Pay("room-2"), GatedTimeout, Now,
                TestContext.Current.CancellationToken),
            fx.Coordinator.SubmitAsync(Req(Alpha, "room-2", "batch-3"), Pay("room-2"), GatedTimeout, Now,
                TestContext.Current.CancellationToken),
            fx.Coordinator.SubmitAsync(Req(Alpha, "room-2", "batch-4"), Pay("room-2"), GatedTimeout, Now,
                TestContext.Current.CancellationToken),
        };
        Assert.Equal(3, fx.Coordinator.QueuedScopeCount(Alpha));

        // 既有删除代次出口先记到写者层，再由协调层作废排队。
        fx.Coordinator.InvalidateConversation("room-2", deletionEpoch: 5, Now);
        Assert.Equal(0, fx.Coordinator.QueuedScopeCount(Alpha));
        foreach (var entry in queued)
        {
            var voided = await AwaitAsync(entry);
            Assert.Equal(NarrativeScopeDisposition.InvalidatedByScopeDeletion, voided.Disposition);
            Assert.Null(voided.Round);
        }

        Assert.Equal(0, fx.Provider.CallsFor("batch-2"));
        Assert.Equal(0, fx.Provider.CallsFor("batch-3"));
        Assert.Equal(0, fx.Provider.CallsFor("batch-4"));

        // NarrativeStorageContractTests.cs:403 的协调层等价件：作废的作用域不复活、迟到提交过不了既有删除门。
        var lateCommit = fx.CommitWriter.TryCommit(ProbeRequest("room-2", "batch-2"), ProbeCommit(deletionEpoch: 3,
            ownershipEpoch: 1));
        Assert.False(lateCommit.Accepted);
        Assert.Equal(NarrativeContractReason.DeletionEpochAdvanced, lateCommit.Reason);

        // 已删除会话的三枚作用域永不回炉；在飞的另一会话走删除路径收场。
        fx.Coordinator.InvalidateConversation("room-1", deletionEpoch: 5, Now);
        var inflightResult = await AwaitAsync(inflight);
        Assert.Equal(NarrativeScopeDisposition.InvalidatedByScopeDeletion, inflightResult.Disposition);
        Assert.Null(inflightResult.Round);
        Assert.Equal(1, fx.Provider.CallsFor("batch-1"));
        Assert.Equal(0, fx.Provider.CallsFor("batch-2"));
        Assert.Empty(fx.Ledger.Snapshot());
        Assert.Null(fx.Coordinator.RunningScope(Alpha));
    }

    // —— §二.5 实例重配置：只作废该实例，别实例照常走 ——

    [Fact]
    public async Task Reconfiguring_one_instance_voids_its_scopes_without_touching_the_other()
    {
        var fx = NewFixture();
        fx.Provider.UseMode("batch-1", Mode.GatedImmediate);
        fx.Provider.UseMode("batch-2", Mode.GatedSilence);
        fx.Provider.UseMode("batch-9", Mode.GatedSilence);

        var alphaInflight = fx.Coordinator.SubmitAsync(Req(Alpha, "room-1", "batch-1"), Pay("room-1"),
            GatedTimeout, Now, TestContext.Current.CancellationToken);
        WaitRunning(fx, KeyOf(Alpha, "room-1", "batch-1"));
        var alphaQueued = fx.Coordinator.SubmitAsync(Req(Alpha, "room-3", "batch-2"), Pay("room-3"),
            GatedTimeout, Now, TestContext.Current.CancellationToken);
        var betaInflight = fx.Coordinator.SubmitAsync(Req(Beta, "room-4", "batch-9"), Pay("room-4"),
            GatedTimeout, Now, TestContext.Current.CancellationToken);
        WaitRunning(fx, KeyOf(Beta, "room-4", "batch-9"));

        fx.Coordinator.ReconfigureInstance(Alpha, Now);

        fx.Provider.Release("batch-1");
        fx.Provider.Release("batch-9");

        var voidedInflight = await AwaitAsync(alphaInflight);
        Assert.Equal(NarrativeScopeDisposition.InvalidatedByReconfigure, voidedInflight.Disposition);
        Assert.Null(voidedInflight.Round);

        var voidedQueued = await AwaitAsync(alphaQueued);
        Assert.Equal(NarrativeScopeDisposition.InvalidatedByReconfigure, voidedQueued.Disposition);
        Assert.Equal(0, fx.Provider.CallsFor("batch-2"));

        var betaResult = await AwaitAsync(betaInflight);
        Assert.Equal(NarrativeScopeDisposition.Completed, betaResult.Disposition);
        Assert.Equal(1, fx.Ownership.CurrentEpoch(Beta));
        Assert.Equal(0, fx.Coordinator.QueuedScopeCount(Alpha));
        Assert.Empty(fx.Ledger.Snapshot());
    }

    // —— §二.6 超额度：被取消的作用域再索要，只会在既有额度强制点上失败 ——

    [Fact]
    public async Task Resubmitting_a_cancelled_scope_fails_at_the_existing_call_budget()
    {
        var fx = NewFixture();
        fx.Provider.UseMode("batch-1", Mode.GatedSilence);
        using var hostCancellation = new CancellationTokenSource();

        var task = fx.Coordinator.SubmitAsync(Req(Alpha, "room-1", "batch-1"), Pay("room-1"),
            GatedTimeout, Now, hostCancellation.Token);
        WaitRunning(fx, KeyOf(Alpha, "room-1", "batch-1"));

        hostCancellation.Cancel();
        fx.Provider.Release("batch-1");
        var cancelled = await AwaitAsync(task);
        Assert.Equal(NarrativeScopeDisposition.CancelledByHost, cancelled.Disposition);

        var overQuota = await AwaitAsync(fx.Coordinator.SubmitAsync(Req(Alpha, "room-1", "batch-1"), Pay("room-1"),
            GatedTimeout, Now, TestContext.Current.CancellationToken));

        Assert.Equal(NarrativeScopeDisposition.FailedByCallBudget, overQuota.Disposition);
        Assert.Null(overQuota.Round);
        Assert.False(overQuota.Accepted);
        Assert.Equal(1, fx.Provider.CallsFor("batch-1"));
        Assert.Equal(NarrativeScopeDisposition.CancelledByHost,
            fx.Coordinator.ResultOf(KeyOf(Alpha, "room-1", "batch-1"))!.Disposition);
        Assert.Equal(0, fx.Coordinator.QueuedScopeCount(Alpha));
    }

    // —— §二.6/§二.7 三种失败可区分，且绝不映射成合法沉默 ——

    [Fact]
    public async Task Over_quota_provider_failure_malformed_and_timeout_are_distinguishable_and_never_silence()
    {
        var fx = NewFixture();
        fx.Provider.UseMode("batch-timeout", Mode.Hangs);
        fx.Provider.UseMode("batch-throw", Mode.Throws);
        fx.Provider.UseMode("batch-malformed", Mode.Malformed);

        var timedOut = await AwaitAsync(fx.Coordinator.SubmitAsync(Req(Alpha, "room-1", "batch-timeout"),
            Pay("room-1"), TimeoutProbe, Now, TestContext.Current.CancellationToken));
        var providerFailed = await AwaitAsync(fx.Coordinator.SubmitAsync(Req(Alpha, "room-2", "batch-throw"),
            Pay("room-2"), GatedTimeout, Now, TestContext.Current.CancellationToken));
        var malformed = await AwaitAsync(fx.Coordinator.SubmitAsync(Req(Alpha, "room-3", "batch-malformed"),
            Pay("room-3"), GatedTimeout, Now, TestContext.Current.CancellationToken));

        Assert.Equal(NarrativeScopeDisposition.FailedByTimeout, timedOut.Disposition);
        Assert.Equal(NarratorOutcomeKind.TimedOut, timedOut.Round!.Outcome!.Kind);
        Assert.Equal(NarrativeScopeDisposition.FailedByProvider, providerFailed.Disposition);
        Assert.Equal(NarratorOutcomeKind.ProviderFailed, providerFailed.Round!.Outcome!.Kind);
        Assert.Equal(NarrativeScopeDisposition.FailedByProvider, malformed.Disposition);
        Assert.Equal(NarratorOutcomeKind.MalformedResponse, malformed.Round!.Outcome!.Kind);

        // 失败不是产出：三种失败都不是合法沉默，账上一条历史都没有。
        Assert.False(timedOut.Accepted);
        Assert.False(providerFailed.Accepted);
        Assert.False(malformed.Accepted);
        foreach (var entry in new[] { timedOut, providerFailed, malformed })
            Assert.NotEqual(NarrativeRoundDisposition.Silence, entry.Round!.Disposition);
        Assert.Empty(fx.Ledger.Snapshot());
    }

    // —— §二.7 坏响应永不伪装合法沉默：沉默只来自真草稿 ——

    [Fact]
    public async Task Silence_is_only_ever_produced_by_a_real_draft_never_by_a_failure()
    {
        var fx = NewFixture();
        fx.Provider.UseMode("batch-malformed", Mode.Malformed);

        var genuine = await AwaitAsync(fx.Coordinator.SubmitAsync(Req(Alpha, "room-1", "batch-silent"),
            Pay("room-1"), GatedTimeout, Now, TestContext.Current.CancellationToken));
        var rejectedBadPayload = await AwaitAsync(fx.Coordinator.SubmitAsync(
            Req(Alpha, "room-1", "batch-malformed"), Pay("room-1"), GatedTimeout, Now,
            TestContext.Current.CancellationToken));

        Assert.Equal(NarrativeScopeDisposition.Completed, genuine.Disposition);
        Assert.Equal(NarrativeRoundDisposition.Silence, genuine.Round!.Disposition);
        Assert.True(genuine.Accepted);

        Assert.Equal(NarrativeScopeDisposition.FailedByProvider, rejectedBadPayload.Disposition);
        Assert.NotEqual(NarrativeRoundDisposition.Silence, rejectedBadPayload.Round!.Disposition);
        Assert.Equal(1, genuine.Round.SilentActionCount);
        Assert.Equal(0, rejectedBadPayload.Round!.SilentActionCount);
    }

    // —— §二.8 来源扫描门：新协调器源码零旧主链引用（扫描器自带阳性探针）——

    [Fact]
    public void Coordinator_source_carries_no_legacy_chain_references_and_the_probe_proves_the_gate_bites()
    {
        var identifiers = new[] { "BehaviorPlanner", "ReplyGenerationRenderer", "ReplyPlanner", "ReplyNecessity" };
        var path = Path.Combine(RepositoryRoot(), "src", "Pry.Core", "Narrative",
            "NarrativeLifecycleCoordinator.cs");
        Assert.True(File.Exists(path), $"协调器源码不在盘上：{path}");
        var source = File.ReadAllText(path);

        foreach (var identifier in identifiers)
            Assert.DoesNotContain(identifier, source, StringComparison.Ordinal);

        // 阳性探针：扫描判据必须能咬住真的引用了旧主链的坏样本，否则门是常绿的假门。
        var sample = Path.ChangeExtension(Path.GetTempFileName(), ".cs");
        try
        {
            File.WriteAllText(sample, "var legacy = new ReplyPlanner(); // 旧主链装配\n");
            var badSample = File.ReadAllText(sample);
            Assert.Contains("ReplyPlanner", badSample, StringComparison.Ordinal);
            Assert.DoesNotContain("ReplyPlanner", source, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(sample);
        }
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Pry.slnx")))
            directory = directory.Parent;
        Assert.True(directory is not null, "自测试输出目录上溯找不到 Pry.slnx，仓库根无法定位。");
        return directory!.FullName;
    }

    // —— 探针文档 ——

    private static NarrativeRequest ProbeRequest(string conversation, string batch) => new()
    {
        InstanceId = Alpha,
        ConversationId = conversation,
        BatchId = batch,
        Inputs = [new InputRef(101, "msg-101", conversation)],
        Trigger = NarrativeTriggerKind.UserInput,
        StoryVersion = 0,
        OwnershipEpoch = 1,
        IssuedByWriter = WriterId,
        IssuedAtUtc = Now,
    };

    private static NarrativeCommit ProbeCommit(long deletionEpoch, long ownershipEpoch) => new()
    {
        CommitId = "commit-probe",
        InstanceId = Alpha,
        ConversationId = "room-2",
        BatchId = "batch-2",
        NarrativeProse = Prose,
        Actions =
        [
            new CommunicationAction
            {
                ActionId = "act-1", Mode = CommunicationMode.Immediate, TargetConversationId = "room-2",
                ProseQuote = BoundQuote, PlannedSegmentCount = 1,
            },
        ],
        ProcessedThrough = new ProcessedThroughSeq(101),
        PerceivedInputRefs = new PerceivedInputRefs { Refs = [new InputRef(101, "msg-101", "room-2")] },
        ExpectedStoryVersion = 0,
        DeletionEpoch = deletionEpoch,
        OwnershipEpoch = ownershipEpoch,
        ContentHash = "sha256:probe",
        CommittedByWriter = WriterId,
        CommittedAtUtc = Now,
        ProposalId = "proposal-probe",
        ConsumedInputSeqs = [101],
    };
}

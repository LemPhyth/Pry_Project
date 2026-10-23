using Pry.Core.Memory;
using Pry.Core.Narrative;
using Pry.Core.Prompting;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// HDS-003 新生成中心隔离原型（第十三波）。四组判据对应工单 §二 的四条最小形状：
/// A 组＝装配器（段数/段名照盘上 ContextBuilder 的 U1–U8）与预算门；B 组＝一次 Narrator 调用的强制点；
/// C 组＝沉默与延迟的分岔；D/E 组＝原文行动绑定与快照连续性。
/// 每条都是"计数＋点名理由"的确切断言，不用 NotEmpty／&gt;0／Skip=。
/// provider 全部是注入的确定性假实现：真实模型对照不在本波，一律登记为未验证。
/// </summary>
public sealed class NarratorPrototypeTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 9, 0, 0, TimeSpan.Zero);
    private const string Conversation = "room-1";
    private const string Batch = "batch-1";
    private const string WriterId = "writer-narrative-1";

    private const string Prose = "She set the brush down, answered him, then kept painting.";
    private const string BoundQuote = "answered him";

    private const string Identity = "Pry canon";
    private const string OutputRules = "json rules";
    private const string CurrentEventText = "hello there";

    private static readonly CharacterInstanceId Alpha = CharacterInstanceId.Create("ci-instance-alpha");
    private static readonly InputRef Input101 = new(101, "msg-101", Conversation);
    private static readonly ConservativeTokenEstimator Estimator = new();

    /// <summary>U1 必需前奏的确切成本：身份＋输出规则一条、当前事件一条，各自加框架开销。</summary>
    private static readonly int PreludeCost =
        Estimator.Count(Identity + "\n" + OutputRules) + NarrativeContextCompiler.MessageFramingOverhead
        + Estimator.Count(CurrentEventText) + NarrativeContextCompiler.MessageFramingOverhead;

    // —— 夹具 ——

    private static NarrativeRequest Request(long storyVersion = 7, long ownershipEpoch = 1,
        long processedBaseline = 100, int maxSegments = 4) => new()
    {
        InstanceId = Alpha,
        ConversationId = Conversation,
        BatchId = Batch,
        Inputs = [Input101],
        Trigger = NarrativeTriggerKind.UserInput,
        StoryVersion = storyVersion,
        DeletionEpoch = 0,
        OwnershipEpoch = ownershipEpoch,
        IssuedByWriter = WriterId,
        MaxOutboundSegments = maxSegments,
        IssuedAtUtc = Now,
        ProcessedThroughAtIssue = new ProcessedThroughSeq(processedBaseline),
    };

    private static NarrativeContextPayloads Payloads(ContextBudget? budget = null, string advisory = "",
        string state = "", IReadOnlyList<NarrativeContextUnit>? recent = null,
        IReadOnlyList<NarrativeContextUnit>? memory = null, string behavior = "", string expression = "",
        string capability = "", string identity = Identity, string currentEvent = CurrentEventText,
        string currentEventConversation = Conversation) => new()
    {
        Budget = budget ?? new ContextBudget(),
        Identity = identity,
        OutputRules = OutputRules,
        CurrentEvent = new NarrativeContextUnit("input:101", currentEventConversation, currentEvent),
        Advisory = advisory,
        State = state,
        RecentMessages = recent ?? [],
        MemoryExcerpts = memory ?? [],
        BehaviorReferences = behavior,
        ExpressionPatterns = expression,
        CapabilityResult = capability,
    };

    private static CommunicationAction SilenceAction(string id = "action-silence") => new()
    {
        ActionId = id, Mode = CommunicationMode.Silence, TargetConversationId = Conversation,
        PlannedSegmentCount = 0,
    };

    private static CommunicationAction ImmediateAction(string quote = BoundQuote, int segments = 3,
        string id = "action-now", string target = Conversation) => new()
    {
        ActionId = id, Mode = CommunicationMode.Immediate, TargetConversationId = target,
        ProseQuote = quote, PlannedSegmentCount = segments,
    };

    private static CommunicationAction DeferredAction(string intentId, string id = "action-later",
        string target = Conversation) => new()
    {
        ActionId = id, Mode = CommunicationMode.Deferred, TargetConversationId = target,
        PlannedSegmentCount = 0, DeferredIntentId = intentId,
    };

    private static NarrativeIntent PendingIntent(string id = "intent-1",
        NarrativeIntentStatus status = NarrativeIntentStatus.Pending, string sourceCommit = Batch) => new()
    {
        IntentId = id, InstanceId = Alpha, Topic = "改天再问他那幅画", Status = status, SourceCommitId = sourceCommit,
    };

    private static NarrativeDraft Draft(IReadOnlyList<CommunicationAction>? actions = null,
        IReadOnlyList<NarrativeIntent>? intents = null, long storyVersionBase = 7, long ownershipEpoch = 1,
        long receivedBaseline = 100, long processedThrough = 101, PerceivedInputRefs? perceived = null,
        string prose = Prose) => new()
    {
        InstanceId = Alpha, ConversationId = Conversation, BatchId = Batch, NarrativeProse = prose,
        ProcessedThrough = new ProcessedThroughSeq(processedThrough),
        PerceivedInputRefs = perceived ?? new PerceivedInputRefs { Refs = [Input101] },
        Actions = actions ?? [SilenceAction()],
        Intents = intents ?? [],
        StoryVersionBase = storyVersionBase, OwnershipEpoch = ownershipEpoch, DeletionEpoch = 0,
        ReceivedThroughSeqAtIssue = receivedBaseline, ProposalId = "proposal-1", ProposedAtUtc = Now,
    };

    private static NarrativeContextUnit Unit(string sourceRef, string content, string conversation = Conversation) =>
        new(sourceRef, conversation, content);

    private enum FakeMode
    {
        Produce, Malformed, HangsPastTimeout, CancelsOnToken, Throws,
    }

    /// <summary>确定性假 provider：只产出本波判据需要的五种形状，不含真实模型、网络或运行库。</summary>
    private sealed class FakeNarrator(FakeMode mode, NarrativeDraft? draft = null) : INarrator
    {
        public int InvocationCount { get; private set; }
        public NarratorCall? LastCall { get; private set; }

        public async Task<NarratorOutcome> ProposeAsync(NarratorCall call, CancellationToken cancellationToken)
        {
            InvocationCount++;
            LastCall = call;
            switch (mode)
            {
                case FakeMode.Produce:
                    return NarratorOutcome.Produced(draft!);
                case FakeMode.Malformed:
                    return NarratorOutcome.Malformed("{ \"prose\": ", "响应缺右括号，不是合法结构响应");
                case FakeMode.HangsPastTimeout:
                    await Task.Delay(Timeout.InfiniteTimeSpan, CancellationToken.None);
                    return NarratorOutcome.Produced(draft!);
                case FakeMode.CancelsOnToken:
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                    return NarratorOutcome.Produced(draft!);
                default:
                    throw new InvalidOperationException("provider 内部故障");
            }
        }
    }

    private static readonly TimeSpan RoundTimeout = TimeSpan.FromMilliseconds(40);

    private sealed class RoundFixture
    {
        public required OutboxOwnershipRegistry Ownership { get; init; }
        public required DeliveryLedger Ledger { get; init; }
        public required FakeNarrator Provider { get; init; }
        public required NarrativeRoundRunner Runner { get; init; }
    }

    /// <summary>装配一轮：写入权由测试自己签（本波不接生产宿主），"真实历史"面用既有投递账本。</summary>
    private static RoundFixture Round(NarrativeDraft draft) => RoundWith(FakeMode.Produce, draft);

    private static RoundFixture RoundWith(FakeMode mode, NarrativeDraft? draft = null)
    {
        var ownership = new OutboxOwnershipRegistry();
        var grant = ownership.TryAcquire(Alpha, EngineMode.NarrativeV1, WriterId, Now);
        Assert.True(grant.Accepted, grant.Detail);

        var ledger = new DeliveryLedger(ownership);
        var provider = new FakeNarrator(mode, draft);
        return new RoundFixture
        {
            Ownership = ownership, Ledger = ledger, Provider = provider,
            Runner = new NarrativeRoundRunner(provider, ledger, WriterId, Estimator),
        };
    }

    // —— A 组：装配器（工单 §二.1）——

    [Fact]
    public void Compiler_emits_the_eight_assembly_units_of_the_disk_builder_in_its_order()
    {
        var assembly = new NarrativeContextCompiler(Estimator).Compile(Request(), Payloads());

        Assert.True(assembly.Accepted, assembly.Detail);
        Assert.Equal(NarrativeContextCompiler.SegmentCount, assembly.SegmentCount);
        Assert.Equal(
            new[] { "Identity", "Advisory", "State", "RecentMessages", "Memory", "BehaviorReferences",
                "ExpressionPatterns", "CapabilityResult" },
            assembly.SegmentNames);
        Assert.All(assembly.Segments, segment => Assert.StartsWith(
            "src/Pry.Core/Prompting/ContextBuilder.cs:", segment.SourceRef, StringComparison.Ordinal));
    }

    [Fact]
    public void Compiler_keeps_absent_sections_as_not_supplied_instead_of_dropping_them()
    {
        var assembly = new NarrativeContextCompiler(Estimator).Compile(Request(), Payloads());

        Assert.Equal(7, assembly.NotSuppliedSegmentCount);
        Assert.Equal(0, assembly.DroppedSegmentCount);
        Assert.Single(assembly.Segments, segment => segment.Disposition == NarrativeSegmentDisposition.Included);
        Assert.Equal("Identity", assembly.Segments.Single(segment =>
            segment.Disposition == NarrativeSegmentDisposition.Included).Name);
    }

    [Fact]
    public void Compiler_drops_an_optional_section_that_exceeds_its_own_share_and_counts_it_exactly()
    {
        var state = new string('x', 40);
        var assembly = new NarrativeContextCompiler(Estimator).Compile(Request(),
            Payloads(new ContextBudget(StateTokens: 20), state: state));

        var section = assembly.Segments.Single(s => s.Name == "State");
        Assert.Equal(NarrativeSegmentDisposition.Dropped, section.Disposition);
        Assert.Equal("budget", section.DispositionReason);
        Assert.Equal(20, section.BudgetTokens);
        Assert.Equal(Estimator.Count("\nState：\n" + state), section.UsedTokens);
        Assert.Equal(PreludeCost, assembly.InputEstimateTokens);
    }

    [Fact]
    public void Compiler_refuses_the_round_when_the_mandatory_prelude_does_not_fit()
    {
        var assembly = new NarrativeContextCompiler(Estimator).Compile(Request(),
            Payloads(new ContextBudget(ContextTokens: 60, OutputTokens: 0, SafetyTokens: 0),
                identity: new string('i', 100)));

        Assert.False(assembly.Accepted);
        Assert.Equal(NarrativeCompileReason.MandatoryExceedsBudget, assembly.ReasonCode);
        Assert.Equal(0, assembly.InputEstimateTokens);
    }

    [Fact]
    public void Compiler_refuses_a_window_that_leaves_no_positive_input_budget()
    {
        var assembly = new NarrativeContextCompiler(Estimator).Compile(Request(),
            Payloads(new ContextBudget(ContextTokens: 256, OutputTokens: 200, SafetyTokens: 100)));

        Assert.False(assembly.Accepted);
        Assert.Equal(NarrativeCompileReason.WindowTooSmall, assembly.ReasonCode);
    }

    [Fact]
    public void Compiler_refuses_a_scene_without_identity_instead_of_inventing_one()
    {
        var assembly = new NarrativeContextCompiler(Estimator).Compile(Request(), Payloads(identity: ""));

        Assert.False(assembly.Accepted);
        Assert.Equal(NarrativeCompileReason.MandatoryContentMissing, assembly.ReasonCode);
    }

    [Fact]
    public void Compiler_refuses_a_current_event_from_another_conversation()
    {
        var assembly = new NarrativeContextCompiler(Estimator).Compile(Request(),
            Payloads(currentEventConversation: "room-2"));

        Assert.False(assembly.Accepted);
        Assert.Equal(NarrativeCompileReason.MandatoryContentMissing, assembly.ReasonCode);
    }

    [Fact]
    public void Compiler_fails_loudly_when_the_reserved_directive_does_not_fit()
    {
        var assembly = new NarrativeContextCompiler(Estimator).Compile(Request(),
            Payloads(advisory: new string('a', 2401)));

        Assert.False(assembly.Accepted);
        Assert.Equal(NarrativeCompileReason.ReservedDirectiveDoesNotFit, assembly.ReasonCode);
        var advisory = assembly.Segments.Single(s => s.Name == "Advisory");
        Assert.Equal(NarrativeSegmentDisposition.Dropped, advisory.Disposition);
        Assert.Equal("reserved_directive_does_not_fit", advisory.DispositionReason);
        Assert.Equal(2400, advisory.BudgetTokens);
        Assert.Equal(1, assembly.DroppedSegmentCount);
    }

    [Fact]
    public void Compiler_trims_recent_messages_only_at_whole_unit_boundaries()
    {
        var units = new[] { Unit("input:103", "0123456789"), Unit("input:102", "abcdefghij"),
            Unit("input:101", "KLMNOPQRST") };
        var assembly = new NarrativeContextCompiler(Estimator).Compile(Request(),
            Payloads(new ContextBudget(RecentTokens: 30), recent: units));

        var section = assembly.Segments.Single(s => s.Name == "RecentMessages");
        Assert.Equal(NarrativeSegmentDisposition.PartiallyIncluded, section.Disposition);
        Assert.Equal("unit_budget", section.DispositionReason);
        Assert.Equal(1, section.IncludedUnitCount);
        Assert.Equal(2, section.DroppedUnitCount);
        Assert.Equal("\n- 0123456789", section.EmittedText);
        Assert.Equal(new[] { "input:103" }, section.SourceRefs);
    }

    [Fact]
    public void Compiler_removes_cross_conversation_units_and_names_the_count()
    {
        var units = new[] { Unit("input:103", "aaa"), Unit("other:1", "bbb", "room-2"), Unit("input:101", "ccc") };
        var assembly = new NarrativeContextCompiler(Estimator).Compile(Request(), Payloads(recent: units));

        var section = assembly.Segments.Single(s => s.Name == "RecentMessages");
        Assert.Equal(NarrativeSegmentDisposition.PartiallyIncluded, section.Disposition);
        Assert.Equal("cross_scope", section.DispositionReason);
        Assert.Equal(1, section.CrossScopeUnitCount);
        Assert.Equal(1, section.DroppedUnitCount);
        Assert.Equal(2, section.IncludedUnitCount);
        Assert.Equal(new[] { "input:103", "input:101" }, section.SourceRefs);
    }

    [Fact]
    public void Compiler_shrinks_the_memory_wallet_unit_by_unit()
    {
        var units = new[] { Unit("memory:12", "0123456789"), Unit("memory:11", "abcdefghij") };
        var assembly = new NarrativeContextCompiler(Estimator).Compile(Request(),
            Payloads(new ContextBudget(MemoryTokens: 30), memory: units));

        var section = assembly.Segments.Single(s => s.Name == "Memory");
        Assert.Equal(1, section.IncludedUnitCount);
        Assert.Equal(1, section.DroppedUnitCount);
        Assert.Equal("unit_budget", section.DispositionReason);
        Assert.Equal("\n- 0123456789", section.EmittedText);
        Assert.Equal(18, section.UsedTokens);
    }

    [Fact]
    public void Compiler_honours_the_unified_budget_inequality_with_exact_numbers()
    {
        var advisory = new string('b', 100);
        var state = "地点：画室";
        var memory = "旧事实一条";
        var assembly = new NarrativeContextCompiler(Estimator).Compile(Request(),
            Payloads(advisory: advisory, state: state, recent: [Unit("input:101", CurrentEventText)],
                memory: [Unit("memory:9", memory)]));

        var expected = PreludeCost + Estimator.Count("\n" + advisory) + Estimator.Count("\nState：\n" + state)
            + Estimator.Count(CurrentEventText) + NarrativeContextCompiler.MessageFramingOverhead
            + Estimator.Count(memory) + NarrativeContextCompiler.MessageFramingOverhead;
        Assert.True(assembly.Accepted, assembly.Detail);
        Assert.Equal(expected, assembly.InputEstimateTokens);
        Assert.Equal(4096 - 512 - 128, assembly.InputBudgetTokens);
        Assert.True(assembly.WithinUnifiedBudget);
    }

    [Fact]
    public void Compiler_rejects_a_negative_section_share()
    {
        var assembly = new NarrativeContextCompiler(Estimator).Compile(Request(),
            Payloads(new ContextBudget(StateTokens: -1)));

        Assert.False(assembly.Accepted);
        Assert.Equal(NarrativeCompileReason.InvalidBudget, assembly.ReasonCode);
    }

    [Fact]
    public void Compiler_rejects_a_window_of_zero_tokens()
    {
        var assembly = new NarrativeContextCompiler(Estimator).Compile(Request(),
            Payloads(new ContextBudget(ContextTokens: 0)));

        Assert.False(assembly.Accepted);
        Assert.Equal(NarrativeCompileReason.InvalidBudget, assembly.ReasonCode);
    }

    [Fact]
    public void Compiler_carries_source_references_into_every_emitted_section()
    {
        var assembly = new NarrativeContextCompiler(Estimator).Compile(Request(),
            Payloads(advisory: "decide", recent: [Unit("input:101", CurrentEventText)]));

        Assert.Equal(4, assembly.EmittedSourceRefs.Count);
        Assert.Contains("identity:canon", assembly.EmittedSourceRefs);
        Assert.Contains("advisory:block", assembly.EmittedSourceRefs);
        Assert.Equal(2, assembly.EmittedSourceRefs.Count(r => r == "input:101"));
    }

    // —— B 组：一次 Narrator 调用（工单 §二.2）——

    [Fact]
    public async Task Round_calls_the_provider_exactly_once_for_one_narration()
    {
        var round = Round(Draft());

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Equal(NarrativeRoundDisposition.Silence, result.Disposition);
        Assert.Equal(1, result.NarratorCallCount);
        Assert.Equal(1, round.Provider.InvocationCount);
        Assert.NotNull(result.Draft);
    }

    [Fact]
    public async Task Round_does_not_call_the_provider_when_the_context_is_rejected()
    {
        var round = Round(Draft());

        var result = await round.Runner.RunAsync(Request(),
            Payloads(new ContextBudget(ContextTokens: 256, OutputTokens: 200, SafetyTokens: 100)), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Equal(NarrativeRoundDisposition.ContextRejected, result.Disposition);
        Assert.Equal(0, result.NarratorCallCount);
        Assert.Equal(0, round.Provider.InvocationCount);
        Assert.Equal(NarrativeCompileReason.WindowTooSmall, result.Assembly!.ReasonCode);
    }

    [Fact]
    public void Second_call_on_the_same_round_budget_is_refused()
    {
        var budget = new NarratorCallBudget();
        budget.Consume();

        var error = Assert.Throws<NarrativeContextAssemblyRejectedException>(() => budget.Consume());

        Assert.Equal(2, budget.CallCount);
        Assert.Equal(NarrativeCompileReason.SecondCallNotPermitted, error.Reason);
    }

    [Fact]
    public void Call_refuses_an_assembly_that_still_carries_a_rejection()
    {
        var rejected = new NarrativeContextCompiler(Estimator).Compile(Request(),
            Payloads(new ContextBudget(ContextTokens: 256, OutputTokens: 200, SafetyTokens: 100)));

        var error = Assert.Throws<NarrativeContextAssemblyRejectedException>(() => new NarratorCall
        {
            Request = Request(), Assembly = rejected, Budget = new NarratorCallBudget(),
        }.Validate());

        Assert.Equal(NarrativeCompileReason.WindowTooSmall, error.Reason);
    }

    [Theory]
    [InlineData("instance")]
    [InlineData("conversation")]
    [InlineData("batch")]
    public void Call_refuses_an_assembly_that_belongs_to_another_scope(string scope)
    {
        var assembly = new NarrativeContextCompiler(Estimator).Compile(Request(), Payloads());
        var mismatched = scope switch
        {
            "instance" => assembly with { InstanceId = CharacterInstanceId.Create("ci-instance-beta") },
            "conversation" => assembly with { ConversationId = "room-2" },
            _ => assembly with { BatchId = "batch-2" },
        };

        var error = Assert.Throws<NarrativeContextAssemblyRejectedException>(() => new NarratorCall
        {
            Request = Request(), Assembly = mismatched, Budget = new NarratorCallBudget(),
        }.Validate());

        Assert.Equal(NarrativeCompileReason.CallScopeMismatch, error.Reason);
    }

    [Fact]
    public async Task Round_never_rewrites_the_narration_after_the_single_call()
    {
        var round = Round(Draft(actions: [ImmediateAction()]));

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Equal(1, round.Provider.InvocationCount);
        Assert.Equal(1, result.NarratorCallCount);
        Assert.Equal(NarrativeRoundDisposition.Immediate, result.Disposition);
        Assert.Equal(Prose, result.Draft!.NarrativeProse);
        Assert.Equal(3, result.HistoryEntries.Count);
        Assert.Equal(BoundQuote, string.Concat(result.HistoryEntries.Select(s => s.Text)));
    }

    [Fact]
    public async Task Round_feeds_the_provider_the_compiled_context_and_nothing_else()
    {
        var round = Round(Draft());

        await round.Runner.RunAsync(Request(), Payloads(advisory: "decide"), RoundTimeout, Now, TestContext.Current.CancellationToken);

        var call = round.Provider.LastCall!;
        Assert.Equal(8, call.Assembly.SegmentCount);
        Assert.Equal(NarrativeTriggerKind.UserInput, call.Assembly.Trigger);
        Assert.Equal(Batch, call.Assembly.BatchId);
        Assert.Contains("advisory:block", call.Assembly.EmittedSourceRefs);
        Assert.Equal(1, call.Budget.CallCount);
    }

    // —— C 组：沉默与延迟的分岔（工单 §二.3）——

    [Fact]
    public async Task Silence_round_produces_no_assistant_message_and_no_intent()
    {
        var round = Round(Draft(actions: [SilenceAction()]));

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Equal(NarrativeRoundDisposition.Silence, result.Disposition);
        Assert.Equal(0, result.AssistantMessageCount);
        Assert.Equal(0, result.OutboundActionCount);
        Assert.Equal(1, result.SilentActionCount);
        Assert.Empty(result.RegisteredIntents);
        Assert.Empty(round.Ledger.Snapshot());
    }

    [Fact]
    public async Task Deferred_round_registers_the_intent_without_touching_real_history()
    {
        var round = Round(Draft(actions: [DeferredAction("intent-1")], intents: [PendingIntent()]));

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Equal(NarrativeRoundDisposition.Deferred, result.Disposition);
        Assert.Single(result.RegisteredIntents);
        Assert.Equal("intent-1", result.RegisteredIntents[0].IntentId);
        Assert.Equal(NarrativeIntentStatus.Pending, result.RegisteredIntents[0].Status);
        Assert.Equal(0, result.AssistantMessageCount);
        Assert.Equal(0, result.OutboundActionCount);
        Assert.Equal(1, result.DeferredActionCount);
        Assert.Empty(round.Ledger.Snapshot());
    }

    [Fact]
    public async Task Draft_without_any_action_is_rejected_rather_than_read_as_silence()
    {
        var round = Round(Draft(actions: []));

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Equal(NarrativeRoundDisposition.ContractRejected, result.Disposition);
        Assert.Single(result.Rejections);
        Assert.Equal(NarrativeContractReason.ActionBindingMissing, result.Rejections[0].Reason);
        Assert.Empty(result.Actions);
        Assert.Empty(round.Ledger.Snapshot());
    }

    [Fact]
    public async Task Malformed_response_is_not_masquerading_as_a_chosen_silence()
    {
        var round = RoundWith(FakeMode.Malformed);

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Equal(NarrativeRoundDisposition.ProviderRejected, result.Disposition);
        Assert.Equal(NarratorOutcomeKind.MalformedResponse, result.Outcome!.Kind);
        Assert.Equal(1, result.NarratorCallCount);
        Assert.Equal(0, result.AssistantMessageCount);
        Assert.Empty(result.RegisteredIntents);
        Assert.Empty(round.Ledger.Snapshot());
    }

    [Fact]
    public async Task A_provider_that_ignores_cancellation_still_exits_as_timeout()
    {
        var round = RoundWith(FakeMode.HangsPastTimeout);

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Equal(NarrativeRoundDisposition.ProviderRejected, result.Disposition);
        Assert.Equal(NarratorOutcomeKind.TimedOut, result.Outcome!.Kind);
        Assert.Equal(1, result.NarratorCallCount);
        Assert.Empty(round.Ledger.Snapshot());
    }

    [Fact]
    public async Task A_provider_that_honours_the_timeout_token_exits_as_timeout_too()
    {
        var round = RoundWith(FakeMode.CancelsOnToken);

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Equal(NarratorOutcomeKind.TimedOut, result.Outcome!.Kind);
        Assert.Equal(NarrativeRoundDisposition.ProviderRejected, result.Disposition);
        Assert.Equal(0, result.AssistantMessageCount);
    }

    [Fact]
    public async Task Provider_failure_becomes_a_named_error_shape_and_writes_nothing()
    {
        var round = RoundWith(FakeMode.Throws);

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Equal(NarratorOutcomeKind.ProviderFailed, result.Outcome!.Kind);
        Assert.Contains("InvalidOperationException", result.Outcome!.Detail);
        Assert.Equal(NarrativeRoundDisposition.ProviderRejected, result.Disposition);
        Assert.Empty(round.Ledger.Snapshot());
    }

    [Fact]
    public async Task Host_cancellation_stays_cancellation_and_is_not_converted_into_a_round()
    {
        var round = RoundWith(FakeMode.CancelsOnToken);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            round.Runner.RunAsync(Request(), Payloads(), TimeSpan.FromSeconds(5), Now, cancelled.Token));
        Assert.Equal(1, round.Provider.InvocationCount);
    }

    [Fact]
    public async Task Deferred_intent_that_claims_to_be_fulfilled_without_delivery_is_refused()
    {
        var round = Round(Draft(actions: [DeferredAction("intent-1")],
            intents: [PendingIntent(status: NarrativeIntentStatus.Fulfilled)]));

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Equal(NarrativeRoundDisposition.ContractRejected, result.Disposition);
        Assert.Single(result.Rejections);
        Assert.Equal(NarrativeContractReason.RealityContamination, result.Rejections[0].Reason);
        Assert.Empty(result.RegisteredIntents);
        Assert.Empty(round.Ledger.Snapshot());
    }

    [Fact]
    public async Task Deferred_action_without_a_pointing_intent_is_refused()
    {
        var round = Round(Draft(actions: [DeferredAction("intent-1")]));

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Equal(NarrativeRoundDisposition.ContractRejected, result.Disposition);
        Assert.Single(result.Rejections);
        Assert.Equal(NarrativeContractReason.IntentClaimedAsFact, result.Rejections[0].Reason);
    }

    // —— D 组：原文行动绑定（工单 §二.4；出口照用 NarrativeDraftValidator 与 DeliveryLedger，不复制）——

    [Fact]
    public async Task Immediate_action_binds_to_the_unique_slice_and_lands_three_segments()
    {
        var round = Round(Draft(actions: [ImmediateAction()]));

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Equal(NarrativeRoundDisposition.Immediate, result.Disposition);
        Assert.Equal(3, result.HistoryEntries.Count);
        Assert.Equal(1, result.OutboundActionCount);
        Assert.Equal(new[] { 0, 1, 2 }, result.HistoryEntries.Select(s => s.SegmentIndex));
        Assert.Equal(BoundQuote, string.Concat(result.HistoryEntries.Select(s => s.Text)));
        Assert.All(result.HistoryEntries, s => Assert.Equal(DeliverySegmentState.Pending, s.State));
        Assert.Empty(result.HistoryRejections);
    }

    [Fact]
    public async Task Quote_absent_from_this_narration_rejects_the_action_without_host_invention()
    {
        var round = Round(Draft(actions: [ImmediateAction(quote: "he replied about the studio visit")]));

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Equal(NarrativeRoundDisposition.ContractRejected, result.Disposition);
        Assert.Single(result.Rejections);
        Assert.Equal(NarrativeContractReason.ActionBindingMissing, result.Rejections[0].Reason);
        Assert.Equal(0, result.OutboundActionCount);
        Assert.Empty(round.Ledger.Snapshot());
    }

    [Fact]
    public async Task Ambiguous_quote_is_rejected_instead_of_the_host_choosing_one()
    {
        var prose = "She wrote maybe later, then said maybe later again.";
        var round = Round(Draft(actions: [ImmediateAction(quote: "maybe later")], prose: prose));

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Single(result.Rejections);
        Assert.Equal(NarrativeContractReason.ActionBindingAmbiguous, result.Rejections[0].Reason);
        Assert.Empty(round.Ledger.Snapshot());
    }

    [Fact]
    public async Task Immediate_action_over_the_host_segment_ceiling_is_refused()
    {
        var round = Round(Draft(actions: [ImmediateAction(segments: 6)]));

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Equal(NarrativeRoundDisposition.ContractRejected, result.Disposition);
        Assert.Single(result.Rejections);
        Assert.Equal(NarrativeContractReason.ActionSegmentCountInvalid, result.Rejections[0].Reason);
        Assert.Empty(round.Ledger.Snapshot());
    }

    [Fact]
    public async Task An_over_split_slice_is_refused_by_the_existing_history_gate()
    {
        var round = Round(Draft(actions: [ImmediateAction(quote: "him", segments: 4)]));

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Equal(NarrativeRoundDisposition.HistoryRejected, result.Disposition);
        Assert.Single(result.HistoryRejections);
        Assert.Equal(NarrativeContractReason.SegmentStickerPayloadInvalid, result.HistoryRejections[0].Reason);
        Assert.Equal(3, result.HistoryEntries.Count);
        Assert.Equal(3, round.Ledger.Snapshot().Count);
    }

    [Fact]
    public async Task Immediate_action_carrying_a_future_intent_is_refused()
    {
        var action = ImmediateAction() with { DeferredIntentId = "intent-9" };
        var round = Round(Draft(actions: [action]));

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Single(result.Rejections);
        Assert.Equal(NarrativeContractReason.IntentClaimedAsFact, result.Rejections[0].Reason);
        Assert.Empty(round.Ledger.Snapshot());
    }

    [Fact]
    public async Task Action_targeting_another_conversation_is_refused()
    {
        var round = Round(Draft(actions: [ImmediateAction(target: "room-2")]));

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Single(result.Rejections);
        Assert.Equal(NarrativeContractReason.ActionTargetMismatch, result.Rejections[0].Reason);
    }

    [Fact]
    public async Task Perceived_reference_outside_the_batch_is_refused()
    {
        var draft = Draft(actions: [SilenceAction()],
            perceived: new PerceivedInputRefs { Refs = [new InputRef(999, "msg-999", Conversation)] });
        var round = Round(draft);

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Single(result.Rejections);
        Assert.Equal(NarrativeContractReason.PerceivedInputUnknown, result.Rejections[0].Reason);
    }

    [Fact]
    public async Task Watermark_beyond_the_batch_upper_bound_is_refused()
    {
        var round = Round(Draft(actions: [SilenceAction()], processedThrough: 500));

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Single(result.Rejections);
        Assert.Equal(NarrativeContractReason.ProcessedBeyondBatch, result.Rejections[0].Reason);
    }

    [Fact]
    public async Task Round_refuses_a_narration_without_prose_instead_of_binding_to_nothing()
    {
        var round = Round(Draft(actions: [ImmediateAction()], prose: "   "));

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Rejections.Count);
        Assert.Equal(NarrativeContractReason.NarrativeProseMissing, result.Rejections[0].Reason);
        Assert.Equal(NarrativeContractReason.ActionBindingMissing, result.Rejections[1].Reason);
        Assert.Empty(round.Ledger.Snapshot());
    }

    // —— E 组：绑定回指与快照连续性 ——

    [Fact]
    public async Task Bound_action_keeps_a_reference_back_to_the_input_it_answers()
    {
        var round = Round(Draft(actions: [ImmediateAction()]));

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Equal(NarrativeRoundDisposition.Immediate, result.Disposition);
        Assert.Equal(new[] { 101L }, result.Draft!.PerceivedInputRefs.Seqs.ToArray());
        var action = result.Actions.Single(a => a.Mode == CommunicationMode.Immediate);
        Assert.Equal(Prose.IndexOf(BoundQuote, StringComparison.Ordinal), action.Binding!.ResolvedOffset);
        Assert.Equal(BoundQuote, action.Binding!.QuotedText);
        Assert.Contains("input:101", result.Assembly!.EmittedSourceRefs);
    }

    [Fact]
    public async Task Draft_on_a_stale_story_version_is_refused_before_any_history()
    {
        var round = Round(Draft(actions: [ImmediateAction()], storyVersionBase: 6));

        var result = await round.Runner.RunAsync(Request(storyVersion: 7), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Equal(NarrativeRoundDisposition.ContractRejected, result.Disposition);
        Assert.Single(result.Rejections);
        Assert.Equal(NarrativeContractReason.StoryVersionConflict, result.Rejections[0].Reason);
        Assert.Equal(1, result.NarratorCallCount);
        Assert.Empty(round.Ledger.Snapshot());
    }

    [Fact]
    public async Task Draft_from_a_fenced_writer_epoch_is_refused()
    {
        var round = Round(Draft(actions: [ImmediateAction()], ownershipEpoch: 1));

        var result = await round.Runner.RunAsync(Request(ownershipEpoch: 2), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Single(result.Rejections);
        Assert.Equal(NarrativeContractReason.OwnershipFenced, result.Rejections[0].Reason);
        Assert.Empty(round.Ledger.Snapshot());
    }

    [Fact]
    public async Task Draft_with_a_wrong_issue_baseline_is_refused_as_late()
    {
        var round = Round(Draft(actions: [ImmediateAction()], receivedBaseline: 90));

        var result = await round.Runner.RunAsync(Request(), Payloads(), RoundTimeout, Now, TestContext.Current.CancellationToken);

        Assert.Single(result.Rejections);
        Assert.Equal(NarrativeContractReason.LateCommitRejected, result.Rejections[0].Reason);
    }

    [Fact]
    public void Contiguous_split_reproduces_the_bound_slice_verbatim()
    {
        var parts = NarrativeRoundRunner.SplitContiguous("abcd", 3);

        Assert.Equal(new[] { "ab", "c", "d" }, parts);
        Assert.Equal("abcd", string.Concat(parts));
        Assert.Equal(new[] { "abcd" }, NarrativeRoundRunner.SplitContiguous("abcd", 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => NarrativeRoundRunner.SplitContiguous("abcd", 0));
    }
}

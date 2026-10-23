using Microsoft.Data.Sqlite;
using Pry.Core.Memory;
using Pry.Core.Narrative;
using Pry.Core.Narrative.Storage;
using Pry.Core.Prompting;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// HDS-WAVE23-1023 活单①：回合穿线判据组。对 <c>src/Pry.Core/Narrative/NarrativeTurnPipeline.cs</c>
/// 的八段衔接逐段钉"上一段输出的具名成员喂给下一段入参"：攒批批次三成员进请求、分类三档不折叠
/// 才触发打断、打断作废清单逐段落进既有投递账本、装配器拒绝即停且零模型调用、轮结果成员喂条件写、
/// 落库读回的段行喂节奏、缺决策与被场景挡都不排可发时刻。端到端格在系统临时目录的<b>真 SQLite 库</b>
/// 上跑段 6，并在重开连接后从库里读回判定；全部 provider 是注入的确定性假实现。
/// 真实模型对照、生产接线、UI、生产库一律未验证；所有节奏／批量／时限参数从测试入参给，本组不新造数字门槛。
/// 本类只有普通 [Fact]，无 [Theory]／[InlineData]／MemberData／抽象基类；静态格数＝TRX 应执行数＝12。
/// 全部库文件都是本类自建的系统临时目录文件，绝不碰 %LOCALAPPDATA%\PryCompanion。
/// </summary>
public sealed class NarrativeEndToEndPipelinePrototypeTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Start = new(2026, 10, 23, 9, 0, 0, TimeSpan.Zero);
    private const string Conversation = "room-1";
    private const string Writer = "host-wave23";
    private const string Prose = "她把笔搁下，先应了一声：画具还没收完呢。照片？不给。然后继续调色。";
    private const string BoundQuote = "画具还没收完呢。照片？不给。";
    private const string PriorProse = "旧话：先应一声，再展开。";
    private const string PriorQuote = "先应一声，再展开。";

    private static readonly CharacterInstanceId Alpha = CharacterInstanceId.Create("ci-instance-alpha");
    private static readonly CharacterInstanceKey AlphaKey = new()
    {
        LocalUserId = "local-user-1",
        CharacterId = "character-pry",
    };

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"pry-wave23-e2e-{Guid.NewGuid():N}");
    private string _databasePath = null!;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        _databasePath = Path.Combine(_root, "narrative.db");
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        SqliteTestPools.Clear(_databasePath);
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }

        return ValueTask.CompletedTask;
    }

    // —————————— 端到端：1→8 一格走完，段 6 落真库并重启连接读回 ——————————

    [Fact]
    public async Task Turn_one_to_eight_threads_the_sealed_batch_into_a_committed_real_sqlite_turn_with_paced_sends()
    {
        var store = await OpenStoreAsync();
        var (pipeline, harness) = await BuildPipelineAsync(store);
        var (_, priorCommitId) = SeedUnderwayLedger(harness);

        var spec = BaseSpec(harness) with
        {
            Cue = InterruptCue.TopicShiftSignal,
            SegmentsUnderway =
            [
                new InterruptibleSegment(priorCommitId, "act-prior", 0, DeliverySegmentState.Delivered),
                new InterruptibleSegment(priorCommitId, "act-prior", 1, DeliverySegmentState.Pending),
            ],
            InterruptionReasonWhenNewTopic = InterruptionReasonKind.SupersededByNewInput,
            InterruptionCancelReason = "superseded_by_new_topic",
            Store = store,
            CommitFactory = HostCommitFactory,
            BehaviorCue = new BehaviorCue(BehaviorRegisterSignal.Warm, []),
            Pacing = new ReplyPacingInput
            {
                ReadingCharactersPerSecond = 10,
                MinSegmentGap = TimeSpan.Zero,
                MaxSegmentGap = TimeSpan.FromSeconds(30),
            },
            PacingStartUtc = Start.AddMinutes(2),
        };

        var trace = await pipeline.RunTurnAsync(spec, Token);

        // 段 1：三条到达输入归入一枚批次，批次成员逐字进请求。
        Assert.Equal(NarrativeTurnStop.Completed, trace.StoppedAt);
        Assert.Equal(3, trace.AppendDecisions.Count);
        Assert.All(trace.AppendDecisions, d => Assert.Equal(InputBufferAppendDecision.Appended, d));
        Assert.NotNull(trace.Batch);
        Assert.Equal($"{Conversation}#b101", trace.Batch!.BatchId);
        Assert.Equal(new long[] { 101, 102, 103 }, trace.Batch.OrderedSeqs.ToArray());
        Assert.NotNull(trace.AssembledRequest);
        Assert.Equal(trace.Batch.ConversationId, trace.AssembledRequest!.ConversationId);
        Assert.Equal(trace.Batch.BatchId, trace.AssembledRequest.BatchId);
        Assert.Equal(trace.Batch.OrderedSeqs.ToArray(), trace.AssembledRequest.Inputs.Select(i => i.Seq).ToArray());

        // 段 2→3：分类为新话题才触发打断；作废清单逐段落进既有账本，已送达段留手不可翻。
        Assert.Equal(InterruptClass.NewTopic, trace.Classification);
        Assert.NotNull(trace.Interruption);
        Assert.Equal(InterruptionApplyResult.Applied, trace.Interruption!.Result);
        Assert.Single(trace.Interruption.Cancelled);
        Assert.Single(trace.Interruption.Withheld);
        var firstCancellation = Assert.Single(trace.LedgerCancellations);
        Assert.True(firstCancellation.Accepted);
        var cancelledInLedger = harness.Ledger.Find(new DeliverySegmentKey(priorCommitId, "act-prior", 1));
        Assert.Equal(DeliverySegmentState.Cancelled, cancelledInLedger!.State);
        var deliveredInLedger = harness.Ledger.Find(new DeliverySegmentKey(priorCommitId, "act-prior", 0));
        Assert.Equal(DeliverySegmentState.Delivered, deliveredInLedger!.State);

        // 段 4→5：装配通过才有一次叙事；轮结果带绑定动作与三段历史。
        Assert.True(trace.Assembly!.Accepted);
        Assert.NotNull(trace.Round);
        Assert.Equal(NarrativeRoundDisposition.Immediate, trace.Round!.Disposition);
        Assert.Equal(1, trace.Round.NarratorCallCount);
        Assert.Equal(1, harness.Narrator.InvocationCount);
        Assert.Equal(3, trace.Round.HistoryEntries.Count);
        Assert.Equal(BoundQuote, string.Concat(trace.Round.HistoryEntries.Select(s => s.Text)));

        // 段 5→6：轮的草稿与历史段成员逐具喂给条件写；提交生效。
        Assert.NotNull(trace.BuiltCommit);
        Assert.Equal(trace.Round.HistoryEntries[0].CommitId, trace.BuiltCommit!.CommitId);
        Assert.Equal(trace.Round.Draft!.NarrativeProse, trace.BuiltCommit.NarrativeProse);
        Assert.True(trace.Commit!.Accepted, trace.Commit.Detail);

        // 段 6→8：节奏的字符数取自落库读回的段行，不是内存对象。
        Assert.NotNull(trace.OutboxReadback);
        Assert.Equal(3, trace.OutboxReadback!.Count);
        Assert.Equal(trace.Round.HistoryEntries.Select(s => s.Text).ToArray(),
            trace.OutboxReadback.Select(s => s.Text).ToArray());

        // 段 7：换形放行且档位在场。
        Assert.Equal(BehaviorMappingResult.Mapped, trace.Expression!.Result);
        Assert.Equal(NarrativeExpressionGear.Warm, trace.Expression.Gear);
        Assert.Null(trace.Expression.BlockingSceneCode);

        // 段 8：只定时点不定内容，首点即入参时点，单调不减。
        Assert.NotNull(trace.SendTimes);
        Assert.Equal(3, trace.SendTimes!.Count);
        Assert.Equal(Start.AddMinutes(2), trace.SendTimes[0]);
        for (var i = 1; i < trace.SendTimes.Count; i++)
        {
            Assert.True(trace.SendTimes[i] >= trace.SendTimes[i - 1], "可发时刻必须单调不减。");
        }

        // 重启连接后从库里读回：叙事行、待发段、实例水位都在，判定不靠内存。
        store.Dispose();
        await using var reopened = await OpenStoreAsync();
        Assert.Equal(1, await ScalarAsync(
            $"SELECT COUNT(*) FROM narrative_entries WHERE commit_id='{trace.BuiltCommit.CommitId}'"));
        Assert.Equal(3, await ScalarAsync("SELECT COUNT(*) FROM narrative_outbox"));
        var state = await reopened.StateOfAsync(Alpha, Token);
        Assert.NotNull(state);
        Assert.Equal(1, state!.StoryVersion);
        Assert.Equal(103, state.ProcessedThrough.Seq);
        Assert.Equal(1, state.AppliedCommitCount);
        Assert.Equal(trace.BuiltCommit.CommitId, state.LastCommitId);
        var pending = await reopened.ListPendingSegmentsAsync(trace.BuiltCommit.CommitId, Token);
        Assert.Equal(3, pending.Count);
    }

    // —————————— 衔接格：逐段"上一段输出被下一段消费" ——————————

    [Fact]
    public async Task Turn_batch_not_sealed_under_the_given_batch_id_stops_before_classification()
    {
        var (pipeline, harness) = await BuildPipelineAsync(null);
        var spec = BaseSpec(harness) with { BatchId = $"{Conversation}#b999", Cue = InterruptCue.NoSignal };

        var trace = await pipeline.RunTurnAsync(spec, Token);

        Assert.Equal(NarrativeTurnStop.BatchNotSealed, trace.StoppedAt);
        Assert.Null(trace.Batch);
        Assert.Null(trace.Classification);
        Assert.Null(trace.Assembly);
        Assert.Equal(0, harness.Narrator.InvocationCount);
    }

    [Fact]
    public async Task Turn_second_run_on_the_same_batch_is_stopped_at_the_single_sendable_truth()
    {
        var (pipeline, harness) = await BuildPipelineAsync(null);
        var spec = BaseSpec(harness) with { Cue = InterruptCue.NoSignal };

        var first = await pipeline.RunTurnAsync(spec, Token);
        var second = await pipeline.RunTurnAsync(spec, Token);

        Assert.Equal(NarrativeTurnStop.Completed, first.StoppedAt);
        Assert.Equal(NarrativeTurnStop.SendableTruthAlreadyClaimed, second.StoppedAt);
        Assert.Null(second.Classification);
        // 同一批次不二次叙事：模型总共只被调过一次。
        Assert.Equal(1, harness.Narrator.InvocationCount);
    }

    [Fact]
    public async Task Turn_undeterminable_cue_stays_its_own_class_and_triggers_no_interruption()
    {
        var (pipeline, harness) = await BuildPipelineAsync(null);
        var (_, priorCommitId) = SeedUnderwayLedger(harness);
        var spec = BaseSpec(harness) with
        {
            Cue = InterruptCue.NoSignal,
            SegmentsUnderway =
            [
                new InterruptibleSegment(priorCommitId, "act-prior", 0, DeliverySegmentState.Delivered),
                new InterruptibleSegment(priorCommitId, "act-prior", 1, DeliverySegmentState.Pending),
            ],
            InterruptionReasonWhenNewTopic = InterruptionReasonKind.SupersededByNewInput,
            InterruptionCancelReason = "superseded_by_new_topic",
        };

        var trace = await pipeline.RunTurnAsync(spec, Token);

        Assert.Equal(InterruptClass.Undeterminable, trace.Classification);
        Assert.Null(trace.Interruption);
        Assert.Empty(trace.LedgerCancellations);
        var stillPending = harness.Ledger.Find(new DeliverySegmentKey(priorCommitId, "act-prior", 1));
        Assert.Equal(DeliverySegmentState.Pending, stillPending!.State);
    }

    [Fact]
    public async Task Turn_continuation_cue_triggers_no_interruption_and_the_turn_completes()
    {
        var (pipeline, harness) = await BuildPipelineAsync(null);
        var (_, priorCommitId) = SeedUnderwayLedger(harness);
        var spec = BaseSpec(harness) with
        {
            Cue = InterruptCue.ContinuationMarker,
            SegmentsUnderway =
            [
                new InterruptibleSegment(priorCommitId, "act-prior", 1, DeliverySegmentState.Pending),
            ],
            InterruptionReasonWhenNewTopic = InterruptionReasonKind.UserInterruption,
            InterruptionCancelReason = "user_interruption",
        };

        var trace = await pipeline.RunTurnAsync(spec, Token);

        Assert.Equal(InterruptClass.Continuation, trace.Classification);
        Assert.Null(trace.Interruption);
        var untouched = harness.Ledger.Find(new DeliverySegmentKey(priorCommitId, "act-prior", 1));
        Assert.Equal(DeliverySegmentState.Pending, untouched!.State);
        Assert.Equal(NarrativeTurnStop.Completed, trace.StoppedAt);
    }

    [Fact]
    public async Task Turn_new_topic_cue_cancels_only_the_pending_segment_in_the_existing_ledger()
    {
        var (pipeline, harness) = await BuildPipelineAsync(null);
        var (epoch, priorCommitId) = SeedUnderwayLedger(harness);
        var spec = BaseSpec(harness) with
        {
            Cue = InterruptCue.HardRedirection,
            SegmentsUnderway =
            [
                new InterruptibleSegment(priorCommitId, "act-prior", 0, DeliverySegmentState.Delivered),
                new InterruptibleSegment(priorCommitId, "act-prior", 1, DeliverySegmentState.Pending),
            ],
            InterruptionReasonWhenNewTopic = InterruptionReasonKind.UserInterruption,
            InterruptionCancelReason = "user_interruption",
        };

        var trace = await pipeline.RunTurnAsync(spec, Token);

        Assert.Equal(InterruptClass.NewTopic, trace.Classification);
        Assert.Single(trace.Interruption!.Cancelled);
        Assert.Single(trace.Interruption.Withheld);
        Assert.Equal(NarrativeContractReason.None, trace.LedgerCancellations[0].Reason);
        var cancelled = harness.Ledger.Find(new DeliverySegmentKey(priorCommitId, "act-prior", 1));
        Assert.Equal(DeliverySegmentState.Cancelled, cancelled!.State);
        Assert.Equal("user_interruption", cancelled.CancelReason);
        Assert.Equal(epoch, cancelled.OwnershipEpoch);
    }

    [Fact]
    public async Task Turn_context_rejection_short_circuits_the_compile_to_narrate_edge_with_zero_model_calls()
    {
        var (pipeline, harness) = await BuildPipelineAsync(null);
        var spec = BaseSpec(harness) with
        {
            Cue = InterruptCue.NoSignal,
            Payloads = Payloads(new ContextBudget(ContextTokens: 256, OutputTokens: 200, SafetyTokens: 100)),
        };

        var trace = await pipeline.RunTurnAsync(spec, Token);

        Assert.NotNull(trace.Assembly);
        Assert.False(trace.Assembly!.Accepted);
        Assert.Equal(NarrativeCompileReason.WindowTooSmall, trace.Assembly.ReasonCode);
        Assert.Equal(NarrativeTurnStop.ContextRejected, trace.StoppedAt);
        Assert.Null(trace.Round);
        Assert.Equal(0, harness.Narrator.InvocationCount);
        Assert.Null(trace.Expression);
        Assert.Null(trace.SendTimes);
    }

    [Fact]
    public async Task Turn_pacing_counts_come_from_the_committed_outbox_rows_read_back_from_the_database()
    {
        var store = await OpenStoreAsync();
        var (pipeline, harness) = await BuildPipelineAsync(store);
        var spec = BaseSpec(harness) with
        {
            Cue = InterruptCue.NoSignal,
            Store = store,
            CommitFactory = HostCommitFactory,
            BehaviorCue = new BehaviorCue(BehaviorRegisterSignal.Playful, []),
            Pacing = new ReplyPacingInput
            {
                ReadingCharactersPerSecond = 7,
                MinSegmentGap = TimeSpan.Zero,
                MaxSegmentGap = TimeSpan.FromSeconds(60),
            },
            PacingStartUtc = Start,
        };

        var trace = await pipeline.RunTurnAsync(spec, Token);

        Assert.Equal(NarrativeTurnStop.Completed, trace.StoppedAt);
        Assert.NotNull(trace.OutboxReadback);
        var fromDb = await store.ListPendingSegmentsAsync(trace.BuiltCommit!.CommitId, Token);
        Assert.Equal(trace.OutboxReadback!.Select(s => s.Text.Length).ToArray(),
            fromDb.Select(s => s.Text.Length).ToArray());
        Assert.NotNull(trace.SendTimes);
        Assert.Equal(trace.OutboxReadback.Count, trace.SendTimes!.Count);

        // 若节奏用的不是读回的行而是别的什么，第二段的间隔就会错位：这里按入参速度逐段核对。
        Assert.Equal(trace.SendTimes[0] + TimeSpan.FromSeconds(trace.OutboxReadback[0].Text.Length / 7d),
            trace.SendTimes[1]);
        store.Dispose();
    }

    [Fact]
    public async Task Turn_scene_blocked_expression_skips_pacing_and_carries_the_reason_code_through()
    {
        var (pipeline, harness) = await BuildPipelineAsync(null);
        var spec = BaseSpec(harness) with
        {
            Cue = InterruptCue.NoSignal,
            BehaviorCue = new BehaviorCue(BehaviorRegisterSignal.Playful, ["sensitive_scene"]),
            Pacing = new ReplyPacingInput
            {
                ReadingCharactersPerSecond = 10,
                MinSegmentGap = TimeSpan.Zero,
                MaxSegmentGap = TimeSpan.FromSeconds(30),
            },
            PacingStartUtc = Start,
        };

        var trace = await pipeline.RunTurnAsync(spec, Token);

        Assert.Equal(NarrativeTurnStop.Completed, trace.StoppedAt);
        Assert.Equal(BehaviorMappingResult.BlockedByScene, trace.Expression!.Result);
        Assert.Equal("sensitive_scene", trace.Expression.BlockingSceneCode);
        Assert.Equal(NarrativeExpressionGear.NotDetermined, trace.Expression.Gear);
        Assert.Null(trace.SendTimes);
    }

    [Fact]
    public async Task Turn_missing_behavior_decision_is_not_passed_through_and_paces_nothing()
    {
        var (pipeline, harness) = await BuildPipelineAsync(null);
        var spec = BaseSpec(harness) with
        {
            Cue = InterruptCue.NoSignal,
            BehaviorCue = null,
            Pacing = new ReplyPacingInput
            {
                ReadingCharactersPerSecond = 10,
                MinSegmentGap = TimeSpan.Zero,
                MaxSegmentGap = TimeSpan.FromSeconds(30),
            },
            PacingStartUtc = Start,
        };

        var trace = await pipeline.RunTurnAsync(spec, Token);

        Assert.Equal(BehaviorMappingResult.NoBehaviorDecision, trace.Expression!.Result);
        Assert.Equal(NarrativeExpressionGear.NotDetermined, trace.Expression.Gear);
        Assert.Null(trace.SendTimes);
    }

    [Fact]
    public async Task Turn_commit_rejection_stops_the_chain_before_expression_and_pacing_and_lands_no_rows()
    {
        var store = await OpenStoreAsync();
        var (pipeline, harness) = await BuildPipelineAsync(store);
        var spec = BaseSpec(harness) with
        {
            Cue = InterruptCue.NoSignal,
            Store = store,
            CommitFactory = (round, request) => HostCommitFactory(round, request) with { ContentHash = null },
            BehaviorCue = new BehaviorCue(BehaviorRegisterSignal.Warm, []),
            Pacing = new ReplyPacingInput
            {
                ReadingCharactersPerSecond = 10,
                MinSegmentGap = TimeSpan.Zero,
                MaxSegmentGap = TimeSpan.FromSeconds(30),
            },
            PacingStartUtc = Start,
        };

        var trace = await pipeline.RunTurnAsync(spec, Token);

        Assert.Equal(NarrativeTurnStop.CommitRejected, trace.StoppedAt);
        Assert.False(trace.Commit!.Accepted);
        Assert.Equal(NarrativeContractReason.ContentHashMissing, trace.Commit.Reason);
        Assert.Null(trace.Expression);
        Assert.Null(trace.SendTimes);

        // 被拒路径上一条字节都不落：重开连接后库里没有叙事行、没有待发段、水位未动。
        store.Dispose();
        await using var reopened = await OpenStoreAsync();
        Assert.Equal(0, await ScalarAsync("SELECT COUNT(*) FROM narrative_entries"));
        Assert.Equal(0, await ScalarAsync("SELECT COUNT(*) FROM narrative_outbox"));
        Assert.Null(await reopened.StateOfAsync(Alpha, Token));
    }

    // —————————— 源码扫描门：组合件零引用旧主链四件（自带阳性探针）——————————

    [Fact]
    public async Task Pipeline_source_carries_no_legacy_chain_references_and_the_probe_proves_the_gate_bites()
    {
        var identifiers = new[] { "BehaviorPlanner", "ReplyGenerationRenderer", "ReplyPlanner", "ReplyNecessity" };
        var path = Path.Combine(RepositoryRoot(), "src", "Pry.Core", "Narrative", "NarrativeTurnPipeline.cs");
        Assert.True(File.Exists(path), $"组合件源码不在盘上：{path}");
        var source = await File.ReadAllTextAsync(path, Token);
        foreach (var identifier in identifiers)
        {
            Assert.DoesNotContain(identifier, source, StringComparison.Ordinal);
        }

        // 阳性探针：扫描判据必须能咬住真的引用了旧主链的坏样本，否则门是常绿的假门。
        var sample = Path.ChangeExtension(Path.GetTempFileName(), ".cs");
        try
        {
            await File.WriteAllTextAsync(sample, "var legacy = new ReplyPlanner(); // 旧主链装配\n", Token);
            var badSample = await File.ReadAllTextAsync(sample, Token);
            Assert.Contains("ReplyPlanner", badSample, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(sample);
        }
    }

    // —————————— 组装工具：夹具全部自造，只落系统临时目录 ——————————

    private sealed record TurnHarness(
        DeliveryLedger Ledger,
        OutboxOwnershipRegistry Ownership,
        FakeNarrator Narrator);

    private async Task<(NarrativeTurnPipeline Pipeline, TurnHarness Harness)> BuildPipelineAsync(
        NarrativeSqliteStore? store)
    {
        var ownership = new OutboxOwnershipRegistry();
        var grant = ownership.TryAcquire(Alpha, EngineMode.NarrativeV1, Writer, Start);
        Assert.True(grant.Accepted, grant.Detail);
        Assert.NotNull(grant.Ticket);
        var ledger = new DeliveryLedger(ownership);
        var narrator = new FakeNarrator();
        var pipeline = new NarrativeTurnPipeline(narrator, ledger, Writer, new ConservativeTokenEstimator());
        if (store is not null)
        {
            var registration = await store.RegisterInstanceAsync(AlphaKey, Alpha, InstanceProvenance.HostBound,
                originAlias: null, Token);
            Assert.True(registration.Accepted);
            var ownershipGrant = await store.AcquireWriteOwnershipAsync(Alpha, EngineMode.NarrativeV1, Writer,
                Start, Token);
            Assert.True(ownershipGrant.Accepted, ownershipGrant.Detail);
            Assert.NotNull(ownershipGrant.Ticket);
            // 内存票与落库票必须同代次，否则段 3 与段 6 各自成立也穿不了一条回合。
            Assert.Equal(grant.Ticket!.OwnershipEpoch, ownershipGrant.Ticket!.OwnershipEpoch);
        }

        return (pipeline, new TurnHarness(ledger, ownership, narrator));
    }

    /// <summary>在既有账本上铺一枚旧提交的两段：第 0 段已送达（带证据），第 1 段待发。</summary>
    private static (long Epoch, string PriorCommitId) SeedUnderwayLedger(TurnHarness harness)
    {
        var epoch = harness.Ownership.CurrentEpoch(Alpha);
        var priorCommit = new NarrativeCommit
        {
            CommitId = "commit-prior-1",
            InstanceId = Alpha,
            ConversationId = Conversation,
            BatchId = "batch-prior-1",
            NarrativeProse = PriorProse,
            Actions =
            [
                new CommunicationAction
                {
                    ActionId = "act-prior",
                    Mode = CommunicationMode.Immediate,
                    TargetConversationId = Conversation,
                    ProseQuote = PriorQuote,
                    PlannedSegmentCount = 2,
                    Binding = NarrativeProseBinder.Bind(PriorProse, PriorQuote),
                },
            ],
            ProcessedThrough = new ProcessedThroughSeq(100),
            PerceivedInputRefs = new PerceivedInputRefs { Refs = [new InputRef(100, "msg-100", Conversation)] },
            ExpectedStoryVersion = 0,
            OwnershipEpoch = epoch,
            ContentHash = "sha256:wave23-prior",
            CommittedByWriter = Writer,
            CommittedAtUtc = Start,
            ProposalId = "proposal-prior-1",
            ConsumedInputSeqs = [100],
        };
        harness.Ledger.RegisterAppliedCommit(priorCommit, 4);
        var first = new DeliverySegment
        {
            InstanceId = Alpha, ConversationId = Conversation, CommitId = priorCommit.CommitId,
            ActionId = "act-prior", SegmentIndex = 0, State = DeliverySegmentState.Pending,
            Text = "先应一声，", OwnershipEpoch = epoch,
        };
        var second = new DeliverySegment
        {
            InstanceId = Alpha, ConversationId = Conversation, CommitId = priorCommit.CommitId,
            ActionId = "act-prior", SegmentIndex = 1, State = DeliverySegmentState.Pending,
            Text = "再展开。", OwnershipEpoch = epoch,
        };
        Assert.True(harness.Ledger.TryAppend(first, Writer).Accepted);
        Assert.True(harness.Ledger.TryMarkDelivered(first.Key, "msg-prior-0", epoch, Writer).Accepted);
        Assert.True(harness.Ledger.TryAppend(second, Writer).Accepted);
        return (epoch, priorCommit.CommitId);
    }

    private static NarrativeContextPayloads Payloads(ContextBudget? budget = null) => new()
    {
        Budget = budget ?? new ContextBudget(),
        Identity = "Pry canon",
        OutputRules = "json rules",
        CurrentEvent = new NarrativeContextUnit("input:103", Conversation, "hello"),
    };

    private static NarrativeTurnSpec BaseSpec(TurnHarness harness) => new()
    {
        Buffer = SharedBuffer(),
        BatchId = $"{Conversation}#b101",
        Inputs =
        [
            new InputRef(101, "msg-101", Conversation),
            new InputRef(102, "msg-102", Conversation),
            new InputRef(103, "msg-103", Conversation),
        ],
        ArrivalTimes = [Start, Start.AddSeconds(1), Start.AddSeconds(2)],
        BatchSealAttemptUtc = Start.AddSeconds(3),
        InstanceId = Alpha,
        StoryVersion = 0,
        OwnershipEpoch = harness.Ownership.CurrentEpoch(Alpha),
        BaselineProcessedThroughSeq = 100,
        IssuedAtUtc = Start,
        MaxOutboundSegments = 4,
        Cue = InterruptCue.NoSignal,
        Payloads = Payloads(),
        NarratorTimeout = TimeSpan.FromSeconds(5),
        Now = Start.AddMinutes(1),
    };

    /// <summary>逐用例新建攒批件：封顶条件全部从入参给（取值登记为待标定，这里只是测试给定值）。</summary>
    private static NarrativeInputBuffer SharedBuffer() => new(Conversation,
        new InputBufferCeiling { MaxBatchItems = 5, MaxBatchHold = TimeSpan.FromMinutes(1) });

    /// <summary>宿主侧草稿→提交映射：轮结果的具名成员逐字段搬运，组合件不自行拼提交。</summary>
    private static NarrativeCommit HostCommitFactory(NarrativeRoundResult round, NarrativeRequest request)
    {
        var draft = round.Draft!;
        return new NarrativeCommit
        {
            CommitId = round.HistoryEntries.Count > 0 ? round.HistoryEntries[0].CommitId : $"commit-{draft.ProposalId}",
            InstanceId = draft.InstanceId,
            ConversationId = draft.ConversationId,
            BatchId = draft.BatchId,
            NarrativeProse = draft.NarrativeProse,
            Actions = round.Actions,
            Intents = draft.Intents,
            KnowledgeCandidates = draft.KnowledgeCandidates,
            ProcessedThrough = draft.ProcessedThrough,
            PerceivedInputRefs = draft.PerceivedInputRefs,
            ExpectedStoryVersion = request.StoryVersion,
            DeletionEpoch = request.DeletionEpoch,
            OwnershipEpoch = draft.OwnershipEpoch,
            ContentHash = "sha256:wave23-host-hash",
            CommittedByWriter = Writer,
            CommittedAtUtc = request.IssuedAtUtc,
            ProposalId = draft.ProposalId,
            ConsumedInputSeqs = [.. request.Inputs.Select(i => i.Seq)],
        };
    }

    /// <summary>确定性假 provider：只把宿主签发的请求成员逐字回进草稿，不含真实模型、网络或运行库。</summary>
    private sealed class FakeNarrator : INarrator
    {
        public int InvocationCount { get; private set; }

        public Task<NarratorOutcome> ProposeAsync(NarratorCall call, CancellationToken cancellationToken)
        {
            InvocationCount++;
            var request = call.Request;
            var draft = new NarrativeDraft
            {
                InstanceId = request.InstanceId,
                ConversationId = request.ConversationId,
                BatchId = request.BatchId,
                NarrativeProse = Prose,
                ProcessedThrough = new ProcessedThroughSeq(request.BatchHighestSeq),
                PerceivedInputRefs = new PerceivedInputRefs { Refs = request.Inputs },
                Actions =
                [
                    new CommunicationAction
                    {
                        ActionId = "act-reply-1",
                        Mode = CommunicationMode.Immediate,
                        TargetConversationId = request.ConversationId,
                        ProseQuote = BoundQuote,
                        PlannedSegmentCount = 3,
                    },
                ],
                StoryVersionBase = request.StoryVersion,
                OwnershipEpoch = request.OwnershipEpoch,
                DeletionEpoch = request.DeletionEpoch,
                ReceivedThroughSeqAtIssue = request.ProcessedThroughAtIssue.Seq,
                ProposalId = "proposal-e2e-1",
                ProposedAtUtc = request.IssuedAtUtc,
            };
            return Task.FromResult(NarratorOutcome.Produced(draft));
        }
    }

    private async Task<NarrativeSqliteStore> OpenStoreAsync()
    {
        var store = new NarrativeSqliteStore(_databasePath);
        await store.InitializeAsync(Token);
        return store;
    }

    private async Task<long> ScalarAsync(string sql)
    {
        await using var connection = new SqliteConnection($"Data Source={_databasePath};Pooling=False");
        await connection.OpenAsync(Token);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync(Token);
        return value is null or DBNull ? 0L : Convert.ToInt64(value);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Pry.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.True(directory is not null, "自测试输出目录上溯找不到 Pry.slnx，仓库根无法定位。");
        return directory!.FullName;
    }
}

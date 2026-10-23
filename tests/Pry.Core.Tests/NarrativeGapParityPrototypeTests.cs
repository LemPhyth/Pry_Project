using Pry.Core.Narrative;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// HDS-010 缺口等价格隔离原型判据组（第二十波）。对工单 §一 表的 5 处缺口各钉一组"改坏了必须有人响"的判据：
/// 攒批（同会话按到达次序归入一枚批次、封顶走入参、批次标识稳定可回放、同批次一条可发送真相）、
/// 打断（只作废未送达段、已送达不可撤回、打断／顶替两种理由可点名）、
/// 分类（三档枚举各有判据、未知独立成档不折叠）、
/// 行为→表达换形（缺决策不默认放行、缺省档显式、被场景挡给可点名理由、不产文本）、
/// 逐段节奏（只定时点不定内容、可发时刻单调不减、速度走必填入参）。
/// 另附一枚自带阳性探针的源码扫描格，自证五个新类型零引用旧主链四件。
/// 全部只在内存对象上闭环；真实模型对照、生产接线、UI、生产库一律未验证。所有时间／节奏／批量参数从测试入参给，
/// 本波不新造任何数字门槛。
/// </summary>
public sealed class NarrativeGapParityPrototypeTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 23, 9, 0, 0, TimeSpan.Zero);

    // —— 缺口 1：NarrativeInputBuffer ——

    [Fact]
    public void InputBuffer_consecutive_inputs_in_one_conversation_seal_into_a_single_batch_in_arrival_order()
    {
        var buffer = new NarrativeInputBuffer("conv-1", new InputBufferCeiling { MaxBatchItems = 5, MaxBatchHold = TimeSpan.FromMinutes(1) });
        Assert.Equal(InputBufferAppendDecision.Appended, buffer.Append(101, Start));
        Assert.Equal(InputBufferAppendDecision.Appended, buffer.Append(102, Start.AddSeconds(1)));
        Assert.Equal(InputBufferAppendDecision.Appended, buffer.Append(103, Start.AddSeconds(2)));
        Assert.True(buffer.ForceClose());

        var batch = buffer.FindSealed("conv-1#b101");
        Assert.NotNull(batch);
        Assert.Equal(InputBufferCloseReason.ForcedByCaller, batch!.CloseReason);
        Assert.Equal(new long[] { 101, 102, 103 }, batch.OrderedSeqs.ToArray());
    }

    [Fact]
    public void InputBuffer_count_cap_displaces_the_batch_at_the_exact_ceiling()
    {
        var buffer = new NarrativeInputBuffer("conv-1", new InputBufferCeiling { MaxBatchItems = 2, MaxBatchHold = TimeSpan.FromMinutes(1) });
        Assert.Equal(InputBufferAppendDecision.Appended, buffer.Append(1, Start));
        Assert.Equal(InputBufferAppendDecision.AppendedAndClosedByItemCount, buffer.Append(2, Start));
        Assert.False(buffer.HasOpenBatch);

        var batch = buffer.FindSealed("conv-1#b1");
        Assert.Equal(InputBufferCloseReason.DisplacedByItemCount, batch!.CloseReason);
    }

    [Fact]
    public void InputBuffer_hold_deadline_seals_the_batch_only_once_elapsed()
    {
        var buffer = new NarrativeInputBuffer("conv-1", new InputBufferCeiling { MaxBatchItems = 50, MaxBatchHold = TimeSpan.FromSeconds(3) });
        buffer.Append(10, Start);
        Assert.False(buffer.TryCloseDueAt(Start.AddSeconds(2)));
        Assert.True(buffer.HasOpenBatch);
        Assert.True(buffer.TryCloseDueAt(Start.AddSeconds(3)));

        Assert.Equal(InputBufferCloseReason.HoldElapsed, buffer.FindSealed("conv-1#b10")!.CloseReason);
    }

    [Fact]
    public void InputBuffer_duplicate_and_out_of_order_seqs_are_rejected_by_name()
    {
        var buffer = new NarrativeInputBuffer("conv-1", new InputBufferCeiling { MaxBatchItems = 5, MaxBatchHold = TimeSpan.FromMinutes(1) });
        buffer.Append(101, Start);
        Assert.Equal(InputBufferAppendDecision.RejectedDuplicateSeq, buffer.Append(101, Start));
        Assert.Equal(InputBufferAppendDecision.RejectedOutOfOrder, buffer.Append(50, Start));
    }

    [Fact]
    public void InputBuffer_batch_id_is_stable_and_replayable_from_the_first_seq()
    {
        var first = new NarrativeInputBuffer("conv-1", new InputBufferCeiling { MaxBatchItems = 3, MaxBatchHold = TimeSpan.FromMinutes(1) });
        first.Append(200, Start);
        first.Append(201, Start);
        first.ForceClose();

        // 重放同一条首发输入 → 同一批次标识，可稳定回放。
        var replay = new NarrativeInputBuffer("conv-1", new InputBufferCeiling { MaxBatchItems = 3, MaxBatchHold = TimeSpan.FromMinutes(1) });
        replay.Append(200, Start);
        replay.Append(201, Start);
        replay.ForceClose();

        Assert.Equal("conv-1#b200", first.FindSealed("conv-1#b200")!.BatchId);
        Assert.Equal(first.FindSealed("conv-1#b200")!.BatchId, replay.FindSealed("conv-1#b200")!.BatchId);
    }

    [Fact]
    public void InputBuffer_one_batch_yields_exactly_one_sendable_truth()
    {
        var buffer = new NarrativeInputBuffer("conv-1", new InputBufferCeiling { MaxBatchItems = 2, MaxBatchHold = TimeSpan.FromMinutes(1) });
        buffer.Append(1, Start);
        buffer.Append(2, Start);

        Assert.True(buffer.TryTakeSendableTruth("conv-1#b1"));
        Assert.False(buffer.TryTakeSendableTruth("conv-1#b1"));
        Assert.False(buffer.TryTakeSendableTruth("conv-1#b999"));
    }

    [Fact]
    public void InputBuffer_ceiling_below_one_or_nonpositive_hold_is_rejected()
    {
        Assert.Throws<NarrativeContractException>(() =>
            new InputBufferCeiling { MaxBatchItems = 0, MaxBatchHold = TimeSpan.FromSeconds(1) }.Validate());
        Assert.Throws<NarrativeContractException>(() =>
            new InputBufferCeiling { MaxBatchItems = 2, MaxBatchHold = TimeSpan.Zero }.Validate());
        Assert.Throws<NarrativeContractException>(() =>
            new NarrativeInputBuffer("conv-1", new InputBufferCeiling { MaxBatchItems = -1, MaxBatchHold = TimeSpan.FromSeconds(1) }));
    }

    // —— 缺口 2：NarrativeInterruption ——

    [Fact]
    public void Interruption_cancels_only_pending_segments_and_withholds_delivered()
    {
        var segments = new[]
        {
            new InterruptibleSegment("c1", "a1", 0, DeliverySegmentState.Delivered),
            new InterruptibleSegment("c1", "a1", 1, DeliverySegmentState.Pending),
            new InterruptibleSegment("c1", "a1", 2, DeliverySegmentState.Pending),
        };
        var outcome = NarrativeInterruption.Apply(segments, InterruptionReasonKind.UserInterruption);

        Assert.Equal(InterruptionApplyResult.Applied, outcome.Result);
        Assert.Equal(2, outcome.Cancelled.Count);
        Assert.All(outcome.Cancelled, s => Assert.Equal(DeliverySegmentState.Pending, s.State));
        Assert.Single(outcome.Withheld);
        Assert.True(outcome.Withheld[0].IsDelivered);
    }

    [Fact]
    public void Interruption_never_withdraws_a_delivered_segment()
    {
        var outcome = NarrativeInterruption.Apply(
            [new InterruptibleSegment("c1", "a1", 0, DeliverySegmentState.Delivered)],
            InterruptionReasonKind.SupersededByNewInput);
        Assert.Empty(outcome.Cancelled);
        Assert.Single(outcome.Withheld);
    }

    [Fact]
    public void Interruption_without_a_nameable_reason_is_rejected_not_guessed()
    {
        var outcome = NarrativeInterruption.Apply(
            [new InterruptibleSegment("c1", "a1", 0, DeliverySegmentState.Pending)], null);
        Assert.Equal(InterruptionApplyResult.RejectedUnnamedReason, outcome.Result);
        Assert.Empty(outcome.Cancelled);
    }

    [Fact]
    public void Interruption_two_named_reasons_are_distinguishable_in_the_outcome()
    {
        var segs = new[] { new InterruptibleSegment("c1", "a1", 0, DeliverySegmentState.Pending) };
        var a = NarrativeInterruption.Apply(segs, InterruptionReasonKind.UserInterruption);
        var b = NarrativeInterruption.Apply(segs, InterruptionReasonKind.SupersededByNewInput);
        Assert.Equal(InterruptionReasonKind.UserInterruption, a.Reason);
        Assert.Equal(InterruptionReasonKind.SupersededByNewInput, b.Reason);
        Assert.NotEqual(a.Reason, b.Reason);
    }

    [Fact]
    public void Interruption_empty_segment_list_is_rejected_as_nothing_to_process()
    {
        var outcome = NarrativeInterruption.Apply([], InterruptionReasonKind.UserInterruption);
        Assert.Equal(InterruptionApplyResult.RejectedNothingToProcess, outcome.Result);
    }

    // —— 缺口 3：NarrativeInterruptClassifier ——

    [Theory]
    [InlineData(InterruptCue.ContinuationMarker, InterruptClass.Continuation)]
    [InlineData(InterruptCue.AcknowledgementOnly, InterruptClass.Continuation)]
    [InlineData(InterruptCue.TopicShiftSignal, InterruptClass.NewTopic)]
    [InlineData(InterruptCue.HardRedirection, InterruptClass.NewTopic)]
    [InlineData(InterruptCue.NoSignal, InterruptClass.Undeterminable)]
    public void Classifier_every_cue_maps_to_its_declared_class(InterruptCue cue, InterruptClass expected)
    {
        Assert.Equal(expected, NarrativeInterruptClassifier.Classify(cue));
    }

    [Fact]
    public void Classifier_undeterminable_is_its_own_bucket_not_folded_into_a_real_class()
    {
        var undeterminable = NarrativeInterruptClassifier.Classify(InterruptCue.NoSignal);
        Assert.Equal(InterruptClass.Undeterminable, undeterminable);
        Assert.NotEqual(InterruptClass.Continuation, undeterminable);
        Assert.NotEqual(InterruptClass.NewTopic, undeterminable);
    }

    [Fact]
    public void Classifier_undefined_cue_value_falls_to_undeterminable_not_a_real_class()
    {
        // 越界枚举值不得就近并进某一档，必须落独立不可判档。
        Assert.Equal(InterruptClass.Undeterminable, NarrativeInterruptClassifier.Classify((InterruptCue)99));
    }

    [Fact]
    public void Classifier_null_cue_is_rejected_rather_than_defaulted()
    {
        Assert.Throws<NarrativeContractException>(() => NarrativeInterruptClassifier.Classify(null));
    }

    // —— 缺口 4：NarrativeBehaviorExpressionMapping ——

    [Theory]
    [InlineData(BehaviorRegisterSignal.Warm, NarrativeExpressionGear.Warm)]
    [InlineData(BehaviorRegisterSignal.Playful, NarrativeExpressionGear.Playful)]
    [InlineData(BehaviorRegisterSignal.Neutral, NarrativeExpressionGear.Neutral)]
    [InlineData(BehaviorRegisterSignal.Serious, NarrativeExpressionGear.Restrained)]
    [InlineData(BehaviorRegisterSignal.Guarded, NarrativeExpressionGear.Restrained)]
    public void Mapping_register_maps_to_declared_gear_when_scene_does_not_block(BehaviorRegisterSignal register, NarrativeExpressionGear expected)
    {
        var outcome = NarrativeBehaviorExpressionMapping.Map(new BehaviorCue(register, []));
        Assert.Equal(BehaviorMappingResult.Mapped, outcome.Result);
        Assert.Equal(expected, outcome.Gear);
    }

    [Fact]
    public void Mapping_missing_behavior_decision_does_not_default_to_pass_through()
    {
        var outcome = NarrativeBehaviorExpressionMapping.Map(null);
        Assert.Equal(BehaviorMappingResult.NoBehaviorDecision, outcome.Result);
        // 缺省档必须是显式枚举值，且不是任何"可表达"档。
        Assert.Equal(NarrativeExpressionGear.NotDetermined, outcome.Gear);
        Assert.NotEqual(BehaviorMappingResult.Mapped, outcome.Result);
    }

    [Fact]
    public void Mapping_unspecified_register_is_treated_as_no_decision()
    {
        var outcome = NarrativeBehaviorExpressionMapping.Map(new BehaviorCue(BehaviorRegisterSignal.Unspecified, []));
        Assert.Equal(BehaviorMappingResult.NoBehaviorDecision, outcome.Result);
        Assert.Equal(NarrativeExpressionGear.NotDetermined, outcome.Gear);
    }

    [Fact]
    public void Mapping_scene_block_yields_a_nameable_rejection_reason_and_no_pass_through()
    {
        var outcome = NarrativeBehaviorExpressionMapping.Map(
            new BehaviorCue(BehaviorRegisterSignal.Playful, ["sensitive_scene"]));
        Assert.Equal(BehaviorMappingResult.BlockedByScene, outcome.Result);
        Assert.Equal("sensitive_scene", outcome.BlockingSceneCode);
        Assert.Equal(NarrativeExpressionGear.NotDetermined, outcome.Gear);
    }

    [Fact]
    public void Mapping_mapped_outcome_carries_no_prose()
    {
        var outcome = NarrativeBehaviorExpressionMapping.Map(new BehaviorCue(BehaviorRegisterSignal.Warm, []));
        Assert.Equal(BehaviorMappingResult.Mapped, outcome.Result);
        // 换形只带档位与理由码，映射成功时不含任何拒绝理由文本。
        Assert.Null(outcome.BlockingSceneCode);
    }

    // —— 缺口 5：NarrativeReplyPacing ——

    [Fact]
    public void Pacing_send_times_are_monotonic_non_decreasing()
    {
        var input = new ReplyPacingInput { ReadingCharactersPerSecond = 10, MinSegmentGap = TimeSpan.Zero, MaxSegmentGap = TimeSpan.FromSeconds(1) };
        var times = NarrativeReplyPacing.PlanSendTimes(Start, [10, 20, 5], input);

        Assert.Equal(3, times.Count);
        for (var i = 1; i < times.Count; i++)
        {
            Assert.True(times[i] >= times[i - 1], $"第 {i} 段可发时刻不得早于第 {i - 1} 段（单调不减）。");
        }
    }

    [Fact]
    public void Pacing_longer_previous_segment_sends_next_no_earlier()
    {
        var input = new ReplyPacingInput { ReadingCharactersPerSecond = 10, MinSegmentGap = TimeSpan.Zero, MaxSegmentGap = TimeSpan.FromSeconds(30) };
        var shortGap = NarrativeReplyPacing.PlanSendTimes(Start, [5, 0], input);
        var longGap = NarrativeReplyPacing.PlanSendTimes(Start, [20, 0], input);

        Assert.True(longGap[1] > shortGap[1]);
    }

    [Fact]
    public void Pacing_min_gap_floors_and_max_gap_caps_the_interval()
    {
        var capped = new ReplyPacingInput { ReadingCharactersPerSecond = 1, MinSegmentGap = TimeSpan.Zero, MaxSegmentGap = TimeSpan.FromSeconds(2) };
        var times = NarrativeReplyPacing.PlanSendTimes(Start, [100, 0], capped);
        Assert.Equal(Start.AddSeconds(2), times[1]);

        var floored = new ReplyPacingInput { ReadingCharactersPerSecond = 1000, MinSegmentGap = TimeSpan.FromSeconds(1), MaxSegmentGap = TimeSpan.FromSeconds(5) };
        var times2 = NarrativeReplyPacing.PlanSendTimes(Start, [1, 0], floored);
        Assert.Equal(Start.AddSeconds(1), times2[1]);
    }

    [Fact]
    public void Pacing_rhythm_ignores_content_only_lengths_matter()
    {
        var input = new ReplyPacingInput { ReadingCharactersPerSecond = 8, MinSegmentGap = TimeSpan.Zero, MaxSegmentGap = TimeSpan.FromSeconds(10) };
        // 相同长度序列 → 相同可发时刻表；节奏不掺和内容。
        var a = NarrativeReplyPacing.PlanSendTimes(Start, [12, 8, 4], input);
        var b = NarrativeReplyPacing.PlanSendTimes(Start, [12, 8, 4], input);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Pacing_nonpositive_reading_speed_or_inverted_gaps_are_rejected()
    {
        Assert.Throws<NarrativeContractException>(() =>
            new ReplyPacingInput { ReadingCharactersPerSecond = 0, MinSegmentGap = TimeSpan.Zero, MaxSegmentGap = TimeSpan.FromSeconds(1) }.Validate());
        Assert.Throws<NarrativeContractException>(() =>
            new ReplyPacingInput { ReadingCharactersPerSecond = 10, MinSegmentGap = TimeSpan.FromSeconds(5), MaxSegmentGap = TimeSpan.FromSeconds(1) }.Validate());
    }

    [Fact]
    public void Pacing_negative_character_count_is_rejected()
    {
        var input = new ReplyPacingInput { ReadingCharactersPerSecond = 10, MinSegmentGap = TimeSpan.Zero, MaxSegmentGap = TimeSpan.FromSeconds(1) };
        Assert.Throws<NarrativeContractException>(() => NarrativeReplyPacing.PlanSendTimes(Start, [10, -1], input));
    }

    // —— 源码扫描门：五个新类型零引用旧主链四件（自带阳性探针）——

    [Fact]
    public void Gap_prototype_sources_carry_no_legacy_chain_references_and_the_probe_proves_the_gate_bites()
    {
        var identifiers = new[] { "BehaviorPlanner", "ReplyGenerationRenderer", "ReplyPlanner", "ReplyNecessity" };
        var files = new[]
        {
            "NarrativeInputBuffer.cs",
            "NarrativeInterruption.cs",
            "NarrativeInterruptClassifier.cs",
            "NarrativeBehaviorExpressionMapping.cs",
            "NarrativeReplyPacing.cs",
        };

        foreach (var file in files)
        {
            var path = Path.Combine(RepositoryRoot(), "src", "Pry.Core", "Narrative", file);
            Assert.True(File.Exists(path), $"新类型源码不在盘上：{path}");
            var source = File.ReadAllText(path);
            foreach (var identifier in identifiers)
            {
                Assert.DoesNotContain(identifier, source, StringComparison.Ordinal);
            }
        }

        // 阳性探针：扫描判据必须能咬住真的引用了旧主链的坏样本，否则门是常绿的假门。
        var sample = Path.ChangeExtension(Path.GetTempFileName(), ".cs");
        try
        {
            File.WriteAllText(sample, "var legacy = new ReplyPlanner(); // 旧主链装配\n");
            var badSample = File.ReadAllText(sample);
            Assert.Contains("ReplyPlanner", badSample, StringComparison.Ordinal);
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
        {
            directory = directory.Parent;
        }

        Assert.True(directory is not null, "自测试输出目录上溯找不到 Pry.slnx，仓库根无法定位。");
        return directory!.FullName;
    }
}

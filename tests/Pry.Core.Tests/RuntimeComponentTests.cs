using Pry.Core.Intelligence;
using Pry.Core.Models;
using Pry.Core.Runtime;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// Runtime 各组件的纯判断逻辑。这些用例直接给时间戳，不依赖真实等待，
/// 因此可以确定性地覆盖"半小时后回来"这类长间隔场景。
/// </summary>
public sealed class RuntimeComponentTests
{
    private static readonly DateTimeOffset T = new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

    private static DateTimeOffset At(int milliseconds) => T + TimeSpan.FromMilliseconds(milliseconds);

    private static InputBuffer Buffer(RuntimeSettings? settings = null) =>
        new(settings ?? RuntimeSettingsFactory.Fast(debounceMs: 1200, maxBufferMs: 5000));

    [Fact]
    public void Three_rapid_messages_stay_one_input_phase_and_one_deadline()
    {
        var buffer = Buffer();
        buffer.Add(new RuntimeUserInput("A"), At(0));
        buffer.Add(new RuntimeUserInput("B"), At(50));
        buffer.Add(new RuntimeUserInput("C"), At(350));

        Assert.False(buffer.Assess(At(400)).Complete);
        Assert.True(buffer.Assess(At(1550)).Complete);
        Assert.Equal("silence_elapsed", buffer.Assess(At(1550)).Reason);
        Assert.Equal("A\nB\nC", buffer.Summary());
    }

    [Fact]
    public void Half_sentence_gets_longer_than_a_plain_inter_message_gap()
    {
        var buffer = Buffer();
        buffer.Add(new RuntimeUserInput("我在想要不"), At(0));

        Assert.Equal("waiting", buffer.Assess(At(1100)).Reason);
        Assert.True(buffer.Assess(At(1200)).Complete);

        var trailing = Buffer();
        trailing.Add(new RuntimeUserInput("我跟你说，"), At(0));
        Assert.False(trailing.Assess(At(1150)).Complete);
        Assert.Equal("waiting", trailing.Assess(At(1150)).Reason);
    }

    [Fact]
    public void Hard_cap_prevents_starvation_under_continuous_input()
    {
        var buffer = Buffer();
        buffer.Add(new RuntimeUserInput("第一句"), At(0));
        for (var offset = 100; offset <= 4500; offset += 100)
        {
            Assert.False(buffer.Assess(At(offset)).Complete);
            buffer.Add(new RuntimeUserInput($"续话{offset}"), At(offset));
        }
        var verdict = buffer.Assess(At(5000));
        Assert.True(verdict.Complete);
        Assert.Equal("buffer_deadline", verdict.Reason);
    }

    [Fact]
    public void Typing_holds_the_turn_but_expires_so_a_dead_client_cannot_wedge_it()
    {
        var buffer = Buffer();
        buffer.NotifyTyping(true, At(0));
        buffer.Add(new RuntimeUserInput("打字中"), At(100));

        Assert.False(buffer.Assess(At(2000)).Complete);
        Assert.True(buffer.IsTypingAt(At(2399)));
        Assert.False(buffer.IsTypingAt(At(2400)));
        Assert.True(buffer.Assess(At(2400)).Complete);
        Assert.Equal("typing_hold_expired", buffer.Assess(At(2400)).Reason);
    }

    [Fact]
    public void Typing_stops_releasing_the_turn_immediately_after_debounce()
    {
        var buffer = Buffer();
        buffer.NotifyTyping(true, At(0));
        buffer.Add(new RuntimeUserInput("想说的话"), At(100));
        buffer.NotifyTyping(false, At(900));

        Assert.False(buffer.IsTypingAt(At(900)));
        Assert.False(buffer.Assess(At(1900)).Complete);
        Assert.True(buffer.Assess(At(2100)).Complete);
    }

    [Fact]
    public void Immediate_submission_short_circuits_every_heuristic()
    {
        var buffer = Buffer();
        buffer.Add(new RuntimeUserInput("别等了"), At(0), forceTurn: true);
        var verdict = buffer.Assess(At(0));
        Assert.True(verdict.Complete);
        Assert.Equal("forced_immediate", verdict.Reason);
    }

    [Fact]
    public void A_thirty_minute_absence_is_a_fresh_input_phase_not_a_continuation()
    {
        var buffer = Buffer();
        buffer.Add(new RuntimeUserInput("先这样"), At(0));
        Assert.True(buffer.Assess(At(1300)).Complete);
        buffer.Drain();

        var later = At(1_800_000);
        buffer.Add(new RuntimeUserInput("我回来啦"), later);
        Assert.Equal(later, buffer.FirstArrivedAt);
        Assert.Equal("waiting", buffer.Assess(later + TimeSpan.FromMilliseconds(500)).Reason);
        Assert.True(buffer.Assess(later + TimeSpan.FromMilliseconds(1200)).Complete);
    }

    [Fact]
    public void Scheduler_waits_for_the_buffer_and_runs_once_it_is_complete()
    {
        var scheduler = new TurnScheduler(RuntimeSettingsFactory.Fast());
        var waiting = scheduler.Decide(Context(false, Buffer().Assess(At(0), false)));
        Assert.Equal(GateDisposition.Wait, waiting.Decision.Disposition);

        var filled = Buffer();
        filled.Add(new RuntimeUserInput("说完了"), At(0));
        var running = scheduler.Decide(Context(true, filled.Assess(At(1300))));
        Assert.Equal(GateDisposition.Run, running.Decision.Disposition);
    }

    [Fact]
    public void Scheduler_refuses_to_open_a_turn_while_another_is_in_flight()
    {
        var scheduler = new TurnScheduler(RuntimeSettingsFactory.Fast());
        var context = Context(true, new BufferAssessment(true, TimeSpan.Zero, "silence_elapsed")) with
        {
            TurnInFlight = true
        };
        Assert.Equal(GateDisposition.Wait, scheduler.Decide(context).Decision.Disposition);
    }

    [Fact]
    public void Repeated_interruptions_widen_the_wait_and_a_clean_reply_decays_them()
    {
        var settings = RuntimeSettingsFactory.Fast(debounceMs: 1000, maxBufferMs: 60_000);
        var scheduler = new TurnScheduler(settings);
        Assert.Equal(TimeSpan.Zero, scheduler.Backoff);

        scheduler.NoteTurnInvalidatedBeforeDelivery();
        Assert.Equal(TimeSpan.FromMilliseconds(1000), scheduler.Backoff);
        scheduler.NoteTurnInvalidatedBeforeDelivery();
        Assert.Equal(TimeSpan.FromMilliseconds(3000), scheduler.Backoff);

        var run = scheduler.Decide(Context(true, new BufferAssessment(true, TimeSpan.Zero, "silence_elapsed")));
        Assert.Equal(GateDisposition.Backoff, run.Decision.Disposition);
        Assert.Equal("interrupt_backoff", run.Decision.Reason);
        Assert.Equal(TimeSpan.FromMilliseconds(3000), run.Decision.RetryIn);

        scheduler.NoteCleanDelivery();
        Assert.Equal(TimeSpan.FromMilliseconds(1000), scheduler.Backoff);
        scheduler.NoteCleanDelivery();
        Assert.Equal(TimeSpan.Zero, scheduler.Backoff);
    }

    [Fact]
    public void Backoff_is_capped_so_the_character_never_goes_silent_forever()
    {
        var scheduler = new TurnScheduler(RuntimeSettingsFactory.Fast(debounceMs: 1000, maxBufferMs: 60_000) with { MaxBackoffMs = 6000 });
        for (var round = 0; round < 12; round++) scheduler.NoteTurnInvalidatedBeforeDelivery();
        Assert.Equal(TimeSpan.FromMilliseconds(6000), scheduler.Backoff);
    }

    [Fact]
    public void Backchannel_input_while_planning_merges_instead_of_cancelling()
    {
        var controller = new InterruptionController(TestClassifier());
        var decision = controller.OnUserInput(new RuntimeUserInput("嗯"),
            Observed(RuntimePhase.Planning, turn: Identity(1)));
        Assert.Equal(InterruptionDisposition.MergeIntoCurrentBuffer, decision.Disposition);
        Assert.Empty(decision.CancelledTurns);
    }

    [Fact]
    public void Hard_interrupt_while_planning_cancels_the_turn()
    {
        var controller = new InterruptionController(TestClassifier());
        var decision = controller.OnUserInput(new RuntimeUserInput("等等，不对"),
            Observed(RuntimePhase.Planning, turn: Identity(7)));
        Assert.Equal(InterruptionDisposition.CancelTurnAndReplan, decision.Disposition);
        Assert.Equal([Identity(7)], decision.CancelledTurns);
    }

    [Fact]
    public void Even_short_non_backchannel_input_replans_so_its_content_is_not_lost()
    {
        var controller = new InterruptionController(TestClassifier());
        var longAside = controller.OnUserInput(new RuntimeUserInput("先别说这个，我问你另一件事"),
            Observed(RuntimePhase.Planning, turn: Identity(3)));
        var shortAside = controller.OnUserInput(new RuntimeUserInput("对了"),
            Observed(RuntimePhase.Planning, turn: Identity(3)));
        Assert.Equal(InterruptionDisposition.CancelTurnAndReplan, longAside.Disposition);
        Assert.Equal(InterruptionDisposition.CancelTurnAndReplan, shortAside.Disposition);
    }

    [Fact]
    public void Interrupt_while_delivering_stops_queued_bubbles_but_not_sent_ones()
    {
        var controller = new InterruptionController(TestClassifier());
        var decision = controller.OnUserInput(new RuntimeUserInput("等等"),
            Observed(RuntimePhase.Delivering, turn: Identity(4), delivering: Identity(4), pendingOutput: 2));
        Assert.Equal(InterruptionDisposition.CancelRemainingBubbles, decision.Disposition);
        Assert.Equal([Identity(4)], decision.CancelledTurns);

        var keepGoing = controller.OnUserInput(new RuntimeUserInput("哈哈"),
            Observed(RuntimePhase.Delivering, turn: Identity(4), delivering: Identity(4), pendingOutput: 1));
        Assert.Equal(InterruptionDisposition.ContinueDelivery, keepGoing.Disposition);
    }

    [Fact]
    public void Idle_runtime_without_output_just_buffers_the_input()
    {
        var controller = new InterruptionController(TestClassifier());
        var decision = controller.OnUserInput(new RuntimeUserInput("你好"), Observed(RuntimePhase.Idle));
        Assert.Equal(InterruptionDisposition.None, decision.Disposition);
    }

    [Fact]
    public async Task Reply_gate_blocks_stale_turns_before_spending_a_model_call()
    {
        var gate = new ReplyGate(ScriptedAdvisor.AlwaysSpeak());
        var stale = await gate.DecideAsync(Inputs(Identity(1), current: false), TestContext.Current.CancellationToken);
        Assert.Equal(ReplyGateDisposition.CancelStale, stale.Disposition);
        Assert.Equal("turn_no_longer_current", stale.Reason);
    }

    [Fact]
    public async Task Reply_gate_defers_while_the_user_is_typing_or_output_is_queued()
    {
        var gate = new ReplyGate(ScriptedAdvisor.AlwaysSpeak());
        Assert.Equal(ReplyGateDisposition.Defer, (await gate.DecideAsync(Inputs(Identity(1), typing: true), TestContext.Current.CancellationToken)).Disposition);
        Assert.Equal(ReplyGateDisposition.Defer,
            (await gate.DecideAsync(Inputs(Identity(1), pendingOutput: 1), TestContext.Current.CancellationToken)).Disposition);
    }

    [Fact]
    public async Task Reply_gate_returns_do_nothing_as_a_first_class_result()
    {
        var gate = new ReplyGate(ScriptedAdvisor.DoNothingFor(_ => true, "not_worth_it"));
        var result = await gate.DecideAsync(Inputs(Identity(1)), TestContext.Current.CancellationToken);
        Assert.Equal(ReplyGateDisposition.DoNothing, result.Disposition);
        Assert.Equal(ResponseIntent.DoNothing, result.Intent);
        Assert.Null(result.Advice);
    }

    [Fact]
    public async Task Reply_gate_passes_the_advice_through_when_it_allows_a_turn()
    {
        var gate = new ReplyGate(ScriptedAdvisor.AlwaysSpeak());
        var result = await gate.DecideAsync(Inputs(Identity(1)), TestContext.Current.CancellationToken);
        Assert.Equal(ReplyGateDisposition.Allow, result.Disposition);
        Assert.NotNull(result.Advice);
    }

    [Fact]
    public void Pacing_scales_with_reading_time_and_never_uses_a_fixed_character_split()
    {
        var settings = RuntimeSettingsFactory.Fast() with
        {
            Typing = new TypingStyle { Speed = 1, MinDelayMs = 1, MaxDelayMs = 100_000, Burstiness = 0 }
        };
        var pacing = new ReplyPacing(new Random(7));
        var afterShort = pacing.DelayFor(new PacingContext(1, 6, false, false, null), settings);
        var afterLong = pacing.DelayFor(new PacingContext(1, 120, false, false, null), settings);
        Assert.True(afterLong > afterShort);

        var proactive = pacing.DelayFor(new PacingContext(1, 6, false, true, null), settings);
        Assert.True(proactive > afterShort);
        var resumed = pacing.DelayFor(new PacingContext(1, 6, true, false, null), settings);
        Assert.True(resumed > afterShort);

        var hinted = pacing.DelayFor(new PacingContext(1, 6, false, false,
            new DeliveryHints(null, [TimeSpan.FromMilliseconds(3)])), settings);
        Assert.Equal(TimeSpan.FromMilliseconds(3), hinted);
    }

    [Theory]
    [InlineData("我跟你说，", true)]
    [InlineData("今天天气不错。", false)]
    [InlineData("你在干嘛", false)]
    [InlineData("我想说的是", true)]
    [InlineData("打开（", true)]
    public void Incomplete_detection_uses_punctuation_and_length_not_a_word_list(string text, bool incomplete) =>
        Assert.Equal(incomplete, InputProbe.LooksIncomplete(text));

    [Theory]
    [InlineData("嗯嗯", true)]
    [InlineData("哈哈哈哈", true)]
    [InlineData("嗯嗯？", true)]
    [InlineData("你今天怎么样", false)]
    [InlineData("嗯，然后那个项目", false)]
    public void Acknowledgement_detection_separates_backchannels_from_real_input(string text, bool ackOnly) =>
        Assert.Equal(ackOnly, InputProbe.IsLightweightAcknowledgement(text, null));

    [Fact]
    public void Visual_length_treats_cjk_and_latin_differently_so_latency_is_not_character_count()
    {
        Assert.Equal(4d, InputProbe.VisualLength("你好世界"));
        Assert.Equal(3d, InputProbe.VisualLength("abcdef"));
    }

    private static TurnIdentity Identity(long turn) => new(turn, 1, 0);

    private static RuntimeObservedState Observed(RuntimePhase phase, TurnIdentity? turn = null,
        TurnIdentity? delivering = null, int pendingOutput = 0) => new()
    {
        ConversationId = "chat-1",
        Phase = phase,
        Revision = 1,
        Epoch = 0,
        UserTyping = false,
        PendingInputCount = 0,
        PendingOutputCount = pendingOutput,
        ActiveTurn = turn,
        DeliveringTurn = delivering
    };

    private static IInterruptClassifier TestClassifier() =>
        new InlineClassifier(input =>
        {
            var text = input.Text.Trim();
            if (text.Contains("等等") || text.Contains("不对") || text.Contains("别说了")) return InterruptStrength.Hard;
            if (text is "嗯" or "哈哈" or "哦") return InterruptStrength.Backchannel;
            return InterruptStrength.Soft;
        });

    private static SchedulingContext Context(bool hasInput, BufferAssessment assessment) => new(At(0),
        RuntimePhase.CollectingInput, hasInput, assessment, false, false, 0, 1, false);

    private static ReplyGateInputs Inputs(TurnIdentity turn, bool current = true, bool typing = false,
        int pendingOutput = 0) =>
        new(turn, current, typing, false, false, "今天过得怎么样", ["小予"], null, TimeSpan.Zero, 0,
            RuntimePhase.Planning, 0, pendingOutput);

    private sealed class InlineClassifier(Func<RuntimeUserInput, InterruptStrength> classify) : IInterruptClassifier
    {
        public InterruptAssessment Classify(RuntimeUserInput input, RuntimeObservedState observed) =>
            new(classify(input), "inline");
    }
}

using Pry.Core.Intelligence;
using Pry.Core.Runtime;
using Xunit;

namespace Pry.Core.Tests;

public sealed class RuntimeRaceTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public void Scheduler_consumes_backoff_at_the_deadline_instead_of_postponing_forever()
    {
        var scheduler = new TurnScheduler(RuntimeSettingsFactory.Fast());
        scheduler.NoteTurnInvalidatedBeforeDelivery();
        var at = DateTimeOffset.UnixEpoch;
        var context = new SchedulingContext(at, RuntimePhase.CollectingInput, true,
            new(true, TimeSpan.Zero, "complete"), false, false, 0, 1, false);
        var first = scheduler.Decide(context);
        Assert.Equal(GateDisposition.Backoff, first.Decision.Disposition);
        Assert.Equal(GateDisposition.Backoff, scheduler.Decide(context with { At = at.AddMilliseconds(1) }).Decision.Disposition);
        Assert.Equal(GateDisposition.Run, scheduler.Decide(context with { At = at + first.Decision.RetryIn!.Value }).Decision.Disposition);
    }

    [Fact]
    public async Task Explicit_cancel_does_not_requeue_the_cancelled_input()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<ResponsePlan>(TaskCreationOptions.RunContinuationsAsynchronously);
        var composer = new ControlledComposer((_, _, _) => { started.SetResult(); return release.Task; });
        var h = await RuntimeHarness.CreateAsync(composer);
        await using var runtime = h.Runtime;
        await runtime.SubmitUserInputAsync(new("cancel me"), true, Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2), Token);
        await runtime.CancelActiveTurnAsync("client_cancelled");
        release.SetResult(ControlledComposer.Plan("obsolete"));
        Assert.True(await runtime.DrainAsync(TimeSpan.FromSeconds(2), Token));
        Assert.Equal(1, composer.Calls);
        Assert.Empty(h.Sink.Delivered);
        Assert.Equal(RuntimePhase.Idle, runtime.Phase);
    }

    [Fact]
    public async Task Return_without_input_invalidates_an_approved_proactive_candidate()
    {
        var h = await RuntimeHarness.CreateAsync(new ControlledComposer((_, _, _) => Task.FromResult(ResponsePlan.Nothing("unused"))));
        await using var runtime = h.Runtime;
        var now = DateTimeOffset.UtcNow;
        var candidate = new ProactiveCandidate("return-race", runtime.ConversationId, runtime.CharacterInstanceId,
            "idle", new(99, 1, runtime.Epoch), runtime.ConversationProgress, now, now.AddMinutes(1),
            ControlledComposer.Plan("obsolete follow-up"));
        Assert.True((await runtime.ReviewProactiveAsync(candidate, Token)).Approved);
        await runtime.NotifyUserReturnedAsync(Token);
        Assert.Equal(0, (await runtime.DeliverProactiveAsync(candidate, Token)).Delivered);
        Assert.Empty(h.Sink.Delivered);
    }

    [Fact]
    public async Task Proactive_delivery_cannot_bypass_the_social_gate()
    {
        var h = await RuntimeHarness.CreateAsync(new ControlledComposer((_, _, _) => Task.FromResult(ResponsePlan.Nothing("unused"))),
            ScriptedAdvisor.DoNothingFor(_ => true, "quiet"));
        await using var runtime = h.Runtime;
        var now = DateTimeOffset.UtcNow;
        var candidate = new ProactiveCandidate("quiet", runtime.ConversationId, runtime.CharacterInstanceId,
            "idle", new(99, 1, runtime.Epoch), runtime.ConversationProgress, now, now.AddMinutes(1), ControlledComposer.Plan("no"));
        Assert.Equal(0, (await runtime.DeliverProactiveAsync(candidate, Token)).Delivered);
        Assert.Empty(h.Store.AssistantTexts());
    }

    [Fact]
    public async Task Capability_wait_is_not_idle_and_a_wrong_result_cannot_consume_it()
    {
        var composer = new ControlledComposer((_, index, _) => Task.FromResult(index == 0
            ? new ResponsePlan(ResponseIntent.UseCapability, [], new("notes", new Dictionary<string, string>(), TurnIdentity.None))
            : ControlledComposer.Plan("done")));
        var h = await RuntimeHarness.CreateAsync(composer);
        await using var runtime = h.Runtime;
        await runtime.SubmitUserInputAsync(new("open notes"), true, Token);
        Assert.True(await runtime.DrainAsync(TimeSpan.FromSeconds(2), Token));
        Assert.Equal(RuntimePhase.WaitingForTurn, runtime.Phase);
        var identity = runtime.Observe().ActiveTurn!;
        await runtime.AcceptAsync(RuntimeEvent.Capability(DateTimeOffset.UtcNow, new("wrong", identity, true, "", null)), Token);
        await runtime.AcceptAsync(RuntimeEvent.Capability(DateTimeOffset.UtcNow, new("notes", identity, true, "", null)), Token);
        Assert.True(await runtime.DrainAsync(TimeSpan.FromSeconds(2), Token));
        Assert.Equal(["done"], h.Store.AssistantTexts());
    }

    [Fact]
    public async Task Heartbeats_do_not_prevent_eviction_or_hide_a_thirty_minute_return()
    {
        var clock = new ManualClock();
        var store = new FakeRuntimeStore(new RuntimeLedger());
        await using var runtime = await ConversationRuntime.CreateAsync(new("room", "character", clock,
            RuntimeSettingsFactory.Fast(), store, new ControlledComposer((_, _, _) => Task.FromResult(ResponsePlan.Nothing("silent"))),
            ScriptedAdvisor.AlwaysSpeak(), ScriptedClassifier.Heuristic(), new RecordingSink()), Token);
        for (var i = 0; i < 30; i++)
        {
            clock.Advance(TimeSpan.FromMinutes(1));
            await runtime.AcceptAsync(new(RuntimeEventKind.IdleElapsed, clock.GetUtcNow()), Token);
        }
        Assert.True(runtime.CanEvict(clock.GetUtcNow()));
        await runtime.SubmitUserInputAsync(new("returned"), true, Token);
        Assert.Contains(runtime.TraceEntries, x => x.Action == "idle_return_observed");
        Assert.DoesNotContain(runtime.TraceEntries, x => x.Reason.Contains("returned", StringComparison.Ordinal));
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UnixEpoch;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan delta) => _now += delta;
    }

    [Fact]
    public async Task Old_model_completion_cannot_set_a_new_generating_turn_to_idle()
    {
        var first = new TaskCompletionSource<ResponsePlan>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<ResponsePlan>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var composer = new ControlledComposer((_, index, _) =>
        {
            if (index == 0) return first.Task;
            secondStarted.TrySetResult();
            return second.Task;
        });
        var h = await RuntimeHarness.CreateAsync(composer);
        await using var runtime = h.Runtime;
        await runtime.SubmitUserInputAsync(new("first"), true, Token);
        await runtime.SubmitUserInputAsync(new("不对"), true, Token);
        await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), Token);
        var current = runtime.Observe().ActiveTurn;
        first.SetResult(ControlledComposer.Plan("obsolete"));
        // A cancelled old result completes its cleanup while the second generation stays blocked.
        await Task.Delay(50, Token);
        Assert.Equal(RuntimePhase.Planning, runtime.Phase);
        Assert.Equal(current, runtime.Observe().ActiveTurn);
        second.SetResult(ControlledComposer.Plan("current"));
        Assert.True(await runtime.DrainAsync(TimeSpan.FromSeconds(2), Token));
        Assert.Equal(["current"], h.Store.AssistantTexts());
    }

    [Fact]
    public void Repeated_typing_reports_renew_the_lease_but_disconnect_still_expires()
    {
        var buffer = new InputBuffer(RuntimeSettingsFactory.Fast() with { TypingHoldMs = 100 });
        var at = DateTimeOffset.UnixEpoch;
        buffer.NotifyTyping(true, at);
        buffer.NotifyTyping(true, at.AddMilliseconds(80));
        Assert.True(buffer.IsTypingAt(at.AddMilliseconds(120)));
        Assert.False(buffer.IsTypingAt(at.AddMilliseconds(181)));
    }

    [Fact]
    public async Task Inputs_at_zero_fifty_and_three_hundred_fifty_ms_form_one_turn()
    {
        var clock = new ManualClock();
        var composer = new ControlledComposer((_, _, _) => Task.FromResult(ResponsePlan.Nothing("silent")));
        await using var runtime = await ConversationRuntime.CreateAsync(new("room", "character", clock,
            RuntimeSettingsFactory.Fast(1200, 5000), new FakeRuntimeStore(new RuntimeLedger()), composer,
            ScriptedAdvisor.AlwaysSpeak(), ScriptedClassifier.Heuristic(), new RecordingSink()), Token);
        await runtime.SubmitUserInputAsync(new("A"), cancellationToken: Token);
        clock.Advance(TimeSpan.FromMilliseconds(50));
        await runtime.SubmitUserInputAsync(new("B"), cancellationToken: Token);
        clock.Advance(TimeSpan.FromMilliseconds(300));
        await runtime.SubmitUserInputAsync(new("C"), cancellationToken: Token);
        Assert.Equal(0, composer.Calls);
        clock.Advance(TimeSpan.FromMilliseconds(1200));
        await runtime.AcceptAsync(RuntimeEvent.TurnDue(clock.GetUtcNow()), Token);
        Assert.Equal(1, composer.Calls);
        Assert.Equal(["A", "B", "C"], composer.Requests.Single().CombinedText);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Slow_or_failed_append_never_becomes_an_unpersisted_turn(bool fail)
    {
        var ledger = new RuntimeLedger();
        var store = new ControlledStore(new FakeRuntimeStore(ledger)) { BlockAppend = true, FailAppend = fail };
        var composer = new ControlledComposer((_, _, _) => Task.FromResult(ResponsePlan.Nothing("silent")));
        await using var runtime = await ConversationRuntime.CreateAsync(new("room", "character", TimeProvider.System,
            RuntimeSettingsFactory.Fast(), store, composer, ScriptedAdvisor.AlwaysSpeak(), ScriptedClassifier.Heuristic(),
            new RecordingSink()), Token);
        var submit = runtime.SubmitUserInputAsync(new("persist first"), true, Token);
        await store.AppendStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), Token);
        var timer = runtime.AcceptAsync(RuntimeEvent.TurnDue(DateTimeOffset.UtcNow.AddSeconds(10)), Token);
        Assert.Equal(0, composer.Calls);
        store.AppendRelease.SetResult();
        if (fail) await Assert.ThrowsAsync<IOException>(() => submit);
        else await submit;
        await timer;
        Assert.Equal(fail ? 0 : 1, composer.Calls);
        Assert.Equal(0, runtime.Observe().PendingInputCount);
    }

    [Fact]
    public async Task Commit_failure_settles_plan_and_turns_typing_off()
    {
        var store = new ControlledStore(new FakeRuntimeStore(new RuntimeLedger())) { FailCommit = true };
        var sink = new RecordingSink();
        using var scheduler = new DeliveryScheduler("room", TimeProvider.System, RuntimeSettingsFactory.Fast(),
            store, sink, new ReplyPacing(new Random(1)), _ => true, _ => { });
        var plan = ControlledComposer.Plan("one", "two");
        var turn = new TurnIdentity(1, 1, 0);
        var permission = await store.RequestDeliveryAsync("room", turn, 0, plan.Messages, "model_reply", null, Token);
        scheduler.Enqueue(plan, permission, 0, false);
        await scheduler.PumpAsync(Token).WaitAsync(TimeSpan.FromSeconds(2), Token);
        var report = await scheduler.WaitForPlanAsync(turn, Token).WaitAsync(TimeSpan.FromSeconds(2), Token);
        Assert.Equal(2, report.Abandoned);
        Assert.Equal(0, report.Delivered);
        Assert.Equal("off", sink.Typing.Last());
        Assert.Equal(0, scheduler.PendingCount);
    }

    [Fact]
    public async Task Candidate_expiring_during_delivery_delay_is_never_committed()
    {
        var h = await RuntimeHarness.CreateAsync(new ControlledComposer((_, _, _) => Task.FromResult(ResponsePlan.Nothing("unused"))),
            settings: RuntimeSettingsFactory.WithBubbleDelay(100, 100));
        await using var runtime = h.Runtime;
        var now = DateTimeOffset.UtcNow;
        var candidate = new ProactiveCandidate("ttl", runtime.ConversationId, runtime.CharacterInstanceId, "idle",
            new(99, 1, runtime.Epoch), runtime.ConversationProgress, now, now.AddMilliseconds(20), ControlledComposer.Plan("expired"));
        var report = await runtime.DeliverProactiveAsync(candidate, Token);
        Assert.Equal(0, report.Delivered);
        Assert.Empty(h.Store.AssistantTexts());
    }

    private sealed class ControlledStore(IRuntimeStore inner) : IRuntimeStore
    {
        public bool BlockAppend, FailAppend, FailCommit;
        public TaskCompletionSource AppendStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource AppendRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<long> AppendUserInputAsync(string id, RuntimeUserInput input, CancellationToken ct)
        {
            AppendStarted.TrySetResult();
            if (BlockAppend) await AppendRelease.Task.WaitAsync(ct);
            if (FailAppend) throw new IOException("append failed");
            return await inner.AppendUserInputAsync(id, input, ct);
        }
        public Task<long?> CommitBubbleAsync(DeliveryPermission p, int sequence, RuntimePlannedMessage m, CancellationToken ct) =>
            FailCommit ? Task.FromException<long?>(new IOException("commit failed")) : inner.CommitBubbleAsync(p, sequence, m, ct);
        public Task<RuntimePersistenceState?> LoadStateAsync(string id, CancellationToken ct) => inner.LoadStateAsync(id, ct);
        public Task SaveStateAsync(string id, RuntimePersistenceState s, CancellationToken ct) => inner.SaveStateAsync(id, s, ct);
        public Task<long> ReadEpochAsync(string id, CancellationToken ct) => inner.ReadEpochAsync(id, ct);
        public Task<long> BumpEpochAsync(string id, CancellationToken ct) => inner.BumpEpochAsync(id, ct);
        public Task<DeliveryPermission> RequestDeliveryAsync(string id, TurnIdentity t, long epoch, IReadOnlyList<RuntimePlannedMessage> m,
            string source, string? key, CancellationToken ct) => inner.RequestDeliveryAsync(id, t, epoch, m, source, key, ct);
        public Task AbandonPlanAsync(DeliveryPermission p, int sequence, CancellationToken ct) => inner.AbandonPlanAsync(p, sequence, ct);
        public Task<IReadOnlyList<long>> SweepStalePlansAsync(CancellationToken ct) => inner.SweepStalePlansAsync(ct);
    }
}

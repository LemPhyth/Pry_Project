// HDS-007「有界连续推进」第一片·隔离原型判据（工单 HDS-WAVE29-1024 §二）。
//
// 逐条对上工单 §一 的五条，每条至少一枚独立可判的格（下面的"枚"＝展开后的节点数；
// 注释里刻意不写方括号属性名，以免顶账时 grep 把注释行算成格）：
//   §一.1 三类触发同入口    ＝ 1 枚 Theory 展开 3 条区分性探针 ＋ 1 枚 Fact（入口只有一枚公开方法）
//   §一.2 冷却与静默可观察  ＝ 4 枚 Fact（冷却内推迟＋最早下一次点名、窗口外沿那一瞬放行、
//                                    静默期推迟且意图仍在待观察清单、闸门打开后补推并离开清单）
//   §一.3 预算从入参传入    ＝ 4 枚 Fact（调用维度耗尽点名已用/剩余/维度、预算为零是另一种形状、
//                                    费用维度单独拦、恰好花完剩余预算仍放行）
//   §一.4 长离线恢复        ＝ 3 枚 Fact（按时钟前跳按登记顺序补投、两次跳表不重复也不漏、
//                                    没到点的仍待投）＋ 1 枚 Fact（迟到的推进被拒且零写回）
//   §一.5 006 最小来源约束  ＝ 1 枚 Fact（来源缺失即明确失败且留在观察日志）
//                                    ＋ 1 枚 Theory 展开 3 条探针（三类触发各自都拒）＋ 1 枚 Fact（成功产出带来源）
//   附带两枚源码扫描格：新原型不引用旧链类型名、不写任何窗口/预算的源码侧默认值（各带阳性探针）
//
// 断言只钉绝对字面量或"当场算出再与字面量比"的形状；没有"两条路径同构造所以恒等"那种覆盖证据。
// 本文件不改任何既有判据、不改上游门、不用 Assert.Skip、不注释既有断言。
// 三句话（新链已接线／旧链可删／不进包已做到）在本文件与产品码里都不成立，也不被任何注释暗示。
using System.Reflection;
using Pry.Core.Narrative.Advancement;
using Xunit;

namespace Pry.Core.Tests;

public sealed class BoundedAdvancementPrototypeTests
{
    private static readonly DateTime Noon = new(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Midnight = new(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc);

    // 静默窗口不生效的形状＝起点与终点重合（不是原型里的开关值）。
    private static readonly AdvancementSourceReference Source = new("src-1", 7);
    private static readonly AdvancementBudget GenerousBudget = new(10, 0, 10m, 0m, 1, 0.25m);

    private static readonly string[] LegacyChainNames =
    [
        "CharacterIntelligenceGateway", "IdleBackoffController", "ReplyGate", "ReplyPlanner", "TurnGates",
    ];

    private static readonly string[] PrototypeFiles =
    [
        "AdvancementContracts.cs", "AdvancementCoordinator.cs", "DueIntentRegistry.cs",
    ];

    // ───────────────────────── §一.1 三类触发同入口 ─────────────────────────

    // 三条区分性探针：三类触发走同一个入口、同一个入参形状，但只有"本类那一条"引用被点名。
    // 每条探针钉自己的绝对期望字面量；把三条字段同时给值正是为了让"别名"形状当场露出来。
    [Theory]
    [InlineData(AdvancementTriggerKind.UserEvent, "ue-1")]
    [InlineData(AdvancementTriggerKind.DueIntent, "due-1")]
    [InlineData(AdvancementTriggerKind.BackgroundScene, "bg-1")]
    public void Each_trigger_kind_enters_the_one_advancement_entry_and_names_its_own_reference(
        AdvancementTriggerKind asked, string expectedReference)
    {
        var coordinator = new AdvancementCoordinator();

        var readout = coordinator.Advance(
            Request(asked, "intent-probe", "req-probe", NoQuietWindow(Noon), GenerousBudget, Source, 0));

        Assert.Equal(asked, readout.Kind);
        Assert.Equal(AdvancementOutcome.Advanced, readout.Outcome);
        Assert.Equal("advanced", readout.OutcomeReason);
        Assert.Equal(expectedReference, readout.TriggerReference);
        var record = Assert.Single(coordinator.ProducedRecords);
        Assert.Equal(asked, record.Kind);
        Assert.Equal(expectedReference, record.TriggerReference);
        Assert.Equal("src-1", record.SourceId);
        Assert.Equal(7, record.SourceVersion);
        Assert.Equal(1, record.EffectVersion);
        Assert.Equal(1, coordinator.CurrentEffectVersion);
    }

    [Fact]
    public void The_advancement_coordinator_exposes_exactly_one_public_entry_method()
    {
        var entries = typeof(AdvancementCoordinator)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(x => x.IsSpecialName is false)
            .Select(x => x.Name)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        Assert.Equal("Advance", string.Join(",", entries));
    }

    // ─────────────────────── §一.2 冷却与静默各自可观察 ───────────────────────

    [Fact]
    public void The_same_intent_resubmitted_inside_the_cooldown_window_is_deferred_and_names_the_earliest_next_instant()
    {
        var coordinator = new AdvancementCoordinator();
        coordinator.Advance(Request(AdvancementTriggerKind.UserEvent, "intent-cool", "req-first",
            NoQuietWindow(Noon, cooldownMinutes: 30), GenerousBudget, Source, 0));

        var second = coordinator.Advance(Request(AdvancementTriggerKind.UserEvent, "intent-cool", "req-second",
            NoQuietWindow(new DateTime(2026, 10, 24, 12, 10, 0, DateTimeKind.Utc), cooldownMinutes: 30),
            GenerousBudget, Source, 1));

        Assert.Equal(AdvancementOutcome.DeferredByCooldown, second.Outcome);
        Assert.Equal("intent_inside_cooldown_window", second.OutcomeReason);
        Assert.Equal("cooldown", second.DeferredBy);
        Assert.Equal(new DateTime(2026, 10, 24, 12, 30, 0, DateTimeKind.Utc), second.EarliestNextAdvanceUtc);
        Assert.Null(second.Record);
        Assert.Equal("req-first", string.Join(",", coordinator.ProducedRecords.Select(x => x.RequestId)));
        // 观察日志里有两行：第一次生效那行不会因为第二次被推迟就消失。
        Assert.Equal("Advanced:intent-cool|DeferredByCooldown:intent-cool", Observations(coordinator));
    }

    [Fact]
    public void An_intent_resubmitted_at_the_instant_the_cooldown_window_closes_advances_again()
    {
        var coordinator = new AdvancementCoordinator();
        coordinator.Advance(Request(AdvancementTriggerKind.UserEvent, "intent-edge", "req-first",
            NoQuietWindow(Noon, cooldownMinutes: 30), GenerousBudget, Source, 0));

        var second = coordinator.Advance(Request(AdvancementTriggerKind.UserEvent, "intent-edge", "req-second",
            NoQuietWindow(new DateTime(2026, 10, 24, 12, 30, 0, DateTimeKind.Utc), cooldownMinutes: 30),
            GenerousBudget, Source, 1));

        Assert.Equal(AdvancementOutcome.Advanced, second.Outcome);
        Assert.Equal("advanced", second.OutcomeReason);
        Assert.Equal(new DateTime(2026, 10, 24, 12, 30, 0, DateTimeKind.Utc), second.Record!.ProducedAtUtc);
        Assert.Equal(2, second.Record.EffectVersion);
        Assert.Equal("Advanced:intent-edge|Advanced:intent-edge", Observations(coordinator));
    }

    [Fact]
    public void A_due_intent_inside_the_quiet_window_is_deferred_until_the_window_closes_and_stays_observable()
    {
        var coordinator = new AdvancementCoordinator();

        var readout = coordinator.Advance(Request(AdvancementTriggerKind.DueIntent, "intent-quiet", "req-quiet",
            new AdvancementWindow(new DateTime(2026, 10, 24, 18, 30, 0, DateTimeKind.Utc), TimeSpan.Zero,
                new DateTime(2026, 10, 24, 18, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 10, 24, 19, 0, 0, DateTimeKind.Utc)),
            GenerousBudget, Source, 0));

        Assert.Equal(AdvancementOutcome.DeferredByQuiet, readout.Outcome);
        Assert.Equal("intent_inside_quiet_window", readout.OutcomeReason);
        Assert.Equal("quiet", readout.DeferredBy);
        Assert.Equal(new DateTime(2026, 10, 24, 19, 0, 0, DateTimeKind.Utc), readout.EarliestNextAdvanceUtc);
        Assert.Null(readout.Record);
        var deferral = Assert.Single(coordinator.PendingDeferrals);
        Assert.Equal("intent-quiet", deferral.IntentKey);
        Assert.Equal(AdvancementTriggerKind.DueIntent, deferral.Kind);
        Assert.Equal("quiet", deferral.DeferredBy);
        Assert.Empty(coordinator.ProducedRecords);
        Assert.Equal("DeferredByQuiet:intent-quiet", Observations(coordinator));
    }

    [Fact]
    public void A_quiet_deferred_intent_advances_once_the_window_closes_and_leaves_the_pending_list()
    {
        var coordinator = new AdvancementCoordinator();
        var quietStart = new DateTime(2026, 10, 24, 18, 0, 0, DateTimeKind.Utc);
        var quietEnd = new DateTime(2026, 10, 24, 19, 0, 0, DateTimeKind.Utc);

        var deferred = coordinator.Advance(Request(AdvancementTriggerKind.DueIntent, "intent-window", "req-first",
            new AdvancementWindow(new DateTime(2026, 10, 24, 18, 30, 0, DateTimeKind.Utc), TimeSpan.Zero,
                quietStart, quietEnd),
            GenerousBudget, Source, 0));
        Assert.Equal(AdvancementOutcome.DeferredByQuiet, deferred.Outcome);

        var advanced = coordinator.Advance(Request(AdvancementTriggerKind.DueIntent, "intent-window", "req-second",
            new AdvancementWindow(quietEnd, TimeSpan.Zero, quietStart, quietEnd),
            GenerousBudget, Source, 0));

        Assert.Equal(AdvancementOutcome.Advanced, advanced.Outcome);
        Assert.Equal("due-1", advanced.TriggerReference);
        Assert.Empty(coordinator.PendingDeferrals);
        Assert.Equal(quietEnd, advanced.Record!.ProducedAtUtc);
        Assert.Equal("DeferredByQuiet:intent-window|Advanced:intent-window", Observations(coordinator));
    }

    // ───────────────────── §一.3 费用／调用预算从入参传入 ─────────────────────

    [Fact]
    public void An_exhausted_call_budget_stops_advancement_and_names_consumed_remaining_and_the_dimension()
    {
        var coordinator = new AdvancementCoordinator();
        coordinator.Advance(Request(AdvancementTriggerKind.UserEvent, "intent-calls", "req-first",
            NoQuietWindow(Noon), new AdvancementBudget(3, 0, 10m, 0m, 1, 0.25m), Source, 0));

        var stopped = coordinator.Advance(Request(AdvancementTriggerKind.UserEvent, "intent-calls", "req-second",
            NoQuietWindow(new DateTime(2026, 10, 24, 12, 5, 0, DateTimeKind.Utc)),
            new AdvancementBudget(3, 3, 10m, 0m, 1, 0.25m), Source, 1));

        Assert.Equal(AdvancementOutcome.StoppedByBudget, stopped.Outcome);
        Assert.Equal("remaining_calls_below_required_calls", stopped.OutcomeReason);
        Assert.Equal("calls", stopped.StoppedBudgetDimension);
        Assert.Equal(3, stopped.ConsumedCalls);
        Assert.Equal(0, stopped.RemainingCalls);
        Assert.Equal(10m, stopped.RemainingCost);
        Assert.Null(stopped.Record);
        Assert.Equal("Advanced:intent-calls|StoppedByBudget:intent-calls", Observations(coordinator));
    }

    [Fact]
    public void A_budget_that_is_zero_at_entry_is_a_distinct_shape_from_one_that_ran_out()
    {
        var coordinator = new AdvancementCoordinator();

        var first = coordinator.Advance(Request(AdvancementTriggerKind.UserEvent, "intent-zero", "req-first",
            NoQuietWindow(Noon), new AdvancementBudget(0, 0, 0m, 0m, 1, 0.25m), Source, 0));
        var second = coordinator.Advance(Request(AdvancementTriggerKind.UserEvent, "intent-zero", "req-second",
            NoQuietWindow(new DateTime(2026, 10, 24, 12, 5, 0, DateTimeKind.Utc)),
            new AdvancementBudget(0, 0, 0m, 0m, 1, 0.25m), Source, 0));

        Assert.Equal(AdvancementOutcome.StoppedByBudget, first.Outcome);
        Assert.Equal("calls", first.StoppedBudgetDimension);
        Assert.Equal(0, first.ConsumedCalls);
        Assert.Equal(0, first.RemainingCalls);
        Assert.Equal(AdvancementOutcome.StoppedByBudget, second.Outcome);
        // 与"耗尽"的差别就在这条日志上：这里从来没有一行 Advanced。
        Assert.Equal("StoppedByBudget:intent-zero|StoppedByBudget:intent-zero", Observations(coordinator));
        Assert.Empty(coordinator.ProducedRecords);
    }

    [Fact]
    public void A_cost_budget_running_out_stops_advancement_while_calls_are_still_available()
    {
        var coordinator = new AdvancementCoordinator();

        var stopped = coordinator.Advance(Request(AdvancementTriggerKind.UserEvent, "intent-cost", "req-cost",
            NoQuietWindow(Noon), new AdvancementBudget(10, 0, 1.00m, 0.80m, 1, 0.25m), Source, 0));

        Assert.Equal(AdvancementOutcome.StoppedByBudget, stopped.Outcome);
        Assert.Equal("remaining_cost_below_required_cost", stopped.OutcomeReason);
        Assert.Equal("cost", stopped.StoppedBudgetDimension);
        Assert.Equal(10, stopped.RemainingCalls);
        Assert.Equal(0.80m, stopped.ConsumedCost);
        Assert.Equal(0.20m, stopped.RemainingCost);
        Assert.Empty(coordinator.ProducedRecords);
        Assert.Equal("StoppedByBudget:intent-cost", Observations(coordinator));
    }

    [Fact]
    public void An_advancement_that_exactly_consumes_the_remaining_call_budget_is_allowed()
    {
        var coordinator = new AdvancementCoordinator();

        var readout = coordinator.Advance(Request(AdvancementTriggerKind.UserEvent, "intent-tight", "req-tight",
            NoQuietWindow(Noon), new AdvancementBudget(3, 2, 10m, 0m, 1, 0.25m), Source, 0));

        Assert.Equal(AdvancementOutcome.Advanced, readout.Outcome);
        Assert.Equal(2, readout.ConsumedCalls);
        Assert.Equal(1, readout.RemainingCalls);
        Assert.Null(readout.StoppedBudgetDimension);
        Assert.Equal("req-tight", readout.Record!.RequestId);
    }

    // ──────────────────────── §一.4 长离线恢复 ────────────────────────

    [Fact]
    public void After_a_clock_forward_jump_due_intents_are_replayed_in_registration_order()
    {
        var registry = new DueIntentRegistry();
        registry.Register(new DueIntentRegistration("intent-first", new DateTime(2026, 10, 24, 9, 0, 0, DateTimeKind.Utc), 1));
        registry.Register(new DueIntentRegistration("intent-second", new DateTime(2026, 10, 24, 7, 0, 0, DateTimeKind.Utc), 2));
        registry.Register(new DueIntentRegistration("intent-third", new DateTime(2026, 10, 24, 8, 0, 0, DateTimeKind.Utc), 3));
        registry.Register(new DueIntentRegistration("intent-future", new DateTime(2026, 10, 24, 20, 0, 0, DateTimeKind.Utc), 4));

        var plan = registry.RecoverAfterClockJump(Noon);

        // 补投顺序＝登记顺序，不是到期时刻顺序（后者会是 second,third,first）。
        Assert.Equal("intent-first,intent-second,intent-third",
            string.Join(",", plan.Replayed.Select(x => x.IntentKey)));
        Assert.Equal("1,2,3", string.Join(",", plan.Replayed.Select(x => x.RegisteredSequence)));
        Assert.Equal(Noon, plan.JumpedToUtc);
        Assert.All(plan.Replayed, x => Assert.Equal(Noon, x.RecoveredAtUtc));
        Assert.Equal("intent-future", string.Join(",", plan.StillPending.Select(x => x.IntentKey)));
    }

    [Fact]
    public void Replay_delivers_each_due_intent_exactly_once_across_two_clock_jumps_and_loses_none()
    {
        var registry = new DueIntentRegistry();
        registry.Register(new DueIntentRegistration("intent-a", new DateTime(2026, 10, 24, 9, 0, 0, DateTimeKind.Utc), 1));
        registry.Register(new DueIntentRegistration("intent-b", new DateTime(2026, 10, 24, 10, 0, 0, DateTimeKind.Utc), 2));

        var first = registry.RecoverAfterClockJump(Noon);
        var second = registry.RecoverAfterClockJump(new DateTime(2026, 10, 24, 18, 0, 0, DateTimeKind.Utc));

        // 本格的对象是"不重复、不漏"，不是顺序（顺序由 §一.4 的登记顺序那格负责，两格各自单点归因）。
        Assert.Equal("intent-a,intent-b", string.Join(",",
            first.Replayed.Select(x => x.IntentKey).OrderBy(x => x, StringComparer.Ordinal)));
        Assert.Equal("", string.Join(",", second.Replayed.Select(x => x.IntentKey)));
        Assert.Equal("", string.Join(",", second.StillPending.Select(x => x.IntentKey)));
        Assert.Equal("intent-a,intent-b", string.Join(",", registry.Registrations.Select(x => x.IntentKey)));
    }

    [Fact]
    public void A_due_intent_that_has_not_reached_its_due_instant_is_left_pending_and_still_reported()
    {
        var registry = new DueIntentRegistry();
        var due = new DateTime(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);
        registry.Register(new DueIntentRegistration("intent-boundary", due, 1));

        var before = registry.RecoverAfterClockJump(new DateTime(2026, 10, 24, 11, 59, 0, DateTimeKind.Utc));

        Assert.Equal("", string.Join(",", before.Replayed.Select(x => x.IntentKey)));
        var pending = Assert.Single(before.StillPending);
        Assert.Equal("intent-boundary", pending.IntentKey);
        Assert.Equal(due, pending.DueUtc);

        var atDue = registry.RecoverAfterClockJump(due);
        var replayed = Assert.Single(atDue.Replayed);
        Assert.Equal("intent-boundary", replayed.IntentKey);
        Assert.Equal("", string.Join(",", atDue.StillPending.Select(x => x.IntentKey)));
    }

    [Fact]
    public void A_late_advancement_carrying_a_superseded_effect_version_is_refused_and_writes_nothing()
    {
        var coordinator = new AdvancementCoordinator();
        coordinator.Advance(Request(AdvancementTriggerKind.UserEvent, "intent-live", "req-live",
            NoQuietWindow(Noon), GenerousBudget, Source, 0));

        var late = coordinator.Advance(Request(AdvancementTriggerKind.UserEvent, "intent-late", "req-late",
            NoQuietWindow(new DateTime(2026, 10, 24, 12, 5, 0, DateTimeKind.Utc)), GenerousBudget, Source, 0));

        Assert.Equal(AdvancementOutcome.RejectedForStaleEffect, late.Outcome);
        Assert.Equal("advancement_never_took_effect", late.OutcomeReason);
        Assert.Equal(0, late.ExpectedEffectVersion);
        Assert.Equal(1, late.ObservedEffectVersion);
        Assert.Null(late.Record);
        Assert.Equal(1, coordinator.CurrentEffectVersion);
        Assert.Equal("req-live", string.Join(",", coordinator.ProducedRecords.Select(x => x.RequestId)));
        Assert.Equal("Advanced:intent-live|RejectedForStaleEffect:intent-late", Observations(coordinator));
    }

    // ──────────────────── §一.5 006 最小来源约束（新链形状自证） ────────────────────

    [Fact]
    public void An_advancement_whose_source_cannot_be_pointed_to_fails_with_a_named_reason_and_stays_in_the_log()
    {
        var coordinator = new AdvancementCoordinator();

        var nullSource = coordinator.Advance(Request(AdvancementTriggerKind.DueIntent, "intent-nosrc", "req-null",
            NoQuietWindow(Noon), GenerousBudget, null, 0));
        var blankSource = coordinator.Advance(Request(AdvancementTriggerKind.DueIntent, "intent-nosrc", "req-blank",
            NoQuietWindow(new DateTime(2026, 10, 24, 12, 5, 0, DateTimeKind.Utc)), GenerousBudget,
            new AdvancementSourceReference("   ", 2), 0));

        Assert.Equal(AdvancementOutcome.FailedForMissingSource, nullSource.Outcome);
        Assert.Equal("source_cannot_be_pointed_to", nullSource.OutcomeReason);
        Assert.Null(nullSource.Record);
        Assert.Equal(AdvancementOutcome.FailedForMissingSource, blankSource.Outcome);
        Assert.Equal("source_cannot_be_pointed_to", blankSource.OutcomeReason);
        Assert.Null(blankSource.SourceId);
        Assert.Equal("due-1", blankSource.TriggerReference);
        // 静默丢弃在这份原型里没有出口：两次失败都各自留下一行观察。
        Assert.Equal("FailedForMissingSource:intent-nosrc|FailedForMissingSource:intent-nosrc",
            Observations(coordinator));
        Assert.Empty(coordinator.ProducedRecords);
    }

    // 三条探针：三类触发都在同一道来源闸上被拒，各自点名自己那次请求。
    [Theory]
    [InlineData(AdvancementTriggerKind.UserEvent, "req-missing-ue")]
    [InlineData(AdvancementTriggerKind.DueIntent, "req-missing-due")]
    [InlineData(AdvancementTriggerKind.BackgroundScene, "req-missing-bg")]
    public void Missing_source_is_refused_for_every_trigger_kind_with_the_same_named_failure(
        AdvancementTriggerKind asked, string requestId)
    {
        var coordinator = new AdvancementCoordinator();

        var readout = coordinator.Advance(
            Request(asked, "intent-src-gate", requestId, NoQuietWindow(Noon), GenerousBudget, null, 0));

        Assert.Equal(asked, readout.Kind);
        Assert.Equal(AdvancementOutcome.FailedForMissingSource, readout.Outcome);
        Assert.Equal("source_cannot_be_pointed_to", readout.OutcomeReason);
        Assert.Equal(requestId, readout.RequestId);
        Assert.Null(readout.Record);
        Assert.Empty(coordinator.ProducedRecords);
    }

    [Fact]
    public void A_successful_advancement_carries_the_source_reference_into_the_record_it_produces()
    {
        var coordinator = new AdvancementCoordinator();

        var readout = coordinator.Advance(Request(AdvancementTriggerKind.UserEvent, "intent-record", "req-record",
            NoQuietWindow(Noon), GenerousBudget, new AdvancementSourceReference("src-record", 4), 0));

        Assert.Equal(AdvancementOutcome.Advanced, readout.Outcome);
        Assert.Equal("src-record", readout.SourceId);
        Assert.Equal(4, readout.SourceVersion);
        var record = Assert.Single(coordinator.ProducedRecords);
        Assert.Equal("req-record", record.RequestId);
        Assert.Equal("intent-record", record.IntentKey);
        Assert.Equal(AdvancementTriggerKind.UserEvent, record.Kind);
        Assert.Equal("ue-1", record.TriggerReference);
        Assert.Equal("src-record", record.SourceId);
        Assert.Equal(4, record.SourceVersion);
        Assert.Equal(1, record.EffectVersion);
        Assert.Equal(Noon, record.ProducedAtUtc);
    }

    // ────────────────────────── 源码扫描两格（各带阳性探针） ──────────────────────────

    [Fact]
    public void Advancement_prototype_sources_carry_no_legacy_chain_names_and_the_probe_proves_the_scan_bites()
    {
        foreach (var file in PrototypeFiles)
        {
            Assert.Empty(LegacyChainNamesFoundIn(PrototypeSource(file)));
        }

        var sample = Path.ChangeExtension(Path.GetTempFileName(), ".cs");
        try
        {
            File.WriteAllText(sample,
                "var a = new CharacterIntelligenceGateway(); var b = new IdleBackoffController();"
                + " var c = new ReplyGate(); var d = new ReplyPlanner(); var e = new TurnGates();");
            Assert.Equal("CharacterIntelligenceGateway,IdleBackoffController,ReplyGate,ReplyPlanner,TurnGates",
                string.Join(",", LegacyChainNamesFoundIn(File.ReadAllText(sample))));
        }
        finally
        {
            File.Delete(sample);
        }
    }

    [Fact]
    public void Advancement_prototype_declares_no_source_side_default_for_windows_or_budgets()
    {
        foreach (var file in PrototypeFiles)
        {
            Assert.Empty(SourceSideDefaultsFoundIn(PrototypeSource(file)));
        }

        var sample = Path.ChangeExtension(Path.GetTempFileName(), ".cs");
        try
        {
            File.WriteAllText(sample,
                "private const int FallbackCalls = 4; private static readonly TimeSpan FallbackCooldown = TimeSpan.Zero;");
            Assert.Equal("const,static readonly",
                string.Join(",", SourceSideDefaultsFoundIn(File.ReadAllText(sample))));
        }
        finally
        {
            File.Delete(sample);
        }
    }

    // ─────────────────────────────── 夹具与仪器 ───────────────────────────────

    private static AdvancementWindow NoQuietWindow(DateTime now, int cooldownMinutes = 0) =>
        new(now, TimeSpan.FromMinutes(cooldownMinutes), Midnight, Midnight);

    // 三类触发各自的引用字段同时给值，Kind 决定读哪一条——这正是区分性探针的落点。
    private static AdvancementRequest Request(
        AdvancementTriggerKind kind,
        string intent,
        string requestId,
        AdvancementWindow window,
        AdvancementBudget budget,
        AdvancementSourceReference? source,
        long expectedVersion) =>
        new(kind, intent, requestId, "ue-1", "due-1", "bg-1", source, window, budget, expectedVersion);

    private static string Observations(AdvancementCoordinator coordinator) =>
        string.Join("|", coordinator.Observations.Select(x => $"{x.Outcome}:{x.IntentKey}"));

    private static IEnumerable<string> LegacyChainNamesFoundIn(string source) =>
        LegacyChainNames.Where(name => source.Contains(name, StringComparison.Ordinal));

    private static IEnumerable<string> SourceSideDefaultsFoundIn(string source)
    {
        if (source.Contains("const ", StringComparison.Ordinal)) yield return "const";
        if (source.Contains("static readonly", StringComparison.Ordinal)) yield return "static readonly";
    }

    private static string PrototypeSource(string fileName)
    {
        var path = Path.Combine(RepositoryRoot(), "src", "Pry.Core", "Narrative", "Advancement", fileName);
        Assert.True(File.Exists(path), $"隔离原型源码不在盘上：{path}");
        return File.ReadAllText(path);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Pry.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.True(directory is not null, "自测试输出目录上溯不到 Pry.slnx，仓库根无法定位。");
        return directory!.FullName;
    }
}

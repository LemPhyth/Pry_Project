using Pry.Core.Runtime;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// 触发层经由 ConversationRuntime 的端到端行为。这些用例证明的不是评分公式（那是
/// RuntimeTriggerTests 的职责），而是"接线之后 Runtime 真的会因此少说话、且不会把消息挂死"。
/// </summary>
public sealed class RuntimeTriggerIntegrationTests
{
    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 500)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (!condition() && Environment.TickCount64 < deadline)
            await Task.Delay(10, cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Silent_frequency_consumes_the_message_without_opening_a_turn()
    {
        var composer = new ControlledComposer((_, _, _) => Task.FromResult(ControlledComposer.Plan("never")));
        var harness = await RuntimeHarness.CreateAsync(composer, replyFrequency: 0.0);

        await harness.Runtime.SubmitUserInputAsync(new RuntimeUserInput("今天心情一般"),
            cancellationToken: TestContext.Current.CancellationToken);
        await harness.SettleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, composer.Calls);
        // 静默不等于丢弃：消息照常落库，只是不成轮、不占着临时缓冲。
        Assert.Equal(1, harness.Store.AppendedInputs);
        Assert.Equal(0, harness.Runtime.Snapshot().PendingInputCount);
        Assert.Equal(RuntimePhase.Idle, harness.Runtime.Phase);
        Assert.Contains(harness.Runtime.TraceEntries,
            entry => entry.Action == "consumed_silently");
        await harness.Runtime.DisposeAsync();
    }

    [Fact]
    public async Task Group_chatter_stays_quiet_in_necessity_mode_until_the_character_is_called()
    {
        var composer = new ControlledComposer((_, _, _) => Task.FromResult(ControlledComposer.Plan("answered")));
        var settings = RuntimeSettingsFactory.Fast() with { TriggerMode = ReplyTriggerMode.ReplyNecessity };
        var harness = await RuntimeHarness.CreateAsync(composer, settings: settings, isGroupChat: true);

        await harness.Runtime.SubmitUserInputAsync(new RuntimeUserInput("今天天气不错"),
            cancellationToken: TestContext.Current.CancellationToken);
        await harness.SettleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, composer.Calls);

        await harness.Runtime.SubmitUserInputAsync(new RuntimeUserInput("@小予 帮我看个东西"),
            cancellationToken: TestContext.Current.CancellationToken);
        await harness.SettleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, composer.Calls);
        Assert.Equal(["answered"], harness.Sink.Delivered.Select(x => x.Text));
        // 没被点名那轮被记成了空转，而不是被悄悄丢掉。
        Assert.Contains(harness.Runtime.TraceEntries, entry => entry.Action == "trigger:Wait");
        await harness.Runtime.DisposeAsync();
    }

    [Fact]
    public async Task A_backlog_below_the_threshold_wakes_itself_without_a_new_message()
    {
        var composer = new ControlledComposer((_, _, _) => Task.FromResult(ControlledComposer.Plan("woken")));
        // 频率 0.25 折算出阈值 4 条；只来了一条，之后没有任何新输入。
        // 没有空窗补偿这条消息会永远挂在缓冲里，所以这里同时是在验证"不会挂死"。
        //
        // MinimumAverage 取 200ms 是**夹具量级**，不是产品参数（`MessageIntervalOptions` 的默认值一位未动，
        // DEF-P2TRIGGER-01 的逐值裁定也不由这里代表）。它要把两件事的先后关系从"墙钟运气"变成"可控量"：
        // 首次评估发生在 debounce 关闭之后 ≈ DebounceMs(40ms)，而空窗补偿要凑够 threshold-1=3 个平均间隔
        // 才会改判 Trigger，即 3 × 200ms = 600ms。余量 560ms 相对定时器迟到是十几倍，分支不再落在灰区里。
        // 旧值 20ms 时该余量只有 20ms，满载并行下首次评估常晚于 60ms ⇒ 门控第一眼就成轮、
        // 这条用例声称的"自醒"根本没发生，trace 里没有 trigger:Backoff（`DEF-P2TRIGGER-02`，
        // 实测隔离 10 次红 6 次、解决方案级 3 次全红）。
        var settings = RuntimeSettingsFactory.Fast() with
        {
            Intervals = new MessageIntervalOptions
            {
                MinimumAverage = TimeSpan.FromMilliseconds(200),
                BurstFloor = TimeSpan.FromMilliseconds(1)
            }
        };
        var harness = await RuntimeHarness.CreateAsync(composer, settings: settings, replyFrequency: 0.25);

        await harness.Runtime.SubmitUserInputAsync(new RuntimeUserInput("在吗"),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(0, composer.Calls);
        Assert.Equal(1, harness.Runtime.Snapshot().PendingInputCount);

        // 超时随量级一起放大到 2.5s（补偿点 600ms，`ArmWakeupLocked` 按 MaxBufferMs=400ms 分两跳挂表）。
        // 这一项**不是**用来压竞态的：把超时调到任意大也修不好旧配置——那条分支缺的是 Backoff 本身，
        // 不是等得不够久。真正的修法是上面那个 200ms。
        await WaitUntilAsync(() => composer.Calls > 0, timeoutMs: 2_500);
        await harness.SettleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, composer.Calls);
        Assert.Equal(["woken"], harness.Sink.Delivered.Select(x => x.Text));
        // 全用例没有再提交任何输入：Turn 是门控自己到点醒来的，不是被第二条消息带起来的。
        Assert.Equal(1, harness.Store.AppendedInputs);
        Assert.Contains(harness.Runtime.TraceEntries, entry => entry.Action == "trigger:Backoff");
        await harness.Runtime.DisposeAsync();
    }

    [Fact]
    public async Task Reaching_the_frequency_threshold_opens_exactly_one_turn_for_the_whole_backlog()
    {
        var composer = new ControlledComposer((_, _, _) => Task.FromResult(ControlledComposer.Plan("batched")));
        var harness = await RuntimeHarness.CreateAsync(composer, replyFrequency: 0.5);

        await harness.Runtime.SubmitUserInputAsync(new RuntimeUserInput("第一段"),
            cancellationToken: TestContext.Current.CancellationToken);
        await harness.Runtime.SubmitUserInputAsync(new RuntimeUserInput("第二段"),
            cancellationToken: TestContext.Current.CancellationToken);
        await harness.SettleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, composer.Calls);
        Assert.Equal(["第一段", "第二段"], composer.Requests[0].CombinedText);
        await harness.Runtime.DisposeAsync();
    }
}

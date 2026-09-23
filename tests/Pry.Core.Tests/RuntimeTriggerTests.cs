using Pry.Core.Runtime;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// 触发层的确定性判据：到达节奏统计、频率折算阈值、回复必要性评分、计数门控与空闲退避。
/// 这一层全部是纯函数或纯状态机，所以用例直接给时间戳，不做任何真实等待。
/// </summary>
public sealed class RuntimeTriggerTests
{
    private static readonly DateTimeOffset T = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    private static DateTimeOffset At(int seconds) => T + TimeSpan.FromSeconds(seconds);

    private static ReplyNecessitySnapshot Snapshot(
        IReadOnlyList<string>? texts = null,
        int pending = 1,
        int threshold = 1,
        bool hasAt = false,
        bool hasMention = false,
        bool group = true,
        bool focus = false,
        int selfReplies = 0,
        int windowMessages = 0,
        double frequency = 1.0,
        TimeSpan idleFor = default,
        bool idleReached = false) =>
        new(texts ?? ["我在想你说的第二件事"], pending, threshold, hasAt, hasMention, group, focus, selfReplies,
            windowMessages, frequency, idleFor, idleReached, ["麦麦"]);

    // ---------------------------------------------------------------- 到达节奏

    [Fact]
    public void Burst_arrivals_refresh_idle_but_never_become_interval_samples()
    {
        var stats = new MessageIntervalStats(new StaticClock(T));
        stats.Record(At(0), fromCharacter: false);
        stats.Record(At(2), fromCharacter: false);
        stats.Record(At(3), fromCharacter: false);

        // 连发不该产生样本，但必须把"最后一次收到"推后：否则刷屏刚停就被判成已经空了很久。
        Assert.Equal(0, stats.SampleCount);
        Assert.Equal(At(3), stats.LastExternalAt);
        Assert.Equal(TimeSpan.FromSeconds(3), stats.IdleFor(At(6)));
    }

    [Fact]
    public void Spaced_arrivals_produce_samples_above_the_floor()
    {
        var stats = new MessageIntervalStats(new StaticClock(T));
        stats.Record(At(0), fromCharacter: false);
        stats.Record(At(60), fromCharacter: false);
        stats.Record(At(120), fromCharacter: false);

        Assert.Equal(2, stats.SampleCount);
        Assert.Equal(TimeSpan.FromSeconds(60), stats.AverageInterval(At(120)));
    }

    [Fact]
    public void Never_having_seen_a_message_is_not_the_same_as_having_no_samples()
    {
        var quiet = new MessageIntervalStats(new StaticClock(T));
        Assert.Null(quiet.AverageInterval(At(600)));

        var onlyBursts = new MessageIntervalStats(new StaticClock(T));
        onlyBursts.Record(At(0), fromCharacter: false);
        onlyBursts.Record(At(1), fromCharacter: false);
        // 见过消息但样本全被连发过滤掉时回退到下限：留一个保守估计，
        // 让待处理消息不至于因为没有统计值而永久挂起。
        Assert.Equal(TimeSpan.FromSeconds(30), onlyBursts.AverageInterval(At(600)));
    }

    [Fact]
    public void Fast_sessions_still_get_a_conservative_average()
    {
        var stats = new MessageIntervalStats(new StaticClock(T));
        stats.Record(At(0), fromCharacter: false);
        stats.Record(At(10), fromCharacter: false);
        stats.Record(At(20), fromCharacter: false);

        Assert.Equal(TimeSpan.FromSeconds(30), stats.AverageInterval(At(20)));
    }

    [Fact]
    public void Samples_older_than_the_window_stop_counting()
    {
        var stats = new MessageIntervalStats(new StaticClock(T));
        stats.Record(At(0), fromCharacter: false);
        stats.Record(At(300), fromCharacter: false);
        Assert.Equal(1, stats.SampleCount);

        stats.Record(At(300 + 16 * 60), fromCharacter: false);
        Assert.Equal(1, stats.SampleCount);
    }

    // ---------------------------------------------------------------- 频率折算

    [Fact]
    public void Zero_frequency_never_accumulates_a_turn()
    {
        Assert.Equal(0, ReplyFrequency.Threshold(0.0, ReplyTriggerMode.MessageCount));
        Assert.Equal(0, ReplyFrequency.Threshold(0.0, ReplyTriggerMode.ReplyNecessity));
    }

    [Fact]
    public void Lower_frequency_always_asks_for_more_messages()
    {
        var previous = 0;
        foreach (var frequency in new[] { 1.0, 0.5, 0.25, 0.1 })
        {
            var threshold = ReplyFrequency.Threshold(frequency, ReplyTriggerMode.MessageCount);
            Assert.True(threshold > previous, $"{frequency} should need more messages than the tier above");
            previous = threshold;
        }
    }

    [Fact]
    public void Necessity_mode_asks_for_a_much_bigger_backlog_as_frequency_falls()
    {
        // 计数模式把阈值当硬门槛；必要性模式把阈值当压力分的分母，所以它按频率平方反比增长。
        // 结果是低频角色在必要性模式下更难只靠"攒了几条"抬分，必须出现点名或真实内容。
        Assert.Equal(ReplyFrequency.Threshold(1.0, ReplyTriggerMode.MessageCount),
            ReplyFrequency.Threshold(1.0, ReplyTriggerMode.ReplyNecessity));
        Assert.True(ReplyFrequency.Threshold(0.3, ReplyTriggerMode.ReplyNecessity) >
                    ReplyFrequency.Threshold(0.3, ReplyTriggerMode.MessageCount));
    }

    // ---------------------------------------------------------------- 必要性评分

    [Fact]
    public void Being_addressed_directly_outweighs_every_content_signal()
    {
        var options = new ReplyNecessityOptions();
        var addressed = ReplyNecessityScorer.Score(Snapshot(hasAt: true, texts: ["在吗"]), options);
        var chatter = ReplyNecessityScorer.Score(Snapshot(texts: ["在吗"]), options);

        Assert.True(ReplyNecessityScorer.ShouldTrigger(addressed, options));
        Assert.False(ReplyNecessityScorer.ShouldTrigger(chatter, options));
    }

    [Fact]
    public void A_reply_to_everything_says_less_as_the_character_talks_more()
    {
        var options = new ReplyNecessityOptions();
        var baseline = ReplyNecessityScorer.Score(Snapshot(hasAt: true, texts: ["在吗"]), options);
        var dominating = ReplyNecessityScorer.Score(
            Snapshot(hasAt: true, texts: ["在吗"], selfReplies: 9, windowMessages: 10), options);

        Assert.True(dominating.Score < baseline.Score);
        Assert.Contains("存在感", dominating.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Frequency_scales_the_whole_score_rather_than_one_layer()
    {
        var options = new ReplyNecessityOptions();
        var loud = ReplyNecessityScorer.Score(Snapshot(texts: ["帮我写一个方案"], frequency: 1.0), options);
        var shy = ReplyNecessityScorer.Score(Snapshot(texts: ["帮我写一个方案"], frequency: 0.5), options);

        Assert.Equal(70, loud.Score);
        Assert.Equal(53, shy.Score);
    }

    [Fact]
    public void Pressure_grows_faster_once_the_backlog_reaches_the_threshold()
    {
        var options = new ReplyNecessityOptions();
        var one = ReplyNecessityScorer.Score(Snapshot(texts: ["在吗"], pending: 1, threshold: 10), options);
        var near = ReplyNecessityScorer.Score(Snapshot(texts: ["在吗"], pending: 10, threshold: 10), options);
        var far = ReplyNecessityScorer.Score(Snapshot(texts: ["在吗"], pending: 50, threshold: 10), options);
        var absurd = ReplyNecessityScorer.Score(Snapshot(texts: ["在吗"], pending: 5000, threshold: 10), options);

        Assert.True(near.Score > one.Score);
        Assert.True(far.Score > near.Score);
        // 刷屏只是同一句话在重复：超过封顶倍数之后压力分不再增长。
        Assert.Equal(far.Score, absurd.Score);
    }

    [Fact]
    public void Quoted_text_does_not_loan_the_reply_its_question_mark()
    {
        var options = new ReplyNecessityOptions();
        var quotedOnly = ReplyNecessityScorer.Score(
            Snapshot(texts: ["[回复了A的消息: 今天天气如何] 哈哈"]), options);
        var bareReaction = ReplyNecessityScorer.Score(Snapshot(texts: ["哈哈"]), options);

        // 剥掉引用外壳后只剩"哈哈"，两条应当同分，且都比有真实内容时低。
        Assert.Equal(bareReaction.Score, quotedOnly.Score);
        Assert.Contains("短反应", quotedOnly.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void A_message_addressed_to_another_assistant_is_not_our_business()
    {
        var options = new ReplyNecessityOptions { OtherAssistantNames = ["DeepSeek"] };
        var toOther = ReplyNecessityScorer.Score(
            Snapshot(texts: ["DeepSeek，帮我写一个方案"], threshold: 1), options);
        var toUs = ReplyNecessityScorer.Score(Snapshot(texts: ["帮我写一个方案"], threshold: 1), options);

        Assert.True(toUs.Score > toOther.Score);
        Assert.DoesNotContain("请求", toOther.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Private_chats_count_as_addressed_without_an_at_sign()
    {
        var options = new ReplyNecessityOptions();
        var privateChat = ReplyNecessityScorer.Score(Snapshot(group: false, texts: ["帮我写一个方案"]), options);
        var inGroup = ReplyNecessityScorer.Score(Snapshot(texts: ["帮我写一个方案"]), options);

        Assert.True(privateChat.Score > inGroup.Score);
        Assert.Contains("私聊", privateChat.Detail, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- 计数门控

    [Fact]
    public void Silence_alone_never_opens_a_turn_how_long_it_lasts()
    {
        var gate = new FrequencyThresholdTurnGate();
        var result = gate.Evaluate(new FrequencyGateInputs(0, 3, TimeSpan.FromMinutes(2),
            TimeSpan.FromHours(6)));

        Assert.Equal(TurnGateDecision.Wait, result.Decision);
        Assert.Contains("不允许纯沉默", result.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Idle_time_borrows_just_enough_weight_to_wake_once()
    {
        var gate = new FrequencyThresholdTurnGate();
        // 攒到 2/3 条，沉默 40 分钟（平均间隔 60s），等效条数被封顶在 threshold-1=2 → 2+2>=3 不成立。
        var capped = gate.Evaluate(new FrequencyGateInputs(2, 3, TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(2400)));
        Assert.Equal(TurnGateDecision.Trigger, capped.Decision);

        // 差额刚好等于还差几条乘以平均间隔，不会让角色多等一轮。
        var justShort = gate.Evaluate(new FrequencyGateInputs(1, 3, TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(60)));
        Assert.Equal(TurnGateDecision.Delay, justShort.Decision);
        Assert.Equal(TimeSpan.FromSeconds(60), justShort.Delay);
    }

    [Fact]
    public void Without_a_measured_cadence_the_gate_waits_instead_of_guessing()
    {
        var result = new FrequencyThresholdTurnGate().Evaluate(
            new FrequencyGateInputs(1, 3, null, TimeSpan.FromHours(1)));

        Assert.Equal(TurnGateDecision.Wait, result.Decision);
    }

    // ---------------------------------------------------------------- 空闲退避

    [Fact]
    public void Idle_backoff_only_applies_to_group_chats()
    {
        var backoff = new IdleBackoffController();
        for (var i = 0; i < 8; i++) backoff.NoteCycleEnded(At(i * 600), idleCycle: true, isGroupChat: false);

        Assert.Null(backoff.ShouldDelay(At(6000), pendingCount: 1, isGroupChat: false, focusActive: false));
        Assert.Equal(0, backoff.ConsecutiveIdleCycles);
    }

    [Fact]
    public void An_answered_turn_clears_the_backoff_immediately()
    {
        var backoff = new IdleBackoffController();
        for (var i = 0; i < 4; i++) backoff.NoteCycleEnded(At(i * 600), idleCycle: true, isGroupChat: true);
        Assert.NotNull(backoff.RemainingDelay(At(1800)));

        backoff.NoteCycleEnded(At(3000), idleCycle: false, isGroupChat: true);
        Assert.Equal(0, backoff.ConsecutiveIdleCycles);
        Assert.Null(backoff.RemainingDelay(At(3001)));
    }

    [Fact]
    public void Backoff_grows_exponentially_and_stops_at_the_cap()
    {
        var backoff = new IdleBackoffController(new IdleBackoffOptions
        {
            Base = TimeSpan.FromSeconds(10), Cap = TimeSpan.FromMinutes(1), StartCount = 1
        });
        var seen = new List<TimeSpan>();
        for (var i = 0; i < 8; i++)
        {
            backoff.NoteCycleEnded(At(i * 600), idleCycle: true, isGroupChat: true);
            seen.Add(backoff.RemainingDelay(At(i * 600)) ?? TimeSpan.Zero);
        }

        Assert.Equal(TimeSpan.FromSeconds(10), seen[0]);
        Assert.Equal(TimeSpan.FromSeconds(20), seen[1]);
        Assert.Equal(TimeSpan.FromSeconds(40), seen[2]);
        Assert.All(seen.Skip(4), value => Assert.Equal(TimeSpan.FromMinutes(1), value));
    }

    [Fact]
    public void Waiting_messages_outrank_a_backoff_window()
    {
        var backoff = new IdleBackoffController(new IdleBackoffOptions { BypassPendingCount = 6 });
        for (var i = 0; i < 8; i++) backoff.NoteCycleEnded(At(0), idleCycle: true, isGroupChat: true);
        var at = At(0);

        Assert.NotNull(backoff.ShouldDelay(at, 5, isGroupChat: true, focusActive: false));
        Assert.Null(backoff.ShouldDelay(at, 6, isGroupChat: true, focusActive: false));
        Assert.Null(backoff.ShouldDelay(at, 1, isGroupChat: true, focusActive: true));
    }

    private sealed class StaticClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

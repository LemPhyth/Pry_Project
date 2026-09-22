using System.Net;
using System.Runtime.CompilerServices;
using Pry.Client;
using Pry.Contracts;
using Xunit;

namespace Pry.Client.Tests;

/// <summary>
/// 这里是"该不该重连"唯一的判据处：两处界面循环都改成消费本 Runner 的状态与终止出口之后，
/// 禁令、退避、游标续传的回归只在这一处钉住即可（QA-P4-01 要的正是"被守住的那条循环就是运行的那条"）。
/// </summary>
public sealed class ConversationEventStreamRunnerTests
{
    [Fact]
    public async Task First_open_resumes_from_the_untouched_cursor()
    {
        var recorder = new Recorder();
        var runner = Build(recorder, (_, token) => OpensThenWaits([5], token));
        recorder.StopAfter(1);

        await runner.RunAsync(recorder.Cancellation.Token);

        Assert.Equal(new long[] { -1 }, recorder.After);
        Assert.Equal(5, runner.LastSequence);
    }

    [Fact]
    public async Task Reconnects_from_the_last_delivered_sequence()
    {
        var recorder = new Recorder();
        var attempts = 0;
        var runner = Build(recorder, (_, token) =>
        {
            attempts++;
            return attempts == 1 ? OpensThenThrows([5, 7, 6]) : OpensThenWaits([8], token);
        });
        recorder.StopAfter(4);

        await runner.RunAsync(recorder.Cancellation.Token);

        Assert.Equal(8, runner.LastSequence);
        Assert.Equal(new long[] { -1, 7 }, recorder.After);
    }

    [Fact]
    public async Task Backoff_grows_doubly_and_stops_growing_at_the_cap()
    {
        var recorder = new Recorder();
        var runner = Build(recorder, (_, _) => Failing());

        await runner.RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(PryStreamState.Offline, runner.State);
        Assert.Equal(
            new[]
            {
                TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(15)
            },
            recorder.Delays);
    }

    [Fact]
    public async Task Giving_up_raises_exactly_one_fault_for_the_interface_to_explain()
    {
        var recorder = new Recorder();
        var runner = Build(recorder, (_, _) => Failing());

        await runner.RunAsync(TestContext.Current.CancellationToken);

        Assert.Single(recorder.Faults);
        Assert.IsType<IOException>(recorder.Faults[0]);
        Assert.Equal(7, recorder.States.Count(state => state == PryStreamState.Reconnecting));
    }

    [Fact]
    public async Task A_stream_that_closes_without_events_retries_instead_of_spinning()
    {
        var recorder = new Recorder();
        var runner = Build(recorder, (_, _) => Empty());

        await runner.RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(7, recorder.Delays.Count);
        Assert.Equal(8, recorder.After.Count);
        Assert.Empty(recorder.Events);
    }

    [Fact]
    public async Task Recovery_announces_itself_once_so_the_ui_can_refetch_history()
    {
        var recorder = new Recorder();
        var attempts = 0;
        var runner = Build(recorder, (_, token) =>
        {
            attempts++;
            return attempts == 1 ? OpensThenThrows([3]) : OpensThenWaits([4], token);
        });
        recorder.StopAfter(2);

        await runner.RunAsync(recorder.Cancellation.Token);

        Assert.Equal(1, recorder.Resumed);
        Assert.Equal(
            new[] { PryStreamState.Connected, PryStreamState.Reconnecting, PryStreamState.Connected },
            recorder.States);
    }

    [Fact]
    public async Task The_first_connection_is_not_reported_as_a_recovery()
    {
        var recorder = new Recorder();
        var runner = Build(recorder, (_, token) => OpensThenWaits([1], token));
        recorder.StopAfter(1);

        await runner.RunAsync(recorder.Cancellation.Token);

        Assert.Equal(0, recorder.Resumed);
        Assert.Empty(recorder.Faults);
    }

    [Fact]
    public async Task Cancellation_ends_the_loop_without_a_fault()
    {
        var recorder = new Recorder();
        var runner = Build(recorder, (_, token) => OpensThenWaits([1, 2], token));
        recorder.StopAfter(2);

        await runner.RunAsync(recorder.Cancellation.Token);

        Assert.Empty(recorder.Faults);
        Assert.Equal(PryStreamState.Connected, runner.State);
    }

    [Fact]
    public void Options_describe_a_calmer_channel_than_the_default()
    {
        var options = new ConversationStreamOptions(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(6), 4);

        Assert.Equal(
            new[]
            {
                TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(6),
                TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(6)
            },
            Enumerable.Range(1, 5).Select(options.DelayForAttempt));
    }

    [Fact]
    public async Task A_handler_failure_is_treated_as_a_broken_connection_without_replaying_the_event()
    {
        using var cancellation = new CancellationTokenSource();
        var cursor = new List<long>();
        var seen = new List<long>();
        var delays = new List<TimeSpan>();
        var runner = new ConversationEventStreamRunner(
            (after, token) =>
            {
                cursor.Add(after);
                return OpensThenWaits(after == -1 ? [1, 2] : [3], token);
            },
            delay: (span, _) =>
            {
                delays.Add(span);
                return Task.CompletedTask;
            });
        runner.EventReceived = item =>
        {
            seen.Add(item.Sequence);
            // 界面侧失败（例如未读汇总请求出错）按断线处理：游标已推进，不重投同一条，
            // 否则一条必然失败的事件会把重连变成死循环；缺失内容由整段历史补取兜住。
            if (item.Sequence == 2) throw new InvalidOperationException("界面刷新失败");
            if (item.Sequence == 3) cancellation.Cancel();
            return Task.CompletedTask;
        };

        await runner.RunAsync(cancellation.Token);

        Assert.Equal(new[] { -1L, 2L }, cursor);
        Assert.Equal(new[] { 1L, 2L, 3L }, seen);
        Assert.Single(delays);
    }

    /// <summary>
    /// 禁行必须发生在第一次退避之前：顺序反了就会先排一轮延迟、把"版本不匹配"演成"网络不稳"，
    /// 而三种禁行码里只有两种登记在分类表里——未登记那一档证明判据是 <c>retryable:false</c>，不是码表。
    /// </summary>
    [Theory]
    [InlineData(PryProtocol.MismatchErrorCode, (HttpStatusCode)426)]
    [InlineData(PryProtocol.UnauthorizedErrorCode, HttpStatusCode.Unauthorized)]
    [InlineData(ProviderErrorCodes.Auth, HttpStatusCode.BadGateway)]
    public async Task A_banned_failure_stops_before_the_first_backoff(string code, HttpStatusCode status)
    {
        var recorder = new Recorder();
        var runner = Build(recorder, (_, _) => FailingWith(new PryBackendException(
            status, code, "服务端说明", null, false)));

        await runner.RunAsync(TestContext.Current.CancellationToken);

        Assert.Single(recorder.After);
        Assert.Empty(recorder.Delays);
        Assert.Single(recorder.Faults);
        Assert.Equal(PryStreamState.Offline, runner.State);
    }

    /// <summary>反方向同权重：服务端沉默或明确可重试都不是禁令，提前终止就是把抖动做成永久失败。</summary>
    [Theory]
    [InlineData("retryable 缺席", null)]
    [InlineData("retryable 为 true", true)]
    public async Task An_undeclared_or_retryable_failure_keeps_reconnecting(string because, bool? retryable)
    {
        var recorder = new Recorder();
        var failure = new PryBackendException(
            HttpStatusCode.ServiceUnavailable, ProviderErrorCodes.ModelUnavailable, "后端繁忙", null, retryable);
        var runner = Build(recorder, (_, _) => FailingWith(failure));

        await runner.RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(8, recorder.After.Count);
        Assert.Equal(7, recorder.Delays.Count);
        Assert.Single(recorder.Faults);
        Assert.True(recorder.Faults[0] == failure, $"{because}：未宣告的可重试性被当成了禁令");
    }

    private static ConversationEventStreamRunner Build(
        Recorder recorder,
        Func<long, CancellationToken, IAsyncEnumerable<ConversationEvent>> open)
    {
        var runner = new ConversationEventStreamRunner(
            (after, token) =>
            {
                recorder.After.Add(after);
                return open(after, token);
            },
            delay: (span, _) =>
            {
                recorder.Delays.Add(span);
                return Task.CompletedTask;
            });
        runner.EventReceived = async item =>
        {
            recorder.Events.Add(item);
            recorder.Seen(item);
            await Task.CompletedTask;
        };
        runner.StateChanged = recorder.States.Add;
        runner.Resumed = () =>
        {
            recorder.Resumed++;
            return Task.CompletedTask;
        };
        runner.Faulted = recorder.Faults.Add;
        return runner;
    }

    private static async IAsyncEnumerable<ConversationEvent> OpensThenWaits(
        long[] sequences,
        [EnumeratorCancellation] CancellationToken token)
    {
        foreach (var sequence in sequences) yield return Event(sequence);
        await Task.Delay(Timeout.Infinite, token);
    }

    private static async IAsyncEnumerable<ConversationEvent> OpensThenThrows(long[] sequences)
    {
        await Task.Yield();
        foreach (var sequence in sequences) yield return Event(sequence);
        throw new IOException("connection reset");
    }

    private static IAsyncEnumerable<ConversationEvent> Failing() => FailingWith(new IOException("connection reset"));

    private static async IAsyncEnumerable<ConversationEvent> FailingWith(Exception error)
    {
        await Task.Yield();
        throw error;
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }

    private static async IAsyncEnumerable<ConversationEvent> Empty()
    {
        await Task.Yield();
        yield break;
    }

    private static ConversationEvent Event(long sequence) =>
        new(sequence, "message.created", new DateTimeOffset(2026, 9, 20, 0, 0, 0, TimeSpan.Zero), new { });

    private sealed class Recorder
    {
        private int _remaining;

        public List<long> After { get; } = [];
        public List<TimeSpan> Delays { get; } = [];
        public List<PryStreamState> States { get; } = [];
        public List<ConversationEvent> Events { get; } = [];
        public List<Exception> Faults { get; } = [];
        public int Resumed;
        public CancellationTokenSource Cancellation { get; } = new();

        public void StopAfter(int events) => _remaining = events;

        public void Seen(ConversationEvent item)
        {
            if (_remaining > 0 && --_remaining == 0) Cancellation.Cancel();
        }
    }
}

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Pry.Api.Runtime;
using Pry.Api.Services;
using Pry.Contracts;
using Pry.Core.Runtime;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>PLAT-005 M0-b：SSE 心跳、Turn 终态事件与两端存活常量的契约。</summary>
public sealed class EventStreamHeartbeatTests
{
    [Fact]
    public async Task Idle_stream_emits_ping_frames_and_keeps_event_order()
    {
        var ct = TestContext.Current.CancellationToken;
        using var journal = await EventJournal.CreateAsync(ct);
        var log = journal.CreateLog();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        await using var frames = SseEventStream
            .FramesAsync(log.ReadAsync(0, cancellation.Token), TimeSpan.FromMilliseconds(30), cancellation.Token)
            .GetAsyncEnumerator(cancellation.Token);

        // 空闲的每个心跳周期都要有一帧注释，否则客户端分不清"会话安静"与"连接已死"。
        for (var ping = 0; ping < 2; ping++)
        {
            Assert.Equal(PryProtocol.HeartbeatFrame, await NextAsync(frames, ct));
        }

        // 电平触发类没有自己的序号，帧 id 复用当前持久头部，因此它不推进游标。
        log.Publish("turn.state", new { phase = "AgentSending" });
        var levelFrame = await NextTypedAsync(frames, ct, "turn.state");
        Assert.StartsWith("id: 0\nevent: turn.state\ndata: ", levelFrame, StringComparison.Ordinal);

        log.Publish("message.created", new { messageId = 7 });
        var eventFrame = await NextTypedAsync(frames, ct, "message.created");
        Assert.StartsWith("id: 1\nevent: message.created\ndata: ", eventFrame, StringComparison.Ordinal);
        Assert.Contains("\"sequence\":1", eventFrame, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Abandoning_the_stream_after_one_heartbeat_frame_releases_the_in_flight_cursor()
    {
        // DEF-P1WALK-03：编译器生成的流在自己那笔 MoveNextAsync 结束前被释放必定抛 NotSupportedException。
        // 心跳能递出就说明内层一定还在飞，所以这条路径的干净必须是构造出来的，不能靠机器安静时内层恰好先落地：
        // 这里的到达等待不认取消、也不会自己结束，放弃流的那一刻它是唯一还没落地的一笔。
        var ct = TestContext.Current.CancellationToken;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        await using var frames = SseEventStream
            .FramesAsync(QuietArrivalWait(Task.Delay(TimeSpan.FromSeconds(1), CancellationToken.None)),
                TimeSpan.FromMilliseconds(20), cancellation.Token)
            .GetAsyncEnumerator(cancellation.Token);

        Assert.Equal(PryProtocol.HeartbeatFrame, await NextAsync(frames, ct));
        // 离开作用域＝消费方放弃流：这里没有第二帧心跳可等、也没有事件到达，只有那笔在飞的 MoveNextAsync。
    }

    [Fact]
    public async Task Disconnect_after_one_heartbeat_frame_reports_cancellation_instead_of_a_dispose_failure()
    {
        // 生产侧唯一的离开路径是客户端断开（ChatController.cs:71 的 await foreach + 请求 token）。
        // 那一次取消必须原样出场：被 Dispose 的 NotSupportedException 顶掉之后，一次正常断连在日志里
        // 就与真实故障同形，下一次归因会被它带偏。内层的到达等待认这个 token，所以结算是有界的。
        var ct = TestContext.Current.CancellationToken;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var frames = SseEventStream
            .FramesAsync(QuietArrivalWait(Task.Delay(TimeSpan.FromSeconds(1), cancellation.Token)),
                TimeSpan.FromMilliseconds(20), cancellation.Token)
            .GetAsyncEnumerator(cancellation.Token);

        Assert.Equal(PryProtocol.HeartbeatFrame, await NextAsync(frames, ct));
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => frames.MoveNextAsync().AsTask());
        await frames.DisposeAsync();
    }

    [Fact]
    public async Task Settling_the_advance_before_release_still_releases_the_cursor()
    {
        // 判据 ① 是"先结算那笔 MoveNextAsync，再 DisposeAsync"两半。只把 Dispose 删掉的假修法在上面两条用例里
        // 都不会红（不抛、也不改离开原因），所以这里给内层记一笔账：让它停在"已交出一帧、尚未结束"的可释放状态。
        var ct = TestContext.Current.CancellationToken;
        var trace = new List<string>();
        var arrival = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using (var frames = SseEventStream
            .FramesAsync(GatedCursor(arrival.Task, trace), TimeSpan.FromMilliseconds(20), ct)
            .GetAsyncEnumerator(ct))
        {
            Assert.Equal(PryProtocol.HeartbeatFrame, await NextAsync(frames, ct));
            arrival.SetResult();
            var eventFrame = await NextTypedAsync(frames, ct, "message.created");
            Assert.StartsWith("id: 1\nevent: message.created\ndata: ", eventFrame, StringComparison.Ordinal);
        }

        // 释放发生过，而且只发生一次：内层的 finally 只能由 DisposeAsync 跑到（它还挂在这一帧之后的等待上）。
        Assert.Equal(["released"], trace);
    }

    [Fact]
    public void Default_heartbeat_sits_inside_the_tunable_band() =>
        Assert.InRange(PryProtocol.HeartbeatIntervalMs, 1_000, PryProtocol.LivenessTimeoutMs / 2);

    [Fact]
    public async Task Finished_stream_closes_without_padding_frames()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var frames = SseEventStream.FramesAsync(Empty(), TimeSpan.FromMilliseconds(30), ct)
            .GetAsyncEnumerator(ct);
        // 已经结束的游标不能被心跳撑成永不停流的假连接，否则上层的退避重连永远轮不到执行。
        Assert.False(await frames.MoveNextAsync());
    }

    [Theory]
    [InlineData(RuntimeTurnOutcome.Completed, true)]
    [InlineData(RuntimeTurnOutcome.PartiallyDelivered, true)]
    [InlineData(RuntimeTurnOutcome.Interrupted, false)]
    [InlineData(RuntimeTurnOutcome.Failed, false)]
    [InlineData(RuntimeTurnOutcome.DoNothing, false)]
    [InlineData(RuntimeTurnOutcome.Deferred, false)]
    [InlineData(RuntimeTurnOutcome.FencedOut, false)]
    public async Task Only_delivered_turns_publish_turn_completed(RuntimeTurnOutcome outcome, bool published)
    {
        var ct = TestContext.Current.CancellationToken;
        using var journal = await EventJournal.CreateAsync(ct);
        var log = journal.CreateLog();
        var sink = new ConversationRuntimeEventSink(log, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);

        await sink.TurnTransitionAsync("room", new TurnIdentity(7, 2, 0), outcome, "test", ct);

        // 取消与失败各有自己的终态事件；沉默、推迟、围栏淘汰什么都没交付，
        // 补发 turn.completed 会把它们显示成一次已完成的回复。
        Assert.Equal(published ? 1 : 0, log.CurrentSequence);
        if (!published) return;

        var item = Assert.Single(await TakeAsync(log, ct));
        Assert.Equal("turn.completed", item.Type);
        var payload = System.Text.Json.JsonSerializer.SerializeToElement(item.Data);
        Assert.Equal(7, payload.GetProperty("turnId").GetInt64());
        Assert.Equal(2, payload.GetProperty("generationId").GetInt64());
        Assert.Equal(outcome.ToString(), payload.GetProperty("outcome").GetString());
        Assert.Equal("test", payload.GetProperty("reason").GetString());
    }

    [Fact]
    public void Liveness_window_covers_at_least_two_heartbeat_periods()
    {
        // 客户端判定半开的时间窗必须长于两个服务端心跳，否则一次刷写延迟就会被误判成断连。
        Assert.True(PryProtocol.LivenessTimeoutMs >= PryProtocol.HeartbeatIntervalMs * 2,
            $"LivenessTimeoutMs={PryProtocol.LivenessTimeoutMs} 不足以覆盖两个 {PryProtocol.HeartbeatIntervalMs} 心跳周期");
    }

    [Fact]
    public async Task Heartbeat_frame_reaches_the_wire_ahead_of_any_event_and_stays_inside_the_band()
    {
        // 单元测试证不了刷写与夹取：注释帧必须真的穿过 Kestrel 到达客户端，且过密的配置不能把心跳变成忙等。
        var directory = Path.Combine(Path.GetTempPath(), $"pry-sse-heartbeat-{Guid.NewGuid():N}");
        var ct = TestContext.Current.CancellationToken;
        try
        {
            await using var app = await BackendApplication.BuildAsync(configure: builder =>
            {
                builder.Configuration["Pry:DataDirectory"] = directory;
                builder.Configuration["Pry:ModelsDirectory"] = Path.Combine(directory, "models");
                builder.Configuration["Pry:Url"] = "http://127.0.0.1:0";
                builder.Configuration["Pry:EventHeartbeatMs"] = "1";
            }, token: ct);
            await app.StartAsync(ct);
            await app.Services.GetRequiredService<Pry.Core.Memory.MemoryDatabase>()
                .EnsureConversationAsync("room", "character", ct);
            using var http = app.CreateBackendHttpClient();

            // Kestrel 要等第一次刷写才把响应头发出去，所以"收到响应头之后过了多久"量不到心跳节奏；
            // 计时必须从发起订阅开始，才能证明首帧确实等满了一个（被夹到 1 秒下限的）心跳周期。
            var clock = System.Diagnostics.Stopwatch.StartNew();
            using var response = await http.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, "api/v1/conversations/room/events?after=0"),
                HttpCompletionOption.ResponseHeadersRead, ct);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new System.IO.StreamReader(stream);
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
            deadline.CancelAfter(5000);
            var firstFrame = await reader.ReadLineAsync(deadline.Token);
            var firstFrameAt = clock.ElapsedMilliseconds;
            clock.Stop();

            Assert.Equal(":ping", firstFrame);
            // 配置写 1ms 仍按 1 秒下限发帧；夹取失效时首帧会在几十毫秒内到达并变成忙等。
            Assert.True(firstFrameAt >= 900, $"心跳未被夹到 1 秒下限，{firstFrameAt}ms 就收到了首帧。");
        }
        finally
        {
            SqliteTestPools.Clear(Path.Combine(directory, "memory.db"));
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task<string> NextTypedAsync(IAsyncEnumerator<string> frames, CancellationToken ct,
        string eventType)
    {
        // 心跳帧会插在任何事件前面，取"下一条指定类型的事件"必须跳过它们，否则时序判据永远量到注释帧。
        while (true)
        {
            var frame = await NextAsync(frames, ct);
            if (frame == PryProtocol.HeartbeatFrame) continue;
            if (frame.Contains($"event: {eventType}\n", StringComparison.InvariantCulture))
            {
                Assert.EndsWith("\n\n", frame, StringComparison.Ordinal);
                return frame;
            }
        }
    }

    private static async Task<string> NextAsync(IAsyncEnumerator<string> frames, CancellationToken ct)
    {
        // 超时必须真的作用在等待上：只创建 CancelAfter 的源而不把它交给 MoveNextAsync，
        // 缺帧时测试不会失败，而是把整个测试宿主挂死到 blame 的非活动上限。
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(3000);
        try
        {
            Assert.True(await frames.MoveNextAsync().AsTask().WaitAsync(deadline.Token), "未在预期窗口内收到帧");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("未在预期窗口内收到帧");
        }
        return frames.Current;
    }

    /// <summary>
    /// 一上来就结束的游标，用于区分"没有事件"与"仍在等待事件"。
    /// 这里刻意用已完成的 await 而不是 <c>Task.Yield()</c>：线程换手会让 30 ms 的心跳抢在那笔推进落地之前，
    /// "已经结束的游标不该被心跳撑长"这条判据就变成去赢一场比赛（#27 防复发 ③）。
    /// </summary>
    private static async IAsyncEnumerable<ConversationEvent> Empty()
    {
        await Task.CompletedTask;
        yield break;
    }

    /// <summary>
    /// 安静会话的内层游标：一笔只由 <paramref name="arrival"/> 放行的到达等待。这里刻意不用
    /// <c>[EnumeratorCancellation]</c>——那个特性会让参数拿到 <c>GetAsyncEnumerator</c> 的 token 而丢掉实参，
    /// "内层此刻一定还在飞"就必须由调用方交进来的这笔任务决定，而不是由游标的取消 token 决定。
    /// </summary>
    private static async IAsyncEnumerable<ConversationEvent> QuietArrivalWait(Task arrival)
    {
        await arrival;
        yield break;
    }

    /// <summary>
    /// 门控内层游标：推进停在 <paramref name="arrival"/> 上，交出一帧事件后再挂到一次不会结束的等待上，
    /// 并把释放记进 <paramref name="trace"/>。停在"已交出、尚未结束"的那一刻正是 <c>DisposeAsync</c> 唯一能跑到
    /// <c>finally</c> 的状态，因此这条 trace 量的就是游标有没有真的被释放。
    /// </summary>
    private static async IAsyncEnumerable<ConversationEvent> GatedCursor(Task arrival, List<string> trace)
    {
        try
        {
            await arrival;
            yield return new ConversationEvent(1, "message.created", DateTimeOffset.UtcNow, new { messageId = 1 });
            await Task.Delay(Timeout.InfiniteTimeSpan, CancellationToken.None);
        }
        finally
        {
            trace.Add("released");
        }
    }

    private static async Task<List<ConversationEvent>> TakeAsync(ConversationEventLog log, CancellationToken ct)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(1000);
        var taken = new List<ConversationEvent>();
        try
        {
            await foreach (var item in log.ReadAsync(0, deadline.Token))
            {
                taken.Add(item);
                break;
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { }
        return taken;
    }
}

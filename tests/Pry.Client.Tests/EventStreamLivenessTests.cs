using System.Diagnostics;
using System.Net;
using System.Text;
using Pry.Client;
using Pry.Contracts;
using Xunit;

namespace Pry.Client.Tests;

/// <summary>PLAT-005 M0-b：客户端把"读到字节"当作存活信号，半开连接必须被检出而不是永远阻塞。</summary>
public sealed class EventStreamLivenessTests
{
    private static readonly TimeSpan Window = TimeSpan.FromMilliseconds(200);

    [Fact]
    public async Task Silent_stream_is_declared_half_open_instead_of_blocking_forever()
    {
        // 服务端不发也不关时，读取本身永远不会报错；没有这条判定，上层退避重连就没有触发机会。
        await using var events = Enumerate(Client(new TimedChunkStream([(30_000, "")])));

        var error = await Assert.ThrowsAsync<TimeoutException>(() => events.MoveNextAsync().AsTask());
        Assert.Contains("事件流", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Pings_spaced_within_the_window_keep_the_subscription_alive()
    {
        // 六个心跳合计已经超过三个存活窗口：能安全读到 EOF，就说明每一帧注释都重置了存活判定。
        var script = Enumerable.Range(0, 6).Select(_ => (40, PryProtocol.HeartbeatFrame)).ToList();
        var clock = Stopwatch.StartNew();
        var received = new List<ConversationEvent>();
        await using var events = Enumerate(Client(new TimedChunkStream(script)));
        while (await events.MoveNextAsync()) received.Add(events.Current);
        clock.Stop();

        Assert.Empty(received);
        Assert.True(clock.Elapsed >= Window, $"注释帧只撑起了 {clock.ElapsedMilliseconds}ms，存活窗口未被重置。");
    }

    [Fact]
    public async Task Bytes_sparser_than_the_window_still_expire()
    {
        // 存活窗口按行计量，不按"连接曾经活着"计量：只发过一次心跳的半开连接仍然要被判死。
        await using var events = Enumerate(Client(new TimedChunkStream(
            [(40, PryProtocol.HeartbeatFrame), (5_000, PryProtocol.HeartbeatFrame)])));

        await Assert.ThrowsAsync<TimeoutException>(() => events.MoveNextAsync().AsTask());
    }

    [Fact]
    public async Task Comment_frames_do_not_disturb_event_parsing_or_break_on_unknown_types()
    {
        var payload = new StringBuilder()
            .Append(PryProtocol.HeartbeatFrame)
            .Append(Event(7, "turn.state", "{\"state\":\"Idle\"}"))
            .Append(PryProtocol.HeartbeatFrame)
            .Append(Event(8, "provider.future.event", "{\"whatever\":1}"))
            .ToString();
        var received = new List<ConversationEvent>();
        await using var events = Enumerate(Client(new TimedChunkStream([(0, payload)])));
        while (await events.MoveNextAsync()) received.Add(events.Current);

        // 注释帧不是事件；未识别的事件类型必须原样交给上层忽略，而不是在传输层断连。
        Assert.Equal(new long[] { 7, 8 }, received.Select(x => x.Sequence).ToArray());
        Assert.Equal("provider.future.event", received[1].Type);
    }

    private static PryBackendClient Client(Stream stream)
    {
        var http = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(stream)
        })) { BaseAddress = new Uri("http://127.0.0.1:5078/") };
        return new PryBackendClient(http);
    }

    private static IAsyncEnumerator<ConversationEvent> Enumerate(PryBackendClient client) =>
        client.ReadEventsAsync("room", 0, TestContext.Current.CancellationToken, Window)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);

    private static string Event(long sequence, string type, string data) =>
        $"id: {sequence}\nevent: {type}\ndata: {{\"sequence\":{sequence},\"type\":\"{type}\",\"occurredAt\":\"2026-09-21T00:00:00Z\",\"data\":{data}}}\n\n";

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) =>
            Task.FromResult(response(request));
    }

    /// <summary>按脚本先等待再吐字节的响应流，用来分别构造"什么都不发"和"只有心跳"的连接。</summary>
    private sealed class TimedChunkStream(IReadOnlyList<(int DelayMs, string Frame)> script) : Stream
    {
        private int offset;
        private byte[]? pending;
        private int step;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
        {
            while (pending is null)
            {
                if (step >= script.Count) return 0;
                var (delay, frame) = script[step++];
                if (delay > 0) await Task.Delay(delay, token);
                pending = Encoding.UTF8.GetBytes(frame);
            }

            var take = Math.Min(pending.Length - offset, buffer.Length);
            if (take == 0) return 0;
            pending.AsMemory(offset, take).CopyTo(buffer);
            offset += take;
            if (offset == pending.Length) { pending = null; offset = 0; }
            return take;
        }
    }
}

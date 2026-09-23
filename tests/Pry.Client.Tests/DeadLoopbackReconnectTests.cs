using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Pry.Client;
using Xunit;

namespace Pry.Client.Tests;

/// <summary>
/// 走查 ③b 超档的那十几秒不在退避阶梯里，而在"被拒的回环握手"上：这台机器一次 RST 已经回来、
/// connect 却压在 TCP 层要 ~2.05 秒。订阅前的存活探测只省掉明知会被拒的那几次握手，
/// 所以这里逐条钉住它没有动过的东西：轮数与阶梯、失败的形状与界面档位、端口复活后必须回到真连接
/// （连接被拒不是快速失败），以及探测只在能肯定"本机没人监听这个地址"时才生效。
/// </summary>
public sealed class DeadLoopbackReconnectTests
{
    private static readonly TimeSpan[] DefaultLadder =
    [
        TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(15)
    ];

    /// <summary>探测与真握手的唯一可观测差别就是这句报错：前者没打出去握手，后者是 .NET 说的。</summary>
    private const string ProbeMark = "监听表";

    [Fact]
    public async Task The_first_subscription_is_a_real_handshake_and_only_later_ones_are_skipped()
    {
        var port = UnusedLoopbackPort();
        var client = StreamClient(port);
        var total = TimeSpan.Zero;
        var handshake = TimeSpan.Zero;

        for (var attempt = 1; attempt <= 8; attempt++)
        {
            var clock = Stopwatch.StartNew();
            var error = await Record.ExceptionAsync(() => FirstEventAsync(client));
            clock.Stop();
            total += clock.Elapsed;
            Assert.IsType<HttpRequestException>(error);
            // 形状一律不变：界面靠内层 SocketException 才落到"本地核心没有启动成功"那一档。
            Assert.IsType<SocketException>(error!.InnerException);
            Assert.Equal(PryErrorKind.LocalCoreUnavailable, PryErrorPresenter.Describe(error).Kind);

            var skipped = error.Message.Contains(ProbeMark, StringComparison.Ordinal);
            // 第一次永远真打：客户端不能"没试过就判死"，否则版本不匹配的禁令也听不到了。
            Assert.True(attempt > 1 == skipped, $"第 {attempt} 次订阅的来历判错了：{error.Message}");
            if (attempt == 1) handshake = clock.Elapsed; // 本机一次真被拒握手的成本，现算而不写死秒数。
        }

        Assert.True(total < handshake + TimeSpan.FromSeconds(1),
            $"八轮总共 {total.TotalSeconds:0.00}s，本机一次握手 {handshake.TotalSeconds:0.00}s：明知会被拒的握手又被打出去了。");
    }

    [Fact]
    public async Task Eight_attempts_still_count_eight_and_keep_the_default_ladder()
    {
        var port = UnusedLoopbackPort();
        var opens = 0;
        var delays = new List<TimeSpan>();
        var faults = new List<Exception>();
        var client = StreamClient(port);
        var runner = new ConversationEventStreamRunner(
            (after, token) =>
            {
                opens++;
                return client.ReadEventsAsync("room", after, token);
            },
            delay: (span, _) =>
            {
                delays.Add(span);
                return Task.CompletedTask;
            });
        runner.Faulted = faults.Add;

        await runner.RunAsync(TestContext.Current.CancellationToken);

        // 八轮还是八轮、阶梯一格没动：探测省的是握手，不是 PLT-002/PLT-006 的判据本体。
        Assert.Equal(8, opens);
        Assert.Equal(DefaultLadder, delays);
        Assert.IsType<HttpRequestException>(Assert.Single(faults));
    }

    [Fact]
    public async Task A_port_that_starts_listening_again_gets_a_real_handshake()
    {
        var port = UnusedLoopbackPort();
        var client = StreamClient(port);

        // 先留下"这个端口拒过我们"的那次记录，再让同一个端口重新有人服务。
        await Assert.ThrowsAsync<HttpRequestException>(() => FirstEventAsync(client));

        using var backend = ScriptedSseListener.Start(port, respond: true, EventFrame(5));
        Assert.True(LoopbackEndpointLiveness.IsListening("127.0.0.1", port),
            "前提不成立：后端已经在这个端口上监听，本机监听表却还看不见它。");

        // 快速失败会把这一步也一起省掉，那样"后端重启后再连"就永远轮不到真握手。
        Assert.Equal(5, (await FirstEventAsync(client)).Sequence);
    }

    [Fact]
    public async Task A_listener_that_never_answers_is_still_judged_by_the_request_budget()
    {
        var port = UnusedLoopbackPort();
        // ③c 的半开形状：端口有人接、永不作答。它一直待在监听表里，所以探测没有插手判断的余地。
        using var halfOpen = ScriptedSseListener.Start(port, respond: false);
        Assert.True(LoopbackEndpointLiveness.IsListening("127.0.0.1", port));

        var client = StreamClient(port, TimeSpan.FromMilliseconds(200));
        var error = await Assert.ThrowsAsync<TimeoutException>(() => FirstEventAsync(client));
        Assert.Contains("事件流订阅", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_dead_loopback_port_is_the_only_shape_the_probe_may_answer_for()
    {
        var port = UnusedLoopbackPort();
        Assert.False(LoopbackEndpointLiveness.IsListening("127.0.0.1", port));
    }

    [Fact]
    public void A_loopback_listener_is_reported_as_listening()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            Assert.True(LoopbackEndpointLiveness.IsListening("127.0.0.1", ((IPEndPoint)listener.LocalEndpoint).Port));
        }
        finally { listener.Stop(); }
    }

    /// <summary>通配监听（0.0.0.0 / ::）也吃回环连接，按"有人监听"回答才是保守的一侧。</summary>
    [Fact]
    public void A_wildcard_listener_counts_as_listening_for_the_loopback_address()
    {
        var listener = new TcpListener(IPAddress.Any, 0);
        listener.Start();
        try
        {
            Assert.True(LoopbackEndpointLiveness.IsListening("127.0.0.1", ((IPEndPoint)listener.LocalEndpoint).Port));
        }
        finally { listener.Stop(); }
    }

    /// <summary>答不了的一律答"有监听者"：把"查不到"读成"没人监听"就是那条禁止的快速失败。</summary>
    [Theory]
    [InlineData("localhost", 5078)]      // 主机名要先解析，本机监听表回答不了它
    [InlineData("192.0.2.1", 80)]        // 非回环：可能是远端，监听表只说明本机
    [InlineData("fe80::1%ethernet", 80)] // 连地址都解析不成回环目标
    [InlineData("127.0.0.1", 0)]
    [InlineData("127.0.0.1", 70000)]
    public void An_ambiguous_target_is_always_answered_as_listening(string host, int port)
    {
        Assert.True(LoopbackEndpointLiveness.IsListening(host, port));
    }

    private static int UnusedLoopbackPort()
    {
        for (var attempt = 1; ; attempt++)
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            // 刚空出来的端口可能被别的进程立刻抢走，抢不到就再要一个：前提不成立的端口判不出产品。
            if (!LoopbackEndpointLiveness.IsListening("127.0.0.1", port)) return port;
            Assert.True(attempt < 5, "连续五个空闲端口都被占用，本机端口池状态异常。");
        }
    }

    private static PryBackendClient StreamClient(int port, TimeSpan? budget = null) => new(new HttpClient
    {
        BaseAddress = new Uri($"http://127.0.0.1:{port}/"),
        Timeout = TimeSpan.FromMilliseconds(PryProtocol.RequestBudgetCeilingMs)
    }, requestBudget: budget);

    private static async Task<ConversationEvent> FirstEventAsync(PryBackendClient client)
    {
        await using var events = client.ReadEventsAsync("room", -1, TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        Assert.True(await events.MoveNextAsync());
        return events.Current;
    }

    private static string EventFrame(long sequence) =>
        $"id: {sequence}\nevent: message.created\ndata: {{\"sequence\":{sequence},\"type\":\"message.created\"," +
        "\"occurredAt\":\"2026-09-21T00:00:00Z\",\"data\":{}}\n\n";

    /// <summary>
    /// 真 socket 上的最小事件流服务。只负责"这个端口有人接、是否作答"两件事，不实现 HTTP 语义：
    /// 这里的判据是客户端拿到响应头与否，不是报文格式。
    /// </summary>
    private sealed class ScriptedSseListener(int port, bool respond, string frames) : IDisposable
    {
        private readonly CancellationTokenSource shutdown = new();
        private readonly TcpListener listener = new(IPAddress.Loopback, port);

        public static ScriptedSseListener Start(int port, bool respond, string frames = "")
        {
            var server = new ScriptedSseListener(port, respond, frames);
            server.listener.Start();
            _ = server.RunAsync();
            return server;
        }

        private async Task RunAsync()
        {
            while (!shutdown.IsCancellationRequested)
            {
                TcpClient client;
                try { client = await listener.AcceptTcpClientAsync(shutdown.Token); }
                // Stop() 会让在途的 accept 抛异常：服务已经决定结束，这里不需要再往上报。
                catch (Exception error) when (error is IOException or SocketException or OperationCanceledException)
                {
                    return;
                }
                _ = ServeAsync(client);
            }
        }

        private async Task ServeAsync(TcpClient client)
        {
            using (client)
            {
                try
                {
                    var stream = client.GetStream();
                    await DiscardRequestHeadAsync(stream);
                    if (respond)
                    {
                        await stream.WriteAsync(Encoding.ASCII.GetBytes(
                            "HTTP/1.1 200 OK\r\nContent-Type: text/event-stream\r\nConnection: close\r\n\r\n"),
                            shutdown.Token);
                        await stream.WriteAsync(Encoding.UTF8.GetBytes(frames), shutdown.Token);
                        await stream.FlushAsync(shutdown.Token);
                    }
                    // 保持到测试结束：要么是一条活着的订阅，要么是一个收下请求却永不作答的连接。
                    await Task.Delay(Timeout.Infinite, shutdown.Token);
                }
                catch (Exception error) when (error is IOException or SocketException or OperationCanceledException) { }
            }
        }

        /// <summary>
        /// 读到请求头结束就丢：这一层不解析 HTTP，但必须真的把报文读干净，
        /// 否则 HttpClient 会以为对端没读完它的请求而直接断掉，测出来的就不是"有没有响应"。
        /// </summary>
        private async Task DiscardRequestHeadAsync(Stream stream)
        {
            var head = new byte[4096];
            var length = 0;
            while (length < head.Length)
            {
                var read = await stream.ReadAsync(head.AsMemory(length), shutdown.Token);
                if (read == 0) return; // 对端不再发了：请求头到哪儿算哪儿。
                length += read;
                if (Encoding.UTF8.GetString(head, 0, length).Contains("\r\n\r\n", StringComparison.Ordinal)) return;
            }
        }

        public void Dispose()
        {
            shutdown.Cancel();
            listener.Stop();
            shutdown.Dispose();
        }
    }
}

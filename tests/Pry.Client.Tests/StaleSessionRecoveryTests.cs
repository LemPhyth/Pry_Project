using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Pry.Contracts;
using Xunit;

namespace Pry.Client.Tests;

/// <summary>
/// `DEF-P1WALK-05` 的双向判据：后端被换掉之后，活着的那个客户端只是握着旧钥匙——
/// <b>401 且本机端口仍在监听</b>时它允许按需重读一次 <c>runtime.json</c>，令牌确实变了就用新令牌重发那一笔；
/// 令牌没变则这一声 401 仍是禁令，文案、零退避与 <c>RetryForbidden</c> 一概照旧（`PLT-002`／`PLT-006` 的判据本体）。
/// <para>
/// 这里用假后端而不是真进程：要判的是"两种 401 在客户端眼里不再同形"这一条判据本身，
/// 而真进程用例（同端口换令牌重起、同一个客户端实例自行恢复）在 <c>Pry.Api.Tests</c> 的
/// <c>BackendSwapRecoveryProcessTests</c>——客户端测试项目引用不到后端，那条只能在那边跑。
/// </para>
/// </summary>
public sealed class StaleSessionRecoveryTests : IDisposable
{
    private const string OldToken = "old-token-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string NewToken = "new-token-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    private readonly string _directory;
    private readonly IdleProcess _owner;
    private readonly TcpPort _port;
    private readonly List<HttpClient> _clients = [];

    public StaleSessionRecoveryTests()
    {
        // 描述符的读取侧只认"pid 还活着"的入口，所以这里必须真起一个旁观进程；
        // 端口也必须是本机监听表里真存在的——判据①的前半问的就是这张表。
        _owner = IdleProcess.Start();
        _port = TcpPort.Open();
        _directory = Path.Combine(Path.GetTempPath(), $"pry-stale-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        WriteDescriptor(OldToken);
    }

    [Fact]
    public async Task A_stale_token_re_reads_the_descriptor_and_re_issues_that_request()
    {
        // 后端换了：文件里此刻是那枚新令牌，而这个客户端头上还挂着旧的那一枚。
        WriteDescriptor(NewToken);
        var gate = new TokenGateHandler((token, _) => token == NewToken
            ? Json(HttpStatusCode.OK, "[]")
            : Unauthorized());
        var client = Discovered(gate, out var restored);

        var rooms = await client.GetConversationsAsync(token: TestContext.Current.CancellationToken);

        Assert.Empty(rooms);
        Assert.Equal([OldToken, NewToken], gate.Tokens);
        Assert.Equal(1, restored.Count);
    }

    [Fact]
    public async Task An_unchanged_token_stays_a_ban_with_no_reissue()
    {
        // 文件里还是同一枚令牌：这就是"这个客户端真的没有权限"，不是"入口换了钥匙"。
        var gate = new TokenGateHandler((_, _) => Unauthorized());
        var client = Discovered(gate, out var restored);

        var error = await Assert.ThrowsAsync<PryBackendException>(() =>
            client.GetConversationsAsync(token: TestContext.Current.CancellationToken));

        Assert.True(error.RetryForbidden);
        Assert.Equal(PryProtocol.UnauthorizedErrorCode, error.Code);
        Assert.Equal([OldToken], gate.Tokens);
        Assert.Equal(0, restored.Count);
        // 呈现表仍然是那一档禁行文案：自愈不能把禁令读成"稍后重试"。
        var notice = PryErrorPresenter.Describe(error);
        Assert.Equal(PryErrorKind.SessionRejected, notice.Kind);
        Assert.False(notice.CanRetry);
    }

    [Fact]
    public async Task A_port_with_no_listener_is_never_re_read()
    {
        // 描述符里换好了新令牌，但判"要不要重读"的那一刻端口已经没人监听 ⇒ 重读接不上，
        // 禁令照旧、一次文件都不读；那一档仍由"连接被拒"与退避阶梯说话，不归这里管。
        WriteDescriptor(NewToken);
        var gate = new TokenGateHandler((_, _) =>
        {
            _port.Stop();
            return Unauthorized();
        });
        var client = Discovered(gate, out var restored);

        var error = await Assert.ThrowsAsync<PryBackendException>(() =>
            client.GetConversationsAsync(token: TestContext.Current.CancellationToken));

        Assert.True(error.RetryForbidden);
        Assert.Equal([OldToken], gate.Tokens);
        Assert.Equal(0, restored.Count);
    }

    [Fact]
    public async Task Only_a_discovered_session_can_recover_the_embedded_path_is_unchanged()
    {
        // 内嵌拓扑的 HttpClient 由同进程后端直接交出（EmbeddedBackend 自己挂令牌头），不经发现，
        // 因此没有"可重读的入口"这件事。描述符里放着新令牌：自愈若错接到这条路径上，这一笔就会拿到 200 而不是禁令。
        WriteDescriptor(NewToken);
        var gate = new TokenGateHandler((_, _) => Unauthorized());
        using var http = new HttpClient(gate) { BaseAddress = new Uri($"http://127.0.0.1:{_port.Port}/") };
        http.DefaultRequestHeaders.Add(PryProtocol.SessionTokenHeader, OldToken);
        var client = new PryBackendClient(http);

        var error = await Assert.ThrowsAsync<PryBackendException>(() =>
            client.GetConversationsAsync(token: TestContext.Current.CancellationToken));

        Assert.True(error.RetryForbidden);
        Assert.Equal([OldToken], gate.Tokens);
    }

    [Fact]
    public async Task A_rejection_after_the_swap_stops_at_one_reissue()
    {
        // 换入口之后仍然被拒：重发一次就到头，不能演成"每次 401 都再读一遍文件"的常驻轮询。
        WriteDescriptor(NewToken);
        var gate = new TokenGateHandler((_, _) => Unauthorized());
        var client = Discovered(gate, out var restored);

        var error = await Assert.ThrowsAsync<PryBackendException>(() =>
            client.GetConversationsAsync(token: TestContext.Current.CancellationToken));

        Assert.True(error.RetryForbidden);
        Assert.Equal([OldToken, NewToken], gate.Tokens);
        Assert.Equal(1, restored.Count);
    }

    [Fact]
    public async Task A_healthy_request_does_not_go_looking_for_a_new_entry()
    {
        // "按需"的另一半：没被拒就一次文件都不读。这里描述符里已经放着一枚不同的令牌——
        // 实现若改成"每笔请求都去核对一下入口"（也就是轮询的形状），这一笔会当场换入口并宣告恢复。
        WriteDescriptor(NewToken);
        var gate = new TokenGateHandler((token, _) => token == OldToken
            ? Json(HttpStatusCode.OK, "[]")
            : Unauthorized());
        var client = Discovered(gate, out var restored);

        var rooms = await client.GetConversationsAsync(token: TestContext.Current.CancellationToken);

        Assert.Empty(rooms);
        Assert.Equal([OldToken], gate.Tokens);
        Assert.Equal(0, restored.Count);
    }

    [Fact]
    public async Task Requests_issued_before_the_swap_do_not_turn_into_a_false_ban()
    {
        // 并发形状：两笔都带着旧令牌发出去，第一笔换好入口之后第二笔才判"要不要重读"。
        // 若判据写成"文件里的令牌跟我现在挂的一样吗"，第二笔会把自己发早了的那声 401 读成真拒绝——假禁令。
        var bothInFlight = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var inFlight = 0;
        var gate = new TokenGateHandler((token, _) =>
        {
            if (token != OldToken) return Json(HttpStatusCode.OK, "[]");
            if (Interlocked.Increment(ref inFlight) == 2) bothInFlight.TrySetResult();
            else bothInFlight.Task.Wait(TimeSpan.FromSeconds(10));
            return Unauthorized();
        });
        WriteDescriptor(NewToken);
        var client = Discovered(gate, out var restored);

        // 各占一条线程池线程：闸门里那句等待会堵住发请求的那条线，同一条线程上先发的那笔永远等不到第二笔。
        var first = Task.Run(() => client.GetConversationsAsync(token: TestContext.Current.CancellationToken));
        var second = Task.Run(() => client.GetConversationsAsync(token: TestContext.Current.CancellationToken));
        var rooms = await Task.WhenAll(first, second);

        Assert.All(rooms, room => Assert.Empty(room));
        Assert.Equal(2, gate.Tokens.Count(token => token == OldToken));
        Assert.Equal(2, gate.Tokens.Count(token => token == NewToken));
        // 一次重启只换一次入口，也只宣告一次恢复。
        Assert.Equal(1, restored.Count);
    }

    [Fact]
    public async Task The_event_plane_redials_before_it_gives_up()
    {
        // 事件面的订阅同样走那一个判据点：第一次订阅带着旧令牌被拒，重读换入口后第二次订阅真的把事件送回来。
        WriteDescriptor(NewToken);
        var frame = """data: {"sequence":3,"type":"message.created","occurredAt":"2026-10-19T00:00:00+00:00","data":{"kind":"none"}}""";
        var gate = new TokenGateHandler((token, _) => token == NewToken
            ? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(frame + "\n\n", Encoding.UTF8, "text/event-stream")
            }
            : Unauthorized());
        var client = Discovered(gate, out var restored);

        var sequences = new List<long>();
        await foreach (var item in client.ReadEventsAsync("room-1", 2,
                       TestContext.Current.CancellationToken, TimeSpan.FromSeconds(5)))
            sequences.Add(item.Sequence);

        Assert.Equal([3L], sequences);
        Assert.Equal([OldToken, NewToken], gate.Tokens);
        Assert.Equal(1, restored.Count);
    }

    /// <summary>
    /// 走真的装配点：<see cref="RuntimeDiscovery.CreateClient"/> 一次给出端口、兜底、令牌头和这条会话的来历，
    /// 用例只把管道换成假后端。自己 <c>new HttpClient</c> 再补一句登记会把"漏了哪一半"这个缺陷形状测不出来。
    /// </summary>
    private PryBackendClient Discovered(TokenGateHandler gate, out RestorationCounter restored)
    {
        // 交给夹具统一收尾：PryBackendClient 本身不是 IDisposable，而换入口时旧的那条链路是故意不就地 Dispose 的。
        var http = RuntimeDiscovery.CreateClient(
            new RuntimeDescriptor(_owner.Id, _port.Port, OldToken, PryProtocol.Version, 9, DateTimeOffset.UtcNow),
            _directory, gate);
        _clients.Add(http);
        var counter = new RestorationCounter();
        var client = new PryBackendClient(http);
        client.SessionRestored = counter.Increment;
        restored = counter;
        return client;
    }

    private void WriteDescriptor(string token) => File.WriteAllText(
        RuntimeDiscovery.DescriptorPath(_directory),
        JsonSerializer.Serialize(new RuntimeDescriptor(_owner.Id, _port.Port, token, PryProtocol.Version, 9,
            DateTimeOffset.UtcNow), new JsonSerializerOptions(JsonSerializerDefaults.Web)));

    private static HttpResponseMessage Unauthorized() => new(HttpStatusCode.Unauthorized)
    {
        Content = new StringContent(
            $$"""{"type":"urn:pry:error:{{PryProtocol.UnauthorizedErrorCode}}","title":"Denied","status":401,"detail":"请求未携带有效的 X-Pry-Token。","code":"{{PryProtocol.UnauthorizedErrorCode}}","traceId":"00-stale","retryable":false}""",
            Encoding.UTF8, "application/problem+json")
    };

    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    public void Dispose()
    {
        foreach (var client in _clients) client.Dispose();
        _owner.Dispose();
        _port.Dispose();
        try { Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private sealed class RestorationCounter
    {
        private int _count;
        public int Count => Volatile.Read(ref _count);
        public void Increment() => Interlocked.Increment(ref _count);
    }

    /// <summary>只认令牌的假后端：每一笔请求挂出去的令牌都记下来，答什么由用例给的那句判据决定。</summary>
    private sealed class TokenGateHandler(Func<string?, int, HttpResponseMessage> respond) : HttpMessageHandler
    {
        private readonly ConcurrentQueue<string?> _tokens = new();

        public IReadOnlyList<string?> Tokens => _tokens.ToArray();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var sent = request.Headers.TryGetValues(PryProtocol.SessionTokenHeader, out var values)
                ? values.FirstOrDefault()
                : null;
            _tokens.Enqueue(sent);
            var response = respond(sent, _tokens.Count);
            response.RequestMessage = request;
            return Task.FromResult(response);
        }
    }

    /// <summary>一个只负责"活着"的旁观进程：描述符的读取侧只认还在跑的 pid。</summary>
    private sealed class IdleProcess : IDisposable
    {
        private readonly Process _process;

        private IdleProcess(Process process) => _process = process;

        public static IdleProcess Start() => new(Process.Start(new ProcessStartInfo(
            OperatingSystem.IsWindows() ? "cmd" : "/bin/sh",
            OperatingSystem.IsWindows() ? "/c ping -n 300 -w 1000 127.0.0.1" : "-c \"sleep 300\"")
        { UseShellExecute = false, CreateNoWindow = true }) ?? throw new InvalidOperationException("无法启动旁观进程。"));

        public int Id => _process.Id;

        public void Dispose()
        {
            try { _process.Kill(entireProcessTree: true); }
            catch { /* 已经自己退出的旁观进程无需处理 */ }
            _process.Dispose();
        }
    }

    /// <summary>本机监听表里真的有这一个监听者：重读的前半判据问的就是它。</summary>
    private sealed class TcpPort : IDisposable
    {
        private TcpListener? _listener;

        private TcpPort(TcpListener listener)
        {
            _listener = listener;
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        }

        public static TcpPort Open()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return new TcpPort(listener);
        }

        public int Port { get; }

        /// <summary>退出监听但把号码留给用例：用来判"401 之后端口已经没人监听就不重读"。</summary>
        public void Stop()
        {
            _listener?.Stop();
            _listener = null;
        }

        public void Dispose() => Stop();
    }
}

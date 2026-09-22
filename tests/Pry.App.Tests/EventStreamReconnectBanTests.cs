using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Pry.App.Services;
using Pry.Client;
using Pry.Contracts;
using Xunit;

namespace Pry.App.Tests;

/// <summary>
/// 事件流的"该不该重连"判据：服务端写明 retryable:false 时界面必须停下并说清原因，
/// 而服务端沉默（旧后端、非 ProblemDetails）时**不能**停下——两个方向都是行为，都要判。
/// <para>
/// 定位（QA-P5-01 起，别再按活路径证据读这一族）：这里驱动的是 <see cref="RemoteConversationTurnManager"/>，
/// 它唯一的构造点在 <c>MainWindow.axaml.cs</c>，而 <c>MainWindow</c> 在全仓（含测试）没有任何构造点——
/// <c>App</c> 打开的是 <c>PryMessengerWindow</c>。所以这五例判的是"同一判据的第二形状"：它守住那条循环
/// 只要被接线就不得违反的两向行为，但今天删掉禁令分支不会由它转红。活路径的判据在
/// <c>Pry.Client.Tests/ConversationEventStreamRunnerTests</c>（Runner 单点）、<c>PryErrorPresentationTests</c>（文案）
/// 与 <c>MessengerEventStreamControllerTests</c>（接线，含跑真 Runner 的两例）。
/// </para>
/// </summary>
public sealed class EventStreamReconnectBanTests
{
    [Theory]
    [InlineData(426, PryProtocol.MismatchErrorCode, "协议版本不一致")]
    [InlineData(401, PryProtocol.UnauthorizedErrorCode, "会话令牌")]
    public async Task A_banned_stream_failure_stops_the_backoff_and_names_the_fix(int status, string code, string notice)
    {
        var handler = new EventStreamHandler((HttpStatusCode)status,
            $$"""{"type":"urn:pry:error:{{code}}","title":"Blocked","status":{{status}},"detail":"服务端说明","code":"{{code}}","traceId":"00-trace","retryable":false}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:5078/") };
        await using var manager = new RemoteConversationTurnManager(new PryBackendClient(http), "room-1");
        var failures = new ConcurrentQueue<string>();
        manager.Failed += error => failures.Enqueue(error.Message);
        handler.Release();

        await WaitUntilAsync(() => !failures.IsEmpty, "没有收到终止提示");
        Assert.Contains(notice, failures.First(), StringComparison.Ordinal);
        Assert.Contains("已停止重连", failures.First(), StringComparison.Ordinal);

        await Task.Delay(BanWindow, TestContext.Current.CancellationToken);
        Assert.Equal(1, handler.EventRequests);
        Assert.Single(failures);
    }

    [Theory]
    [InlineData("retryable 缺席", """{"type":"urn:pry:error:internal_error","title":"Busy","status":500,"detail":"后端繁忙"}""")]
    [InlineData("retryable 为 true", """{"type":"urn:pry:error:provider_unavailable","title":"Busy","status":503,"detail":"后端繁忙","code":"provider_unavailable","retryable":true}""")]
    public async Task An_unbanned_stream_failure_keeps_reconnecting(string because, string body)
    {
        var handler = new EventStreamHandler(HttpStatusCode.InternalServerError, body);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:5078/") };
        await using var manager = new RemoteConversationTurnManager(new PryBackendClient(http), "room-1");
        var warnings = new ConcurrentQueue<string>();
        var failures = 0;
        manager.Warning += warning => warnings.Enqueue(warning);
        manager.Failed += _ => Interlocked.Increment(ref failures);
        handler.Release();

        await WaitUntilAsync(() => handler.EventRequests >= 2, $"{because}：重连没有发生");
        Assert.Contains("正在重连", warnings.First(), StringComparison.Ordinal);
        Assert.Equal(0, Volatile.Read(ref failures));
    }

    [Fact]
    public async Task An_unlisted_ban_still_stops_and_shows_the_server_code()
    {
        var handler = new EventStreamHandler(HttpStatusCode.BadGateway,
            """{"type":"urn:pry:error:provider_auth","title":"Auth","status":502,"detail":"密钥不对","code":"provider_auth","retryable":false}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:5078/") };
        await using var manager = new RemoteConversationTurnManager(new PryBackendClient(http), "room-1");
        var failures = new ConcurrentQueue<string>();
        manager.Failed += error => failures.Enqueue(error.Message);
        handler.Release();

        await WaitUntilAsync(() => !failures.IsEmpty, "未登记的禁行码没有给出终止提示");
        Assert.Contains("provider_auth", failures.First(), StringComparison.Ordinal);
        await Task.Delay(BanWindow, TestContext.Current.CancellationToken);
        Assert.Equal(1, handler.EventRequests);
    }

    private static readonly TimeSpan BanWindow = TimeSpan.FromMilliseconds(1_500);

    private static async Task WaitUntilAsync(Func<bool> condition, string because)
    {
        var token = TestContext.Current.CancellationToken;
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) Assert.Fail($"{because}（等待 5 秒超时）。");
            await Task.Delay(50, token);
        }
    }

    private sealed class EventStreamHandler(HttpStatusCode status, string problemBody) : HttpMessageHandler
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _eventRequests;
        public int EventRequests => _eventRequests;

        /// <summary>
        /// 宿主在构造函数里就开始订阅，所以"只发生一次"的终止提示会抢在测试挂处理器之前丢掉，
        /// 第一次请求必须等测试放行。放行前不能占住调用线程——宿主是同步进入 SendAsync 的，
        /// 阻塞在这里等于阻塞测试自己，5 秒后超时反而把第一次请求变成一条假的重连。
        /// </summary>
        public void Release() => _release.TrySetResult();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (!path.EndsWith("/events", StringComparison.Ordinal))
                throw new InvalidOperationException($"本用例只驱动事件流订阅，不该请求 {path}。");
            await _release.Task.WaitAsync(TimeSpan.FromSeconds(5), token);
            Interlocked.Increment(ref _eventRequests);
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(problemBody, Encoding.UTF8, "application/problem+json")
            };
        }
    }
}

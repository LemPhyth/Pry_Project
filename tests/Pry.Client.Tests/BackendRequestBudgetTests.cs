using System.Net;
using System.Text;
using Pry.Client;
using Pry.Contracts;
using Xunit;

namespace Pry.Client.Tests;

/// <summary>
/// 本机客户端的等待预算（PLAT-001）。这些值不是随手挑的：事件流在第一个心跳帧之前连响应头都没有，
/// 所以"有界超时"若比心跳周期还短，就会把一次正常的空闲订阅读成超时。
/// </summary>
public sealed class BackendRequestBudgetTests
{
    [Fact]
    public void Idle_subscription_still_fits_inside_the_request_budget()
    {
        Assert.True(PryProtocol.BackendRequestTimeoutMs > PryProtocol.HeartbeatIntervalMs,
            $"起头上限 {PryProtocol.BackendRequestTimeoutMs}ms 必须大于心跳周期 {PryProtocol.HeartbeatIntervalMs}ms。");
        Assert.Equal(PryProtocol.LivenessTimeoutMs, PryProtocol.BackendRequestTimeoutMs);
    }

    [Fact]
    public void Transcription_gets_its_own_budget_without_loosening_the_shared_one()
    {
        Assert.True(PryProtocol.TranscriptionRequestTimeoutMs > PryProtocol.BackendRequestTimeoutMs);
        // 两档都得是正数：InfiniteTimeSpan 在 .NET 里是 -1 毫秒，负值不是"超时"而是"永不超时"。
        Assert.True(PryProtocol.BackendRequestTimeoutMs > 0);
        Assert.True(PryProtocol.TranscriptionRequestTimeoutMs > 0);
        // 兜底必须宽于做主的那一档。HttpClient.Timeout 是客户端级的，一个数管所有请求；
        // 只要它比最宽的逐请求预算小，声明的那档就永远轮不到做主——4 分钟语音预算曾被 32 秒顶掉就是这么来的。
        Assert.True(PryProtocol.RequestBudgetCeilingMs > PryProtocol.TranscriptionRequestTimeoutMs,
            $"兜底 {PryProtocol.RequestBudgetCeilingMs}ms 必须宽于最宽的逐请求预算 {PryProtocol.TranscriptionRequestTimeoutMs}ms。");
    }

    [Fact]
    public async Task An_exhausted_whole_request_budget_reports_a_timeout_instead_of_a_cancellation()
    {
        using var handler = new StallingHandler();
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://127.0.0.1/"),
            Timeout = TimeSpan.FromMilliseconds(PryProtocol.RequestBudgetCeilingMs)
        };
        // 150 毫秒是注入进来的预算，产品侧取 PryProtocol 的 4 分钟；被判到点的是它，不是上面那档兜底。
        var client = new PryBackendClient(http, longRequestBudget: TimeSpan.FromMilliseconds(150));

        var error = await Assert.ThrowsAsync<TimeoutException>(() =>
            client.TranscribeAsync("media-1", null, TestContext.Current.CancellationToken));
        Assert.Contains("没有完成这一请求", error.Message);
    }

    [Fact]
    public async Task An_event_subscription_that_never_gets_headers_fails_on_the_local_budget()
    {
        using var handler = new StallingHandler();
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://127.0.0.1/"),
            Timeout = TimeSpan.FromMilliseconds(PryProtocol.RequestBudgetCeilingMs)
        };
        var client = new PryBackendClient(http, requestBudget: TimeSpan.FromMilliseconds(150));

        // 兜底抬到 4 分钟以上之后，流式起头不能跟着一起放宽：后端收下连接却永不作答时，
        // 一次订阅要按本机预算失败并交给上层退避，而不是把界面钉在这里等四分多钟。
        await Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await using var events = client.ReadEventsAsync("c", -1, TestContext.Current.CancellationToken)
                .GetAsyncEnumerator(TestContext.Current.CancellationToken);
            await events.MoveNextAsync();
        });
    }

    [Fact]
    public async Task Cancelling_a_long_request_stays_a_cancellation_instead_of_becoming_a_timeout()
    {
        using var handler = new StallingHandler();
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://127.0.0.1/"), Timeout = TimeSpan.FromSeconds(30)
        };
        var client = new PryBackendClient(http);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(120));

        // 这一档自带预算，用户主动停止与预算耗尽必须各报各的，否则界面以为是自己按下的那一下没生效。
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.TranscribeAsync("media-1", null, cancellation.Token));
    }

    private sealed class StallingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken token)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), token);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8) };
        }
    }
}

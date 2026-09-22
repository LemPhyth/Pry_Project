using System.Net;
using System.Text;
using System.Text.Json;
using Pry.Client;
using Pry.Contracts;
using Xunit;

namespace Pry.Client.Tests;

public sealed class ProtocolHandshakeClientTests
{
    [Fact]
    public async Task Every_request_declares_the_protocol_version_the_client_speaks()
    {
        HttpRequestMessage? seen = null;
        using var http = new HttpClient(new StubHandler(request =>
        {
            seen = request;
            return Json(HttpStatusCode.OK,
                @"{""protocolVersion"":""1.0"",""minSupportedClientVersion"":""1.0"",""coreVersion"":""0.0.2"","
                + @"""schemaVersion"":8,""features"":[""protocol-version-negotiation""]}");
        }))
        { BaseAddress = new Uri("http://127.0.0.1:5078/") };
        var client = new PryBackendClient(http);

        var info = await client.GetProtocolInfoAsync(TestContext.Current.CancellationToken);

        Assert.Equal(PryProtocol.Version, seen!.Headers.GetValues(PryProtocol.ClientHeader).Single());
        Assert.Equal("1.0", info.ProtocolVersion);
        Assert.Equal(8, info.SchemaVersion);
        Assert.Equal(["protocol-version-negotiation"], info.Features);
        Assert.Equal("0.0.2", info.CoreVersion);
    }

    [Fact]
    public async Task A_version_the_client_forced_is_not_overwritten()
    {
        using var http = new HttpClient(new StubHandler(_ => Json(HttpStatusCode.OK,
            """{"protocolVersion":"1.0","minSupportedClientVersion":"1.0","coreVersion":"0","schemaVersion":1,"features":[]}""")))
        { BaseAddress = new Uri("http://127.0.0.1:5078/") };
        http.DefaultRequestHeaders.TryAddWithoutValidation(PryProtocol.ClientHeader, "1.4");

        await new PryBackendClient(http).GetProtocolInfoAsync(TestContext.Current.CancellationToken);

        Assert.Equal("1.4", http.DefaultRequestHeaders.GetValues(PryProtocol.ClientHeader).Single());
    }

    [Fact]
    public async Task Version_mismatch_surfaces_as_a_typed_non_retryable_backend_error()
    {
        using var http = new HttpClient(new StubHandler(_ => Problem()))
        { BaseAddress = new Uri("http://127.0.0.1:5078/") };

        var error = await Assert.ThrowsAsync<PryBackendException>(() =>
            new PryBackendClient(http).GetProtocolInfoAsync(TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.UpgradeRequired, error.StatusCode);
        Assert.Equal(PryProtocol.MismatchErrorCode, error.Code);
        Assert.False(string.IsNullOrWhiteSpace(error.TraceId));
        // 用例名叫"non_retryable"却不判这一项，正是 QA-P2-01 抓到的断链：服务端写了，客户端边界丢了。
        Assert.False(error.Retryable);
    }

    [Fact]
    public async Task The_ban_reaches_the_stream_subscribers_that_do_the_reconnecting()
    {
        // 界面里"无限重连"的是事件流订阅者，不是普通请求。同一个 EnsureSuccessAsync 覆盖不到这条路径时，
        // 补了字段也还是丢——所以断言必须落在 ReadEventsAsync 上。
        using var http = new HttpClient(new StubHandler(_ => Problem(HttpStatusCode.UpgradeRequired)))
        { BaseAddress = new Uri("http://127.0.0.1:5078/") };
        var client = new PryBackendClient(http);

        var error = await Assert.ThrowsAsync<PryBackendException>(async () =>
        {
            await using var events = client.ReadEventsAsync("room", -1, TestContext.Current.CancellationToken)
                .GetAsyncEnumerator(TestContext.Current.CancellationToken);
            await events.MoveNextAsync();
        });

        Assert.Equal(HttpStatusCode.UpgradeRequired, error.StatusCode);
        Assert.False(error.Retryable);
    }

    [Fact]
    public async Task An_absent_retryable_is_not_invented_as_a_ban()
    {
        // 旧服务端与非 ProblemDetails 响应都落在这里。把 null 读成 false 会让界面在只是"没听见宣告"时停住；
        // 读成 true 更糟——新服务端刚发出的禁令会被退避重连淹没。两个方向都不许猜。
        using var http = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("""{"status":404,"detail":"会话不存在","code":"resource_not_found"}""",
                Encoding.UTF8, "application/problem+json")
        })) { BaseAddress = new Uri("http://127.0.0.1:5078/") };

        var error = await Assert.ThrowsAsync<PryBackendException>(() =>
            new PryBackendClient(http).GetConversationAsync("missing", TestContext.Current.CancellationToken));

        Assert.Null(error.Retryable);
        Assert.Equal("resource_not_found", error.Code);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private static HttpResponseMessage Problem(HttpStatusCode status = HttpStatusCode.UpgradeRequired)
    {
        var problem = new Dictionary<string, object?>
        {
            ["type"] = "urn:pry:error:protocol_mismatch", ["title"] = "协议版本不匹配", ["status"] = (int)status,
            ["detail"] = "本地核心的协议版本与本界面不兼容，请使用同一发行版本启动后重试。",
            ["code"] = PryProtocol.MismatchErrorCode, ["traceId"] = Guid.NewGuid().ToString("N"),
            ["retryable"] = false
        };
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(JsonSerializer.Serialize(problem, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                Encoding.UTF8, "application/problem+json")
        };
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) =>
            Task.FromResult(response(request));
    }
}

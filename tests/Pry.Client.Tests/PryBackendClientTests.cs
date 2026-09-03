using System.Net;
using System.Text;
using Pry.Client;
using Xunit;

namespace Pry.Client.Tests;

public sealed class PryBackendClientTests
{
    [Fact]
    public async Task Problem_details_become_typed_client_exception()
    {
        using var http = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("""{"status":404,"detail":"会话不存在","code":"resource_not_found","traceId":"trace-1"}""", Encoding.UTF8, "application/problem+json")
        })) { BaseAddress = new Uri("http://127.0.0.1:5078/") };
        var client = new PryBackendClient(http);

        var error = await Assert.ThrowsAsync<PryBackendException>(() => client.GetConversationAsync("missing", TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.NotFound, error.StatusCode);
        Assert.Equal("resource_not_found", error.Code);
        Assert.Equal("trace-1", error.TraceId);
    }

    [Fact]
    public async Task Sse_reader_parses_replayable_conversation_event()
    {
        var payload = """
            id: 7
            event: turn.state
            data: {"sequence":7,"type":"turn.state","occurredAt":"2026-09-03T00:00:00Z","data":{"state":"Idle"}}

            """;
        using var http = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "text/event-stream")
        })) { BaseAddress = new Uri("http://127.0.0.1:5078/") };
        var client = new PryBackendClient(http);

        await using var events = client.ReadEventsAsync("room", 6, TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        Assert.True(await events.MoveNextAsync());
        Assert.Equal(7, events.Current.Sequence);
        Assert.Equal("turn.state", events.Current.Type);
    }

    [Fact]
    public async Task Media_download_only_accepts_backend_api_paths()
    {
        using var http = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3])
        })) { BaseAddress = new Uri("http://127.0.0.1:5078/") };
        var client = new PryBackendClient(http);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.DownloadAsync("https://example.com/private.png", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.DownloadAsync("/api/v1/media/%2e%2e/private", TestContext.Current.CancellationToken));
        var content = await client.DownloadAsync("/api/v1/media/safe/content", TestContext.Current.CancellationToken);
        Assert.Equal([1, 2, 3], content.Bytes);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response(request));
    }
}

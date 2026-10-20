using System.Net;
using System.Text;
using Pry.Client;
using Pry.Contracts;
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

    [Fact]
    public async Task Embedding_selection_uses_independent_backend_endpoint()
    {
        HttpRequestMessage? captured = null;
        using var http = new HttpClient(new StubHandler(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"enabled":true,"state":"idle","modelId":"embed-1","indexedCount":0,"pendingCount":2,"dimensions":null,"errorCode":null,"retryable":false}""",
                    Encoding.UTF8, "application/json")
            };
        })) { BaseAddress = new Uri("http://127.0.0.1:5078/") };
        var client = new PryBackendClient(http);

        var result = await client.UpdateEmbeddingSettingsAsync(new UpdateEmbeddingSettingsRequest(true, "embed-1"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Put, captured!.Method);
        Assert.Equal("/api/v1/memory/embedding", captured.RequestUri!.AbsolutePath);
        Assert.Equal("embed-1", result.ModelId);
        Assert.Equal(2, result.PendingCount);
    }

    [Fact]
    public async Task Character_interaction_is_sent_to_the_conversation_endpoint()
    {
        HttpRequestMessage? captured = null;
        using var http = new HttpClient(new StubHandler(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Content = new StringContent("""{"actionId":"poke","characterId":"character","messageId":5,"outcome":"message_created","replayed":false,"acceptedAt":"2026-09-13T00:00:00Z","expressionIntent":null}""",
                    Encoding.UTF8, "application/json")
            };
        })) { BaseAddress = new Uri("http://127.0.0.1:5078/") };
        var client = new PryBackendClient(http);

        var result = await client.ExecuteInteractionAsync("room id",
            new ExecuteInteractionRequest("poke", "gesture-1"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("/api/v1/conversations/room%20id/interactions", captured.RequestUri!.AbsolutePath);
        Assert.Equal(5, result.MessageId);
    }

    [Fact]
    public async Task Delete_conversation_asks_for_the_cascade_in_lowercase_on_the_query()
    {
        string? sent = null;
        using var http = new HttpClient(new StubHandler(request =>
        {
            sent = $"{request.Method.Method} {request.RequestUri!.PathAndQuery}";
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        })) { BaseAddress = new Uri("http://127.0.0.1:5078/") };
        var client = new PryBackendClient(http);

        await client.DeleteConversationAsync("a&b", forgetExclusiveMemories: true, TestContext.Current.CancellationToken);
        Assert.Equal("DELETE /api/v1/conversations/a%26b?forgetExclusiveMemories=true", sent);

        // 第二笔不写那个开关：一次同时钉住"界面没勾选就不级联"的默认值和小写的 false。
        await client.DeleteConversationAsync("a&b", token: TestContext.Current.CancellationToken);
        Assert.Equal("DELETE /api/v1/conversations/a%26b?forgetExclusiveMemories=false", sent);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response(request));
    }
}

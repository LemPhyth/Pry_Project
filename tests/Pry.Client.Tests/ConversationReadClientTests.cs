using System.Net;
using System.Text;
using System.Text.Json;
using Pry.Client;
using Xunit;

namespace Pry.Client.Tests;

public sealed class ConversationReadClientTests
{
    [Fact]
    public async Task Client_preserves_explicit_watermark_and_escapes_conversation_id()
    {
        using var handler = new Handler(); using var http = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1/") };
        var client = new PryBackendClient(http); var ct = TestContext.Current.CancellationToken;
        var state = await client.MarkConversationReadAsync("room id", 42, ct);
        Assert.Equal(HttpMethod.Put, handler.Method); Assert.Equal("/api/v1/conversations/room%20id/read-state", handler.Path);
        using (var body = JsonDocument.Parse(handler.Body!)) Assert.Equal(42, body.RootElement.GetProperty("throughMessageId").GetInt64());
        Assert.Equal(2, state.UnreadCount);
        await client.GetConversationReadStateAsync("room id", ct); Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.Equal(8, (await client.GetUnreadSummaryAsync(ct)).TotalUnreadCount);
        Assert.Equal("/api/v1/conversations/unread-summary", handler.Path);
    }
    private sealed class Handler : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public string? Path { get; private set; }
        public string? Body { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Method = request.Method; Path = request.RequestUri!.AbsolutePath;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(token);
            return new(HttpStatusCode.OK) { Content = new StringContent(Path.EndsWith("unread-summary", StringComparison.Ordinal)
                ? """{"totalUnreadCount":8,"unreadConversationCount":3}"""
                : """{"conversationId":"room id","lastReadMessageId":42,"unreadCount":2}""", Encoding.UTF8, "application/json") };
        }
    }
}

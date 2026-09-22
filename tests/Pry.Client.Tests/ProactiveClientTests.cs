using System.Net;
using System.Text;
using System.Text.Json;
using Pry.Client;
using Pry.Contracts;
using Xunit;

namespace Pry.Client.Tests;

public sealed class ProactiveClientTests
{
    [Fact]
    public async Task Policy_and_activity_use_typed_backend_requests()
    {
        using var handler = new Handler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1/") };
        var client = new PryBackendClient(http);
        var token = TestContext.Current.CancellationToken;
        await client.GetProactivePolicyAsync("role id", token);
        Assert.Equal((HttpMethod.Get, "/api/v1/characters/role%20id/proactive-policy"), (handler.Method, handler.Path));
        var saved = await client.SaveProactivePolicyAsync("role id", new SaveProactivePolicyRequest(7, "room"), token);
        Assert.False(saved.DeliveryAvailable);
        Assert.Equal(HttpMethod.Put, handler.Method);
        using (var json = JsonDocument.Parse(handler.Body!))
        {
            Assert.Equal(7, json.RootElement.GetProperty("expectedRevision").GetInt64());
            Assert.Equal("room", json.RootElement.GetProperty("conversationId").GetString());
        }
        var id = Guid.NewGuid().ToString("N");
        await client.ReportCompanionActivityAsync(id, new ReportCompanionActivityRequest(2, "room", 0, IsTyping: true), token);
        Assert.Equal($"/api/v1/companion/activity/{id}", handler.Path);
        using (var json = JsonDocument.Parse(handler.Body!)) Assert.True(json.RootElement.GetProperty("isTyping").GetBoolean());
        await client.RemoveCompanionActivityAsync(id, token);
        Assert.Equal(HttpMethod.Delete, handler.Method);
        var status = await client.GetProactiveStatusAsync("role id", token);
        Assert.Equal("model_not_loaded", status.State);
        Assert.Equal("/api/v1/characters/role%20id/proactive-status", handler.Path);
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
            return new(request.Method == HttpMethod.Delete ? HttpStatusCode.NoContent : HttpStatusCode.OK)
            {
                Content = new StringContent(Path.EndsWith("proactive-status", StringComparison.Ordinal)
                    ? """{"characterId":"role id","conversationId":"room","state":"model_not_loaded","lastOutcome":null,"lastAttemptAt":null,"nextAttemptAt":null,"sentToday":0,"awaitingUserReply":false,"modelLoadPolicy":"already_loaded_only"}"""
                    : Path.Contains("activity", StringComparison.Ordinal)
                    ? """{"clientId":"client","sequence":2,"expiresAt":"2026-09-14T12:00:45Z"}"""
                    : """{"characterId":"role id","revision":8,"conversationId":"room","enabled":false,"idleSeconds":600,"minimumIntervalSeconds":7200,"dailyLimit":3,"timeZoneId":"UTC","quietStartMinute":1320,"quietEndMinute":480,"quietHoursEnabled":true,"deliveryAvailable":false}""",
                    Encoding.UTF8, "application/json")
            };
        }
    }
}

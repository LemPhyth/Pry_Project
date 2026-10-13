using System.Net;
using System.Text;
using System.Text.Json;
using Pry.Client;
using Pry.Contracts;
using Xunit;

namespace Pry.Client.Tests;

public sealed class MemoryManagementClientTests
{
    [Fact]
    public async Task Memory_details_and_clear_use_versioned_dtos_and_explicit_confirmation()
    {
        var handler = new CaptureHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1/") };
        var client = new PryBackendClient(http);
        Assert.Empty(await client.GetMemoryDetailsAsync("a&b", TestContext.Current.CancellationToken));
        Assert.Contains("characterId=a%26b", handler.Uri);
        await client.ClearMemoriesAsync(new("a&b", true), TestContext.Current.CancellationToken);
        Assert.Equal("/api/v1/memories/clear", handler.Uri);
        using var body = JsonDocument.Parse(handler.Body);
        Assert.True(body.RootElement.GetProperty("confirm").GetBoolean());
        Assert.Equal("a&b", body.RootElement.GetProperty("characterId").GetString());
    }
    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string Uri { get; private set; } = "";
        public string Body { get; private set; } = "";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Uri = request.RequestUri!.PathAndQuery;
            Body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(token);
            return request.Method == HttpMethod.Get
                ? new(HttpStatusCode.OK) { Content = new StringContent("[]", Encoding.UTF8, "application/json") }
                : new(HttpStatusCode.NoContent);
        }
    }
}

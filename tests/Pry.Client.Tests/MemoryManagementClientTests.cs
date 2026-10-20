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

    [Fact]
    public async Task Confirm_keep_is_an_empty_post_that_escapes_the_character_on_the_query()
    {
        var handler = new CaptureHandler
        {
            Json = """{"id":17,"characterId":"a&b","kind":"fact","content":"kept","status":"active","importance":0.5,"confidence":0.5,"origin":"chat","revision":1,"topicKey":"topic","evidence":[]}"""
        };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1/") };
        var client = new PryBackendClient(http);

        var result = await client.ConfirmKeepMemoryAsync(17, "a&b", TestContext.Current.CancellationToken);

        Assert.Equal("/api/v1/memories/17/confirm-keep?characterId=a%26b", handler.Uri);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Empty(handler.Body);
        Assert.Equal(17, result.Id);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string Uri { get; private set; } = "";
        public string Body { get; private set; } = "";
        public HttpMethod Method { get; private set; } = HttpMethod.Get;
        /// <summary>会读回 DTO 的那些请求不能拿 204 敷衍，客户端把空体判成"后端返回了空响应"。</summary>
        public string? Json { get; init; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Uri = request.RequestUri!.PathAndQuery;
            Method = request.Method;
            Body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(token);
            if (Json is not null)
                return new(HttpStatusCode.OK) { Content = new StringContent(Json, Encoding.UTF8, "application/json") };
            return request.Method == HttpMethod.Get
                ? new(HttpStatusCode.OK) { Content = new StringContent("[]", Encoding.UTF8, "application/json") }
                : new(HttpStatusCode.NoContent);
        }
    }
}

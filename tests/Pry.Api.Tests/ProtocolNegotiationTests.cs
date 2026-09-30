using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Pry.Contracts;
using Pry.Core.Memory;
using Xunit;

namespace Pry.Api.Tests;

public sealed class ProtocolNegotiationTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData(null, true)]      // 版本化上线前的已发布客户端必须继续可用
    [InlineData("", true)]
    [InlineData("1.0", true)]
    [InlineData("0.9", false)]    // major 不等
    [InlineData("1.4", false)]    // 客户端可能依赖本核心没有的端点：显式不匹配优于运行到一半 404
    [InlineData("2.0", false)]
    [InlineData("1", false)]      // 非 major.minor
    [InlineData("abc", false)]
    public void Negotiation_accepts_only_the_frozen_major(string? clientVersion, bool supported) =>
        Assert.Equal(supported, PryProtocol.IsSupported(clientVersion));

    [Fact]
    public async Task Info_advertises_the_frozen_version_and_only_real_features()
    {
        await using var backend = await Backend.StartAsync();
        using var response = await backend.Http.GetAsync("api/v1/info", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(PryProtocol.Version, ProtocolVersionOf(response));
        var info = await response.Content.ReadFromJsonAsync<ProtocolInfoResponse>(WebJson, TestContext.Current.CancellationToken);
        Assert.Equal(PryProtocol.Version, info!.ProtocolVersion);
        Assert.Equal(PryProtocol.MinSupportedVersion, info.MinSupportedClientVersion);
        Assert.True(info.SchemaVersion >= 1);
        Assert.Equal(PryProtocol.Features, info.Features);
        Assert.Contains("protocol-version-negotiation", info.Features);
        // 未实现的传输能力不得被宣告，否则客户端会朝不存在的地基降级。
        Assert.DoesNotContain("websocket-events", info.Features);
        // M0-c 落地的那一次提交才允许宣告持久游标；这里同时证明表真的建成了（游标回归见 ProtocolEventCursorTests）。
        Assert.Contains("durable-event-cursor", info.Features);
    }

    [Fact]
    public async Task Supported_client_is_served_and_every_api_response_carries_the_version()
    {
        await using var backend = await Backend.StartAsync();
        backend.Http.DefaultRequestHeaders.TryAddWithoutValidation(PryProtocol.ClientHeader, "1.0");
        using var response = await backend.Http.GetAsync("api/v1/conversations?limit=5", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(PryProtocol.Version, ProtocolVersionOf(response));
    }

    [Fact]
    public async Task Unsupported_client_is_rejected_before_the_write_reaches_the_business_route()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var backend = await Backend.StartAsync();
        var http = backend.Http;
        var before = await CountConversationsAsync(http, ct);

        http.DefaultRequestHeaders.TryAddWithoutValidation(PryProtocol.ClientHeader, "2.0");
        using var refused = await http.PostAsJsonAsync("api/v1/conversations", new CreateConversationRequest(null), ct);

        Assert.Equal(HttpStatusCode.UpgradeRequired, refused.StatusCode);
        // 沿用 ApiExceptionMiddleware 的既有事实：错误体有 ProblemDetails 字段，但传输类型仍是 application/json。
        // docs/api-v1.md 声称 problem+json 语义，两者对齐属 M1 传输收敛，不在本片单方面改动。
        Assert.Equal("application/json", refused.Content.Headers.ContentType?.MediaType);
        var problem = JsonDocument.Parse(await refused.Content.ReadAsStringAsync(ct)).RootElement;
        Assert.Equal("urn:pry:error:protocol_mismatch", problem.GetProperty("type").GetString());
        Assert.Equal("protocol_mismatch", problem.GetProperty("code").GetString());
        Assert.Equal("2.0", problem.GetProperty("clientProtocolVersion").GetString());
        Assert.Equal(PryProtocol.Version, problem.GetProperty("serverProtocolVersion").GetString());
        Assert.False(problem.GetProperty("retryable").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
        Assert.Equal(PryProtocol.Version, ProtocolVersionOf(refused));

        http.DefaultRequestHeaders.Remove(PryProtocol.ClientHeader);
        Assert.Equal(before, await CountConversationsAsync(http, ct));
    }

    [Fact]
    public async Task Handshake_stays_reachable_for_a_client_the_core_refuses_everywhere_else()
    {
        await using var backend = await Backend.StartAsync();
        backend.Http.DefaultRequestHeaders.TryAddWithoutValidation(PryProtocol.ClientHeader, "99.0");
        using var refused = await backend.Http.GetAsync("api/v1/runtime", TestContext.Current.CancellationToken);
        using var info = await backend.Http.GetAsync("api/v1/info", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UpgradeRequired, refused.StatusCode);
        Assert.Equal(HttpStatusCode.OK, info.StatusCode);
    }

    [Fact]
    public async Task Health_probe_stays_unversioned()
    {
        await using var backend = await Backend.StartAsync();
        using var response = await backend.Http.GetAsync("health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(ProtocolVersionOf(response));
    }

    [Fact]
    public async Task Schema_version_reported_matches_the_database_the_backend_opened()
    {
        await using var backend = await Backend.StartAsync();
        var actual = await backend.App.Services.GetRequiredService<MemoryDatabase>()
            .GetSchemaVersionAsync(TestContext.Current.CancellationToken);
        using var response = await backend.Http.GetAsync("api/v1/info", TestContext.Current.CancellationToken);
        var info = await response.Content.ReadFromJsonAsync<ProtocolInfoResponse>(WebJson, TestContext.Current.CancellationToken);

        Assert.Equal(actual, info!.SchemaVersion);
    }

    private static string? ProtocolVersionOf(HttpResponseMessage response) =>
        response.Headers.TryGetValues(PryProtocol.ServerHeader, out var values) ? values.Single() : null;

    private static async Task<int> CountConversationsAsync(HttpClient http, CancellationToken ct)
    {
        using var response = await http.GetAsync("api/v1/conversations?limit=200", ct);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct)).RootElement.GetArrayLength();
    }

    /// <summary>隔离数据目录的后端；SQLite 连接池会在 Windows 上继续占用文件，必须先清池再删目录。</summary>
    private sealed class Backend(WebApplication app, HttpClient http, string dataDirectory) : IAsyncDisposable
    {
        public WebApplication App { get; } = app;
        public HttpClient Http { get; } = http;

        public async ValueTask DisposeAsync()
        {
            Http.Dispose();
            await App.DisposeAsync();
            SqliteTestPools.Clear(Path.Combine(dataDirectory, "memory.db"));
            if (System.IO.Directory.Exists(dataDirectory)) System.IO.Directory.Delete(dataDirectory, true);
        }

        public static async Task<Backend> StartAsync()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"pry-protocol-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var app = await BackendApplication.BuildAsync(configure: builder =>
            {
                builder.Configuration["Pry:DataDirectory"] = directory;
                builder.Configuration["Pry:ModelsDirectory"] = Path.Combine(directory, "models");
                builder.Configuration["Pry:Url"] = "http://127.0.0.1:0";
            }, token: TestContext.Current.CancellationToken);
            await app.StartAsync(TestContext.Current.CancellationToken);
            return new Backend(app, app.CreateBackendHttpClient(), directory);
        }
    }
}

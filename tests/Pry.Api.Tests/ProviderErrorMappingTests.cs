using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Pry.Api.Middleware;
using Pry.Core.Inference;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// PLAT-001 的宿主一侧：Core 不认识 HTTP，所以"错误码 → 状态码"的翻译只能发生在这里。
/// 界面按 <c>code</c> 行动，状态码是给不读 Pry 错误码的通用调用方看的，两者必须各自稳定。
/// </summary>
public sealed class ProviderErrorMappingTests
{
    [Theory]
    [InlineData(ProviderErrorCodes.Auth, 502)]
    [InlineData(ProviderErrorCodes.RateLimited, 429)]
    [InlineData(ProviderErrorCodes.Timeout, 504)]
    [InlineData(ProviderErrorCodes.InvalidResponse, 502)]
    [InlineData(ProviderErrorCodes.ModelUnavailable, 503)]
    [InlineData(ProviderErrorCodes.ContextOverflow, 400)]
    [InlineData(ProviderErrorCodes.Cancelled, 503)]
    public async Task Each_provider_code_has_one_http_status(string code, int expectedStatus)
    {
        var context = await RunAsync(new ModelProviderException(code, "模型服务给出的中文说明", true),
            TestContext.Current.CancellationToken);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        var problem = await ReadProblemAsync(context);
        Assert.Equal(code, problem.GetProperty("code").GetString());
        Assert.Equal($"urn:pry:error:{code}", problem.GetProperty("type").GetString());
    }

    [Fact]
    public async Task Rate_limit_forwards_the_wait_the_provider_asked_for()
    {
        var context = await RunAsync(new ModelProviderException(ProviderErrorCodes.RateLimited, "稍后再试", true,
            TimeSpan.FromSeconds(30)), TestContext.Current.CancellationToken);

        Assert.Equal("30", context.Response.Headers.RetryAfter.ToString());
        Assert.Equal(30, (await ReadProblemAsync(context)).GetProperty("retryAfterSeconds").GetInt32());
    }

    [Fact]
    public async Task Provider_diagnostics_stay_out_of_the_response()
    {
        // 诊断片段是 Provider 的错误体，里面有上游地址、路由与可能的凭据回显：只能进本机日志。
        var context = await RunAsync(new ModelProviderException(ProviderErrorCodes.InvalidResponse, "回复无法处理",
                false, null, "internal-url https://vendor.internal/v1/chat Authorization: sk-leak"),
            TestContext.Current.CancellationToken);

        var body = await ReadBodyAsync(context);
        Assert.DoesNotContain("vendor.internal", body, StringComparison.Ordinal);
        Assert.DoesNotContain("sk-leak", body, StringComparison.Ordinal);
        Assert.Contains("回复无法处理", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_cancelled_turn_does_not_write_a_response_the_caller_cannot_read()
    {
        using var aborted = new CancellationTokenSource();
        aborted.Cancel();
        var context = await RunAsync(new ModelProviderException(ProviderErrorCodes.Cancelled, "请求已取消。", true),
            aborted.Token);

        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal("", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task Unstructured_provider_failures_still_reach_the_client_as_a_typed_error()
    {
        var context = await RunAsync(
            ModelProviderError.FromStatus(HttpStatusCode.Unauthorized, null, null, "qwen"),
            TestContext.Current.CancellationToken);

        Assert.Equal(502, context.Response.StatusCode);
        Assert.Equal(ProviderErrorCodes.Auth, (await ReadProblemAsync(context)).GetProperty("code").GetString());
    }

    private static async Task<HttpContext> RunAsync(Exception failure, CancellationToken requestAborted = default)
    {
        var middleware = new ApiExceptionMiddleware(_ => throw failure,
            NullLogger<ApiExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext { RequestAborted = requestAborted };
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);
        return context;
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<JsonElement> ReadProblemAsync(HttpContext context)
    {
        using var document = JsonDocument.Parse(await ReadBodyAsync(context));
        return document.RootElement.Clone();
    }
}

using System.Net;
using System.Net.Sockets;
using Pry.Client;
using Pry.Contracts;
using Xunit;

namespace Pry.Client.Tests;

/// <summary>
/// 错误呈现的判据：用户看到的每一句都要能照着做，且服务端的原文只能出现在展开区。
/// 禁行那一组与 Runner 的终止出口同源——文案改一处就要在这里同时判，
/// 界面两处循环合流后不再各写一份提示（QA-P4-02）。
/// </summary>
public sealed class PryErrorPresentationTests
{
    [Theory]
    [InlineData("cuda_out_of_memory", "显存")]
    [InlineData("startup_timeout", "模型服务")]
    [InlineData("process_start_failed", "模型服务")]
    [InlineData("model_missing", "本地模型")]
    [InlineData("runtime_missing", "推理组件")]
    [InlineData("validation_error", "填写")]
    [InlineData("resource_not_found", "找不到")]
    [InlineData("resource_conflict", "还在被使用")]
    [InlineData("turn_busy", "正在回复")]
    [InlineData("interaction_cooldown", "太快")]
    [InlineData("proactive_not_ready", "主动话题")]
    [InlineData("internal_error", "暂时不可用")]
    public void Stable_codes_surface_in_user_language(string code, string expectedFragment)
    {
        var error = new PryBackendException(HttpStatusCode.OK, code, "Not Found", null);

        var presented = PryErrorPresenter.Describe(error);

        Assert.Contains(expectedFragment, presented.Headline + presented.Body);
        Assert.DoesNotContain("Not Found", presented.Headline);
        Assert.DoesNotContain("Not Found", presented.Body);
    }

    /// <summary>PLAT-001 的七类码在界面侧是必须分档的：合并成一句"稍后重试"就等于把"改密钥"说成"等一会儿"。</summary>
    [Theory]
    [InlineData(ProviderErrorCodes.Auth, "API Key", false)]
    [InlineData(ProviderErrorCodes.RateLimited, "限流", true)]
    [InlineData(ProviderErrorCodes.Timeout, "超时", true)]
    [InlineData(ProviderErrorCodes.InvalidResponse, "格式不合法", false)]
    [InlineData(ProviderErrorCodes.ModelUnavailable, "排队", true)]
    [InlineData(ProviderErrorCodes.ContextOverflow, "太长", false)]
    [InlineData(ProviderErrorCodes.Cancelled, "打断", false)]
    public void Each_provider_code_gets_its_own_action(string code, string expectedFragment, bool canRetry)
    {
        var presented = PryErrorPresenter.Describe(
            new PryBackendException(HttpStatusCode.BadGateway, code, "provider detail", "trace-p"));

        Assert.Contains(expectedFragment, presented.Headline + presented.Body, StringComparison.Ordinal);
        Assert.Equal(canRetry, presented.CanRetry);
    }

    /// <summary>禁令优先于分类表：三档禁行都要说"已停止重连"并给出用户自己做的那件事，且不得留"再试一次"的口风。</summary>
    [Theory]
    [InlineData(PryProtocol.MismatchErrorCode, "协议版本不一致", "同一个发行版")]
    [InlineData(PryProtocol.UnauthorizedErrorCode, "会话令牌", "完全退出")]
    [InlineData(ProviderErrorCodes.Auth, "provider_auth", "重试不会成功")]
    public void A_ban_stops_and_names_the_fix_without_offering_a_retry(string code, string expected, string fix)
    {
        var presented = PryErrorPresenter.Describe(
            new PryBackendException((HttpStatusCode)426, code, "服务端说明", "trace-b", false));

        Assert.Contains(expected, presented.Body, StringComparison.Ordinal);
        Assert.Contains(fix, presented.Body, StringComparison.Ordinal);
        Assert.Contains("已停止重连", presented.Body, StringComparison.Ordinal);
        Assert.False(presented.CanRetry);
        Assert.DoesNotContain("可以再试一次", presented.Headline + presented.Body);
    }

    /// <summary>同一条码在"服务端没写 retryable"时仍是可重试档：禁行判据只能是那个字段，不能是码本身。</summary>
    [Fact]
    public void The_same_provider_code_stays_retryable_when_the_backend_stays_silent()
    {
        var banned = PryErrorPresenter.Describe(
            new PryBackendException(HttpStatusCode.BadGateway, ProviderErrorCodes.Auth, "detail", null, false));
        var silent = PryErrorPresenter.Describe(
            new PryBackendException(HttpStatusCode.BadGateway, ProviderErrorCodes.Auth, "detail", null));

        Assert.Contains("已停止重连", banned.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("已停止重连", silent.Body, StringComparison.Ordinal);
        Assert.Contains("API Key", silent.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void Transport_status_stays_out_of_headline_but_remains_expandable()
    {
        var error = new PryBackendException(HttpStatusCode.NotFound, null, "Not Found", "trace-9");

        var presented = PryErrorPresenter.Describe(error);

        Assert.DoesNotContain("404", presented.Headline);
        Assert.DoesNotContain("Not Found", presented.Body);
        Assert.Contains("404", presented.TechnicalDetail);
        Assert.Contains("Not Found", presented.TechnicalDetail);
        Assert.Contains("trace-9", presented.TechnicalDetail);
    }

    [Fact]
    public void Unknown_code_keeps_a_safe_fallback_and_records_the_trace_id()
    {
        var error = new PryBackendException(
            HttpStatusCode.InternalServerError, "some_future_code", "detail", "trace-4");

        var presented = PryErrorPresenter.Describe(error);

        Assert.Equal(PryErrorKind.BackendFault, presented.Kind);
        Assert.Contains("some_future_code", presented.TechnicalDetail);
        Assert.Contains("trace-4", presented.TechnicalDetail);
    }

    [Fact]
    public void Unrecognized_credential_style_status_stays_out_of_user_text()
    {
        var presented = PryErrorPresenter.Describe(
            new PryBackendException((HttpStatusCode)401, "provider_auth_failed", "401 Unauthorized", "trace-4"));

        Assert.Equal(PryErrorKind.RequestRejected, presented.Kind);
        Assert.DoesNotContain("401", presented.Headline + presented.Body);
        Assert.Contains("provider_auth_failed", presented.TechnicalDetail);
    }

    [Fact]
    public void Model_startup_failures_are_offered_for_retry_but_missing_files_are_not()
    {
        Assert.True(PryErrorPresenter.Describe(Backend("startup_timeout")).CanRetry);
        Assert.False(PryErrorPresenter.Describe(Backend("model_missing")).CanRetry);
        Assert.False(PryErrorPresenter.Describe(Backend("validation_error")).CanRetry);
    }

    [Fact]
    public void Refused_loopback_reads_as_local_core_not_started()
    {
        var transport = new HttpRequestException(
            "No connection could be made because the target machine 127.0.0.1:5078",
            new SocketException((int)SocketError.ConnectionRefused));

        var presented = PryErrorPresenter.Describe(transport);

        Assert.Equal(PryErrorKind.LocalCoreUnavailable, presented.Kind);
        Assert.Equal("本地核心没有启动成功", presented.Headline);
        Assert.Contains("Pry", presented.Body);
    }

    [Fact]
    public void Provider_failure_without_refusal_reads_as_no_response()
    {
        var presented = PryErrorPresenter.Describe(new HttpRequestException("The response ended prematurely."));

        Assert.Equal(PryErrorKind.ModelServiceUnavailable, presented.Kind);
        Assert.DoesNotContain("prematurely", presented.Body);
        Assert.Contains("prematurely", presented.TechnicalDetail);
    }

    [Fact]
    public void Timeout_reads_as_slow_not_as_an_error_code()
    {
        var presented = PryErrorPresenter.Describe(new TimeoutException("The operation was cancelled."));

        Assert.Equal(PryErrorKind.Timeout, presented.Kind);
        Assert.True(presented.CanRetry);
        Assert.DoesNotContain("cancelled", presented.Headline);
    }

    [Fact]
    public void Unplanned_exceptions_never_show_a_stack_frame_as_advice()
    {
        var presented = PryErrorPresenter.Describe(new InvalidOperationException("本地服务尚未就绪。"));

        Assert.Contains("未预期", presented.Headline);
        Assert.DoesNotContain("at Pry.", presented.Body);
        Assert.Contains("本地服务尚未就绪。", presented.TechnicalDetail);
    }

    private static PryBackendException Backend(string code) =>
        new(HttpStatusCode.ServiceUnavailable, code, "detail for " + code, "trace-x");
}

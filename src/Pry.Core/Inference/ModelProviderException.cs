using System.Net;
using System.Text.Json;
using Pry.Contracts;

namespace Pry.Core.Inference;

/// <summary>
/// Provider 侧（远端 OpenAI-compatible 与本地 llama-server 都算）失败的结构化表示。
/// 与 <see cref="ModelRuntimeException"/> 分开是刻意的：后者讲"本地模型进程起不来"，
/// 前者讲"模型服务回答了，但回答本身不行"，两者的可重试语义与用户提示不是一回事。
/// 这里不带 HTTP 状态码：Core 不认识 HTTP，状态由宿主按 <see cref="Code"/> 映射。
/// </summary>
public sealed class ModelProviderException(
    string code,
    string safeMessage,
    bool retryable,
    TimeSpan? retryAfter = null,
    string? diagnosticDetails = null,
    Exception? innerException = null) : Exception(safeMessage, innerException)
{
    public string Code { get; } = code;
    public string SafeMessage { get; } = safeMessage;
    public bool Retryable { get; } = retryable;

    /// <summary>Provider 明确给出的等待建议（429 的 <c>Retry-After</c>）。没有就是 <c>null</c>，不猜。</summary>
    public TimeSpan? RetryAfter { get; } = retryAfter;

    /// <summary>只供本机日志使用的有界诊断，已经遮过密钥；不得进响应体。</summary>
    public string? DiagnosticDetails { get; } = diagnosticDetails;
}

/// <summary>
/// 把 HTTP 事实（状态码、<c>Retry-After</c>、错误体片段）与 .NET 异常归类成七类之一。
/// 归类只有一个入口：调用方不得各自 <c>EnsureSuccessStatusCode()</c> 后凭异常类型猜原因——
/// 那正是 401/429 被压成 500 的路径。
/// </summary>
public static class ModelProviderError
{
    /// <summary>错误体只取有界片段：Provider 的错误 JSON 里可能回显请求头，长度与密钥都要限住。</summary>
    public const int MaximumDiagnosticCharacters = 2 * 1024;

    private static readonly string[] ContextOverflowSignals =
    [
        "context_length_exceeded", "exceeds_context", "exceed context", "context length",
        "maximum context", "reduce the length", "too many tokens", "prompt is too long",
        "input is too long", "context window"
    ];

    /// <summary>非 2xx 的统一归类。<paramref name="response"/> 尚未被读取正文时调用。</summary>
    public static ModelProviderException FromStatus(HttpStatusCode status, string? bodyExcerpt = null,
        TimeSpan? retryAfter = null, string? modelId = null, Exception? innerException = null)
    {
        var text = bodyExcerpt ?? string.Empty;
        var suffix = modelId is null ? string.Empty : $"（模型 {modelId}）";
        if (IsContextOverflow(status, text))
            return new(ProviderErrorCodes.ContextOverflow, "对话内容超出模型可处理的上下文长度，请缩短历史或降低上下文设置。",
                false, null, Trim(text), innerException);
        return status switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new(ProviderErrorCodes.Auth,
                "远端模型拒绝了当前凭据，请检查 API 密钥配置。" + suffix, false, null, Trim(text), innerException),
            HttpStatusCode.RequestTimeout => new(ProviderErrorCodes.Timeout,
                "远端模型没有在规定时间内响应。" + suffix, true, null, Trim(text), innerException),
            HttpStatusCode.TooManyRequests => new(ProviderErrorCodes.RateLimited,
                "远端模型暂时限流，稍后可自动重试。" + suffix, true, retryAfter, Trim(text), innerException),
            >= (HttpStatusCode)500 => new(ProviderErrorCodes.ModelUnavailable,
                "远端模型暂时不可用。" + suffix, true, retryAfter, Trim(text), innerException),
            // 没被识别的 4xx：既不是凭据也不是上下文，只能按"无法据以行动"处理，
            // 但绝不能伪装成成功——保留片段用于本机诊断。
            _ => new(ProviderErrorCodes.InvalidResponse,
                "远端模型返回了无法处理的响应。" + suffix, false, null, Trim(text), innerException)
        };
    }

    /// <summary>2xx 但内容不是能用的 SSE/JSON：与"Provider 拒绝请求"区分开，重试语义也不同。</summary>
    public static ModelProviderException InvalidResponse(string detail, string? modelId = null,
        Exception? innerException = null) =>
        new(ProviderErrorCodes.InvalidResponse,
            $"远端模型返回了无法解析的内容{Suffix(modelId)}。", false, null, Trim(detail), innerException);

    /// <summary>流在存活窗口内没有再吐任何字节：半开连接与"Provider 静默挂住"的同一条出口。</summary>
    public static ModelProviderException StreamIdle(TimeSpan window, string? modelId = null) =>
        new(ProviderErrorCodes.Timeout,
            $"远端模型在 {(int)window.TotalSeconds} 秒内没有继续返回内容{Suffix(modelId)}。", true, null, null, null);

    private static string Suffix(string? modelId) => modelId is null ? string.Empty : $"（模型 {modelId}）";

    /// <summary>
    /// 把传输层异常归类。<paramref name="cancellationRequested"/> 必须是"调用方真的取消了"这一事实，
    /// 而不是 <c>token.IsCancellationRequested</c> 的瞬时读值：HttpClient 的总超时同样表现为
    /// <see cref="TaskCanceledException"/>，两者混为一谈就会把"有界超时"重新写成"无限等待"。
    /// </summary>
    public static ModelProviderException FromException(Exception exception, bool cancellationRequested,
        string? modelId = null)
    {
        var suffix = modelId is null ? string.Empty : $"（模型 {modelId}）";
        if (cancellationRequested) return Cancelled(exception);
        return exception switch
        {
            TimeoutException => new(ProviderErrorCodes.Timeout, "远端模型没有在规定时间内响应。" + suffix, true,
                null, null, exception),
            // HttpClient 超时在 .NET 里是带内部 TimeoutException 的 TaskCanceledException；
            // 没有那个内部异常就是真的有人取消了，不能一律当超时。
            TaskCanceledException taskCanceled when taskCanceled.InnerException is TimeoutException =>
                new(ProviderErrorCodes.Timeout, "远端模型没有在规定时间内响应。" + suffix, true, null, null, exception),
            OperationCanceledException operationCanceled => Cancelled(operationCanceled),
            HttpRequestException request when IsTransport(request) =>
                new(ProviderErrorCodes.ModelUnavailable, "连不上远端模型服务。" + suffix, true, null,
                    Trim(request.Message), exception),
            HttpRequestException request => new(ProviderErrorCodes.ModelUnavailable,
                "远端模型暂时不可用。" + suffix, true, null, Trim(request.Message), exception),
            JsonException json => InvalidResponse(json.Message, modelId, json),
            _ => new(ProviderErrorCodes.InvalidResponse, "远端模型返回了无法处理的响应。" + suffix, false, null,
                Trim(exception.Message), exception)
        };
    }

    /// <summary>调用方取消：不是故障，但必须可观测（PLAT-001 的"取消静默 yield break"就是它不可观测的形态）。</summary>
    public static ModelProviderException Cancelled(Exception? innerException = null) =>
        new(ProviderErrorCodes.Cancelled, "请求已取消。", true, null, null, innerException);

    /// <summary>
    /// 给 <c>turn.failed.code</c> 与 HTTP 映射用的单一归类入口：已经结构化的直接用自己的码，
    /// 其余异常按类型归类，未知异常保持调用方给的兜底码而不谎报原因。
    /// </summary>
    public static string CodeOf(Exception exception, string fallbackCode) => exception switch
    {
        ModelProviderException provider => provider.Code,
        OperationCanceledException => ProviderErrorCodes.Cancelled,
        TimeoutException => ProviderErrorCodes.Timeout,
        HttpRequestException => ProviderErrorCodes.ModelUnavailable,
        JsonException or InvalidDataException or FormatException => ProviderErrorCodes.InvalidResponse,
        _ => fallbackCode
    };

    public static bool IsRetryable(Exception exception) => exception switch
    {
        ModelProviderException provider => provider.Retryable,
        OperationCanceledException => true,
        TimeoutException or HttpRequestException => true,
        _ => false
    };

    /// <summary>400 里绝大多数是"请求本身有问题"，只有上下文超限值得单独分类：用户能做的事完全不同。
    /// 状态码不作为前提——llama.cpp 对超上下文回 500、OpenAI 回 400，判据是措辞而不是端口。</summary>
    private static bool IsContextOverflow(HttpStatusCode status, string text) =>
        (int)status >= 400 && !string.IsNullOrEmpty(text) &&
        ContextOverflowSignals.Any(signal => text.Contains(signal, StringComparison.OrdinalIgnoreCase));

    /// <summary>连接失败与"服务端明确回答了自己不健康"都算可重试的不可用；其余按响应处理。</summary>
    private static bool IsTransport(HttpRequestException exception) =>
        exception.InnerException is System.Net.Sockets.SocketException;

    private static string? Trim(string? text) => string.IsNullOrEmpty(text)
        ? null
        : text.Length <= MaximumDiagnosticCharacters
            ? text
            : "[...]" + text[^MaximumDiagnosticCharacters..];
}

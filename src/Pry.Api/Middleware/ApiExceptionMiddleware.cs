using Microsoft.AspNetCore.Mvc;
using Pry.Api.Services;
using Pry.Contracts;
using Pry.Core.Inference;

namespace Pry.Api.Middleware;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (ApiValidationException ex)
        {
            await WriteAsync(context, 400, "validation_error", "请求参数不合法", ex.Message,
                new Dictionary<string, object?> { ["field"] = ex.Field });
        }
        catch (ResourceNotFoundException ex)
        {
            await WriteAsync(context, 404, "resource_not_found", "资源不存在", ex.Message);
        }
        catch (ResourceConflictException ex)
        {
            await WriteAsync(context, 409, "resource_conflict", "资源仍在使用", ex.Message,
                new Dictionary<string, object?> { ["resource"] = ex.Resource, ["id"] = ex.Id });
        }
        catch (PayloadTooLargeException ex)
        {
            await WriteAsync(context, 413, "payload_too_large", "上传内容超过允许大小",
                $"{ex.Resource} 超过允许大小 {ex.MaximumBytes} 字节",
                new Dictionary<string, object?> { ["resource"] = ex.Resource, ["maximumBytes"] = ex.MaximumBytes });
        }
        catch (InteractionRejectedException ex)
        {
            var status = ex.Code == "interaction_cooldown" ? 429 : 409;
            if (ex.RetryAt is { } retryAt)
                context.Response.Headers.RetryAfter = Math.Max(1,
                    (int)Math.Ceiling((retryAt - DateTimeOffset.UtcNow).TotalSeconds)).ToString();
            await WriteAsync(context, status, ex.Code, "角色互动暂不可执行", ex.Message,
                ex.RetryAt is null ? null : new Dictionary<string, object?> { ["retryAt"] = ex.RetryAt });
        }
        catch (ExpressionFeedbackRejectedException ex)
        {
            // 一次表态被拒要说清是哪种拒法：开关没开／认错会话／拿不到投递凭据／消息不存在／队列当下满，
            // 各有各的码与状态，不许都塞进 500（AGENTS 第 5 条）。未登记的码落在 409 那一档，
            // 最坏结果是状态码保守，而不是把一次没被收下的表态说成服务端故障。
            var status = ex.Code switch
            {
                ExpressionFeedbackErrorCodes.Disabled => 403,
                ExpressionFeedbackErrorCodes.MessageMissing => 404,
                ExpressionFeedbackErrorCodes.QueueFull => 503,
                _ => 409
            };
            await WriteAsync(context, status, ex.Code, "这条反应没有被收下", ex.Message);
        }
        catch (MemoryConfirmRejectedException ex)
        {
            // 确认保留只在孤立态上有意义；非孤立或同指纹已占用都拒，各有各的码，都不冒充 500。
            await WriteAsync(context, 409, ex.Code, "这条记忆没有被确认保留", ex.Message,
                new Dictionary<string, object?> { ["id"] = ex.Id });
        }
        catch (ModelRuntimeException ex)
        {
            await WriteAsync(context, 503, ex.Code, "本地模型暂时不可用", ex.SafeMessage,
                new Dictionary<string, object?> { ["retryable"] = ex.Retryable });
        }
        catch (ModelProviderException ex)
        {
            // 取消不是故障：调用方已经断开时再写响应没有读者，与下面的 OperationCanceledException 分支同一处理。
            if (ex.Code == ProviderErrorCodes.Cancelled && context.RequestAborted.IsCancellationRequested)
            {
                logger.LogInformation("Request {TraceId} was cancelled while waiting for the model provider",
                    context.TraceIdentifier);
                return;
            }
            logger.LogWarning("Model provider failed with {Code} for request {TraceId}", ex.Code,
                context.TraceIdentifier);
            var extensions = new Dictionary<string, object?> { ["retryable"] = ex.Retryable };
            if (ex.RetryAfter is { } retryAfter)
            {
                var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
                context.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
                extensions["retryAfterSeconds"] = retryAfterSeconds;
            }
            await WriteAsync(context, ProviderStatusOf(ex.Code), ex.Code, "模型服务暂时无法完成请求", ex.SafeMessage,
                extensions);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request {TraceId} was cancelled by the client", context.TraceIdentifier);
        }
        catch (BadHttpRequestException ex)
        {
            var status = ex.StatusCode is >= 400 and < 500 ? ex.StatusCode : 400;
            await WriteAsync(context, status, status == 413 ? "payload_too_large" : "invalid_request",
                "请求无法处理", status == 413 ? "请求体超过允许大小" : "请求格式不正确");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled API error for {Method} {Path}; trace {TraceId}", context.Request.Method, context.Request.Path, context.TraceIdentifier);
            await WriteAsync(context, 500, "internal_error", "服务暂时不可用", "请稍后重试，并在反馈时提供 traceId");
        }
    }

    /// <summary>
    /// Provider 错误码到 HTTP 状态的映射。Core 不认识 HTTP，所以这层翻译只能在这里；契约里的判据是
    /// <c>urn:pry:error:&lt;code&gt;</c> 与 <c>code</c>，状态码只给不读 Pry 错误码的通用调用方看。
    /// 未登记的码一律按"暂时不可用"处理：新增码忘了映射，最坏结果是状态码保守，而不是被当成成功语义。
    /// </summary>
    private static int ProviderStatusOf(string code) => code switch
    {
        ProviderErrorCodes.Auth or ProviderErrorCodes.InvalidResponse => 502,
        ProviderErrorCodes.RateLimited => 429,
        ProviderErrorCodes.Timeout => 504,
        ProviderErrorCodes.ContextOverflow => 400,
        _ => 503
    };

    private static async Task WriteAsync(HttpContext context, int status, string code, string title, string detail,
        IDictionary<string, object?>? extensions = null)
    {
        if (context.Response.HasStarted) return;
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail, Type = $"urn:pry:error:{code}", Instance = context.Request.Path };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        if (extensions is not null) foreach (var item in extensions) problem.Extensions[item.Key] = item.Value;
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem);
    }
}

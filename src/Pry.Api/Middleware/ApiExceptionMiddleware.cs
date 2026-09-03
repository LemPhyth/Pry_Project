using Microsoft.AspNetCore.Mvc;
using Pry.Api.Contracts;

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

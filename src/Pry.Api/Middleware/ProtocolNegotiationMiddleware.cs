using Microsoft.AspNetCore.Mvc;
using Pry.Contracts;

namespace Pry.Api.Middleware;

/// <summary>
/// 协议版本协商：为所有 api/ 响应盖上服务端版本头，并在客户端声明的版本不受支持时先于业务拒绝。
/// GET api/v1/info 永不被拒绝，否则版本不匹配的客户端将无法读到服务端支持的区间。
/// </summary>
public sealed class ProtocolNegotiationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        if (!path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        context.Response.Headers[PryProtocol.ServerHeader] = PryProtocol.Version;

        var requested = context.Request.Headers[PryProtocol.ClientHeader].ToString();
        var isHandshake = HttpMethods.IsGet(context.Request.Method) &&
                          path.StartsWithSegments("/api/v1/info", StringComparison.OrdinalIgnoreCase);
        if (PryProtocol.IsSupported(requested) || isHandshake)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status426UpgradeRequired;
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status426UpgradeRequired,
            Title = "协议版本不匹配",
            Type = $"urn:pry:error:{PryProtocol.MismatchErrorCode}",
            Detail = "本地核心的协议版本与本界面不兼容，请使用同一发行版本启动后重试。",
            Instance = path
        };
        problem.Extensions["code"] = PryProtocol.MismatchErrorCode;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        problem.Extensions["clientProtocolVersion"] = requested;
        problem.Extensions["serverProtocolVersion"] = PryProtocol.Version;
        problem.Extensions["minSupportedClientVersion"] = PryProtocol.MinSupportedVersion;
        problem.Extensions["retryable"] = false;
        await context.Response.WriteAsJsonAsync(problem);
    }
}

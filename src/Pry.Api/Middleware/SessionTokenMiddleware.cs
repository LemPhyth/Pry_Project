using Microsoft.AspNetCore.Mvc;
using Pry.Api.Services;
using Pry.Contracts;

namespace Pry.Api.Middleware;

/// <summary>
/// 默认拒绝的本机会话令牌门。除两个豁免端点外，任何请求都必须出示 <c>X-Pry-Token</c>：
/// 回环地址不区分用户，同一台机器上的任何同用户进程都能连上监听端口，
/// 而端口后面是整本本地对话数据库。豁免表必须是白名单而不是黑名单——
/// 新增端点默认落在保护一侧，忘记登记才会被漏掉。
/// </summary>
public sealed class SessionTokenMiddleware(RequestDelegate next, BackendSessionToken sessionToken)
{
    /// <summary>
    /// 无令牌的豁免面，只有这两条：存活探针，以及版本不匹配的客户端唯一还能读到的自述端点。
    /// 两者都只读且不含用户数据。整段相等而非前缀匹配，否则以后挂在 <c>/health</c> 下面的端点会静默继承豁免。
    /// </summary>
    private static readonly (string Method, PathString Path)[] Exemptions =
    [
        (HttpMethods.Get, new PathString("/health")),
        (HttpMethods.Get, new PathString("/api/v1/info")),
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsExempt(context.Request) ||
            sessionToken.Matches(context.Request.Headers[PryProtocol.SessionTokenHeader]))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "缺少本机会话令牌",
            Type = $"urn:pry:error:{PryProtocol.UnauthorizedErrorCode}",
            // Detail 不含期望值也不回显来值：错误响应是调用方读的诊断文本，不是令牌通道。
            Detail = "请求未携带有效的 X-Pry-Token。内嵌调用方应复用同一后端实例，" +
                     "独立进程应从 DataDirectory 下的 runtime.json 发现令牌。",
            Instance = context.Request.Path
        };
        problem.Extensions["code"] = PryProtocol.UnauthorizedErrorCode;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        problem.Extensions["retryable"] = false;
        await context.Response.WriteAsJsonAsync(problem);
    }

    private static bool IsExempt(HttpRequest request)
    {
        foreach (var (method, path) in Exemptions)
            if (string.Equals(request.Method, method, StringComparison.OrdinalIgnoreCase) &&
                request.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}

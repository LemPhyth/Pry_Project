namespace Pry.Client;

/// <summary>
/// 一次发现会话里"还能自己接回来的那部分"：从哪个目录读过入口、当前挂出去的是哪一枚令牌、
/// 装配这个客户端时用的是哪一条管道（<c>null</c> 即真实管道）。
/// 令牌值只在进程内做同一性比对，不落日志、不进异常文本（<see cref="Pry.Contracts.RuntimeDescriptor"/> 的
/// <c>ToString</c> 已经把它遮掉，这里也不提供任何把它交回调用方的口）。
/// </summary>
internal sealed class DiscoveredSession(string dataDirectory, string token, HttpMessageHandler? pipeline)
{
    public string DataDirectory { get; } = dataDirectory;

    /// <summary>此刻挂在客户端默认请求头上的那一枚令牌；换入口时只在这里更新，所以"令牌有没有变"问得出事实。</summary>
    public string Token { get; set; } = token;

    /// <summary>换入口时沿用装配时的同一条管道，重连不会悄悄换一套网络栈。</summary>
    public HttpMessageHandler? Pipeline { get; } = pipeline;
}

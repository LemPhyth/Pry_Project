using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Pry.Api.Services;
using Pry.Contracts;

namespace Pry.Api;

/// <summary>
/// 同进程内嵌后端的发现入口。<c>runtime.json</c> 是给"另一个进程"准备的本机发现文件；
/// 界面与后端跑在同一个进程里时，读同一份内存对象才是事实源——去读自己刚写的文件会把
/// 磁盘 I/O 变成启动路径的依赖，还会在覆盖窗口里读到上一实例的端口。
/// </summary>
public static class EmbeddedBackend
{
    /// <summary>
    /// Kestrel 实际绑定的回环地址。端口必须从服务器特征里读回，而不是先预留一个空闲端口再让
    /// 后端去绑：预留与绑定之间端口可以被别的进程抢走，那样描述文件写的是一个连不上的入口。
    /// </summary>
    public static string BoundUrl(this WebApplication app)
    {
        var addresses = app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()?.Addresses;
        var address = addresses?.FirstOrDefault(uri => Uri.TryCreate(uri, UriKind.Absolute, out var parsed) && parsed.IsLoopback);
        if (address is null)
            throw new InvalidOperationException("本地后端尚未完成回环端口绑定，无法发布发现信息。");
        return address.TrimEnd('/');
    }

    /// <summary>
    /// 建立带令牌的客户端。默认请求头在这里一次装配完成，调用方拿到的是一个已经能过
    /// 令牌门的 <see cref="HttpClient"/>，不需要也不应该自己知道 <c>X-Pry-Token</c> 的存在。
    /// </summary>
    public static HttpClient CreateBackendHttpClient(this WebApplication app)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(app.BoundUrl() + "/"),
            // 兜底档：做主的是 PryBackendClient 里逐请求声明的预算，所以这里必须宽于最宽的那一档（语音识别）。
            Timeout = TimeSpan.FromMilliseconds(PryProtocol.RequestBudgetCeilingMs)
        };
        client.DefaultRequestHeaders.Add(PryProtocol.SessionTokenHeader,
            app.Services.GetRequiredService<BackendSessionToken>().Value);
        return client;
    }
}

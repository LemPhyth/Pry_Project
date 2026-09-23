using Microsoft.AspNetCore.Builder;
using Pry.Api.Services;
using Pry.Client;

namespace Pry.App.Services;

/// <summary>
/// 界面连接本机后端的唯一入口。数据目录由后端宿主自己的配置根解析（写 <c>runtime.json</c> 的用的就是它），
/// 界面不许自认默认值：读侧固定看默认目录时，配了 <c>Pry:DataDirectory</c> 的实例永远发现不了，
/// 界面会在同一台机器上再起一个内嵌宿主去争那份数据目录。
/// 这里的参数是 builder 而不是可空的目录字符串——缺了配置根就不该编译得过。
/// </summary>
public static class BackendLocator
{
    public static string ResolveDataDirectory(WebApplicationBuilder hostBuilder) =>
        RuntimeDescriptorService.ResolveDataDirectory(hostBuilder.Configuration);

    /// <summary>接上别处已在跑的后端；发现文件缺席、失效或连不上都返回 null，由调用方回落内嵌。</summary>
    public static Task<HttpClient?> TryConnectAsync(WebApplicationBuilder hostBuilder, CancellationToken token = default) =>
        RuntimeDiscovery.TryConnectAsync(ResolveDataDirectory(hostBuilder), token);
}

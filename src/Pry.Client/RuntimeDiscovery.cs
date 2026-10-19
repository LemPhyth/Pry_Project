using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Pry.Contracts;

namespace Pry.Client;

/// <summary>
/// 独立进程模式的本机发现入口：只读 <c>runtime.json</c> 就建出带令牌、带版本的客户端，
/// 调用方不需要、也不允许自己写端口。同进程内嵌的调用方走 <c>EmbeddedBackend</c>，不经这里。
/// 判定一律"不确定就不用"（返回 null）而不是猜一个入口：陈旧文件里的端口可能已被别的进程占用，
/// 令牌也随那个后端一起失效，连错比连不上更难诊断。
/// </summary>
public static class RuntimeDiscovery
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// "这个客户端的令牌是从哪份描述符来的"记在客户端实例上。<b>挂在实例上而不是让调用方传参</b>是有意的：
    /// 换入口这件事必须由连接自己完成，界面或任何装配点一旦需要"记得"传一个目录，漏传的那一半就静默失效
    /// （F-1 的缺陷形状）。弱键因此不是省内存的手段，而是"会话与连接同寿命"的表达。
    /// </summary>
    private static readonly ConditionalWeakTable<HttpClient, DiscoveredSession> Sessions = new();

    public static string DescriptorPath(string? dataDirectory = null) =>
        Path.Combine(ResolveDirectory(dataDirectory), PryProtocol.RuntimeDescriptorFileName);

    /// <summary>
    /// 按描述符装配一个客户端，同时把这枚令牌与它的来源目录钉在这个实例上：此后它的 401 才有"按需重读入口"的资格。
    /// 端口、兜底预算、令牌头与协议头只在这里定义一次，首次连接与换入口重连共用同一个装配点，
    /// 否则两条路径会各自漂出一个入口。只有经这里连上的会话有资格重读，内嵌那条路径的 401 一字不变仍是禁令。
    /// </summary>
    /// <param name="pipeline">
    /// 传 <c>null</c> 就是真实管道；换入口时沿用第一次连接所用的那一条，所以重连不会悄悄换一套网络栈。
    /// 这个参数只给判据用：装配点漏传它只会让那一笔请求打到真网络上响，不会让产品路径少做什么。
    /// </param>
    public static HttpClient CreateClient(RuntimeDescriptor descriptor, string dataDirectory,
        HttpMessageHandler? pipeline = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        var http = pipeline is null ? new HttpClient() : new HttpClient(pipeline);
        // 会话挂在实例上而不是让调用方自己传目录是有意的：换入口这件事必须由连接自己完成，
        // 界面或任何装配点一旦需要"记得"传一个来源，漏传的那一半就静默失效（F-1 的缺陷形状）。
        Sessions.Add(http, new DiscoveredSession(ResolveDirectory(dataDirectory), descriptor.Token, pipeline));
        http.BaseAddress = new Uri($"http://127.0.0.1:{descriptor.Port}/");
        // 兜底而不是做主的那一档：逐请求预算在 PryBackendClient 里声明。它必须宽于最宽的一档
        // （语音识别），否则这个客户端连上后端后，长请求会被先跳的兜底顶掉——见 PryProtocol.RequestBudgetCeilingMs。
        http.Timeout = TimeSpan.FromMilliseconds(PryProtocol.RequestBudgetCeilingMs);
        http.DefaultRequestHeaders.Add(PryProtocol.SessionTokenHeader, descriptor.Token);
        if (!http.DefaultRequestHeaders.Contains(PryProtocol.ClientHeader))
            http.DefaultRequestHeaders.TryAddWithoutValidation(PryProtocol.ClientHeader, PryProtocol.Version);
        return http;
    }

    /// <summary>这个客户端是不是发现来的会话；不是就没有可重读的入口。</summary>
    internal static DiscoveredSession? SessionOf(HttpClient httpClient) =>
        Sessions.TryGetValue(httpClient, out var session) ? session : null;

    private static string ResolveDirectory(string? dataDirectory) => dataDirectory ?? PryProtocol.DefaultDataDirectory;

    /// <summary>
    /// 读出仍然可用的描述文件。缺席、不可解析、字段非法或所属进程已退出都返回 null；
    /// 失败路径不返回任何内容片段，令牌因此不会经异常文本外泄。
    /// </summary>
    public static RuntimeDescriptor? TryRead(string? dataDirectory = null)
    {
        RuntimeDescriptor? descriptor;
        try
        {
            descriptor = JsonSerializer.Deserialize<RuntimeDescriptor>(File.ReadAllText(DescriptorPath(dataDirectory)), JsonOptions);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or JsonException)
        {
            return null;
        }

        if (descriptor is null) return null;
        if (string.IsNullOrWhiteSpace(descriptor.Token)) return null;
        if (descriptor.Port is < 1 or > 65_535) return null;
        // 本进程自己写的文件不是"另一个进程的后端"。内嵌时读内存服务对象才是事实源，
        // 绕磁盘读回来会把文件 I/O 变成启动路径的依赖，还可能读到覆盖窗口里的旧端口。
        if (descriptor.Pid == Environment.ProcessId || !IsProcessAlive(descriptor.Pid)) return null;
        return descriptor;
    }

    /// <summary>
    /// 凭发现文件连上本机后端，返回一个已带令牌与协议头的客户端。除了"文件说的端口真在服务"之外
    /// 不做别的猜测：版本是否兼容由握手决定（不兼容时业务调用返回 <c>426</c> 并带禁行宣告），
    /// 界面据此说明该怎么修。令牌只进请求头，调用方拿不到、也就不可能把它写进日志。
    /// </summary>
    public static async Task<HttpClient?> TryConnectAsync(string? dataDirectory = null, CancellationToken token = default)
    {
        var descriptor = TryRead(dataDirectory);
        if (descriptor is null) return null;

        HttpClient? http = null;
        try
        {
            http = CreateClient(descriptor, ResolveDirectory(dataDirectory));
            // 探测走免鉴权的握手端点：那个进程还活着但 HTTP 面已经不再服务时，宁可不接。
            await new PryBackendClient(http).GetProtocolInfoAsync(token);
            var connected = http;
            http = null;
            return connected;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // 调用方取消不是"发现失败"，原样浮出才不会把退出流程伪装成"再试一次"。
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException
            or JsonException or InvalidOperationException)
        {
            return null;
        }
        finally
        {
            http?.Dispose();
        }
    }

    private static bool IsProcessAlive(int pid)
    {
        if (pid <= 0) return false;
        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NotSupportedException)
        {
            return false;
        }
    }
}

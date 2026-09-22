using System.Diagnostics;
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

    public static string DescriptorPath(string? dataDirectory = null) =>
        Path.Combine(dataDirectory ?? PryProtocol.DefaultDataDirectory, PryProtocol.RuntimeDescriptorFileName);

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
            http = new HttpClient
            {
                BaseAddress = new Uri($"http://127.0.0.1:{descriptor.Port}/"),
                // 兜底而不是做主的那一档：逐请求预算在 PryBackendClient 里声明。它必须宽于最宽的一档
                // （语音识别），否则这个客户端连上后端后，长请求会被先跳的兜底顶掉——见 PryProtocol.RequestBudgetCeilingMs。
                Timeout = TimeSpan.FromMilliseconds(PryProtocol.RequestBudgetCeilingMs)
            };
            http.DefaultRequestHeaders.Add(PryProtocol.SessionTokenHeader, descriptor.Token);
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

using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
using Pry.Contracts;
using Pry.Core.Memory;

namespace Pry.Api.Services;

/// <summary>
/// 把"本进程在哪个端口、用什么令牌"写成 <c>runtime.json</c>：启动完成后写、优雅退出时删。
/// 文件是给另一个进程看的发现入口；同进程内嵌的调用方必须直接读 <see cref="BackendSessionToken"/>
/// 与已绑定的地址，不能靠读自己刚写的文件绕一圈——那样会把磁盘 I/O 变成启动路径的依赖。
/// </summary>
public sealed class RuntimeDescriptorService(IConfiguration configuration, IServer server,
    BackendSessionToken sessionToken, IHostApplicationLifetime lifetime,
    ILogger<RuntimeDescriptorService> logger) : IHostedService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _dataDirectory = ResolveDataDirectory(configuration);
    private CancellationTokenRegistration _started;
    private CancellationTokenRegistration _stopping;
    private int _writtenPort;

    public static string ResolveDataDirectory(IConfiguration configuration)
    {
        var configured = configuration["Pry:DataDirectory"];
        return Path.GetFullPath(string.IsNullOrWhiteSpace(configured) ? PryProtocol.DefaultDataDirectory : configured);
    }

    public string DescriptorPath => Path.Combine(_dataDirectory, PryProtocol.RuntimeDescriptorFileName);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // 随机端口要等 Kestrel 自己绑定完才可知，而宿主服务之间的启动顺序不在契约里，
        // 所以只挂 ApplicationStarted，不假设自己排在谁后面。
        _started = lifetime.ApplicationStarted.Register(Write);
        _stopping = lifetime.ApplicationStopping.Register(Clear);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _started.DisposeAsync();
        await _stopping.DisposeAsync();
    }

    private void Write()
    {
        var port = BoundLoopbackPort();
        if (port is null)
        {
            // 绑不到已知的回环端口就不写：宁可让外部进程发现失败，也不能发布一个连不上的入口。
            logger.LogWarning("未能解析本地后端的回环监听端口，{File} 未写入", PryProtocol.RuntimeDescriptorFileName);
            return;
        }

        ReportLeftover();
        Directory.CreateDirectory(_dataDirectory);
        var descriptor = new RuntimeDescriptor(Environment.ProcessId, port.Value, sessionToken.Value,
            PryProtocol.Version, MemoryDatabase.SchemaVersion, DateTimeOffset.UtcNow);
        WriteAtomically(descriptor);
        _writtenPort = port.Value;
    }

    /// <summary>
    /// 上一进程留下的描述文件。只有"那个 pid 已经不在了"才是崩溃恢复；pid 还活着说明有两个后端
    /// 共用同一数据目录，那是配置冲突，两种情况都必须留一条日志而不是静默覆盖。
    /// </summary>
    private void ReportLeftover()
    {
        if (!File.Exists(DescriptorPath)) return;
        RuntimeDescriptor? leftover = null;
        var parseable = true;
        try
        {
            leftover = JsonSerializer.Deserialize<RuntimeDescriptor>(File.ReadAllText(DescriptorPath), JsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            parseable = false;
        }

        var observed = !parseable ? "内容无法解析"
            : leftover is null ? "内容为空"
            : IsProcessAlive(leftover.Pid) ? $"进程 {leftover.Pid} 仍在运行" : $"进程 {leftover.Pid} 已不存在";
        logger.LogWarning("发现上一实例遗留的 {File}（{Observed}），本次启动将覆盖它；" +
            "遗留文件的端口与令牌自此失效，读取方必须重新发现", PryProtocol.RuntimeDescriptorFileName, observed);
    }

    private void WriteAtomically(RuntimeDescriptor descriptor)
    {
        // 先写临时名再原子替换：崩溃在写一半时，发现方读到的是旧文件而不是半截 JSON。
        var temporary = $"{DescriptorPath}.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(descriptor, JsonOptions));
        File.Move(temporary, DescriptorPath, overwrite: true);
    }

    private void Clear()
    {
        try
        {
            if (!File.Exists(DescriptorPath)) return;
            var current = JsonSerializer.Deserialize<RuntimeDescriptor>(File.ReadAllText(DescriptorPath), JsonOptions);
            // 只删自己写的那份：后继实例已经覆盖过时，删除会把它的入口一起抹掉。
            if (current?.Pid != Environment.ProcessId || current.Port != _writtenPort) return;
            File.Delete(DescriptorPath);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "{File} 在退出清理时不可读写，留给下次启动的崩溃恢复处理",
                PryProtocol.RuntimeDescriptorFileName);
        }
    }

    private int? BoundLoopbackPort()
    {
        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;
        if (addresses is null) return null;
        foreach (var address in addresses)
        {
            if (Uri.TryCreate(address, UriKind.Absolute, out var uri) &&
                (uri.IsLoopback || uri.Host is "localhost" or "::1"))
                return uri.Port;
        }
        return null;
    }

    private static bool IsProcessAlive(int pid)
    {
        if (pid <= 0) return false;
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NotSupportedException)
        {
            return false;
        }
    }
}

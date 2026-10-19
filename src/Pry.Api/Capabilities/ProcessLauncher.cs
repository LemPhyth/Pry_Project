using System.Diagnostics;

namespace Pry.Api.Capabilities;

/// <summary>
/// 启动外部东西的那一次系统调用（RT-007）。这一道缝存在的原因是判据，不是风格：
/// 这一刀要钉的是"没被批准的时候一次都不许起"，而真起一次会在跑测试的机器上留下进程和窗口。
/// 有了它，"该不该启动"能由测试断言启动参数与启动次数，产品侧只需替换这一个类。
/// </summary>
public interface IProcessLauncher
{
    /// <summary>用系统默认程序打开一个已经过能力自己校验过的 URI。</summary>
    Task OpenUrlAsync(string url, CancellationToken cancellationToken);

    /// <summary>启动一个已经落在宿主白名单里的可执行文件。参数一律不接——见 <see cref="OpenApplicationCapability"/>。</summary>
    Task StartApplicationAsync(string executablePath, CancellationToken cancellationToken);
}

/// <summary>
/// 真机实现。两条路各走各的：<b>链接</b>走 <c>UseShellExecute = true</c>（"用默认程序打开这个 URI"是
/// 系统调用，不经过命令行解释）；<b>程序</b>走 <c>UseShellExecute = false</c> 加显式路径，
/// 绝不拼 <c>cmd /c</c>——拼 shell 字符串等于把参数注入面搬回白名单后面。
/// </summary>
public sealed class DesktopProcessLauncher(ILogger<DesktopProcessLauncher> logger) : IProcessLauncher
{
    public Task OpenUrlAsync(string url, CancellationToken cancellationToken) =>
        StartAsync(new ProcessStartInfo { FileName = url, UseShellExecute = true }, cancellationToken, "外部链接");

    public Task StartApplicationAsync(string executablePath, CancellationToken cancellationToken) =>
        StartAsync(new ProcessStartInfo
        {
            FileName = executablePath, UseShellExecute = false, CreateNoWindow = true
        }, cancellationToken, "本机程序");

    private async Task StartAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken, string what)
    {
        // Process.Start 是同步阻塞调用，而调用方给的令牌可能正好在这之间被取消（用户中断了那一轮）：
        // 放到工作线程上，取消才有地方发生，而不是变成一个卡死的等待。
        var process = await Task.Run(() =>
        {
            try
            {
                return Process.Start(startInfo);
            }
            catch (Exception error)
            {
                throw new CapabilityLaunchException(what, error);
            }
        }, cancellationToken);
        // 只登记"起了哪一类东西"，不登记 URL 与路径本身：链接里可以带访问令牌，日志会落盘。
        logger.LogInformation("能力已启动{What}", what);
        process?.Dispose();
    }
}

/// <summary>系统拒绝启动（没有默认程序、路径被安全软件拦下等）。原文只进日志，不进她的回复。</summary>
public sealed class CapabilityLaunchException(string what, Exception inner) : Exception($"无法启动{what}", inner)
{
    public string What { get; } = what;
}

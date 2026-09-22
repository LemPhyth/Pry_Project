using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Pry.Core.Inference;

public sealed record LlamaComputeDevice(string Id, string Name, bool IsIntegrated,
    long? TotalMemoryMiB = null, long? FreeMemoryMiB = null)
{
    public override string ToString() => $"{Name}（{Id}）";
}

public static partial class LlamaHardwareDetector
{
    private static readonly Dictionary<string, IReadOnlyList<LlamaComputeDevice>> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly SemaphoreSlim Gate = new(1, 1);

    /// <summary>
    /// <c>--list-devices</c> 探测的最长等待（PLAT-001 收尾 / QA-P2-04②）。此前无界：一次挂起不仅让真机启动路径
    /// 停在"检测设备"，还会永久占住上面的单槽 <see cref="Gate"/>，之后每一次探测都排在它后面。
    /// 取值要容得下冷启动时 ggml 后端的加载，又不能长到让界面以为程序死了。
    /// </summary>
    public static readonly TimeSpan DeviceProbeTimeout = TimeSpan.FromSeconds(20);

    /// <summary>显存富化查询的最长等待。它只补充显存数字，失败/超时都只降级不报错，所以比设备探测更短。</summary>
    public static readonly TimeSpan MemoryProbeTimeout = TimeSpan.FromSeconds(10);

    public static async Task<IReadOnlyList<LlamaComputeDevice>> ListDevicesAsync(string executablePath,
        bool refresh = false, CancellationToken cancellationToken = default, TimeSpan? probeTimeout = null)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            if (!refresh && Cache.TryGetValue(executablePath, out var cached)) return cached;
            if (!File.Exists(executablePath)) return [];
            var startInfo = new ProcessStartInfo(executablePath)
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            startInfo.ArgumentList.Add("--list-devices");
            string output;
            try
            {
                var probe = await RunProbeAsync(startInfo, probeTimeout ?? DeviceProbeTimeout, cancellationToken);
                output = probe.StandardOutput + Environment.NewLine + probe.StandardError;
            }
            catch (TimeoutException ex)
            {
                // 超时不静默当成"这台机器没有设备"：那会让自动选择在 GPU 机器上悄悄退回 CPU，而且没人知道。
                // 也不写缓存，下一次请求可以重新探测。
                throw new InvalidOperationException(
                    "本地推理设备探测超时，已终止探测进程。请重试，或在设置里显式选择设备。", ex);
            }
            var devices = new List<LlamaComputeDevice>();
            var nvidiaMemory = await QueryNvidiaMemoryAsync(cancellationToken);
            foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                var match = DeviceLine().Match(line);
                if (!match.Success) continue;
                var id = match.Groups[1].Value; var name = match.Groups[2].Value.Trim();
                if (id.Equals("Available", StringComparison.OrdinalIgnoreCase)) continue;
                var integrated = name.Contains("Intel", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("integrated", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("iGPU", StringComparison.OrdinalIgnoreCase);
                var memory = nvidiaMemory.FirstOrDefault(item => NamesMatch(item.Name, name));
                devices.Add(new LlamaComputeDevice(id, name, integrated, memory?.TotalMiB, memory?.FreeMiB));
            }
            Cache[executablePath] = devices;
            return devices;
        }
        finally { Gate.Release(); }
    }

    public static async Task<LlamaComputeDevice?> ResolveAsync(string executablePath, string requested,
        CancellationToken cancellationToken = default)
    {
        if (requested.Equals("cpu", StringComparison.OrdinalIgnoreCase)) return null;
        var devices = await ListDevicesAsync(executablePath, cancellationToken: cancellationToken);
        if (requested.Equals("auto-discrete", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(requested))
            return devices.FirstOrDefault(x => !x.IsIntegrated) ?? devices.FirstOrDefault();
        return devices.FirstOrDefault(x => x.Id.Equals(requested, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"设置的计算设备“{requested}”当前不可用。");
    }

    [GeneratedRegex("^\\s*([A-Za-z]+\\d+):\\s*(.+?)\\s*$")]
    private static partial Regex DeviceLine();

    private static async Task<IReadOnlyList<NvidiaMemoryInfo>> QueryNvidiaMemoryAsync(CancellationToken token)
    {
        try
        {
            var startInfo = new ProcessStartInfo("nvidia-smi")
            {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.ArgumentList.Add("--query-gpu=name,memory.total,memory.free");
            startInfo.ArgumentList.Add("--format=csv,noheader,nounits");
            var probe = await RunProbeAsync(startInfo, MemoryProbeTimeout, token);
            if (probe.ExitCode != 0) return [];
            return probe.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(ParseMemoryLine).Where(item => item is not null).Cast<NvidiaMemoryInfo>().ToArray();
        }
        // 显存数字只是富化信息：没有 nvidia-smi、非零退出、探测超时都只让设备少两个字段，不改变可选设备。
        catch (Exception) when (!token.IsCancellationRequested) { return []; }
    }

    /// <summary>
    /// 运行一个探测用子进程。三条不变量：等待有界；到期只杀**本方法刚拉起的那棵树**（绝不按进程名扫描或结束系统里的
    /// 其他 <c>llama-server</c>，那是模型进程所有权的边界）；调用方取消仍以 <see cref="OperationCanceledException"/>
    /// 冒出去、只有预算到点才报成超时——两者混为一谈，界面就会把"用户停了"当成一次硬件故障。
    /// </summary>
    private static async Task<ProbeResult> RunProbeAsync(ProcessStartInfo startInfo, TimeSpan budget,
        CancellationToken token)
    {
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("无法启动本地硬件探测进程。");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(budget);
        try
        {
            // 两个流与退出并发等。先等退出、再读流，会在"子进程写满管道缓冲、我们还在等它退出"时互锁成永久等待。
            var stdout = process.StandardOutput.ReadToEndAsync(deadline.Token);
            var stderr = process.StandardError.ReadToEndAsync(deadline.Token);
            await Task.WhenAll(stdout, stderr, process.WaitForExitAsync(deadline.Token));
            return new ProbeResult(process.ExitCode, stdout.Result, stderr.Result);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            Terminate(process);
            throw;
        }
        catch (OperationCanceledException)
        {
            Terminate(process);
            throw new TimeoutException($"本地硬件探测在 {(int)budget.TotalSeconds} 秒内没有结束，已终止探测进程。");
        }
    }

    /// <summary>杀不掉时不再往外抛：调用方要的是"别无限等"，多一种失败形状只会把探测变成新故障。</summary>
    private static void Terminate(Process process)
    {
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            process.WaitForExit(1_000);
        }
        catch (Exception failure) when (failure is InvalidOperationException or System.ComponentModel.Win32Exception) { }
    }

    private sealed record ProbeResult(int ExitCode, string StandardOutput, string StandardError);

    private static NvidiaMemoryInfo? ParseMemoryLine(string line)
    {
        var parts = line.Split(',', StringSplitOptions.TrimEntries);
        return parts.Length == 3 && long.TryParse(parts[1], out var total) && long.TryParse(parts[2], out var free)
            ? new NvidiaMemoryInfo(parts[0], total, free)
            : null;
    }

    private static bool NamesMatch(string left, string right)
    {
        static string Normalize(string value) => Regex.Replace(value, "[^A-Za-z0-9]", "").ToUpperInvariant();
        var a = Normalize(left); var b = Normalize(right);
        return a.Contains(b, StringComparison.Ordinal) || b.Contains(a, StringComparison.Ordinal);
    }

    private sealed record NvidiaMemoryInfo(string Name, long TotalMiB, long FreeMiB);
}

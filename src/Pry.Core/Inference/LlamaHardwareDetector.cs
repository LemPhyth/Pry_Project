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

    public static async Task<IReadOnlyList<LlamaComputeDevice>> ListDevicesAsync(string executablePath,
        bool refresh = false, CancellationToken cancellationToken = default)
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
            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("无法检测本地推理设备。");
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var output = (await outputTask) + Environment.NewLine + (await errorTask);
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
            using var process = Process.Start(startInfo);
            if (process is null) return [];
            var output = await process.StandardOutput.ReadToEndAsync(token);
            await process.WaitForExitAsync(token);
            if (process.ExitCode != 0) return [];
            return output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(ParseMemoryLine).Where(item => item is not null).Cast<NvidiaMemoryInfo>().ToArray();
        }
        catch (Exception) when (!token.IsCancellationRequested) { return []; }
    }

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

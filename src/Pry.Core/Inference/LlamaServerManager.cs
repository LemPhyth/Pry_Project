using System.Diagnostics;
using Pry.Core.Models;

namespace Pry.Core.Inference;

public sealed class LlamaServerManager : IAsyncDisposable
{
    private Process? _process;

    public bool IsRunning => _process is { HasExited: false };
    public string ActiveComputeDevice { get; private set; } = "CPU";

    public async Task StartAsync(string executablePath, ModelProfile profile, CancellationToken cancellationToken = default)
    {
        if (IsRunning || profile.Provider != "local-llama") return;
        if (!File.Exists(executablePath)) throw new FileNotFoundException("未找到本地推理运行时。请安装完整模型包或在高级设置中指定 llama-server。", executablePath);
        if (string.IsNullOrWhiteSpace(profile.ModelPath) || !File.Exists(profile.ModelPath)) throw new FileNotFoundException("未找到本地模型文件。", profile.ModelPath);
        var computeDevice = await LlamaHardwareDetector.ResolveAsync(executablePath, profile.ComputeDevice, cancellationToken);
        var gpuLayers = computeDevice is null ? 0 : profile.GpuLayers <= 0 ? 999 : profile.GpuLayers;
        ActiveComputeDevice = computeDevice?.ToString() ?? "CPU";
        var args = new List<string>
        {
            "-m", profile.ModelPath, "-c", profile.ContextSize.ToString(), "-ngl", gpuLayers.ToString(),
            "--host", "127.0.0.1", "--port", new Uri(profile.BaseUrl).Port.ToString(),
            "--reasoning", profile.EnableThinking ? "on" : "off"
        };
        if (computeDevice is not null) { args.Add("--device"); args.Add(computeDevice.Id); }
        if (!string.IsNullOrWhiteSpace(profile.ApiKey)) { args.Add("--api-key"); args.Add(profile.ApiKey); }
        if (!string.IsNullOrWhiteSpace(profile.MmprojPath)) { args.Add("--mmproj"); args.Add(profile.MmprojPath); }
        // llama.cpp writes verbose request logs to stderr. Redirecting without continuously draining the
        // stream eventually fills the OS pipe and stalls generation, so the packaged process inherits no pipe.
        var startInfo = new ProcessStartInfo(executablePath) { UseShellExecute = false, CreateNoWindow = true };
        foreach (var arg in args) startInfo.ArgumentList.Add(arg);
        _process = Process.Start(startInfo) ?? throw new InvalidOperationException("无法启动本地推理服务。");
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var healthUri = new Uri(new Uri(profile.BaseUrl), "/health");
        for (var attempt = 0; attempt < 60; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_process.HasExited) throw new InvalidOperationException($"本地推理服务提前退出，代码 {_process.ExitCode}。");
            try
            {
                using var response = await client.GetAsync(healthUri, cancellationToken);
                if (response.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { }
            await Task.Delay(500, cancellationToken);
        }
        throw new TimeoutException("本地模型加载超时。");
    }

    public async ValueTask DisposeAsync()
    {
        if (_process is { HasExited: false }) { _process.Kill(true); await _process.WaitForExitAsync(); }
        _process?.Dispose();
    }
}

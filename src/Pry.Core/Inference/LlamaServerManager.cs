using System.Diagnostics;
using Pry.Core.Models;

namespace Pry.Core.Inference;

public sealed class LlamaServerManager : IAsyncDisposable
{
    private const int MaximumDiagnosticCharacters = 32 * 1024;
    private Process? _process;
    private WindowsProcessJob? _processJob;
    private Task? _stderrDrainTask;
    private readonly Queue<string> _diagnosticLines = new();
    private int _diagnosticCharacters;
    private string? _diagnosticSecret;

    public bool IsRunning => _process is { HasExited: false };
    public string ActiveComputeDevice { get; private set; } = "CPU";

    public async Task StartAsync(string executablePath, ModelProfile profile, CancellationToken cancellationToken = default)
    {
        if (IsRunning || profile.Provider != "local-llama") return;
        if (!File.Exists(executablePath)) throw new ModelRuntimeException("runtime_missing", "未找到本地推理运行库。", false);
        if (string.IsNullOrWhiteSpace(profile.ModelPath) || !File.Exists(profile.ModelPath))
            throw new ModelRuntimeException("model_missing", "未找到本地模型文件。", false);
        var computeDevice = await LlamaHardwareDetector.ResolveAsync(executablePath, profile.ComputeDevice, cancellationToken);
        var gpuLayers = computeDevice is null ? 0 : profile.GpuLayers <= 0 ? 999 : profile.GpuLayers;
        ActiveComputeDevice = computeDevice?.ToString() ?? "CPU";
        var args = new List<string>
        {
            "-m", profile.ModelPath, "-c", profile.ContextSize.ToString(), "-ngl", gpuLayers.ToString(),
            "--host", "127.0.0.1", "--port", new Uri(profile.BaseUrl).Port.ToString(),
            "--reasoning", profile.EnableThinking ? "on" : "off"
        };
        _diagnosticSecret = profile.ApiKey;
        if (computeDevice is not null) { args.Add("--device"); args.Add(computeDevice.Id); }
        if (!string.IsNullOrWhiteSpace(profile.ApiKey)) { args.Add("--api-key"); args.Add(profile.ApiKey); }
        if (!string.IsNullOrWhiteSpace(profile.MmprojPath)) { args.Add("--mmproj"); args.Add(profile.MmprojPath); }
        var startInfo = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true
        };
        foreach (var arg in args) startInfo.ArgumentList.Add(arg);
        try
        {
            _process = Process.Start(startInfo) ?? throw new InvalidOperationException("进程未创建。");
            _processJob = WindowsProcessJob.Attach(_process);
            _stderrDrainTask = DrainStandardErrorAsync(_process);
        }
        catch (Exception ex)
        {
            if (_process is { HasExited: false }) _process.Kill(true);
            throw new ModelRuntimeException("process_start_failed", "无法启动本地模型服务。", true, null, ex);
        }
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var healthUri = new Uri(new Uri(profile.BaseUrl), "/health");
        for (var attempt = 0; attempt < 60; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_process.HasExited)
            {
                if (_stderrDrainTask is not null) await _stderrDrainTask;
                throw ModelRuntimeException.FromProcessExit(_process.ExitCode, GetDiagnostics());
            }
            try
            {
                using var response = await client.GetAsync(healthUri, cancellationToken);
                if (response.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { }
            await Task.Delay(500, cancellationToken);
        }
        throw new ModelRuntimeException("startup_timeout", "本地模型加载超时。", true, GetDiagnostics());
    }

    private async Task DrainStandardErrorAsync(Process process)
    {
        while (await process.StandardError.ReadLineAsync() is { } line)
        {
            if (!string.IsNullOrEmpty(_diagnosticSecret))
                line = line.Replace(_diagnosticSecret, "[redacted]", StringComparison.Ordinal);
            lock (_diagnosticLines)
            {
                _diagnosticLines.Enqueue(line);
                _diagnosticCharacters += line.Length + Environment.NewLine.Length;
                while (_diagnosticCharacters > MaximumDiagnosticCharacters && _diagnosticLines.TryDequeue(out var removed))
                    _diagnosticCharacters -= removed.Length + Environment.NewLine.Length;
            }
        }
    }

    private string GetDiagnostics()
    {
        lock (_diagnosticLines) return string.Join(Environment.NewLine, _diagnosticLines);
    }

    public async ValueTask DisposeAsync()
    {
        if (_process is { HasExited: false })
        {
            _process.Kill(true);
            await _process.WaitForExitAsync();
        }
        if (_stderrDrainTask is not null) try { await _stderrDrainTask; } catch { }
        _processJob?.Dispose();
        _process?.Dispose();
        _processJob = null;
        _process = null;
    }
}

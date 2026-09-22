using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Pry.Core.Models;

namespace Pry.Core.Inference;

public sealed class LlamaServerManager(ILogger? logger = null) : IAsyncDisposable
{
    private const int MaximumDiagnosticCharacters = 32 * 1024;

    /// <summary>要写进日志的诊断文本上界：32 KiB 的环形缓冲是给异常用的，日志文件只留尾部。</summary>
    private const int MaximumLoggedDiagnosticCharacters = 2 * 1024;

    /// <summary>宿主注入的日志器（PLAT-006）。默认不记任何东西，未接线的调用方行为不变。</summary>
    private readonly ILogger _logger = logger ?? NullLogger.Instance;
    private Process? _process;
    private WindowsProcessJob? _processJob;
    private Task? _stderrDrainTask;
    private readonly Queue<string> _diagnosticLines = new();
    private int _diagnosticCharacters;
    private string? _diagnosticSecret;

    public bool IsRunning => _process is { HasExited: false };
    public string ActiveComputeDevice { get; private set; } = "CPU";

    public async Task<LlamaServerStartupMetrics> StartAsync(string executablePath, ModelProfile profile,
        CancellationToken cancellationToken = default, bool embeddingsOnly = false)
    {
        if (IsRunning || profile.Provider != "local-llama") return new LlamaServerStartupMetrics(null);
        if (!File.Exists(executablePath)) throw new ModelRuntimeException("runtime_missing", "未找到本地推理运行库。", false);
        if (string.IsNullOrWhiteSpace(profile.ModelPath) || !File.Exists(profile.ModelPath))
            throw new ModelRuntimeException("model_missing", "未找到本地模型文件。", false);
        var computeDevice = await LlamaHardwareDetector.ResolveAsync(executablePath, profile.ComputeDevice, cancellationToken);
        ActiveComputeDevice = computeDevice?.ToString() ?? "CPU";
        _diagnosticSecret = profile.ApiKey;
        var startInfo = CreateStartInfo(executablePath, profile, computeDevice, embeddingsOnly);
        var started = Environment.TickCount64;
        _logger.LogInformation(
            "本地模型服务启动中：运行库 {RuntimeFileName}，模型 {ModelFileName}，模式 {Mode}，上下文 {ContextSize}，" +
            "端口 {Port}，设备 {ComputeDevice}",
            Path.GetFileName(executablePath), Path.GetFileName(profile.ModelPath),
            embeddingsOnly ? "embedding" : "chat", profile.ContextSize, new Uri(profile.BaseUrl).Port,
            computeDevice?.ToString() ?? "CPU");
        try
        {
            _process = Process.Start(startInfo) ?? throw new InvalidOperationException("进程未创建。");
            _processJob = WindowsProcessJob.Attach(_process);
            _stderrDrainTask = DrainStandardErrorAsync(_process);
        }
        catch (Exception ex)
        {
            if (_process is { HasExited: false }) _process.Kill(true);
            _logger.LogError(ex, "本地模型服务无法启动：运行库 {RuntimeFileName}", Path.GetFileName(executablePath));
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
                _logger.LogError("本地模型服务进程退出：退出码 {ExitCode}，最后诊断 {Diagnostics}",
                    _process.ExitCode, LoggedDiagnostics());
                throw ModelRuntimeException.FromProcessExit(_process.ExitCode, GetDiagnostics());
            }
            try
            {
                using var response = await client.GetAsync(healthUri, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var metrics = new LlamaServerStartupMetrics(embeddingsOnly ? null :
                        await MeasureGenerationSpeedAsync(profile, cancellationToken));
                    _logger.LogInformation("本地模型服务就绪：用时 {ElapsedMilliseconds} 毫秒，{TokensPerSecond} tok/s",
                        Environment.TickCount64 - started,
                        metrics.TokensPerSecond is { } speed ? speed.ToString("F2") : "未测速");
                    return metrics;
                }
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { }
            await Task.Delay(500, cancellationToken);
        }
        _logger.LogError("本地模型服务加载超时：端口 {Port}，最后诊断 {Diagnostics}",
            new Uri(profile.BaseUrl).Port, LoggedDiagnostics());
        throw new ModelRuntimeException("startup_timeout", "本地模型加载超时。", true, GetDiagnostics());
    }

    internal static ProcessStartInfo CreateStartInfo(string executablePath, ModelProfile profile,
        LlamaComputeDevice? computeDevice, bool embeddingsOnly = false)
    {
        var gpuLayers = computeDevice is null ? 0 : profile.GpuLayers <= 0 ? 999 : profile.GpuLayers;
        var args = new List<string>
        {
            "-m", profile.ModelPath!, "-c", profile.ContextSize.ToString(), "-ngl", gpuLayers.ToString(),
            "--host", "127.0.0.1", "--port", new Uri(profile.BaseUrl).Port.ToString()
        };
        if (embeddingsOnly) args.Add("--embedding");
        else { args.Add("--reasoning"); args.Add(profile.EnableThinking ? "on" : "off"); }
        if (computeDevice is not null) { args.Add("--device"); args.Add(computeDevice.Id); }
        if (!string.IsNullOrWhiteSpace(profile.MmprojPath)) { args.Add("--mmproj"); args.Add(profile.MmprojPath); }
        var startInfo = new ProcessStartInfo(executablePath)
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true
        };
        foreach (var arg in args) startInfo.ArgumentList.Add(arg);
        ConfigureAuthentication(startInfo, profile.ApiKey);
        return startInfo;
    }

    internal static void ConfigureAuthentication(ProcessStartInfo startInfo, string? apiKey)
    {
        // 只改这个子进程的环境块：既不修改宿主环境，也不让密钥经 argv 暴露；
        // 继承来的密钥文件会带入无关密钥，或在文件缺失时直接让服务起不来。
        startInfo.Environment.Remove("LLAMA_ARG_API_KEY_FILE");
        startInfo.Environment.Remove("LLAMA_API_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey)) startInfo.Environment["LLAMA_API_KEY"] = apiKey;
    }

    private static async Task<double?> MeasureGenerationSpeedAsync(ModelProfile profile, CancellationToken token)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            if (!string.IsNullOrWhiteSpace(profile.ApiKey))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", profile.ApiKey);
            var payload = JsonSerializer.Serialize(new
            {
                model = profile.ModelName,
                messages = new[] { new { role = "user", content = "Reply only: OK" } },
                stream = false,
                temperature = 0.0,
                max_tokens = 8,
                chat_template_kwargs = new { enable_thinking = false }
            });
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync($"{profile.BaseUrl.TrimEnd('/')}/chat/completions", content, token);
            if (!response.IsSuccessStatusCode) return null;
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
            return document.RootElement.TryGetProperty("timings", out var timings) &&
                   timings.TryGetProperty("predicted_per_second", out var speed)
                ? speed.GetDouble()
                : null;
        }
        catch (Exception) when (!token.IsCancellationRequested) { return null; }
    }

    private async Task DrainStandardErrorAsync(Process process)
    {
        while (await process.StandardError.ReadLineAsync() is { } line)
        {
            line = RedactSecret(line, _diagnosticSecret);
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

    /// <summary>进日志的那一份诊断：先按已知密钥再遮一遍（子进程在 <c>_diagnosticSecret</c> 赋值前就吐出的行
    /// 没经过逐行遮蔽），再截尾。日志文件不是诊断缓冲区的副本。</summary>
    private string LoggedDiagnostics()
    {
        var text = RedactSecret(GetDiagnostics(), _diagnosticSecret);
        return text.Length <= MaximumLoggedDiagnosticCharacters
            ? text
            : "[...]" + text[^MaximumLoggedDiagnosticCharacters..];
    }

    internal const string RedactedPlaceholder = "[redacted]";

    /// <summary>把已知密钥从任何要离开本类的文本里换掉。子进程 stderr 与日志共用这一条路径：
    /// 遮蔽规则分成两套时，漏掉的那一套就是泄露点。</summary>
    internal static string RedactSecret(string text, string? secret) =>
        string.IsNullOrEmpty(secret) ? text : text.Replace(secret, RedactedPlaceholder, StringComparison.Ordinal);

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

public sealed record LlamaServerStartupMetrics(double? TokensPerSecond);

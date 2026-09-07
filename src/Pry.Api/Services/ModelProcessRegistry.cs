using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Pry.Core.Abstractions;
using Pry.Core.Inference;
using Pry.Core.Models;

namespace Pry.Api.Services;

public sealed class ModelProcessRegistry(ILogger<ModelProcessRegistry> logger, ModelPerformancePolicy policy) : IAsyncDisposable
{
    public ModelProcessRegistry(ILogger<ModelProcessRegistry> logger) : this(logger, new ModelPerformancePolicy()) { }

    private readonly ConcurrentDictionary<string, Lazy<Task<ModelHandle>>> _models = new(StringComparer.Ordinal);

    public Task<ModelHandle> GetAsync(string executablePath, ModelProfile profile, CancellationToken token)
    {
        var key = BuildKey(profile);
        var lazy = _models.GetOrAdd(key, _ => new Lazy<Task<ModelHandle>>(() => StartAsync(executablePath, profile, token),
            LazyThreadSafetyMode.ExecutionAndPublication));
        return AwaitAndRemoveFailedAsync(key, lazy);
    }

    private async Task<ModelHandle> AwaitAndRemoveFailedAsync(string key, Lazy<Task<ModelHandle>> lazy)
    {
        try { return await lazy.Value; }
        catch { _models.TryRemove(new KeyValuePair<string, Lazy<Task<ModelHandle>>>(key, lazy)); throw; }
    }

    private async Task<ModelHandle> StartAsync(string executablePath, ModelProfile source, CancellationToken token)
    {
        var profile = source;
        LlamaServerManager? server = null;
        if (profile.Provider == "local-llama")
        {
            profile = profile with { BaseUrl = WithFreePort(profile.BaseUrl), ApiKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)) };
            ModelRuntimeException? lastFailure = null;
            var device = await LlamaHardwareDetector.ResolveAsync(executablePath, profile.ComputeDevice, token);
            var hardwareTier = policy.Classify(device);
            foreach (var attemptedProfile in policy.CreateCandidates(profile, device))
            {
                server = new LlamaServerManager();
                try
                {
                    var metrics = await server.StartAsync(executablePath, attemptedProfile, token);
                    if (policy.IsTooSlow(attemptedProfile, metrics))
                        throw new ModelRuntimeException("context_performance_low",
                            "当前上下文配置的生成速度过低，正在尝试更合适的档位。", true,
                            $"Measured generation speed: {metrics.TokensPerSecond:F2} tokens/s at context {attemptedProfile.ContextSize}.");
                    profile = attemptedProfile;
                    if (profile.ContextSize != source.ContextSize)
                        logger.LogWarning("Model {ModelId} started with reduced context {ContextSize} after the requested context could not be allocated",
                            profile.Id, profile.ContextSize);
                    var adjustmentReason = profile.ContextSize == source.ContextSize ? null : lastFailure?.Code ?? $"hardware_{hardwareTier.Id}";
                    lastFailure = null;
                    var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
                    var model = new OpenAiCompatibleChatModel(client, profile);
                    logger.LogInformation("Model {ModelId} registered using {Provider}", profile.Id, profile.Provider);
                    return new ModelHandle(model, server, client, source.ContextSize, profile.ContextSize,
                        metrics.TokensPerSecond, adjustmentReason);
                }
                catch (ModelRuntimeException ex) when (policy.MayTryLowerContext(ex) && attemptedProfile.ContextSize > 4096)
                {
                    lastFailure = ex;
                    logger.LogWarning("Model {ModelId} could not start with context {ContextSize} ({ErrorCode}); retrying with a smaller context",
                        profile.Id, attemptedProfile.ContextSize, ex.Code);
                    await server.DisposeAsync();
                    server = null;
                }
                catch (ModelRuntimeException ex)
                {
                    if (!string.IsNullOrWhiteSpace(ex.DiagnosticDetails))
                        logger.LogError("Model {ModelId} failed with {ErrorCode}: {Diagnostics}", profile.Id, ex.Code,
                            ex.DiagnosticDetails);
                    await server.DisposeAsync();
                    throw;
                }
                catch
                {
                    await server.DisposeAsync();
                    throw;
                }
            }
            if (lastFailure is not null || server is null)
                throw lastFailure ?? new ModelRuntimeException("process_start_failed", "无法启动本地模型服务。", true);
        }
        var remoteClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        var remoteModel = new OpenAiCompatibleChatModel(remoteClient, profile);
        logger.LogInformation("Model {ModelId} registered using {Provider}", profile.Id, profile.Provider);
        return new ModelHandle(remoteModel, server, remoteClient, profile.ContextSize, profile.ContextSize, null, null);
    }

    private static string BuildKey(ModelProfile p) => string.Join('|', p.Provider, p.Id, p.ModelPath, p.MmprojPath,
        p.ContextSize, p.GpuLayers, p.ComputeDevice, p.EnableThinking, p.BaseUrl);

    private static string WithFreePort(string baseUrl)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        return new UriBuilder(new Uri(baseUrl)) { Port = port }.Uri.ToString().TrimEnd('/');
    }

    public async ValueTask DisposeAsync()
    {
        await ResetAsync();
    }

    public async Task ResetAsync()
    {
        foreach (var lazy in _models.Values)
        {
            if (!lazy.IsValueCreated) continue;
            try { await (await lazy.Value).DisposeAsync(); } catch (Exception ex) { logger.LogWarning(ex, "Failed to stop a model process"); }
        }
        _models.Clear();
    }
}

public sealed class ModelHandle(IChatModel model, LlamaServerManager? server, HttpClient client,
    int requestedContextSize, int effectiveContextSize, double? measuredTokensPerSecond, string? adjustmentReason) : IAsyncDisposable
{
    public IChatModel Model { get; } = model;
    public string ComputeDevice => server?.ActiveComputeDevice ?? "API";
    public int RequestedContextSize { get; } = requestedContextSize;
    public int EffectiveContextSize { get; } = effectiveContextSize;
    public double? MeasuredTokensPerSecond { get; } = measuredTokensPerSecond;
    public string? AdjustmentReason { get; } = adjustmentReason;
    public async ValueTask DisposeAsync() { if (server is not null) await server.DisposeAsync(); client.Dispose(); }
}

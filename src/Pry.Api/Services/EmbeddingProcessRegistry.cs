using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Pry.Core.Abstractions;
using Pry.Core.Inference;
using Pry.Core.Models;

namespace Pry.Api.Services;

public sealed class EmbeddingProcessRegistry(ILogger<EmbeddingProcessRegistry> logger) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, Lazy<Task<EmbeddingHandle>>> _models = new(StringComparer.Ordinal);

    public Task<EmbeddingHandle> GetAsync(string executablePath, ModelProfile profile, CancellationToken token)
    {
        var key = string.Join('|', "embedding", profile.Provider, profile.Id, profile.ModelPath, profile.ContextSize,
            profile.GpuLayers, profile.ComputeDevice, profile.BaseUrl);
        var lazy = _models.GetOrAdd(key, _ => new Lazy<Task<EmbeddingHandle>>(
            () => StartAsync(executablePath, profile, token), LazyThreadSafetyMode.ExecutionAndPublication));
        return AwaitAndRemoveFailedAsync(key, lazy);
    }

    private async Task<EmbeddingHandle> AwaitAndRemoveFailedAsync(string key, Lazy<Task<EmbeddingHandle>> lazy)
    {
        try { return await lazy.Value; }
        catch { _models.TryRemove(new KeyValuePair<string, Lazy<Task<EmbeddingHandle>>>(key, lazy)); throw; }
    }

    private async Task<EmbeddingHandle> StartAsync(string executablePath, ModelProfile profile, CancellationToken token)
    {
        LlamaServerManager? server = null;
        if (profile.Provider == "local-llama")
        {
            profile = profile with
            {
                BaseUrl = WithFreePort(profile.BaseUrl),
                ApiKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
                ContextSize = Math.Clamp(profile.ContextSize, 512, 8192),
                EnableThinking = false
            };
            server = new LlamaServerManager(logger);
            try { await server.StartAsync(executablePath, profile, token, embeddingsOnly: true); }
            catch { await server.DisposeAsync(); throw; }
        }
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        logger.LogInformation("Embedding model {ModelId} registered using {Provider}", profile.Id, profile.Provider);
        return new EmbeddingHandle(new OpenAiCompatibleEmbeddingProvider(client, profile), server, client);
    }

    private static string WithFreePort(string baseUrl)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        return new UriBuilder(new Uri(baseUrl)) { Port = port }.Uri.ToString().TrimEnd('/');
    }

    public async Task ResetAsync()
    {
        foreach (var lazy in _models.Values)
            if (lazy.IsValueCreated) try { await (await lazy.Value).DisposeAsync(); }
                catch (Exception ex) { logger.LogWarning(ex, "Failed to stop an embedding process"); }
        _models.Clear();
    }

    public async ValueTask DisposeAsync() => await ResetAsync();
}

public sealed class EmbeddingHandle(IEmbeddingProvider provider, LlamaServerManager? server, HttpClient client)
    : IAsyncDisposable
{
    public IEmbeddingProvider Provider { get; } = provider;
    public string ComputeDevice => server?.ActiveComputeDevice ?? "API";
    public async ValueTask DisposeAsync() { if (server is not null) await server.DisposeAsync(); client.Dispose(); }
}

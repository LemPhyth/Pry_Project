using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Pry.Core.Abstractions;
using Pry.Core.Inference;
using Pry.Core.Models;

namespace Pry.Api.Services;

public sealed class ModelProcessRegistry(ILogger<ModelProcessRegistry> logger) : IAsyncDisposable
{
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
            server = new LlamaServerManager();
            try { await server.StartAsync(executablePath, profile, token); }
            catch { await server.DisposeAsync(); throw; }
        }
        var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        var model = new OpenAiCompatibleChatModel(client, profile);
        logger.LogInformation("Model {ModelId} registered using {Provider}", profile.Id, profile.Provider);
        return new ModelHandle(model, server, client);
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

public sealed class ModelHandle(IChatModel model, LlamaServerManager? server, HttpClient client) : IAsyncDisposable
{
    public IChatModel Model { get; } = model;
    public string ComputeDevice => server?.ActiveComputeDevice ?? "API";
    public async ValueTask DisposeAsync() { if (server is not null) await server.DisposeAsync(); client.Dispose(); }
}

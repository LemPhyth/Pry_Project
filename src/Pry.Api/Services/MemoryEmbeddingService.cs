using Pry.Contracts;
using Pry.Core.Abstractions;
using Pry.Core.Inference;
using Pry.Core.Memory;
using Pry.Core.Models;

namespace Pry.Api.Services;

public sealed class MemoryEmbeddingService(MemoryDatabase database, BackendRuntime runtime,
    EmbeddingProcessRegistry registry, ILogger<MemoryEmbeddingService> logger) : BackgroundService, IMemoryRetriever
{
    private readonly SemaphoreSlim _indexGate = new(1, 1);
    private readonly SemaphoreSlim _providerGate = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private readonly object _stateGate = new();
    private EmbeddingHandle? _handle;
    private Task? _requestedIndexing;
    private string _state = "disabled";
    private string? _errorCode;
    private bool _retryable;

    public async Task<EmbeddingStatusResponse> GetStatusAsync(CancellationToken token)
    {
        var modelId = runtime.ActiveEmbeddingModelId;
        var counts = modelId is null ? (0, 0) : await database.GetMemoryEmbeddingCountsAsync(modelId, token);
        lock (_stateGate)
            return new EmbeddingStatusResponse(modelId is not null, modelId is null ? "disabled" :
                _state == "disabled" ? "idle" : _state, modelId,
                counts.Item1, counts.Item2, _handle?.Provider.Dimensions is > 0 ? _handle.Provider.Dimensions : null,
                _errorCode, _retryable);
    }

    public async Task<EmbeddingStatusResponse> RequestReindexAsync(bool clear, CancellationToken token)
    {
        _ = runtime.ActiveEmbeddingModelId ?? throw new ApiValidationException("modelId", "尚未启用 Embedding 模型");
        lock (_stateGate)
        {
            if (_requestedIndexing is null || _requestedIndexing.IsCompleted || clear)
            {
                var previous = _requestedIndexing;
                _state = "indexing"; _errorCode = null; _retryable = false;
                _requestedIndexing = Task.Run(() => RunRequestedIndexAsync(clear, previous, _lifetime.Token));
            }
        }
        return await GetStatusAsync(token);
    }

    private async Task RunRequestedIndexAsync(bool clear, Task? previous, CancellationToken token)
    {
        try
        {
            if (previous is { IsCompleted: false })
                try { await previous.WaitAsync(token); } catch (Exception) when (!token.IsCancellationRequested) { }
            if (clear)
            {
                var modelId = runtime.ActiveEmbeddingModelId;
                if (modelId is null) return;
                await _indexGate.WaitAsync(token);
                try { await database.ClearMemoryEmbeddingsAsync(modelId, token); }
                finally { _indexGate.Release(); }
            }
            await IndexPendingAsync(drain: true, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex)
        {
            // 索引这条路径自己每 30 秒重试，所以失败一律可重试；错误码要具体，界面才知道是密钥错了还是模型没了。
            SetState("degraded", ModelProviderError.CodeOf(ex, "embedding_index_failed"), true);
            logger.LogWarning(ex, "Requested memory embedding index failed");
        }
    }

    public async Task ResetAsync()
    {
        await _indexGate.WaitAsync();
        try
        {
            _handle = null;
            await registry.ResetAsync();
            SetState(runtime.ActiveEmbeddingModelId is null ? "disabled" : "idle", null, false);
        }
        finally { _indexGate.Release(); }
    }

    public async Task<IReadOnlyList<MemoryRecord>> SearchMemoriesAsync(string characterId, string query, int count = 6,
        CancellationToken cancellationToken = default)
    {
        var keyword = await database.SearchMemoriesAsync(characterId, query, count, cancellationToken);
        if (runtime.ActiveEmbeddingModelId is null || string.IsNullOrWhiteSpace(query)) return keyword;
        try
        {
            await IndexPendingAsync(drain: false, cancellationToken);
            var provider = await GetProviderAsync(cancellationToken);
            var queryVector = await EncodeAsync(provider, query, cancellationToken);
            var vectors = await database.ListMemoryVectorsAsync(characterId, provider.ModelId, cancellationToken);
            var semantic = vectors.Where(item => item.Vector.Length == queryVector.Length)
                .Select(item => (item.Memory, Score: Cosine(queryVector, item.Vector)))
                .OrderByDescending(item => item.Score).Take(Math.Max(count * 3, count)).ToArray();
            var scores = new Dictionary<long, (MemoryRecord Memory, double Score)>();
            for (var index = 0; index < keyword.Count; index++)
                scores[keyword[index].Id] = (keyword[index], .45 * (1d - index / (double)Math.Max(1, keyword.Count)));
            foreach (var item in semantic)
            {
                var semanticScore = .55 * Math.Clamp((item.Score + 1d) / 2d, 0, 1) + .1 * item.Memory.Importance;
                if (scores.TryGetValue(item.Memory.Id, out var existing)) semanticScore += existing.Score;
                scores[item.Memory.Id] = (item.Memory, semanticScore);
            }
            var result = scores.Values.OrderByDescending(item => item.Score).Take(count).Select(item => item.Memory).ToArray();
            await database.TouchMemoriesAsync(result.Select(item => item.Id), cancellationToken);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // 检索这条路径不会自己重来，所以可重试性必须照实说：密钥错与网络抖动在界面上是两种提示。
            SetState("degraded", ModelProviderError.CodeOf(ex, "embedding_unavailable"),
                ModelProviderError.IsRetryable(ex));
            logger.LogWarning(ex, "Embedding retrieval failed; using keyword memory retrieval");
            return keyword;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (runtime.ActiveEmbeddingModelId is not null) await IndexPendingAsync(drain: true, stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                SetState("degraded", ModelProviderError.CodeOf(ex, "embedding_index_failed"), true);
                logger.LogWarning(ex, "Background memory embedding index failed");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task IndexPendingAsync(bool drain, CancellationToken token)
    {
        if (runtime.ActiveEmbeddingModelId is null) return;
        await _indexGate.WaitAsync(token);
        try
        {
            var modelId = runtime.ActiveEmbeddingModelId;
            if (modelId is null) return;
            var pending = await database.ListPendingMemoryEmbeddingsAsync(modelId, 32, token);
            if (pending.Count == 0) { SetState("ready", null, false); return; }
            var provider = await GetProviderAsync(token);
            SetState("indexing", null, false);
            do
            {
                foreach (var source in pending)
                {
                    var vector = await EncodeAsync(provider, source.Content, token);
                    await database.UpsertMemoryEmbeddingAsync(source, provider.ModelId, vector, token);
                }
                if (!drain) break;
                pending = await database.ListPendingMemoryEmbeddingsAsync(provider.ModelId, 32, token);
                if (pending.Count == 0) break;
            } while (true);
            SetState("ready", null, false);
        }
        finally { _indexGate.Release(); }
    }

    private async Task<IEmbeddingProvider> GetProviderAsync(CancellationToken token)
    {
        if (_handle is not null && _handle.Provider.ModelId == runtime.ActiveEmbeddingModelId) return _handle.Provider;
        var selected = runtime.ResolveEmbeddingRuntime();
        _handle = await registry.GetAsync(selected.ExecutablePath, selected.Profile, token);
        return _handle.Provider;
    }

    private async Task<float[]> EncodeAsync(IEmbeddingProvider provider, string text, CancellationToken token)
    {
        await _providerGate.WaitAsync(token);
        try { return await provider.EncodeAsync(text, token); }
        finally { _providerGate.Release(); }
    }

    private void SetState(string state, string? errorCode, bool retryable)
    {
        lock (_stateGate) { _state = state; _errorCode = errorCode; _retryable = retryable; }
    }

    private static double Cosine(float[] left, float[] right)
    {
        double dot = 0, leftNorm = 0, rightNorm = 0;
        for (var index = 0; index < left.Length; index++)
        {
            dot += left[index] * right[index];
            leftNorm += left[index] * left[index];
            rightNorm += right[index] * right[index];
        }
        return leftNorm <= 0 || rightNorm <= 0 ? -1 : dot / Math.Sqrt(leftNorm * rightNorm);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _lifetime.Cancel();
        await base.StopAsync(cancellationToken);
        Task? requested;
        lock (_stateGate) requested = _requestedIndexing;
        if (requested is not null)
            try { await requested.WaitAsync(cancellationToken); }
            catch (OperationCanceledException) { }
        await registry.ResetAsync();
    }
}

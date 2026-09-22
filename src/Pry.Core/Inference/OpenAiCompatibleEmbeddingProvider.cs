using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Pry.Core.Abstractions;
using Pry.Core.Models;

namespace Pry.Core.Inference;

public sealed class OpenAiCompatibleEmbeddingProvider(HttpClient client, ModelProfile profile) : IEmbeddingProvider
{
    private int _dimensions;
    public string ModelId => profile.Id;
    public int Dimensions => Volatile.Read(ref _dimensions);

    public async Task<float[]> EncodeAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Embedding input cannot be empty.", nameof(text));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{profile.BaseUrl.TrimEnd('/')}/embeddings");
        if (!string.IsNullOrWhiteSpace(profile.ApiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", profile.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(new { model = profile.ModelName, input = text }),
            Encoding.UTF8, "application/json");
        string content;
        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                // 向量服务的错误体同样可能回显凭据；取有界片段并遮密钥后才归类。
                var excerpt = await response.Content.ReadAsStringAsync(cancellationToken);
                throw ModelProviderError.FromStatus(response.StatusCode,
                    LlamaServerManager.RedactSecret(excerpt, profile.ApiKey),
                    response.Headers.RetryAfter?.Delta, profile.Id);
            }
            content = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw ex switch
            {
                ModelProviderException provider => provider,
                // 只有调用方真的取消才按取消传播。HttpClient 自己的总超时同样表现为 TaskCanceledException，
                // 一律原样抛出去就是把"有界超时"重新写成一次没有原因的取消。
                OperationCanceledException when cancellationToken.IsCancellationRequested => ex,
                _ => ModelProviderError.FromException(ex, cancellationToken.IsCancellationRequested, profile.Id)
            };
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(content);
        }
        catch (JsonException ex)
        {
            throw ModelProviderError.InvalidResponse("响应不是合法 JSON", profile.Id, ex);
        }
        using (document)
        {
            if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array ||
                data.GetArrayLength() == 0 || !data[0].TryGetProperty("embedding", out var embedding) ||
                embedding.ValueKind != JsonValueKind.Array)
                throw ModelProviderError.InvalidResponse("响应缺少 data[0].embedding", profile.Id);
            var vector = embedding.EnumerateArray().Select(value => value.GetSingle()).ToArray();
            if (vector.Length == 0 || vector.Any(value => !float.IsFinite(value)))
                throw ModelProviderError.InvalidResponse("向量含非有限数值", profile.Id);
            Interlocked.CompareExchange(ref _dimensions, vector.Length, 0);
            if (Dimensions != vector.Length)
                throw ModelProviderError.InvalidResponse("维度与首答不一致", profile.Id);
            return vector;
        }
    }
}

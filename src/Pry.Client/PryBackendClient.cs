using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pry.Contracts;
using Pry.Core.Models;

namespace Pry.Client;

public sealed class PryBackendClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<bool> IsHealthyAsync(CancellationToken token = default)
    {
        try { using var response = await httpClient.GetAsync("health", token); return response.IsSuccessStatusCode; }
        catch (HttpRequestException) { return false; }
    }

    public Task<RuntimeStatusResponse> GetRuntimeAsync(CancellationToken token = default) => GetAsync<RuntimeStatusResponse>("api/v1/runtime", token);
    public Task<IReadOnlyList<ConversationRoom>> GetConversationsAsync(int limit = 100, CancellationToken token = default) => GetAsync<IReadOnlyList<ConversationRoom>>($"api/v1/conversations?limit={Math.Clamp(limit, 1, 200)}", token);
    public Task<ConversationRoom> GetConversationAsync(string id, CancellationToken token = default) => GetAsync<ConversationRoom>($"api/v1/conversations/{Escape(id)}", token);
    public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string id, int limit = 200, CancellationToken token = default) => GetAsync<IReadOnlyList<ChatMessage>>($"api/v1/conversations/{Escape(id)}/messages?limit={Math.Clamp(limit, 1, 500)}", token);
    public Task<ConversationRoom> CreateConversationAsync(string? characterId, CancellationToken token = default) => SendAsync<ConversationRoom>(HttpMethod.Post, "api/v1/conversations", new CreateConversationRequest(characterId), token);
    public Task<ConversationRoom> UpdateConversationAsync(string id, UpdateConversationRequest request, CancellationToken token = default) => SendAsync<ConversationRoom>(HttpMethod.Patch, $"api/v1/conversations/{Escape(id)}", request, token);
    public Task DeleteConversationAsync(string id, CancellationToken token = default) => SendNoContentAsync(HttpMethod.Delete, $"api/v1/conversations/{Escape(id)}", null, token);
    public Task<SubmitTurnResponse> SubmitTurnAsync(string id, SubmitTurnRequest request, CancellationToken token = default) => SendAsync<SubmitTurnResponse>(HttpMethod.Post, $"api/v1/conversations/{Escape(id)}/turns", request, token);
    public Task CancelTurnAsync(string id, CancellationToken token = default) => SendNoContentAsync(HttpMethod.Post, $"api/v1/conversations/{Escape(id)}/turns/cancel", null, token);
    public Task<ConversationMutationResponse> DeleteMessageAsync(string conversationId, long messageId, CancellationToken token = default) => SendAsync<ConversationMutationResponse>(HttpMethod.Delete, $"api/v1/conversations/{Escape(conversationId)}/messages/{messageId}", null, token);
    public Task RegenerateAsync(string conversationId, long messageId, CancellationToken token = default) => SendNoContentAsync(HttpMethod.Post, $"api/v1/conversations/{Escape(conversationId)}/messages/{messageId}/regenerate", null, token);
    public Task UndoAsync(string conversationId, CancellationToken token = default) => SendNoContentAsync(HttpMethod.Post, $"api/v1/conversations/{Escape(conversationId)}/mutations/undo", null, token);
    public Task<ChatMessage> SendListeningSignalAsync(string conversationId, CancellationToken token = default) => SendAsync<ChatMessage>(HttpMethod.Post, $"api/v1/conversations/{Escape(conversationId)}/listening-signals", null, token);

    public Task<IReadOnlyList<CharacterSummaryResponse>> GetCharactersAsync(CancellationToken token = default) => GetAsync<IReadOnlyList<CharacterSummaryResponse>>("api/v1/characters", token);
    public Task<CharacterResponse> GetCharacterAsync(string id, CancellationToken token = default) => GetAsync<CharacterResponse>($"api/v1/characters/{Escape(id)}", token);
    public Task<CharacterResponse> CreateCharacterAsync(SaveCharacterRequest request, CancellationToken token = default) => SendAsync<CharacterResponse>(HttpMethod.Post, "api/v1/characters", request, token);
    public Task<CharacterResponse> UpdateCharacterAsync(string id, SaveCharacterRequest request, CancellationToken token = default) => SendAsync<CharacterResponse>(HttpMethod.Put, $"api/v1/characters/{Escape(id)}", request, token);
    public Task DeleteCharacterAsync(string id, CancellationToken token = default) => SendNoContentAsync(HttpMethod.Delete, $"api/v1/characters/{Escape(id)}", null, token);
    public Task<ClientPreferencesResponse> GetPreferencesAsync(CancellationToken token = default) => GetAsync<ClientPreferencesResponse>("api/v1/preferences", token);
    public Task<ClientPreferencesResponse> UpdatePreferencesAsync(UpdateClientPreferencesRequest request, CancellationToken token = default) => SendAsync<ClientPreferencesResponse>(HttpMethod.Patch, "api/v1/preferences", request, token);
    public Task<IReadOnlyList<ModelProfileResponse>> GetModelsAsync(CancellationToken token = default) => GetAsync<IReadOnlyList<ModelProfileResponse>>("api/v1/models", token);
    public Task<IReadOnlyList<ModelProfileResponse>> UpdateModelSelectionAsync(UpdateModelSelectionRequest request, CancellationToken token = default) => SendAsync<IReadOnlyList<ModelProfileResponse>>(HttpMethod.Put, "api/v1/models/selection", request, token);
    public Task<IReadOnlyList<StickerResponse>> GetStickersAsync(CancellationToken token = default) => GetAsync<IReadOnlyList<StickerResponse>>("api/v1/stickers", token);
    public Task<IReadOnlyList<MemoryRecord>> GetMemoriesAsync(string characterId, string? query = null, CancellationToken token = default) => GetAsync<IReadOnlyList<MemoryRecord>>($"api/v1/memories?characterId={Escape(characterId)}&query={Escape(query ?? "")}", token);
    public Task<IReadOnlyList<SpeechModelResponse>> GetSpeechModelsAsync(CancellationToken token = default) => GetAsync<IReadOnlyList<SpeechModelResponse>>("api/v1/speech/models", token);
    public Task<TranscribeSpeechResponse> TranscribeAsync(string mediaId, string? modelId = null, CancellationToken token = default) => SendAsync<TranscribeSpeechResponse>(HttpMethod.Post, "api/v1/speech/transcriptions", new TranscribeSpeechRequest(mediaId, modelId), token);
    public Task<MediaUploadPolicyResponse> GetMediaPolicyAsync(CancellationToken token = default) => GetAsync<MediaUploadPolicyResponse>("api/v1/media/policy", token);
    public Task<IReadOnlyList<ConversationFolder>> GetFoldersAsync(CancellationToken token = default) => GetAsync<IReadOnlyList<ConversationFolder>>("api/v1/conversation-folders", token);
    public Task<ConversationFolder> CreateFolderAsync(string name, CancellationToken token = default) => SendAsync<ConversationFolder>(HttpMethod.Post, "api/v1/conversation-folders", new CreateFolderRequest(name), token);
    public Task RenameFolderAsync(string id, string name, CancellationToken token = default) => SendNoContentAsync(HttpMethod.Patch, $"api/v1/conversation-folders/{Escape(id)}", new RenameFolderRequest(name), token);
    public Task DeleteFolderAsync(string id, CancellationToken token = default) => SendNoContentAsync(HttpMethod.Delete, $"api/v1/conversation-folders/{Escape(id)}", null, token);
    public Task<MemoryRecord> CreateMemoryAsync(CreateMemoryRequest request, CancellationToken token = default) => SendAsync<MemoryRecord>(HttpMethod.Post, "api/v1/memories", request, token);
    public Task<MemoryRecord> UpdateMemoryAsync(long id, string characterId, UpdateMemoryRequest request, CancellationToken token = default) => SendAsync<MemoryRecord>(HttpMethod.Put, $"api/v1/memories/{id}?characterId={Escape(characterId)}", request, token);
    public Task DeleteMemoryAsync(long id, string characterId, CancellationToken token = default) => SendNoContentAsync(HttpMethod.Delete, $"api/v1/memories/{id}?characterId={Escape(characterId)}", null, token);

    public async Task<MediaAssetResponse> UploadAsync(Stream content, string fileName, string contentType,
        CancellationToken token = default)
    {
        using var form = new MultipartFormDataContent(); using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(streamContent, "file", fileName);
        using var response = await httpClient.PostAsync("api/v1/media", form, token);
        return await ReadAsync<MediaAssetResponse>(response, token);
    }

    public async IAsyncEnumerable<ConversationEvent> ReadEventsAsync(string conversationId, long after = 0,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/conversations/{Escape(conversationId)}/events?after={after}");
        request.Headers.Accept.ParseAdd("text/event-stream");
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        await EnsureSuccessAsync(response, token);
        await using var stream = await response.Content.ReadAsStreamAsync(token); using var reader = new StreamReader(stream);
        string? data = null;
        while (!token.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(token);
            if (line is null)
            {
                if (data is not null) yield return ParseEvent(data);
                yield break;
            }
            if (line.StartsWith("data: ", StringComparison.Ordinal)) data = line[6..];
            else if (line.Length == 0 && data is not null)
            {
                var item = ParseEvent(data); data = null; yield return item;
            }
        }
    }

    private async Task<T> GetAsync<T>(string uri, CancellationToken token)
    {
        using var response = await httpClient.GetAsync(uri, token); return await ReadAsync<T>(response, token);
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string uri, object? body, CancellationToken token)
    {
        using var request = new HttpRequestMessage(method, uri) { Content = body is null ? null : JsonContent.Create(body, options: JsonOptions) };
        using var response = await httpClient.SendAsync(request, token); return await ReadAsync<T>(response, token);
    }

    private async Task SendNoContentAsync(HttpMethod method, string uri, object? body, CancellationToken token)
    {
        using var request = new HttpRequestMessage(method, uri) { Content = body is null ? null : JsonContent.Create(body, options: JsonOptions) };
        using var response = await httpClient.SendAsync(request, token); await EnsureSuccessAsync(response, token);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken token)
    {
        await EnsureSuccessAsync(response, token);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, token)
               ?? throw new InvalidDataException("后端返回了空响应。");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken token)
    {
        if (response.IsSuccessStatusCode) return;
        ApiProblemResponse? problem = null;
        try { problem = await response.Content.ReadFromJsonAsync<ApiProblemResponse>(JsonOptions, token); } catch (JsonException) { }
        throw new PryBackendException(response.StatusCode, problem?.Code, problem?.Detail ?? response.ReasonPhrase,
            problem?.TraceId);
    }

    private static string Escape(string value) => Uri.EscapeDataString(value);
    private static ConversationEvent ParseEvent(string data) => JsonSerializer.Deserialize<ConversationEvent>(data, JsonOptions)
        ?? throw new InvalidDataException("后端返回了无效事件。");
}

public sealed class PryBackendException(HttpStatusCode statusCode, string? code, string? message, string? traceId)
    : Exception(message ?? $"Pry backend returned {(int)statusCode}")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string? Code { get; } = code;
    public string? TraceId { get; } = traceId;
}

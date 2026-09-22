using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pry.Contracts;

namespace Pry.Client;

public sealed class PryBackendClient
{
    private readonly HttpClient httpClient;
    private readonly TimeSpan defaultRequestBudget;
    private readonly TimeSpan longRequestBudget;

    /// <param name="requestBudget">
    /// 普通请求预算的注入点；默认取 <see cref="PryProtocol.BackendRequestTimeoutMs"/>。
    /// </param>
    /// <param name="longRequestBudget">
    /// 长请求（语音识别）预算的注入点；默认取 <see cref="PryProtocol.TranscriptionRequestTimeoutMs"/>。
    /// 两个注入点都是给测试在毫秒级判"到点报成什么"用的，产品侧一律走默认值，且都不接受无界。
    /// </param>
    public PryBackendClient(HttpClient httpClient, TimeSpan? requestBudget = null, TimeSpan? longRequestBudget = null)
    {
        this.httpClient = httpClient;
        this.defaultRequestBudget = requestBudget ?? TimeSpan.FromMilliseconds(PryProtocol.BackendRequestTimeoutMs);
        this.longRequestBudget = longRequestBudget ?? TimeSpan.FromMilliseconds(PryProtocol.TranscriptionRequestTimeoutMs);
        // 每个请求声明本界面会说哪一版协议，服务端据此拒绝不兼容组合，而不是静默错配字段。
        if (!httpClient.DefaultRequestHeaders.Contains(PryProtocol.ClientHeader))
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation(PryProtocol.ClientHeader, PryProtocol.Version);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// 供无参构造器（设计器与不触网的界面单测）使用的占位客户端：不指向任何可用端口，
    /// 所以一次意外请求会直接失败，而不是连上"恰好在本机 5078 上的某个进程"。
    /// 真实路径必须注入内嵌或经 <see cref="RuntimeDiscovery"/> 发现的客户端，界面层不再自带端口。
    /// </summary>
    public static PryBackendClient Unattached() => new(new HttpClient
    {
        BaseAddress = new Uri("http://127.0.0.1:0/"),
        Timeout = TimeSpan.FromMilliseconds(PryProtocol.BackendRequestTimeoutMs)
    });

    public Task<ProtocolInfoResponse> GetProtocolInfoAsync(CancellationToken token = default) =>
        GetAsync<ProtocolInfoResponse>("api/v1/info", token);

    public async Task<bool> IsHealthyAsync(CancellationToken token = default)
    {
        try { using var response = await httpClient.GetAsync("health", token); return response.IsSuccessStatusCode; }
        catch (HttpRequestException) { return false; }
    }

    public Task<RuntimeStatusResponse> GetRuntimeAsync(CancellationToken token = default) => GetAsync<RuntimeStatusResponse>("api/v1/runtime", token);
    public Task<ConversationReadStateResponse> GetConversationReadStateAsync(string id, CancellationToken token = default) =>
        GetAsync<ConversationReadStateResponse>($"api/v1/conversations/{Escape(id)}/read-state", token);
    public Task<ConversationReadStateResponse> MarkConversationReadAsync(string id, long throughMessageId,
        CancellationToken token = default) => SendAsync<ConversationReadStateResponse>(HttpMethod.Put,
        $"api/v1/conversations/{Escape(id)}/read-state", new MarkConversationReadRequest(throughMessageId), token);
    public Task<UnreadSummaryResponse> GetUnreadSummaryAsync(CancellationToken token = default) =>
        GetAsync<UnreadSummaryResponse>("api/v1/conversations/unread-summary", token);
    public Task<ProactivePolicyResponse> GetProactivePolicyAsync(string characterId, CancellationToken token = default) =>
        GetAsync<ProactivePolicyResponse>($"api/v1/characters/{Escape(characterId)}/proactive-policy", token);
    public Task<ProactiveStatusResponse> GetProactiveStatusAsync(string characterId, CancellationToken token = default) =>
        GetAsync<ProactiveStatusResponse>($"api/v1/characters/{Escape(characterId)}/proactive-status", token);
    public Task<ProactivePolicyResponse> SaveProactivePolicyAsync(string characterId, SaveProactivePolicyRequest request,
        CancellationToken token = default) => SendAsync<ProactivePolicyResponse>(HttpMethod.Put,
        $"api/v1/characters/{Escape(characterId)}/proactive-policy", request, token);
    public Task<CompanionActivityResponse> ReportCompanionActivityAsync(string clientId, ReportCompanionActivityRequest request,
        CancellationToken token = default) => SendAsync<CompanionActivityResponse>(HttpMethod.Put,
        $"api/v1/companion/activity/{Escape(clientId)}", request, token);
    public Task RemoveCompanionActivityAsync(string clientId, CancellationToken token = default) =>
        SendNoContentAsync(HttpMethod.Delete, $"api/v1/companion/activity/{Escape(clientId)}", null, token);
    public Task<RuntimeStatusResponse> RetryRuntimeAsync(CancellationToken token = default) => SendAsync<RuntimeStatusResponse>(HttpMethod.Post, "api/v1/runtime/retry", null, token);
    public Task<IReadOnlyList<ComputeDeviceResponse>> GetComputeDevicesAsync(CancellationToken token = default) => GetAsync<IReadOnlyList<ComputeDeviceResponse>>("api/v1/runtime/compute-devices", token);
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
    public Task<InteractionProfileResponse> GetInteractionProfileAsync(string characterId,
        CancellationToken token = default) => GetAsync<InteractionProfileResponse>(
        $"api/v1/characters/{Escape(characterId)}/interaction-profile", token);
    public Task<InteractionProfileResponse> SaveInteractionProfileAsync(string characterId,
        SaveInteractionProfileRequest request, CancellationToken token = default) =>
        SendAsync<InteractionProfileResponse>(HttpMethod.Put,
            $"api/v1/characters/{Escape(characterId)}/interaction-profile", request, token);
    public Task<InteractionCommandResponse> ExecuteInteractionAsync(string conversationId,
        ExecuteInteractionRequest request, CancellationToken token = default) =>
        SendAsync<InteractionCommandResponse>(HttpMethod.Post,
            $"api/v1/conversations/{Escape(conversationId)}/interactions", request, token);
    public Task<ClientPreferencesResponse> GetPreferencesAsync(CancellationToken token = default) => GetAsync<ClientPreferencesResponse>("api/v1/preferences", token);
    public Task<ClientPreferencesResponse> UpdatePreferencesAsync(UpdateClientPreferencesRequest request, CancellationToken token = default) => SendAsync<ClientPreferencesResponse>(HttpMethod.Patch, "api/v1/preferences", request, token);
    public Task<IReadOnlyList<ModelProfileResponse>> GetModelsAsync(CancellationToken token = default) => GetAsync<IReadOnlyList<ModelProfileResponse>>("api/v1/models", token);
    public Task<ModelScanResponse> ScanModelsAsync(CancellationToken token = default) =>
        SendAsync<ModelScanResponse>(HttpMethod.Post, "api/v1/models/scan", null, token);
    public Task<ModelProfileResponse> UpdateModelTagsAsync(string id, UpdateModelTagsRequest request,
        CancellationToken token = default) => SendAsync<ModelProfileResponse>(HttpMethod.Put,
        $"api/v1/models/{Escape(id)}/tags", request, token);
    public Task<IReadOnlyList<ModelProfileResponse>> UpdateModelSelectionAsync(UpdateModelSelectionRequest request, CancellationToken token = default) => SendAsync<IReadOnlyList<ModelProfileResponse>>(HttpMethod.Put, "api/v1/models/selection", request, token);
    public Task<ClientPreferencesResponse> UpdateAppearanceAsync(UpdateAppearanceMediaRequest request, CancellationToken token = default) => SendAsync<ClientPreferencesResponse>(HttpMethod.Put, "api/v1/appearance/media", request, token);
    public Task<SaveSettingsResponse> SaveSettingsAsync(SaveSettingsRequest request, CancellationToken token = default) => SendAsync<SaveSettingsResponse>(HttpMethod.Put, "api/v1/settings", request, token);
    public Task<ClientPreferencesResponse> SaveUserProfileAsync(SaveUserProfileRequest request, CancellationToken token = default) => SendAsync<ClientPreferencesResponse>(HttpMethod.Put, "api/v1/user-profile", request, token);
    public Task<ModelProfileResponse> CreateCustomModelAsync(SaveCustomModelRequest request, CancellationToken token = default) => SendAsync<ModelProfileResponse>(HttpMethod.Post, "api/v1/models/custom", request, token);
    public Task<ModelProfileResponse> UpdateCustomModelAsync(string id, SaveCustomModelRequest request, CancellationToken token = default) => SendAsync<ModelProfileResponse>(HttpMethod.Put, $"api/v1/models/custom/{Escape(id)}", request, token);
    public Task DeleteCustomModelAsync(string id, CancellationToken token = default) => SendNoContentAsync(HttpMethod.Delete, $"api/v1/models/custom/{Escape(id)}", null, token);
    public Task<ModelProfileResponse> CreateNetworkModelAsync(SaveNetworkModelRequest request, CancellationToken token = default) => SendAsync<ModelProfileResponse>(HttpMethod.Post, "api/v1/models/network", request, token);
    public Task<ModelProfileResponse> UpdateNetworkModelAsync(string id, SaveNetworkModelRequest request, CancellationToken token = default) => SendAsync<ModelProfileResponse>(HttpMethod.Put, $"api/v1/models/network/{Escape(id)}", request, token);
    public Task DeleteNetworkModelAsync(string id, CancellationToken token = default) => SendNoContentAsync(HttpMethod.Delete, $"api/v1/models/network/{Escape(id)}", null, token);
    public Task<IReadOnlyList<StickerResponse>> GetStickersAsync(CancellationToken token = default) => GetAsync<IReadOnlyList<StickerResponse>>("api/v1/stickers", token);
    public Task<StickerResponse> ImportStickerAsync(string mediaId, string name, IReadOnlyList<string>? emotions = null, CancellationToken token = default) => SendAsync<StickerResponse>(HttpMethod.Post, "api/v1/stickers", new ImportStickerRequest(mediaId, name, emotions), token);
    public Task<StickerResponse> UpdateStickerAsync(string id, UpdateStickerRequest request, CancellationToken token = default) => SendAsync<StickerResponse>(HttpMethod.Put, $"api/v1/stickers/{Escape(id)}", request, token);
    public Task DeleteStickerAsync(string id, CancellationToken token = default) => SendNoContentAsync(HttpMethod.Delete, $"api/v1/stickers/{Escape(id)}", null, token);
    public Task<IReadOnlyList<MemoryRecord>> GetMemoriesAsync(string characterId, string? query = null, CancellationToken token = default) => GetAsync<IReadOnlyList<MemoryRecord>>($"api/v1/memories?characterId={Escape(characterId)}&query={Escape(query ?? "")}", token);
    public Task<EmbeddingStatusResponse> GetEmbeddingStatusAsync(CancellationToken token = default) =>
        GetAsync<EmbeddingStatusResponse>("api/v1/memory/embedding", token);
    public Task<EmbeddingStatusResponse> UpdateEmbeddingSettingsAsync(UpdateEmbeddingSettingsRequest request,
        CancellationToken token = default) => SendAsync<EmbeddingStatusResponse>(HttpMethod.Put,
        "api/v1/memory/embedding", request, token);
    public Task<EmbeddingStatusResponse> ReindexMemoriesAsync(bool clear = false, CancellationToken token = default) =>
        SendAsync<EmbeddingStatusResponse>(HttpMethod.Post,
            $"api/v1/memory/embedding/reindex?clear={clear.ToString().ToLowerInvariant()}", null, token);
    public Task<IReadOnlyList<SpeechModelResponse>> GetSpeechModelsAsync(CancellationToken token = default) => GetAsync<IReadOnlyList<SpeechModelResponse>>("api/v1/speech/models", token);
    public Task<SpeechModelResponse> CreateSpeechModelAsync(SaveSpeechModelRequest request, CancellationToken token = default) => SendAsync<SpeechModelResponse>(HttpMethod.Post, "api/v1/speech/models/custom", request, token);
    public Task<SpeechModelResponse> UpdateSpeechModelAsync(string id, SaveSpeechModelRequest request, CancellationToken token = default) => SendAsync<SpeechModelResponse>(HttpMethod.Put, $"api/v1/speech/models/custom/{Escape(id)}", request, token);
    public Task DeleteSpeechModelAsync(string id, CancellationToken token = default) => SendNoContentAsync(HttpMethod.Delete, $"api/v1/speech/models/custom/{Escape(id)}", null, token);
    // 后端等整个识别过程结束才回包，所以它是唯一需要长的一档；其余本机请求仍是 defaultRequestBudget。
    public Task<TranscribeSpeechResponse> TranscribeAsync(string mediaId, string? modelId = null, CancellationToken token = default) =>
        SendAsync<TranscribeSpeechResponse>(HttpMethod.Post, "api/v1/speech/transcriptions",
            new TranscribeSpeechRequest(mediaId, modelId), token, longRequest: true);
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
        using var response = await SendWithBudgetAsync(cancellation => httpClient.PostAsync("api/v1/media", form, cancellation),
            defaultRequestBudget, token);
        return await ReadAsync<MediaAssetResponse>(response, token);
    }

    public async Task<DownloadedContent> DownloadAsync(string relativeUrl, CancellationToken token = default)
    {
        var decodedUrl = Uri.UnescapeDataString(relativeUrl);
        if (!relativeUrl.StartsWith("/api/v1/", StringComparison.Ordinal) || decodedUrl.Contains("..", StringComparison.Ordinal)
            || !Uri.TryCreate(relativeUrl, UriKind.Relative, out var uri))
            throw new ArgumentException("只允许下载后端返回的相对资源 URL。", nameof(relativeUrl));
        // 这里下一刻就要把正文整体读进内存，所以不用 ResponseHeadersRead：那一档下逐请求预算只管到响应头，
        // 正文读到一半连接停住就成了永久等待。
        using var response = await SendWithBudgetAsync(cancellation => httpClient.GetAsync(uri, cancellation),
            defaultRequestBudget, token);
        await EnsureSuccessAsync(response, token);
        return new DownloadedContent(await response.Content.ReadAsByteArrayAsync(token),
            response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
    }

    public async IAsyncEnumerable<ConversationEvent> ReadEventsAsync(string conversationId, long after = 0,
        [EnumeratorCancellation] CancellationToken token = default, TimeSpan? liveness = null)
    {
        var idleDeadline = liveness ?? TimeSpan.FromMilliseconds(PryProtocol.LivenessTimeoutMs);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/conversations/{Escape(conversationId)}/events?after={after}");
        request.Headers.Accept.ParseAdd("text/event-stream");
        using var response = await OpenEventStreamAsync(request, token);
        await EnsureSuccessAsync(response, token);
        await using var stream = await response.Content.ReadAsStreamAsync(token); using var reader = new StreamReader(stream);
        string? data = null;
        while (true)
        {
            var line = await ReadLineWithLivenessAsync(reader, idleDeadline, token);
            if (line is null)
            {
                if (data is not null) yield return ParseEvent(data);
                yield break;
            }
            // 读到任意一行都算存活，所以心跳注释帧不必解析也不必转发：它唯一的作用就是让下面的等待重新开始计时。
            if (line.StartsWith("data: ", StringComparison.Ordinal)) data = line[6..];
            else if (line.Length == 0 && data is not null)
            {
                var item = ParseEvent(data); data = null; yield return item;
            }
        }
    }

    /// <summary>
    /// 事件流的起头等响应头：仍按本机请求预算判，后端收下连接却永不作答时不该拖到兜底那一档。
    /// 这个 cancellation 的作用域**就到响应头为止**（方法返回即 Dispose，不 Cancel）：流要活过整个会话，
    /// 中流静默由 <see cref="ReadLineWithLivenessAsync"/> 的存活看门狗判，若在这里留一个会到点的定时器，
    /// 一次正常的长会话订阅会被它自己掐死。
    /// </summary>
    private async Task<HttpResponseMessage> OpenEventStreamAsync(HttpRequestMessage request, CancellationToken token)
    {
        using var headers = CancellationTokenSource.CreateLinkedTokenSource(token);
        headers.CancelAfter(defaultRequestBudget);
        try
        {
            return await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, headers.Token);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            throw new TimeoutException($"本地后端在 {defaultRequestBudget.TotalSeconds:0.#} 秒内没有接受事件流订阅。");
        }
    }

    private static async Task<string?> ReadLineWithLivenessAsync(StreamReader reader, TimeSpan liveness,
        CancellationToken token)
    {
        using var readCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        var read = reader.ReadLineAsync(readCancellation.Token).AsTask();
        using var watchdog = CancellationTokenSource.CreateLinkedTokenSource(token);
        watchdog.CancelAfter(liveness);
        var expired = Task.Delay(Timeout.Infinite, watchdog.Token);
        if (await Task.WhenAny(read, expired) == read) { watchdog.Cancel(); return await read; }
        token.ThrowIfCancellationRequested();
        // 服务端不发也不关的半开连接不会让读取报错，只会永远挂着，重连因此没有触发机会。
        // 这里放弃等待并抛出：让响应流随本方法退出而释放，由上层按既有退避重新订阅。
        readCancellation.Cancel();
        try { await read; } catch (OperationCanceledException) { }
        throw new TimeoutException($"事件流在 {liveness.TotalSeconds:0.#} 秒内没有任何数据，判定与本地后端的连接已失效。");
    }

    private async Task<T> GetAsync<T>(string uri, CancellationToken token)
    {
        using var response = await SendWithBudgetAsync(cancellation => httpClient.GetAsync(uri, cancellation),
            defaultRequestBudget, token);
        return await ReadAsync<T>(response, token);
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string uri, object? body, CancellationToken token,
        bool longRequest = false)
    {
        using var request = new HttpRequestMessage(method, uri) { Content = body is null ? null : JsonContent.Create(body, options: JsonOptions) };
        using var response = await SendWithBudgetAsync(cancellation => httpClient.SendAsync(request, cancellation),
            longRequest ? longRequestBudget : defaultRequestBudget, token);
        return await ReadAsync<T>(response, token);
    }

    private async Task SendNoContentAsync(HttpMethod method, string uri, object? body, CancellationToken token)
    {
        using var request = new HttpRequestMessage(method, uri) { Content = body is null ? null : JsonContent.Create(body, options: JsonOptions) };
        using var response = await SendWithBudgetAsync(cancellation => httpClient.SendAsync(request, cancellation),
            defaultRequestBudget, token);
        await EnsureSuccessAsync(response, token);
    }

    /// <summary>
    /// 逐请求声明等待预算。<see cref="HttpClient.Timeout"/> 是客户端级的，一个数管所有请求，所以它只能当兜底
    /// （见 <see cref="PryProtocol.RequestBudgetCeilingMs"/>）：真正做主的是这里的 cancellation。
    /// 两档共存时先到点的那一档会赢，因此"把语音那一档调到 4 分钟"若不同时抬高兜底，声明的那档永远轮不到做主。
    /// 预算到点必须报成超时：否则它以"取消"的面目出现，界面以为用户自己关掉了这一请求（PLAT-001）。
    /// 只用于读完即返回的请求/响应调用——流式订阅要活过整个会话，见 <see cref="ReadEventsAsync"/>。
    /// </summary>
    private static async Task<HttpResponseMessage> SendWithBudgetAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> send, TimeSpan budget, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(budget);
        try
        {
            return await send(deadline.Token);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            throw new TimeoutException($"本地后端在 {budget.TotalSeconds:0.#} 秒内没有完成这一请求。");
        }
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
        // 原样透传，不替服务端猜：非 ProblemDetails 响应、坏 JSON、旧服务端都停在 null 这一档。
        throw new PryBackendException(response.StatusCode, problem?.Code, problem?.Detail ?? response.ReasonPhrase,
            problem?.TraceId, problem?.Retryable);
    }

    private static string Escape(string value) => Uri.EscapeDataString(value);
    private static ConversationEvent ParseEvent(string data) => JsonSerializer.Deserialize<ConversationEvent>(data, JsonOptions)
        ?? throw new InvalidDataException("后端返回了无效事件。");
}

public sealed class PryBackendException(HttpStatusCode statusCode, string? code, string? message, string? traceId,
    bool? retryable = null)
    : Exception(message ?? $"Pry backend returned {(int)statusCode}")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string? Code { get; } = code;
    public string? TraceId { get; } = traceId;

    /// <summary>
    /// 服务端对本类错误的可重试宣告。<c>false</c> 表示"再发一次也不会变好"，界面必须停在明确提示而不是退避重连；
    /// <c>null</c> 是服务端未宣告（含非 ProblemDetails 响应与旧服务端），调用方只能按各自的既有策略处理，
    /// 不得把它当 <c>false</c> 用。
    /// </summary>
    public bool? Retryable { get; } = retryable;

    /// <summary>
    /// 唯一的"禁令"判据：只有服务端明确写了 <c>retryable:false</c> 才算禁行。
    /// 缺席（<c>null</c>）与 <c>true</c> 都不是禁令，调用方仍按各自的退避策略重连——
    /// 把旧服务端的沉默读成禁行，等于把一次网络抖动做成永久失败。
    /// </summary>
    public bool RetryForbidden => Retryable == false;
}

public sealed record DownloadedContent(byte[] Bytes, string ContentType);

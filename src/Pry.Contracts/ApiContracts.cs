namespace Pry.Contracts;

/// <summary>
/// ProblemDetails 的客户端形状。<c>retryable</c> 是线契约的一部分，不是调试附件：服务端对"重试也不会变好"的错误
/// （协议不匹配、令牌缺失/陈旧、凭据被拒、上下文超限）明确写 <c>false</c>，客户端据此停止自动重连。
/// 用 <c>bool?</c> 而不是 <c>bool</c>：字段缺席是"服务端没有宣告"，与"宣告了不可重试"是两件事，
/// 把缺席读成 <c>false</c> 会让旧服务端把界面钉死，读成 <c>true</c> 则会让新服务端的禁行提示被重试淹没。
/// </summary>
public sealed record ApiProblemResponse(string? Type, string? Title, int? Status, string? Detail, string? Code,
    string? TraceId, bool? Retryable = null);

/// <summary>
/// 七类稳定的 Provider 错误码（PLAT-001）。字符串本身就是线契约：它们会出现在
/// <c>urn:pry:error:*</c> 与 <c>turn.failed.code</c> 里，改名等于改协议，新增必须同时补映射与文档。
/// 放在 Contracts 而不是 Core：界面的错误表（<c>Pry.Client</c>）与 HTTP 状态映射（<c>Pry.Api</c>）都要按同一批码判读，
/// 而 Client 不得依赖 Core——各写一份字面量就是让这七个码有漂移的余地。
/// </summary>
public static class ProviderErrorCodes
{
    public const string Auth = "provider_auth";
    public const string RateLimited = "provider_rate_limited";
    public const string Timeout = "provider_timeout";
    public const string InvalidResponse = "provider_invalid_response";
    public const string ModelUnavailable = "provider_unavailable";
    public const string ContextOverflow = "context_overflow";
    public const string Cancelled = "provider_cancelled";
}

public sealed record CreateConversationRequest(string? CharacterId);
public sealed record UpdateConversationRequest(string? Title, bool? IsPinned, string? FolderId, bool ClearFolder = false);
public sealed record CreateFolderRequest(string Name);
public sealed record RenameFolderRequest(string Name);
public sealed record CreateMessageRequest(ChatRole Role, string Content, string? ImagePath = null, string? StickerId = null);
public sealed record SubmitTurnRequest(string Content, string? StickerId = null, bool Immediate = false,
    IReadOnlyList<string>? AttachmentIds = null);
public sealed record SubmitTurnResponse(long MessageId);
public sealed record RuntimeStatusResponse(string State, string? TextModelId, string? VisionModelId, string? Error)
{
    public string ApiState { get; init; } = "ready";
    public string ConfigurationState { get; init; } = "ready";
    public string ModelState { get; init; } = "not_loaded";
    public string? ErrorCode { get; init; }
    public bool Retryable { get; init; }
    public int? RequestedContextSize { get; init; }
    public int? EffectiveContextSize { get; init; }
    public double? MeasuredTokensPerSecond { get; init; }
    public string? ModelAdjustmentReason { get; init; }
}
public sealed record ComputeDeviceResponse(string Id, string Name, bool IsIntegrated, long? TotalMemoryMiB = null,
    long? FreeMemoryMiB = null, int PerformanceTier = 1, string PerformanceTierId = "entry",
    int RecommendedContextSize = 4096);
public sealed record ConversationEvent(long Sequence, string Type, DateTimeOffset OccurredAt, object Data);
public sealed record MediaAssetResponse(string Id, string Name, string ContentType, long Size, string Kind,
    DateTimeOffset CreatedAt, string DownloadUrl, IReadOnlyList<string> Warnings);
public sealed record MediaUploadPolicyResponse(long WarningThresholdBytes, long? MaximumBytes,
    int MaximumAttachmentsPerTurn);
public sealed record ConversationMutationResponse(string ConversationId, long MessageId, string Scope,
    int RemovedMessageCount, bool CanUndo);
public sealed record CharacterSummaryResponse(string Id, string Name, string CardName, string? AvatarUrl, bool Selected);
public sealed record CharacterResponse(int SchemaVersion, string Id, string Name, string CardName, string UserName,
    string Identity, string Personality, string SpeechStyle, IReadOnlyList<string> BehavioralRules,
    IReadOnlyList<string> WorldFacts, RuntimeState InitialState, string Greeting, CharacterPromptMode PromptMode,
    string LegacySystemPrompt, string? AvatarUrl, ImageDisplayPreferences AvatarDisplay);
public sealed record SaveCharacterRequest(string Name, string CardName, string UserName, string Identity,
    string Personality, string SpeechStyle, IReadOnlyList<string>? BehavioralRules, IReadOnlyList<string>? WorldFacts,
    RuntimeState? InitialState, string Greeting, CharacterPromptMode PromptMode, string LegacySystemPrompt,
    string? AvatarMediaId, bool ClearAvatar, ImageDisplayPreferences? AvatarDisplay);
public sealed record ClientPreferencesResponse(string? SelectedCharacterId, string? ActiveConversationId,
    string? ActiveModelId, string? ActiveVisionModelId, string? ActiveSpeechModelId, UserProfilePreferences UserProfile,
    DesktopPetPreferences DesktopPet, ShortcutSettings Shortcuts, TurnTakingSettings? TurnTaking,
    ClientThemePreferences Theme, string? BackgroundUrl, string? UserAvatarUrl)
{
    public string? ActiveEmbeddingModelId { get; init; }

    /// <summary>高级（调试）开关的当前状态：界面按它决定那两条反应项出不出现，后端按同一条拒收。</summary>
    public bool ExplicitExpressionFeedback { get; init; }
}
public sealed record ClientThemePreferences(string ThemeMode, string AccentColor, bool UseGlassEffects,
    bool LiveSidebarResize, double BackgroundDimOpacity, double BackgroundImageOpacity, string BackgroundBlurMode,
    double BackgroundBlurRadius, double AvatarSize, double BubbleFontSize, double BubbleMaxWidth, double BubbleSpacing)
{
    public string MainWindowLayoutMode { get; init; } = MainWindowLayoutModes.Messenger;
}
public sealed record UpdateClientPreferencesRequest(string? SelectedCharacterId, string? ActiveConversationId,
    UserProfilePreferences? UserProfile, DesktopPetPreferences? DesktopPet, ShortcutSettings? Shortcuts,
    TurnTakingSettings? TurnTaking, ClientThemePreferences? Theme, bool? ExplicitExpressionFeedback = null);
public sealed record StickerResponse(string Id, string Name, StickerSource Source, IReadOnlyList<string> Emotions,
    IReadOnlyList<string> Situations, IReadOnlyList<string> AvoidWhen, double Intensity, bool Enabled,
    string InteractionRole, bool LikelyBackchannel, string ContentUrl);
public sealed record ImportStickerRequest(string MediaId, string Name, IReadOnlyList<string>? Emotions);
public sealed record UpdateStickerRequest(string Name, IReadOnlyList<string>? Emotions, string InteractionRole,
    bool LikelyBackchannel);
public sealed record SpeechModelResponse(string Id, string DisplayName, string Provider, string ModelName,
    string Language, int SampleRate, bool Selected, bool Available, bool Custom);
public sealed record TranscribeSpeechRequest(string MediaId, string? ModelId = null);
public sealed record TranscribeSpeechResponse(string Text, string ModelId);
public sealed record SaveSpeechModelRequest(string DisplayName, string Provider, string ModelName,
    string? LocalModelDirectory, string BaseUrl, string Language, int SampleRate);
public sealed record ModelProfileResponse(string Id, string DisplayName, string Provider, string ModelName,
    ModelCapabilities Capabilities, int ContextSize, int MaxOutputTokens, double Temperature, int GpuLayers,
    string ComputeDevice, bool EnableThinking, bool SelectedForText, bool SelectedForVision, bool Custom)
{
    public string Source { get; init; } = "built-in";
    public bool Available { get; init; } = true;
    public IReadOnlyList<string> Tags { get; init; } = [];
    public bool SelectedForEmbedding { get; init; }
}
public sealed record ModelScanResponse(int FoundCount, int AddedCount, IReadOnlyList<ModelProfileResponse> Models);
public sealed record UpdateModelTagsRequest(IReadOnlyList<string>? Tags);
public sealed record UpdateEmbeddingSettingsRequest(bool Enabled, string? ModelId);
public sealed record EmbeddingStatusResponse(bool Enabled, string State, string? ModelId, int IndexedCount,
    int PendingCount, int? Dimensions, string? ErrorCode, bool Retryable);
public sealed record UpdateModelSelectionRequest(string ActiveModelId, string? ActiveVisionModelId,
    string? ActiveSpeechModelId, IReadOnlyDictionary<string, ModelTuningPreferences>? ModelTunings);
public sealed record UpdateAppearanceMediaRequest(string? BackgroundMediaId, bool ClearBackground,
    string? UserAvatarMediaId, bool ClearUserAvatar, ImageDisplayPreferences? BackgroundDisplay,
    ImageDisplayPreferences? UserAvatarDisplay);
public sealed record SaveSettingsRequest(UpdateClientPreferencesRequest Preferences,
    UpdateAppearanceMediaRequest Appearance, UpdateModelSelectionRequest Models);
public sealed record SaveSettingsResponse(ClientPreferencesResponse Preferences,
    IReadOnlyList<ModelProfileResponse> Models)
{
    public string RuntimeAction { get; init; } = "settings_applied";
}
public sealed record SaveUserProfileRequest(UserProfilePreferences Profile, string? AvatarMediaId,
    bool ClearAvatar, ImageDisplayPreferences? AvatarDisplay);
public sealed record SaveCustomModelRequest(string DisplayName, string Provider, string ModelName, string BaseUrl,
    string? LocalModelPath, string? LocalMmprojPath, ModelCapabilities Capabilities, int ContextSize,
    int MaxOutputTokens, double Temperature, int GpuLayers, string ComputeDevice, bool EnableThinking)
{
    public IReadOnlyList<string>? Tags { get; init; }
}
public sealed record SaveNetworkModelRequest(string DisplayName, string ModelName, string BaseUrl,
    ModelCapabilities Capabilities, int ContextSize, int MaxOutputTokens, double Temperature, int GpuLayers,
    string ComputeDevice, bool EnableThinking, IReadOnlyList<string>? Tags);
public sealed record CreateMemoryRequest(string CharacterId, string Kind, string Summary, string Tags, double Importance = 0.5, long? SourceMessageId = null);
public sealed record UpdateMemoryRequest(string Kind, string Summary, string Tags, double Importance);
public sealed record InteractionReplyPresetDto(string Id, string Text, bool Enabled, double Weight);
public sealed record InteractionActionDto(string Id, string DisplayName, bool Enabled, string ResponseMode,
    string? ExpressionIntent, IReadOnlyList<InteractionReplyPresetDto>? Replies);
public sealed record SaveInteractionProfileRequest(int SchemaVersion, int CooldownSeconds,
    IReadOnlyList<InteractionReplyPresetDto>? PokeReplies, IReadOnlyList<InteractionActionDto>? Actions);
public sealed record InteractionProfileResponse(int SchemaVersion, string CharacterId, int CooldownSeconds,
    IReadOnlyList<InteractionReplyPresetDto> PokeReplies, IReadOnlyList<InteractionActionDto> Actions, string Source);
public sealed record ExecuteInteractionRequest(string ActionId, string IdempotencyKey);
public sealed record InteractionCommandResponse(string ActionId, string CharacterId, long? MessageId,
    string Outcome, bool Replayed, DateTimeOffset AcceptedAt, string? ExpressionIntent);
/// <summary>
/// 用户对某一条她已经投递出去的回复的显式反应。一次点击只表一个态度：请求里没有"她这次说得怎么样"
/// 之类的评分字段，也没有让服务端去猜的余地——不点就永远不算认可。
/// </summary>
public sealed record ExpressionFeedbackRequest(bool Approved);

/// <summary>
/// 这次表态的处理结果。<c>Outcome</c> 是线契约里的固定文本，界面只照它说一句人话，
/// 这里刻意没有计数、没有档位、没有任何内部刻度——那条"数值不许出门"的门禁扫的就是这一层。
/// </summary>
public sealed record ExpressionFeedbackResponse(long MessageId, string Outcome);

/// <summary>反应被拒的几种情形，各自成一种错误语义（AGENTS 第 5 条：不许都塞 500）。</summary>
public static class ExpressionFeedbackErrorCodes
{
    /// <summary>高级开关没开：入口不存在，请求也不该被当成一次表态。</summary>
    public const string Disabled = "expression_feedback_disabled";

    /// <summary>这条气泡属于另一个会话：不能拿它去教她，那是把话挂到别人身上。</summary>
    public const string WrongConversation = "expression_feedback_wrong_conversation";

    /// <summary>这一条拿不到投递凭据（进程重启前的旧气泡、或归属当时就说不清）：宁可拒，不回退成"按最近一轮归属"。</summary>
    public const string NotAttributable = "expression_feedback_not_attributable";

    /// <summary>这个会话里根本没有这条消息。</summary>
    public const string MessageMissing = "expression_feedback_message_missing";

    /// <summary>后台学习队列当下是满的：表态没有被收下，稍后可以再点一次。</summary>
    public const string QueueFull = "expression_feedback_queue_full";
}

/// <summary>反应被接受后的两种结局文本。</summary>
public static class ExpressionFeedbackOutcomes
{
    public const string Recorded = "recorded";

    /// <summary>那一轮她没有真的说出可学的说法（占位符那一类）：不算表态，也不算失败。</summary>
    public const string NothingToLearn = "nothing_to_learn";
}

public sealed record ResourceId<T>(T Id);

public static class ContractValidation
{
    /// <summary>
    /// 消息正文的唯一上限（AUD-07）。纯文本消息与贴纸／表情附带文本共用这一把尺，
    /// 上限数字只允许出现在这一处；会话的两个写入入口都经 <see cref="MessageContent"/> 走它。
    /// </summary>
    public const int MessageContentMaxLength = 20_000;

    public static string Required(string? value, string field, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) throw new ApiValidationException(field, "不能为空");
        if (normalized.Length > maxLength) throw new ApiValidationException(field, $"长度不能超过 {maxLength}");
        return normalized;
    }

    /// <summary>
    /// 会话消息正文的唯一校验点：先裁空白，再按同一条 <see cref="MessageContentMaxLength"/> 判长度。
    /// <paramref name="requireText"/> 只决定"能不能一个字都不带"——纯文本消息必须带字，
    /// 带贴纸／表情的消息允许只发表情；只要带了文本，两支过的都是同一把尺，超长一律拒。
    /// 原缺陷（AUD-07）正是带贴纸那一支绕过长度校验，所以这里刻意把长度判断放在分支之前。
    /// </summary>
    public static string MessageContent(string? value, bool requireText)
    {
        var normalized = value?.Trim() ?? "";
        if (normalized.Length > MessageContentMaxLength)
            throw new ApiValidationException("content", $"长度不能超过 {MessageContentMaxLength}");
        if (requireText && normalized.Length == 0)
            throw new ApiValidationException("content", "不能为空");
        return normalized;
    }

    public static string? OptionalId(string? value, string field) => string.IsNullOrWhiteSpace(value) ? null : Required(value, field, 128);

    public static void Importance(double value)
    {
        if (double.IsNaN(value) || value is < 0 or > 1) throw new ApiValidationException("importance", "必须在 0 到 1 之间");
    }
}

public sealed class ApiValidationException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}

public sealed class ResourceNotFoundException(string resource, object id) : Exception($"{resource} '{id}' 不存在");
public sealed class ResourceConflictException(string resource, object id, string message) : Exception(message)
{
    public string Resource { get; } = resource;
    public object Id { get; } = id;
}

public sealed class InteractionRejectedException(string code, string message, DateTimeOffset? retryAt = null)
    : Exception(message)
{
    public string Code { get; } = code;
    public DateTimeOffset? RetryAt { get; } = retryAt;
}

/// <summary>
/// 服务端拒掉一次显式表达反应。文案给人读，<see cref="Code"/> 给机器判读，取值只允许
/// <see cref="ExpressionFeedbackErrorCodes"/> 里那五个——状态码由 <c>ApiExceptionMiddleware</c> 按码映射，
/// 新增码忘了映射会落到保守的那一档，而不是被当成 500。
/// </summary>
public sealed class ExpressionFeedbackRejectedException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>
/// 一次"确认保留"没能转成状态迁移时，拒法要能被机器分辨：这条记忆不是孤立态、还是同指纹已被占用，
/// 各成一种码（不属于该作用域的 ID 走 <see cref="ResourceNotFoundException"/>）。状态码由
/// <c>ApiExceptionMiddleware</c> 按码映射到 409；新增码忘了映射只会落到保守那一档，不会冒充 500。
/// </summary>
public sealed class MemoryConfirmRejectedException(string code, long id, string message) : Exception(message)
{
    public string Code { get; } = code;
    public long Id { get; } = id;
}

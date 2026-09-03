using Pry.Core.Models;

namespace Pry.Contracts;

public sealed record ApiProblemResponse(string? Type, string? Title, int? Status, string? Detail, string? Code,
    string? TraceId);

public sealed record CreateConversationRequest(string? CharacterId);
public sealed record UpdateConversationRequest(string? Title, bool? IsPinned, string? FolderId, bool ClearFolder = false);
public sealed record CreateFolderRequest(string Name);
public sealed record RenameFolderRequest(string Name);
public sealed record CreateMessageRequest(ChatRole Role, string Content, string? ImagePath = null, string? StickerId = null);
public sealed record SubmitTurnRequest(string Content, string? StickerId = null, bool Immediate = false,
    IReadOnlyList<string>? AttachmentIds = null);
public sealed record SubmitTurnResponse(long MessageId);
public sealed record RuntimeStatusResponse(string State, string? TextModelId, string? VisionModelId, string? Error);
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
    ClientThemePreferences Theme, string? BackgroundUrl, string? UserAvatarUrl);
public sealed record ClientThemePreferences(string ThemeMode, string AccentColor, bool UseGlassEffects,
    bool LiveSidebarResize, double BackgroundDimOpacity, double BackgroundImageOpacity, string BackgroundBlurMode,
    double BackgroundBlurRadius, double AvatarSize, double BubbleFontSize, double BubbleMaxWidth, double BubbleSpacing);
public sealed record UpdateClientPreferencesRequest(string? SelectedCharacterId, string? ActiveConversationId,
    UserProfilePreferences? UserProfile, DesktopPetPreferences? DesktopPet, ShortcutSettings? Shortcuts,
    TurnTakingSettings? TurnTaking, ClientThemePreferences? Theme);
public sealed record StickerResponse(string Id, string Name, StickerSource Source, IReadOnlyList<string> Emotions,
    IReadOnlyList<string> Situations, IReadOnlyList<string> AvoidWhen, double Intensity, bool Enabled,
    string InteractionRole, bool LikelyBackchannel, string ContentUrl);
public sealed record ImportStickerRequest(string MediaId, string Name, IReadOnlyList<string>? Emotions);
public sealed record UpdateStickerRequest(string Name, IReadOnlyList<string>? Emotions, string InteractionRole,
    bool LikelyBackchannel);
public sealed record SpeechModelResponse(string Id, string DisplayName, string Provider, string ModelName,
    string Language, int SampleRate, bool Selected, bool Available);
public sealed record TranscribeSpeechRequest(string MediaId, string? ModelId = null);
public sealed record TranscribeSpeechResponse(string Text, string ModelId);
public sealed record SaveSpeechModelRequest(string DisplayName, string Provider, string ModelName,
    string? LocalModelDirectory, string BaseUrl, string Language, int SampleRate);
public sealed record ModelProfileResponse(string Id, string DisplayName, string Provider, string ModelName,
    ModelCapabilities Capabilities, int ContextSize, int MaxOutputTokens, double Temperature, int GpuLayers,
    string ComputeDevice, bool EnableThinking, bool SelectedForText, bool SelectedForVision, bool Custom);
public sealed record UpdateModelSelectionRequest(string ActiveModelId, string? ActiveVisionModelId,
    string? ActiveSpeechModelId, IReadOnlyDictionary<string, ModelTuningPreferences>? ModelTunings);
public sealed record UpdateAppearanceMediaRequest(string? BackgroundMediaId, bool ClearBackground,
    string? UserAvatarMediaId, bool ClearUserAvatar, ImageDisplayPreferences? BackgroundDisplay,
    ImageDisplayPreferences? UserAvatarDisplay);
public sealed record SaveCustomModelRequest(string DisplayName, string Provider, string ModelName, string BaseUrl,
    string? LocalModelPath, string? LocalMmprojPath, ModelCapabilities Capabilities, int ContextSize,
    int MaxOutputTokens, double Temperature, int GpuLayers, string ComputeDevice, bool EnableThinking);
public sealed record CreateMemoryRequest(string CharacterId, string Kind, string Summary, string Tags, double Importance = 0.5, long? SourceMessageId = null);
public sealed record UpdateMemoryRequest(string Kind, string Summary, string Tags, double Importance);
public sealed record ResourceId<T>(T Id);

public static class ContractValidation
{
    public static string Required(string? value, string field, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) throw new ApiValidationException(field, "不能为空");
        if (normalized.Length > maxLength) throw new ApiValidationException(field, $"长度不能超过 {maxLength}");
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

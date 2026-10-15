using Pry.Core.Memory;
using System.Text.Json.Serialization;

namespace Pry.Core.Models;

public sealed record CharacterDefinition
{
    public int SchemaVersion { get; init; } = 1;
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string CardName { get; init; } = "";
    public string UserName { get; init; } = "你";
    public required string Identity { get; init; }
    public required string Personality { get; init; }
    public required string SpeechStyle { get; init; }
    public IReadOnlyList<string> BehavioralRules { get; init; } = [];
    public IReadOnlyList<string> WorldFacts { get; init; } = [];
    public RuntimeState InitialState { get; init; } = new();
    public string Greeting { get; init; } = "你好。";
    public CharacterPromptMode PromptMode { get; init; } = CharacterPromptMode.Structured;
    public string LegacySystemPrompt { get; init; } = "";
    public string? AvatarPath { get; init; }
    public ImageDisplayPreferences AvatarDisplay { get; init; } = new();
}

public enum DeliveryState { Pending, Delivered, Cancelled }

public static class InteractionResponseModes
{
    public const string Preset = "preset";
    public const string Silent = "silent";
}

public sealed record InteractionReplyPreset
{
    public required string Id { get; init; }
    public required string Text { get; init; }
    public bool Enabled { get; init; } = true;
    public double Weight { get; init; } = 1;
}

public sealed record InteractionActionDefinition
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public bool Enabled { get; init; } = true;
    public string ResponseMode { get; init; } = InteractionResponseModes.Preset;
    public string? ExpressionIntent { get; init; }
    public IReadOnlyList<InteractionReplyPreset> Replies { get; init; } = [];
}

public sealed record CharacterInteractionProfile
{
    public int SchemaVersion { get; init; } = 1;
    public required string CharacterId { get; init; }
    public int CooldownSeconds { get; init; } = 3;
    public IReadOnlyList<InteractionReplyPreset> PokeReplies { get; init; } = [];
    public IReadOnlyList<InteractionActionDefinition> Actions { get; init; } = [];
}

public enum InteractionCommandWriteStatus { Created, Replayed, IdempotencyConflict, Cooldown }

public sealed record InteractionCommandWriteResult(InteractionCommandWriteStatus Status, string ActionId,
    string CharacterId, long? MessageId, string Outcome, DateTimeOffset AcceptedAt, DateTimeOffset? RetryAt = null,
    string? ExpressionIntent = null);

// Deleting chat records detaches the memories that cited them; the snapshot is the only place the
// original evidence excerpts survive, and it is never persisted.
public sealed record ConversationMutationSnapshot(string ConversationId, ConversationRoom Room,
    IReadOnlyList<ChatMessage> Messages, IReadOnlyList<DetachedMemory> DetachedMemories,
    long MaxMessageIdBeforeMutation);

public sealed record StickerDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string FilePath { get; init; }
    public StickerSource Source { get; init; }
    public IReadOnlyList<string> Emotions { get; init; } = [];
    public IReadOnlyList<string> Situations { get; init; } = [];
    public IReadOnlyList<string> AvoidWhen { get; init; } = [];
    public double Intensity { get; init; } = 0.5;
    public bool Enabled { get; init; } = true;
    public string InteractionRole { get; init; } = "reaction";
    public bool LikelyBackchannel { get; init; }
}

public sealed record ExpressionIntent(string? Emotion, double Intensity, string? StickerId,
    string? Live2DExpression, string? Live2DMotion);

public enum ConversationUpdateKind { Text, Expression }
public sealed record ConversationUpdate(ConversationUpdateKind Kind, string? Text = null,
    ExpressionIntent? Expression = null);

public enum ReplyMessageType { Text, Sticker }

public sealed record PlannedReplyMessage
{
    public long Id { get; init; }
    public required ReplyMessageType Type { get; init; }
    public string? Content { get; init; }
    public string? StickerId { get; init; }
    public DeliveryState State { get; init; } = DeliveryState.Pending;
    public int Sequence { get; init; }
}

public sealed record ReplyPlan
{
    public long Id { get; init; }
    public required string ConversationId { get; init; }
    public IReadOnlyList<PlannedReplyMessage> Messages { get; init; } = [];
}

public enum ChatAttachmentKind { Image, Text, Csv, Docx }

public sealed record ChatAttachment(string Path, ChatAttachmentKind Kind, string Name)
{
    public bool IsImage => Kind == ChatAttachmentKind.Image;
}

public sealed record UserInputPart(string Text, string? StickerId = null,
    IReadOnlyList<ChatAttachment>? Attachments = null)
{
    public IReadOnlyList<ChatAttachment> SafeAttachments => Attachments ?? [];
}

public enum InterruptKind { Backchannel, SoftInterrupt, HardInterrupt }
public enum TurnState { Idle, UserPending, ModelThinking, AgentPending, AgentSending }

public sealed record ChatRequestOptions(bool StructuredReplyPlan = false, int? MaxOutputTokens = null,
    bool? EnableThinking = null);

public sealed record MemoryEmbeddingSource(long Id, string Summary, string Tags)
{
    public string Content => $"{Summary}\n{Tags}";
}

public sealed record MemoryVectorRecord(MemoryRecord Memory, float[] Vector);

public sealed record PromptContext(CharacterDefinition Character, RuntimeState State,
    IReadOnlyList<MemoryRecord> Memories, IReadOnlyList<ChatMessage> RecentMessages,
    IReadOnlyList<StickerDefinition> Stickers, string UserMessage, string? ImagePath);

public sealed record ModelProfile
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public string Provider { get; init; } = "local-llama";
    public string ModelName { get; init; } = "local-model";
    public string? ModelPath { get; init; }
    public string? MmprojPath { get; init; }
    public string BaseUrl { get; init; } = "http://127.0.0.1:8080/v1";
    [JsonIgnore] public string? ApiKey { get; init; }
    public ModelCapabilities Capabilities { get; init; } = new();
    public int ContextSize { get; init; } = 4096;
    public int MaxOutputTokens { get; init; } = 512;
    public double Temperature { get; init; } = 0.8;
    public double? TopP { get; init; }
    public int? TopK { get; init; }
    public double? MinP { get; init; }
    public double? PresencePenalty { get; init; }
    public double? RepetitionPenalty { get; init; }
    public int GpuLayers { get; init; }
    public string ComputeDevice { get; init; } = "auto-discrete";
    public bool EnableThinking { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
}

public sealed record AppSettings
{
    public string ActiveModelId { get; init; } = "qwen3-1.7b-local";
    public string? VisionModelId { get; init; }
    public bool AllowOnlineFallback { get; init; }
    public string LlamaServerPath { get; init; } = "runtime/llama-server.exe";
    public IReadOnlyList<ModelProfile> Models { get; init; } = [];
    public TurnTakingSettings TurnTaking { get; init; } = new();
    public ShortcutSettings Shortcuts { get; init; } = new();
    public string? ActiveSpeechModelId { get; init; }
    public IReadOnlyList<SpeechModelProfile> SpeechModels { get; init; } = [];
}

public sealed record SpeechModelProfile
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public string Provider { get; init; } = "sherpa-onnx";
    public string ModelName { get; init; } = "sensevoice";
    public string ModelPath { get; init; } = "";
    public string BaseUrl { get; init; } = "";
    [JsonIgnore] public string? ApiKey { get; init; }
    public string Language { get; init; } = "zh";
    public int SampleRate { get; init; } = 16000;
}

public sealed record UserPreferences
{
    public bool EnableThinking { get; init; } = true;
    public ShortcutSettings Shortcuts { get; init; } = new();
    public ModelTuningPreferences ModelTuning { get; init; } = new();
    public TurnTakingSettings? TurnTakingOverride { get; init; }
    public IReadOnlyList<ModelProfile> CustomModels { get; init; } = [];
    public IReadOnlyList<ModelProfile> DiscoveredModels { get; init; } = [];
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ModelTags { get; init; } =
        new Dictionary<string, IReadOnlyList<string>>();
    public string? SelectedCharacterId { get; init; }
    public DesktopPetPreferences DesktopPet { get; init; } = new();
    public string? ActiveModelId { get; init; }
    public string? ActiveVisionModelId { get; init; }
    public string? ActiveSpeechModelId { get; init; }
    public string? ActiveEmbeddingModelId { get; init; }
    public IReadOnlyDictionary<string, ModelTuningPreferences> ModelTunings { get; init; } =
        new Dictionary<string, ModelTuningPreferences>();
    public IReadOnlyList<SpeechModelProfile> CustomSpeechModels { get; init; } = [];
    public string? ActiveConversationId { get; init; }
    public ThemePreferences Theme { get; init; } = new();
    public UserProfilePreferences UserProfile { get; init; } = new();

    /// <summary>
    /// 高级（调试）开关：开启后界面上才对某一条回复出现"就是这样／别这样说"，后端同时才开始收这类反应。
    /// 默认关——作者 2026-10-15 裁的是"用户跟 AI 慢慢打磨说话习惯"，手动干预是给需要它的人的准备项，
    /// 不是默认路径。关掉不撤销已经学到的东西，只关掉入口。
    /// </summary>
    public bool ExplicitExpressionFeedback { get; init; }
}

public sealed record ThemePreferences
{
    public string? BackgroundImagePath { get; init; }
    public IReadOnlyList<string> BackgroundHistory { get; init; } = [];
    public IReadOnlyDictionary<string, ImageDisplayPreferences> BackgroundDisplays { get; init; } =
        new Dictionary<string, ImageDisplayPreferences>();
    public string? CharacterAvatarPath { get; init; }
    public string? UserAvatarPath { get; init; }
    public IReadOnlyList<string> UserAvatarHistory { get; init; } = [];
    public IReadOnlyDictionary<string, ImageDisplayPreferences> UserAvatarDisplays { get; init; } =
        new Dictionary<string, ImageDisplayPreferences>();
    public string ThemeMode { get; init; } = "system";
    public string MainWindowLayoutMode { get; init; } = MainWindowLayoutModes.Messenger;
    public string AccentColor { get; init; } = "#B148C6";
    public bool UseGlassEffects { get; init; } = true;
    public bool LiveSidebarResize { get; init; }
    public double BackgroundDimOpacity { get; init; } = 0.34;
    public double BackgroundImageOpacity { get; init; } = 1.0;
    public string BackgroundBlurMode { get; init; } = "none";
    public double BackgroundBlurRadius { get; init; }
    public double AvatarSize { get; init; } = 48;
    public double BubbleFontSize { get; init; } = 14;
    public double BubbleMaxWidth { get; init; } = 620;
    public double BubbleSpacing { get; init; } = 10;
}

using System.Text.Json.Serialization;

namespace Pry.Core.Models;

public enum CharacterPromptMode { Structured, Legacy }

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

public sealed record RuntimeState
{
    public string Location { get; init; } = "日常空间";
    public string Mood { get; init; } = "平静";
    public string CurrentGoal { get; init; } = "陪伴用户";
    public int Trust { get; init; } = 50;
    public int Familiarity { get; init; } = 10;
}

public enum ChatRole { System, User, Assistant }
public enum DeliveryState { Pending, Delivered, Cancelled }

public sealed record ChatMessage(long Id, string ConversationId, ChatRole Role, string Content,
    DateTimeOffset CreatedAt, string? ImagePath = null, string? StickerId = null,
    DateTimeOffset? DeliveredAt = null);

public sealed record ConversationRoom(string Id, string Title, string? CharacterId,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, int MessageCount,
    string? FolderId = null, bool IsPinned = false);

public sealed record ConversationFolder(string Id, string Name, DateTimeOffset CreatedAt);

public sealed record ConversationMutationSnapshot(string ConversationId, ConversationRoom Room,
    IReadOnlyList<ChatMessage> Messages, IReadOnlyList<MemoryRecord> Memories,
    long MaxMessageIdBeforeMutation);

public enum StickerSource { BuiltIn, User }

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

public sealed record TypingStyle
{
    public double Speed { get; init; } = 1.0;
    public double Burstiness { get; init; } = 0.75;
    public int MinDelayMs { get; init; } = 450;
    public int MaxDelayMs { get; init; } = 3200;
}

public sealed record TurnTakingSettings
{
    public int DebounceMs { get; init; } = 1200;
    public int MaxPendingMs { get; init; } = 5000;
    public bool SplitReplies { get; init; } = true;
    public bool AutoClassifyInterrupts { get; init; } = true;
    public TypingStyle TypingStyle { get; init; } = new();
    public int MinReplyMessages { get; init; } = 1;
    public int MaxReplyMessages { get; init; } = 4;
    public int MaxMessageCharacters { get; init; } = 90;
    public bool EnableListeningSignals { get; init; } = true;
    public int ListeningSignalDelayMs { get; init; } = 4500;
    public string StyleInstruction { get; init; } = "像即时通讯聊天一样自然、简短，不要一次倾倒过多信息。";
}

public sealed record ChatRequestOptions(bool StructuredReplyPlan = false);

public sealed record MemoryRecord(long Id, string CharacterId, string Kind, string Summary,
    string Tags, double Importance, long? SourceMessageId, DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt = null, DateTimeOffset? LastUsedAt = null);

public sealed record PromptContext(CharacterDefinition Character, RuntimeState State,
    IReadOnlyList<MemoryRecord> Memories, IReadOnlyList<ChatMessage> RecentMessages,
    IReadOnlyList<StickerDefinition> Stickers, string UserMessage, string? ImagePath);

public sealed record ModelCapabilities(bool Text = true, bool Vision = false,
    bool Embeddings = false, bool ImageGeneration = false, bool ToolCalling = false);

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
    public int GpuLayers { get; init; }
    public string ComputeDevice { get; init; } = "auto-discrete";
    public bool EnableThinking { get; init; }
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

public sealed record ShortcutSettings
{
    public string Send { get; init; } = "Enter";
    public string SendImmediately { get; init; } = "Ctrl+Enter";
    public string NewLine { get; init; } = "Shift+Enter";
    public string CancelReply { get; init; } = "Escape";
    public string NewConversation { get; init; } = "Ctrl+N";
    public string OpenStickers { get; init; } = "Ctrl+E";
    public string OpenCharacterEditor { get; init; } = "Ctrl+Shift+C";
}

public sealed record UserPreferences
{
    public bool EnableThinking { get; init; } = true;
    public ShortcutSettings Shortcuts { get; init; } = new();
    public ModelTuningPreferences ModelTuning { get; init; } = new();
    public TurnTakingSettings? TurnTakingOverride { get; init; }
    public IReadOnlyList<ModelProfile> CustomModels { get; init; } = [];
    public string? SelectedCharacterId { get; init; }
    public DesktopPetPreferences DesktopPet { get; init; } = new();
    public string? ActiveModelId { get; init; }
    public string? ActiveVisionModelId { get; init; }
    public string? ActiveSpeechModelId { get; init; }
    public IReadOnlyDictionary<string, ModelTuningPreferences> ModelTunings { get; init; } =
        new Dictionary<string, ModelTuningPreferences>();
    public IReadOnlyList<SpeechModelProfile> CustomSpeechModels { get; init; } = [];
    public string? ActiveConversationId { get; init; }
    public ThemePreferences Theme { get; init; } = new();
    public UserProfilePreferences UserProfile { get; init; } = new();
}

public sealed record UserProfilePreferences
{
    public string DisplayName { get; init; } = "你";
    public string Signature { get; init; } = "愿今天也有好心情";
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

public sealed record ImageDisplayPreferences
{
    public double FocusX { get; init; } = 0.5;
    public double FocusY { get; init; } = 0.5;
    public double Zoom { get; init; } = 1.0;
}

public sealed record DesktopPetPreferences
{
    public bool Enabled { get; init; }
    public bool AlwaysOnTop { get; init; } = true;
    public double Scale { get; init; } = 1.0;
}

public sealed record ModelTuningPreferences
{
    public string? ActiveModelId { get; init; }
    public double? Temperature { get; init; }
    public int? MaxOutputTokens { get; init; }
    public int? ContextSize { get; init; }
    public int? GpuLayers { get; init; }
    public string? ComputeDevice { get; init; }
    public bool? EnableThinking { get; init; }
}

namespace Pry.Contracts;

// 线协议与配置存储共用的值类型。它们刻意待在契约程序集里：`Pry.Core` 反过来引用本程序集，
// 这样改动这些形状必然是一次显式的契约变更，而不是 Core 内部重命名的副作用。
// 属性名与默认值即线格式，`ContractShapeTests` 逐字钉住。

public enum CharacterPromptMode { Structured, Legacy }

public enum ChatRole { System, User, Assistant }

public enum StickerSource { BuiltIn, User }

public static class MainWindowLayoutModes
{
    public const string Messenger = "messenger";
    public const string Card = "card";
}

public sealed record RuntimeState
{
    public string Location { get; init; } = "日常空间";
    public string Mood { get; init; } = "平静";
    public string CurrentGoal { get; init; } = "陪伴用户";
    public int Trust { get; init; } = 50;
    public int Familiarity { get; init; } = 10;
}

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

    /// <summary>
    /// 这个角色回复得多勤（0~1）。运行时据此折算"攒够几条才开一轮"，所以它同时是防刷屏与省模型调用的那道闸。
    /// 默认 1.0 保持与"有输入即成轮"完全一致；0 是宿主主动关闭自动回复——消息照常落库，但永不成轮。
    /// </summary>
    public double ReplyFrequency { get; init; } = 1.0;

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

public sealed record ModelCapabilities(bool Text = true, bool Vision = false,
    bool Embeddings = false, bool ImageGeneration = false, bool ToolCalling = false);

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

public sealed record UserProfilePreferences
{
    public string DisplayName { get; init; } = "你";
    public string Signature { get; init; } = "愿今天也有好心情";
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

// 以下四条是 HTTP 响应直接反序列化的形状，`Pry.Client` 把它们作为返回值暴露给界面层。
// 它们此前住在 `Pry.Core`，但 Core 类型出现在线协议上正是 PLAT-005 要解除的耦合：
// 留在 Core 就意味着 Core 内部改名会静默改掉线协议。搬进来之后，改形状只能是一次契约变更。

public static class MessageSources
{
    public const string Legacy = "legacy";
    public const string UserInput = "user_input";
    public const string ModelReply = "model_reply";
    public const string InteractionPreset = "interaction_preset";
    public const string InteractionGenerated = "interaction_generated";
    public const string Proactive = "proactive";
    public const string ListeningSignal = "listening_signal";
}

public sealed record ChatMessage(long Id, string ConversationId, ChatRole Role, string Content,
    DateTimeOffset CreatedAt, string? ImagePath = null, string? StickerId = null,
    DateTimeOffset? DeliveredAt = null, string Source = MessageSources.Legacy);

public sealed record ConversationRoom(string Id, string Title, string? CharacterId,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, int MessageCount,
    string? FolderId = null, bool IsPinned = false)
{
    public string? LastMessagePreview { get; init; }
    public ChatRole? LastMessageRole { get; init; }
    public string? LastMessageKind { get; init; }
    public DateTimeOffset? LastMessageAt { get; init; }
    public long? LastMessageId { get; init; }
    public long LastReadMessageId { get; init; }
    public long UnreadCount { get; init; }
}

public sealed record ConversationFolder(string Id, string Name, DateTimeOffset CreatedAt);

public sealed record MemoryRecord(long Id, string CharacterId, string Kind, string Summary,
    string Tags, double Importance, long? SourceMessageId, DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt = null, DateTimeOffset? LastUsedAt = null);

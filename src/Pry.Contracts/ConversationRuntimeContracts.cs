namespace Pry.Contracts;

/// <summary>
/// 只读运行时诊断。供 Developer Mode 与 Eval 读取，不参与任何写路径。
/// 字段为 null 表示该运行时当前不在内存里（未启动或已被空闲驱逐），此时只剩持久层事实可读。
/// 任何字段都不包含消息正文。
/// </summary>
public sealed record ConversationRuntimeStateResponse
{
    public required string ConversationId { get; init; }
    /// <summary>会话绑定的角色；主动消息等系统会话可能为空。</summary>
    public string? CharacterId { get; init; }
    public required bool Started { get; init; }
    public string? Phase { get; init; }
    public int? Revision { get; init; }
    /// <summary>内存中的 fencing epoch；未启动时等于持久层水位。</summary>
    public required long Epoch { get; init; }
    public long? ConversationProgress { get; init; }
    public bool? UserTyping { get; init; }
    public int? PendingInputCount { get; init; }
    public int? PendingOutputCount { get; init; }
    public IReadOnlyList<string> PendingOutputTurns { get; init; } = [];
    public string? ActiveTurn { get; init; }
    public string? DeliveringTurn { get; init; }
    public DateTimeOffset? LastUserActivity { get; init; }
    public DateTimeOffset? LastCharacterActivity { get; init; }
    public double? IdleForMs { get; init; }
    public bool? Evictable { get; init; }
    public int? InvalidatedTurns { get; init; }
    public double? SchedulerBackoffMs { get; init; }
    public IReadOnlyList<RuntimeTurnSummary> RecentTurns { get; init; } = [];
    public RuntimePersistenceSummary? Persisted { get; init; }
}

/// <summary>一轮的终局摘要。轮次输入原文刻意不外露。</summary>
public sealed record RuntimeTurnSummary(long TurnId, long GenerationId, long FenceEpoch, DateTimeOffset OpenedAt,
    DateTimeOffset? CompletedAt, string Outcome, string? OutcomeReason, int InputCount, int PlannedCount,
    int DeliveredCount);

/// <summary>重启与驱逐之后仍然成立的那部分状态：fencing 水位与说话计数。</summary>
public sealed record RuntimePersistenceSummary(long Epoch, bool LastSpeakerWasCharacter,
    DateTimeOffset? LastActivityAt, int ConsecutiveCharacterTurns);

public sealed record ConversationRuntimeTraceResponse(string ConversationId, int Revision, long Epoch,
    IReadOnlyList<RuntimeTraceEntryResponse> Entries);

public sealed record RuntimeTraceEntryResponse(int Sequence, string Kind, string Action, string? Event,
    long? TurnId, long? GenerationId, long? FenceEpoch, string Reason, int RevisionBefore, int RevisionAfter,
    DateTimeOffset At);

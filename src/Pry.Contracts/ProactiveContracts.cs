namespace Pry.Contracts;

public sealed record SaveProactivePolicyRequest(long ExpectedRevision, string? ConversationId,
    bool Enabled = false, int IdleSeconds = 600, int MinimumIntervalSeconds = 7200, int DailyLimit = 3,
    string TimeZoneId = "Asia/Shanghai", int QuietStartMinute = 1320, int QuietEndMinute = 480,
    bool QuietHoursEnabled = true);

public sealed record ProactivePolicyResponse(string CharacterId, long Revision, string? ConversationId,
    bool Enabled, int IdleSeconds, int MinimumIntervalSeconds, int DailyLimit, string TimeZoneId,
    int QuietStartMinute, int QuietEndMinute, bool QuietHoursEnabled, bool DeliveryAvailable);

public sealed record ReportCompanionActivityRequest(long Sequence, string? ConversationId,
    int IdleSeconds, bool IsTyping = false, bool IsRecording = false,
    bool IsTranscribing = false, bool IsInteractionOpen = false);

public sealed record CompanionActivityResponse(string ClientId, long Sequence, DateTimeOffset ExpiresAt);

public sealed record ProactiveStatusResponse(string CharacterId, string? ConversationId, string State,
    string? LastOutcome, DateTimeOffset? LastAttemptAt, DateTimeOffset? NextAttemptAt,
    int SentToday, bool AwaitingUserReply, string ModelLoadPolicy = "already_loaded_only");

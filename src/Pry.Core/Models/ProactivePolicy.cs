namespace Pry.Core.Models;

public sealed record ProactivePolicy(
    string CharacterId, string? ConversationId = null, bool Enabled = false,
    int IdleSeconds = 600, int MinimumIntervalSeconds = 7200, int DailyLimit = 3,
    string TimeZoneId = "Asia/Shanghai", int QuietStartMinute = 1320,
    int QuietEndMinute = 480, bool QuietHoursEnabled = true);

public sealed record StoredProactivePolicy(ProactivePolicy Policy, long Revision);

public sealed record ProactiveHistory(string? AttemptId, string? LastOutcome, DateTimeOffset? LastAttemptAt,
    DateTimeOffset? NextAttemptAt, long? LastMessageId, int SentToday, bool AwaitingUserReply);

public sealed record ProactiveAttempt(string Id, StoredProactivePolicy Policy, long LastHistoryMessageId);

public sealed class ProactiveTargetMismatchException() : Exception("Proactive target does not belong to the character.");

public sealed record ProactiveEligibility(string Reason, DateTimeOffset? NextEligibleAt = null)
{
    public bool Eligible => Reason == "eligible";
}

/// <summary>Pure scheduling rules; eligibility never authorizes a write without a final state check.</summary>
public static class ProactivePolicyRules
{
    public static ProactiveEligibility Evaluate(ProactivePolicy policy, DateTimeOffset now,
        bool activityFresh, bool userBusy, TimeSpan idleFor, bool targetMatches, bool turnBusy,
        DateTimeOffset? lastAttemptAt, int sentToday, bool awaitingUserReply)
    {
        if (!policy.Enabled) return new("disabled");
        if (!targetMatches) return new("target_unavailable");
        if (!activityFresh) return new("activity_stale");
        if (userBusy || turnBusy) return new("user_busy");
        var local = TimeZoneInfo.ConvertTime(now, TimeZoneInfo.FindSystemTimeZoneById(policy.TimeZoneId));
        var minute = local.Hour * 60 + local.Minute;
        var quiet = policy.QuietStartMinute < policy.QuietEndMinute
            ? minute >= policy.QuietStartMinute && minute < policy.QuietEndMinute
            : minute >= policy.QuietStartMinute || minute < policy.QuietEndMinute;
        if (policy.QuietHoursEnabled && quiet) return new("quiet_hours");
        if (awaitingUserReply) return new("awaiting_user_reply");
        if (sentToday >= policy.DailyLimit) return new("daily_limit");
        var next = lastAttemptAt?.AddSeconds(policy.MinimumIntervalSeconds);
        if (next > now) return new("minimum_interval", next);
        if (idleFor.TotalSeconds < policy.IdleSeconds)
            return new("idle_threshold", now.AddSeconds(policy.IdleSeconds - Math.Max(0, idleFor.TotalSeconds)));
        return new("eligible");
    }
}

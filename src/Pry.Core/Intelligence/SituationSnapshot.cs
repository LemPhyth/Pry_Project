using Pry.Core.Models;

namespace Pry.Core.Intelligence;

/// <summary>
/// Outcome vocabulary declared by MEM-005 §6.4 and owned by the Memory contract; repeated here
/// only so the intelligence side can consume it before M1 lands. When <c>MemoryContext</c> ships,
/// this projection and the adapter collapse into that one type instead of living beside it.
/// </summary>
public enum MemoryQueryOutcome { Ok, DegradedKeywordOnly, Empty, Unavailable, Rejected }

public sealed record MemoryObservation(long Id, string Summary, string Kind, double Importance,
    DateTimeOffset CreatedAt, long? EvidenceMessageId)
{
    private static readonly string[] RelationshipKinds =
        ["relationship", "preference", "identity", "关系", "称呼", "偏好"];

    public static MemoryObservation From(MemoryRecord record) =>
        new(record.Id, record.Summary.Trim(), record.Kind, record.Importance, record.CreatedAt, record.SourceMessageId);

    public bool IsRelationshipEvidence =>
        RelationshipKinds.Any(x => Kind.Contains(x, StringComparison.OrdinalIgnoreCase)
            || Summary.Contains(x, StringComparison.Ordinal));
}

/// <summary>
/// The intelligence-side read model of one scoped memory lookup. Empty and unavailable stay
/// separate on purpose: only one of them licenses "we have no history of this".
/// </summary>
public sealed record RelevantMemoryView
{
    public MemoryQueryOutcome Outcome { get; init; } = MemoryQueryOutcome.Empty;
    public IReadOnlyList<MemoryObservation> Observations { get; init; } = [];

    public bool MayClaimNoHistory => Outcome == MemoryQueryOutcome.Empty;

    /// <summary>Nothing usable arrived, so absence of a fact proves nothing about the past.</summary>
    public bool IsDegraded => Outcome is MemoryQueryOutcome.Unavailable or MemoryQueryOutcome.Rejected;
    public bool IsVectorDegraded => Outcome == MemoryQueryOutcome.DegradedKeywordOnly;
    public IReadOnlyList<MemoryObservation> RelationshipEvidence =>
        Observations.Where(x => x.IsRelationshipEvidence).ToArray();

    public static RelevantMemoryView Of(IReadOnlyList<MemoryObservation> observations) =>
        new()
        {
            Observations = observations,
            Outcome = observations.Count > 0 ? MemoryQueryOutcome.Ok : MemoryQueryOutcome.Empty
        };

    public static RelevantMemoryView Empty() =>
        new() { Outcome = MemoryQueryOutcome.Empty, Observations = [] };

    public static RelevantMemoryView Unavailable() =>
        new() { Outcome = MemoryQueryOutcome.Unavailable, Observations = [] };
}

/// <summary>
/// What Runtime reports about the moment of speaking. Timing decisions (how long to wait, when
/// to release a bubble, whether the turn is stale) stay on the Runtime side.
/// </summary>
public sealed record SituationSnapshot
{
    public required string CharacterInstanceId { get; init; }
    public required string ConversationId { get; init; }
    public required string TurnId { get; init; }
    public required string UserInput { get; init; }
    public required DateTimeOffset Now { get; init; }

    public RelevantMemoryView Memory { get; init; } = RelevantMemoryView.Empty();
    public IReadOnlyList<ChatMessage> RecentMessages { get; init; } = [];

    /// <summary>Set when Runtime must answer (e.g. the user explicitly demanded a reply).</summary>
    public bool RequiresReply { get; init; }

    /// <summary>
    /// Runtime already knows this from its own activity clock; when it is supplied the planner
    /// uses it instead of re-deriving a gap from whatever transcript happened to be attached.
    /// </summary>
    public TimeSpan? SinceLastAssistantReply { get; init; }

    public int ConsecutiveLowContentInputs { get; init; }
    public bool HasImage { get; init; }
    public bool HasAttachment { get; init; }
    public IReadOnlyList<string> AvailableCapabilityNames { get; init; } = [];

    /// <summary>Assistant turns at the tail of the transcript that ended by asking something.</summary>
    public int TrailingAssistantQuestionStreak
    {
        get
        {
            var streak = 0;
            for (var i = RecentMessages.Count - 1; i >= 0; i--)
            {
                var message = RecentMessages[i];
                if (message.Role != ChatRole.Assistant) continue;
                if (!EndsAsQuestion(message.Content)) break;
                streak++;
            }
            return streak;
        }
    }

    public bool AssistantJustReplied
    {
        get
        {
            if (SinceLastAssistantReply is { } supplied) return supplied < TimeSpan.FromSeconds(35);
            return RecentMessages.LastOrDefault(x => x.Role == ChatRole.Assistant) is { } last
                && (Now - last.CreatedAt) < TimeSpan.FromSeconds(35);
        }
    }

    /// <summary>The last thing the character said, used to stop re-asking and re-phrasing.</summary>
    public string? LastAssistantText =>
        RecentMessages.LastOrDefault(x => x.Role == ChatRole.Assistant)?.Content;

    public string LastUserTextBeforeCurrent =>
        RecentMessages.Reverse().SkipWhile(x => x.Role != ChatRole.User)
            .SkipWhile(x => x.Content == UserInput).FirstOrDefault(x => x.Role == ChatRole.User)?.Content ?? string.Empty;

    private static bool EndsAsQuestion(string? content)
    {
        var text = (content ?? string.Empty).TrimEnd();
        return text.Length > 0 && (text.EndsWith('?') || text.EndsWith('？') || text.EndsWith("吗")
            || text.EndsWith("呢") || text.EndsWith("么"));
    }
}

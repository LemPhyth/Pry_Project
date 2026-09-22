using Pry.Core.Intelligence;

namespace Pry.Core.Expression;

public sealed record ExpressionSelection(IReadOnlyList<ExpressionHint> Hints, IReadOnlyList<string> Rejections)
{
    public bool ChoseNothing => Hints.Count == 0;

    public static ExpressionSelection Nothing(string reason) => new([], [reason]);
}

/// <summary>
/// Decides which retrieved habits genuinely fit this turn. Returning nothing is a normal,
/// expected outcome; the whole point of a selector is that it can say no to all of them.
/// </summary>
public sealed class ExpressionSelector
{
    public static readonly TimeSpan DefaultCooldown = TimeSpan.FromMinutes(12);
    private const int RecentReplyWindow = 3;

    private readonly int _maxHints;
    private readonly TimeSpan _cooldown;
    private readonly double _minimumConfidence;

    public ExpressionSelector(int maxHints = 1, TimeSpan? cooldown = null, double minimumConfidence = 0.35)
    {
        _maxHints = Math.Clamp(maxHints, 0, 2);
        _cooldown = cooldown ?? DefaultCooldown;
        _minimumConfidence = minimumConfidence;
    }

    public ExpressionSelection Select(SituationSnapshot situation, CharacterStateSnapshot state,
        BehaviorDecision decision, IReadOnlyList<ScoredExpression> candidates)
    {
        var rejections = new List<string>();
        if (_maxHints == 0) return ExpressionSelection.Nothing("selector:disabled");

        if (decision.HabitToneAllowed == HabitToneAllowed.None)
        {
            rejections.Add($"selector:register_{decision.Register}_blocks_habits");
            return new ExpressionSelection([], rejections);
        }
        if (candidates.Count == 0)
        {
            rejections.Add("selector:no_candidates");
            return new ExpressionSelection([], rejections);
        }

        var ownRecentText = string.Concat(situation.RecentMessages
            .Where(x => x.Role == ChatRole.Assistant)
            .OrderByDescending(x => x.CreatedAt)
            .Take(RecentReplyWindow)
            .Select(x => ExpressionGuard.Normalize(x.Content)));

        var hints = new List<ExpressionHint>();
        foreach (var candidate in candidates.OrderByDescending(x => x.Score))
        {
            var pattern = candidate.Pattern;
            var why = Reject(pattern, decision, state, ownRecentText, situation.Now);
            if (why is not null)
            {
                rejections.Add(why);
                continue;
            }
            if (hints.Count >= _maxHints)
            {
                rejections.Add($"selector:budget_{_maxHints}");
                break;
            }
            hints.Add(new ExpressionHint(pattern.Id, pattern.Situation, pattern.Style, pattern.Tone,
                Math.Round(Math.Clamp(pattern.Confidence * (0.6 + candidate.Score), 0.1, 1), 3)));
        }

        if (hints.Count == 0 && rejections.Count == 0) rejections.Add("selector:all_rejected");
        return new ExpressionSelection(hints, rejections);
    }

    private string? Reject(ExpressionPattern pattern, BehaviorDecision decision, CharacterStateSnapshot state,
        string ownRecentText, DateTimeOffset now)
    {
        if (pattern.Suppressed) return $"cooldown:{pattern.Id}:suppressed";
        if (pattern.Confidence < _minimumConfidence) return $"low_confidence:{pattern.Id}";

        var playfulish = pattern.Tone is ExpressionTone.Playful or ExpressionTone.Sharp;
        if (decision.HabitToneAllowed == HabitToneAllowed.GroundedOnly && playfulish)
            return $"tone_blocked:{pattern.Id}";

        if (playfulish && !state.Mood.SupportsPlayfulRegister) return $"mood_blocked:{pattern.Id}";
        if (playfulish && !state.Relationship.AllowsTeasing) return $"relationship_blocked:{pattern.Id}";
        if (pattern.Tone == ExpressionTone.Affectionate && !state.Relationship.AllowsAffectionateAddress)
            return $"relationship_blocked:{pattern.Id}";

        if (pattern.LastUsedAt is { } lastUsed && now - lastUsed < _cooldown)
            return $"cooldown:{pattern.Id}";

        var style = ExpressionGuard.Normalize(pattern.Style);
        if (style.Length >= 2 && ownRecentText.Contains(style, StringComparison.Ordinal))
            return $"already_recent:{pattern.Id}";

        return null;
    }
}

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Pry.Core.Abstractions;

namespace Pry.Core.Expression;

/// <summary>
/// One finished turn, as observed after delivery. Only text the user actually received is
/// admissible: a cancelled or replaced output must never become a habit.
/// </summary>
public sealed record DeliveredTurn(
    string ConversationId,
    string TurnId,
    DateTimeOffset DeliveredAt,
    string UserInput,
    string DeliveredAssistantText,
    bool UserAskedToStopTalkingLikeThat = false)
{
    /// <summary>Stable reference to explicit user feedback about the listed styles.</summary>
    public string? FeedbackEvidenceId { get; init; }
    public IReadOnlyList<string> AcceptedStyles { get; init; } = [];
    public IReadOnlyList<string> RejectedStyles { get; init; } = [];
}

public enum LearningOutcome
{
    Created,
    Merged,
    RejectedTooLong,
    RejectedBoilerplate,
    RejectedCrude,
    RejectedFactual,
    RejectedInsufficientEvidence,
    RejectedUserEcho,
    RejectedPersonaConflict,
    RejectedExplicitCorrection,
    DroppedQueueFull,
    RejectedUnconfirmed,
    Unchanged
}

public sealed record LearningResult(string Style, LearningOutcome Outcome, long EvidenceCount);

/// <summary>
/// Background-only. Enqueue uses only a short local lock and rejects a full queue; extraction
/// runs when the host drains, so a slow store cannot delay a visible reply.
/// </summary>
public sealed class ExpressionLearner
{
    public const int QueueCapacity = 64;
    private const int MinimumEvidence = 2;
    private const int MaximumShapeCharacters = 10;
    private const int MaxTrackedShapes = 512;
    private const int TrimTrackedShapesTo = 384;
    private const int TurnKeysPerShape = 32;

    private static readonly string[] HabitParticles =
        ["啦", "喵", "撒", "叭", "咯", "捏", "哟", "呗", "罢", "哇", "嘞", "哼"];

    private static readonly string[] InterjectionOpeners =
        ["啊这", "不是", "好好好", "行吧", "得嘞", "笑不活", "我嘞个", "咱就是说"];

    private readonly ConcurrentQueue<DeliveredTurn> _queue = new();
    private readonly object _queueGate = new();
    private readonly SemaphoreSlim _drainGate = new(1, 1);
    private readonly ConcurrentDictionary<string, ShapeEvidence> _shapes = new(StringComparer.Ordinal);
    private readonly IExpressionPatternRepository _store;
    private readonly string _instanceId;
    private readonly Func<DateTimeOffset> _clock;
    private int _dropped;

    public ExpressionLearner(IExpressionPatternRepository store, string characterInstanceId,
        Func<DateTimeOffset>? clock = null)
    {
        _store = store;
        _instanceId = characterInstanceId;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public int PendingCount => _queue.Count;
    public int DroppedCount => _dropped;

    public bool TryEnqueue(DeliveredTurn turn)
    {
        lock (_queueGate)
        {
            if (_queue.Count >= QueueCapacity)
            {
                Interlocked.Increment(ref _dropped);
                return false;
            }
            // Do not retain caller-owned mutable lists in background work.
            _queue.Enqueue(turn with
            {
                AcceptedStyles = turn.AcceptedStyles.ToArray(),
                RejectedStyles = turn.RejectedStyles.ToArray()
            });
            return true;
        }
    }

    public async Task<IReadOnlyList<LearningResult>> DrainAsync(CancellationToken cancellationToken = default)
    {
        await _drainGate.WaitAsync(cancellationToken);
        try { return await DrainCoreAsync(cancellationToken); }
        finally { _drainGate.Release(); }
    }

    private async Task<IReadOnlyList<LearningResult>> DrainCoreAsync(CancellationToken cancellationToken)
    {
        var batch = new List<DeliveredTurn>();
        lock (_queueGate)
            while (batch.Count < QueueCapacity && _queue.TryDequeue(out var turn)) batch.Add(turn);

        foreach (var turn in batch) Observe(turn);
        if (!_shapes.Values.Any(x => x.Dirty)) return [];

        var stored = await _store.ListForCharacterAsync(_instanceId, cancellationToken);
        var byKey = stored.GroupBy(x => x.DedupKey, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        var results = new List<LearningResult>();
        foreach (var evidence in _shapes.Values)
        {
            if (!evidence.Dirty) continue;
            var (outcome, count) = await CommitAsync(evidence, byKey, cancellationToken);
            evidence.Dirty = false;
            results.Add(new LearningResult(evidence.Shape, outcome, count));
        }
        return results;
    }

    private void Observe(DeliveredTurn turn)
    {
        var extracted = ExtractShapes(turn.DeliveredAssistantText).Distinct(StringComparer.Ordinal).ToArray();
        // A correction may arrive when the next delivered reply no longer contains the habit.
        foreach (var shape in extracted.Concat(turn.RejectedStyles).Distinct(StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(shape) || ExpressionGuard.IsTooLong(shape)) continue;
            var evidence = _shapes.GetOrAdd(shape, key => new ShapeEvidence(key));
            if (turn.RejectedStyles.Contains(shape, StringComparer.Ordinal)
                || (turn.UserAskedToStopTalkingLikeThat && extracted.Contains(shape, StringComparer.Ordinal)))
            {
                evidence.Rejected = true;
                evidence.Dirty = true;
            }
            var echoedByUser = turn.UserInput.Contains(shape, StringComparison.Ordinal);
            if (echoedByUser) evidence.UserSaidIt = true;
            // Delivery replay can carry later feedback, but cannot manufacture another occurrence.
            var turnKey = $"{turn.ConversationId.Length}:{turn.ConversationId}{turn.TurnId}";
            evidence.Dirty = true;
            if (evidence.FirstSeenAt == default) evidence.FirstSeenAt = turn.DeliveredAt;
            evidence.SeenAt = turn.DeliveredAt;
            if (!echoedByUser)
            {
                // Explicit approval is rare and user-bounded, so it is always kept; the bare
                // occurrence tally is what would otherwise grow for the life of the process.
                if (extracted.Contains(shape, StringComparer.Ordinal)
                    && turn.AcceptedStyles.Contains(shape, StringComparer.Ordinal)
                    && !string.IsNullOrWhiteSpace(turn.FeedbackEvidenceId))
                {
                    evidence.AcceptedIds.Add(turn.FeedbackEvidenceId);
                    evidence.AcceptedTurnKeys.Add(turnKey);
                }
                if (evidence.IndependentTurns.Count < TurnKeysPerShape
                    || evidence.IndependentTurns.Contains(turnKey)) evidence.IndependentTurns.Add(turnKey);
            }
        }
        TrimTrackedShapes();
    }

    /// <summary>A learner lives as long as the app, so cold candidates must leave: their accepted
    /// evidence is already on the stored pattern, and a resurfacing shape just starts over.</summary>
    private void TrimTrackedShapes()
    {
        var excess = _shapes.Count - MaxTrackedShapes;
        if (excess <= 0) return;
        foreach (var cold in _shapes.Values
            .Where(x => !x.Dirty && !x.Rejected)
            .OrderBy(x => x.SeenAt)
            .Take(excess)
            .ToArray())
        {
            _shapes.TryRemove(cold.Shape, out _);
        }
    }

    private async Task<(LearningOutcome, long)> CommitAsync(ShapeEvidence evidence,
        Dictionary<string, ExpressionPattern> byKey, CancellationToken cancellationToken)
    {
        var supported = evidence.IndependentTurns.Count;
        var tone = InferTone(evidence.Shape);
        var key = $"{tone}|{ExpressionGuard.Normalize(evidence.Shape)}";
        byKey.TryGetValue(key, out var existing);
        if (evidence.Rejected || existing?.Suppressed == true)
        {
            foreach (var variant in byKey.Values.Where(x =>
                ExpressionGuard.Normalize(x.Style) == ExpressionGuard.Normalize(evidence.Shape)).ToArray())
            {
                var blocked = variant with { Suppressed = true, UpdatedAt = _clock() };
                await _store.SaveAsync(blocked, cancellationToken);
                byKey[blocked.DedupKey] = blocked;
            }
            // Keep the suppression in the store so a new learner cannot resurrect this habit.
            var suppressed = existing is null ? new ExpressionPattern
            {
                Id = $"ep:{_instanceId}:{StableCode(evidence.Shape)}",
                CharacterInstanceId = _instanceId, Situation = DescribeSituation(tone),
                Style = evidence.Shape, Tone = tone, Confidence = 0, CreatedAt = _clock()
            } : existing with { };
            suppressed.Suppressed = true;
            suppressed.UpdatedAt = _clock();
            await _store.SaveAsync(suppressed, cancellationToken);
            byKey[key] = suppressed;
            return (LearningOutcome.RejectedExplicitCorrection, supported);
        }
        if (ExpressionGuard.IsTooLong(evidence.Shape)) return (LearningOutcome.RejectedTooLong, supported);
        if (ExpressionGuard.IsCrude(evidence.Shape)) return (LearningOutcome.RejectedCrude, supported);
        if (ExpressionGuard.LooksLikeAssistance(evidence.Shape)) return (LearningOutcome.RejectedBoilerplate, supported);
        if (ExpressionGuard.ClaimsIdentity(evidence.Shape)) return (LearningOutcome.RejectedPersonaConflict, supported);
        if (ExpressionGuard.MakesFactualClaim(evidence.Shape)) return (LearningOutcome.RejectedFactual, supported);
        if (supported < MinimumEvidence) return (LearningOutcome.RejectedInsufficientEvidence, supported);
        // A habit the user themselves keeps saying is theirs, not ours; require one more
        // independent occurrence before it is treated as the character's own.
        if (evidence.UserSaidIt && supported < MinimumEvidence + 1) return (LearningOutcome.RejectedUserEcho, supported);
        var acceptedIds = ExpressionPattern.Confirmed((existing?.AcceptedEvidenceIds ?? []).Concat(evidence.AcceptedIds));
        var acceptedTurns = ExpressionPattern.Confirmed((existing?.AcceptedTurnKeys ?? []).Concat(evidence.AcceptedTurnKeys));
        var acceptedCount = Math.Min(acceptedIds.Count, acceptedTurns.Count);
        if (acceptedCount < MinimumEvidence) return (LearningOutcome.RejectedUnconfirmed, acceptedCount);
        if (existing is not null)
        {
            if (acceptedIds.SequenceEqual(existing.AcceptedEvidenceIds)
                && acceptedTurns.SequenceEqual(existing.AcceptedTurnKeys))
                return (LearningOutcome.Unchanged, existing.EvidenceCount);
            var merged = existing with { AcceptedEvidenceIds = acceptedIds, AcceptedTurnKeys = acceptedTurns };
            merged.EvidenceCount = acceptedCount;
            // Confidence depends on independent feedback, never the number of drain/retry calls.
            if (merged.Source == ExpressionPatternSource.Learned)
                merged.Confidence = Math.Clamp(0.35 + 0.03 * (acceptedCount - MinimumEvidence), 0.35, 0.8);
            merged.UpdatedAt = _clock();
            await _store.SaveAsync(merged, cancellationToken);
            byKey[key] = merged;
            return (LearningOutcome.Merged, merged.EvidenceCount);
        }

        var created = new ExpressionPattern
        {
            Id = $"ep:{_instanceId}:{StableCode(evidence.Shape)}",
            CharacterInstanceId = _instanceId,
            Situation = DescribeSituation(tone),
            Style = evidence.Shape,
            Tone = tone,
            Source = ExpressionPatternSource.Learned,
            Confidence = Math.Clamp(0.35 + 0.03 * (acceptedCount - MinimumEvidence), 0.35, 0.8),
            EvidenceCount = acceptedCount,
            AcceptedEvidenceIds = acceptedIds,
            AcceptedTurnKeys = acceptedTurns,
            CreatedAt = evidence.FirstSeenAt == default ? _clock() : evidence.FirstSeenAt,
            UpdatedAt = _clock()
        };
        await _store.SaveAsync(created, cancellationToken);
        byKey[key] = created;
        return (LearningOutcome.Created, created.EvidenceCount);
    }

    /// <summary>
    /// 一条已投递气泡里真的出现过的形状。宿主把"用户对她这句话明确按了『就是这样／别这样说』"
    /// 翻译成学习器认得的形状列表，靠的就是这一个读口——它不判断态度，也不新增判断通道：
    /// 谁被认可永远由那次点击决定，这里只回答"这一条里面有哪些形状"。
    /// </summary>
    public static IReadOnlyList<string> ShapesIn(string? deliveredAssistantText) =>
        ExtractShapes(deliveredAssistantText ?? string.Empty).Distinct(StringComparer.Ordinal).ToArray();

    internal static IEnumerable<string> ExtractShapes(string assistantText)
    {
        var text = (assistantText ?? string.Empty).Trim();
        if (text.Length == 0) yield break;

        foreach (var opener in InterjectionOpeners)
            if (text.StartsWith(opener, StringComparison.Ordinal)) yield return opener;

        var segments = text.Split(['，', '。', '！', '？', '；', '、', '\n', ' ', '~', '～', '…'],
            StringSplitOptions.RemoveEmptyEntries);
        foreach (var raw in segments)
        {
            var segment = raw.Trim();
            if (segment.Length == 0 || segment.Length > MaximumShapeCharacters) continue;
            if (HabitParticles.Any(p => segment.EndsWith(p, StringComparison.Ordinal))) yield return segment;
            else if (segment.Length <= 4 && segments.Length > 1) yield return segment;
        }
    }

    private static ExpressionTone InferTone(string shape)
    {
        if (shape is "我嘞个" or "咱就是说" or "笑不活" or "啊这") return ExpressionTone.Playful;
        if (shape.Contains('哼', StringComparison.Ordinal)) return ExpressionTone.Sharp;
        if (HabitParticles.Any(p => shape.EndsWith(p, StringComparison.Ordinal))) return ExpressionTone.Affectionate;
        return ExpressionTone.Neutral;
    }

    private static string DescribeSituation(ExpressionTone tone) => tone switch
    {
        ExpressionTone.Playful => "轻微吐槽",
        ExpressionTone.Affectionate => "日常接话",
        ExpressionTone.Sharp => "被逗到",
        _ => "随口接话"
    };

    private static string StableCode(string shape) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(shape)))[..12];

    private sealed class ShapeEvidence(string shape)
    {
        public string Shape { get; } = shape;
        public HashSet<string> IndependentTurns { get; } = new(StringComparer.Ordinal);
        public HashSet<string> AcceptedIds { get; } = new(StringComparer.Ordinal);
        public HashSet<string> AcceptedTurnKeys { get; } = new(StringComparer.Ordinal);
        public bool UserSaidIt { get; set; }
        public bool Rejected { get; set; }
        public bool Dirty { get; set; }
        public DateTimeOffset FirstSeenAt { get; set; }
        public DateTimeOffset SeenAt { get; set; }
    }
}

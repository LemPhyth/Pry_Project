using Pry.Core.Intelligence;

namespace Pry.Core.Expression;

public sealed record ScoredExpression(ExpressionPattern Pattern, double Score, string MatchedOn);

/// <summary>
/// Pulls a handful of habits for the current moment. The whole store is never handed to the
/// model; the limit is a hard ceiling, not a suggestion.
/// </summary>
public sealed class ExpressionRetriever
{
    private readonly int _limit;

    public ExpressionRetriever(int limit = 3) => _limit = Math.Clamp(limit, 1, 6);

    public IReadOnlyList<ScoredExpression> Retrieve(IEnumerable<ExpressionPattern> store, string scenarioTag,
        string userInput, DateTimeOffset now)
    {
        var needle = Tokenize(scenarioTag + " " + userInput);
        if (needle.Count == 0) needle = Tokenize(scenarioTag);

        var scored = new List<ScoredExpression>();
        foreach (var pattern in store)
        {
            if (pattern.Suppressed) continue;
            var situationTokens = Tokenize(pattern.Situation);
            var overlap = situationTokens.Count == 0 ? 0
                : situationTokens.Count(needle.Contains) / (double)Math.Min(situationTokens.Count, needle.Count);
            var recency = 1 / (1 + Math.Max(0, (now - pattern.UpdatedAt).TotalDays) / 30);
            var evidence = Math.Min(1, pattern.EvidenceCount / 4.0);
            var score = overlap * 0.45 + pattern.Confidence * 0.25 + recency * 0.15 + evidence * 0.15;
            if (score <= 0.12) continue;
            scored.Add(new ScoredExpression(pattern, Math.Round(score, 4),
                overlap > 0 ? "situation-overlap" : "confidence-prior"));
        }

        return scored
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Pattern.UpdatedAt)
            .Take(_limit)
            .ToArray();
    }

    /// <summary>Character bigrams: Chinese has no spaces, and this keeps retrieval dependency-free.</summary>
    public static HashSet<string> Tokenize(string value)
    {
        var normalized = ExpressionGuard.Normalize(value);
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < normalized.Length - 1; i++) tokens.Add(normalized.Substring(i, 2));
        if (normalized.Length == 1) tokens.Add(normalized);
        return tokens;
    }
}

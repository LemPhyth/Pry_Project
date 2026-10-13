using System.Text.RegularExpressions;

namespace Pry.Core.Memory;

public sealed class MemoryQueryService(IMemoryRepository repository, IMemorySemanticSignal? semantic = null,
    ITokenEstimator? estimator = null) : IMemoryQuery
{
    private readonly ITokenEstimator _tokens = estimator ?? new ConservativeTokenEstimator();

    public async Task<MemoryContext> RetrieveAsync(MemoryQuery query, CancellationToken token = default)
    {
        query.Scope.Validate(); token.ThrowIfCancellationRequested();
        MemoryContext Result(MemoryQueryOutcome outcome, IReadOnlyList<MemoryHit> hits, int considered,
            int relevant, long revision, bool available, params string[] reasons) =>
            new(query.Scope, outcome, hits, new(considered, relevant, hits.Count, revision, available, reasons));
        if (query.BudgetTokens < 1 || query.MaxMemories < 1 || query.Query.Length > 20000)
            return Result(MemoryQueryOutcome.Rejected, [], 0, 0, 0, false, "invalid_budget_or_query");
        try
        {
            var revision = await repository.RevisionAsync(query.Scope, token);
            var current = (await repository.ListAsync(query.Scope, token))
                .Where(x => x.Status == "active" && x.Confidence >= .5 && x.ScopeKey == query.Scope.Key).ToArray();
            IReadOnlyDictionary<long, double> similarities = new Dictionary<long, double>();
            var available = false;
            if (semantic is not null)
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeout.CancelAfter(TimeSpan.FromSeconds(2));
                try
                {
                    similarities = await semantic.ScoreAsync(query.Scope, query.Query, current, timeout.Token).WaitAsync(timeout.Token);
                    available = true;
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
                catch (Exception) { /* Report degraded outcome even if the result is empty. */ }
            }
            var terms = Terms(query.Query); var now = query.Now ?? DateTimeOffset.UtcNow;
            var ranked = new List<MemoryHit>();
            foreach (var item in current)
            {
                var words = Terms(item.Record.Summary + " " + item.Record.Tags);
                var overlap = terms.Intersect(words).Count();
                var keyword = terms.Count == 0 || words.Count == 0 ? 0 : (double)overlap / Math.Min(terms.Count, words.Count);
                var sim = similarities.GetValueOrDefault(item.Record.Id);
                if (!double.IsFinite(sim)) sim = 0;
                sim = Math.Clamp(sim, 0, 1);
                // Relevance eligibility precedes all popularity/recency/relationship boosts.
                if (keyword < .25 && (!available || sim < .65)) continue;
                var age = Math.Max(0, (now - (item.Record.UpdatedAt ?? item.Record.CreatedAt)).TotalDays);
                var halfLife = item.Record.Kind == "event" ? 14 : item.Record.Kind == "preference" ? 365 : 90;
                var recency = Math.Pow(.5, age / halfLife);
                var conversation = item.Evidence.Any(x => x.ConversationId == query.ConversationId) ? 1 : 0;
                var relationship = query.RelationshipRelevance?.GetValueOrDefault(item.Record.Id) ?? 0;
                if (!double.IsFinite(relationship)) relationship = 0;
                var score = .35 * Math.Max(keyword, sim) + .20 * keyword + .10 * recency +
                    .15 * item.Record.Importance + .10 * conversation + .05 * Math.Clamp(relationship, 0, 1) + .05 * item.Confidence;
                if (score < .35) continue;
                ranked.Add(new(item, score, [$"keyword:{keyword:F2}", $"semantic:{sim:F2}", $"recency:{recency:F2}",
                    $"importance:{item.Record.Importance:F2}", $"conversation:{conversation}", $"relationship:{relationship:F2}"]));
            }
            var selected = new List<MemoryHit>(); var topics = new HashSet<string>(); var remaining = query.BudgetTokens;
            foreach (var hit in ranked.OrderByDescending(x => x.Score).ThenByDescending(x => x.Memory.Record.Id))
            {
                if (selected.Count >= Math.Min(6, query.MaxMemories)) break;
                var topic = hit.Memory.TopicKey.Length == 0 ? $"id:{hit.Memory.Record.Id}" : hit.Memory.TopicKey;
                // Whole facts only: truncation can remove a negation or a qualifying clause.
                var cost = _tokens.Count(hit.Memory.Record.Summary) + 8;
                if (cost > remaining || !topics.Add(topic)) continue;
                selected.Add(hit); remaining -= cost;
            }
            if (revision != await repository.RevisionAsync(query.Scope, token))
                return Result(MemoryQueryOutcome.Rejected, [], current.Length, ranked.Count, revision, available, "memory_changed_during_retrieval");
            var outcome = semantic is not null && !available ? MemoryQueryOutcome.DegradedKeywordOnly
                : selected.Count == 0 ? MemoryQueryOutcome.Empty : MemoryQueryOutcome.Ok;
            return Result(outcome, selected, current.Length, ranked.Count, revision, available,
                semantic is null ? "keyword_only_configured" : available ? "hybrid" : "semantic_unavailable");
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            return Result(MemoryQueryOutcome.Unavailable, [], 0, 0, 0, false, "repository_unavailable");
        }
    }

    private static readonly HashSet<string> Stop = ["用户", "喜欢", "现在", "今天", "一下", "什么", "可以", "我们", "你们", "我想", "聊聊", "the", "is", "a", "i", "you", "like"];
    private static HashSet<string> Terms(string value)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Regex.Matches(value.ToLowerInvariant(), @"[a-z0-9]+|[\u3400-\u9fff]+"))
        {
            var word = match.Value;
            if (word[0] <= 127) { if (word.Length >= 2 && !Stop.Contains(word)) result.Add(word); }
            else for (var i = 0; i < word.Length - 1; i++)
            {
                var pair = word.Substring(i, 2);
                if (!Stop.Contains(pair)) result.Add(pair);
            }
        }
        return result;
    }
}

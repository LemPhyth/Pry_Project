using Pry.Core.Models;

namespace Pry.Core.Memory;

// Scope is supplied by the host. An instance scope must never be derived from a character id alone.
public sealed record MemoryScope(string Key, string CharacterId)
{
    public static MemoryScope ForCharacter(string id) => new($"character:{id}", id);
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Key) || Key.Length > 256 ||
            string.IsNullOrWhiteSpace(CharacterId) || CharacterId.Length > 128)
            throw new ArgumentException("A bounded, nonempty memory scope and character id are required.");
    }
}

public sealed record MemoryEvidence(long MessageId, string ConversationId, string Excerpt);

// Which messages an operation is about to destroy. The detach helper is the only place that turns this
// into SQL, so a caller can never pass an arbitrary predicate.
internal enum DoomedMessages
{
    EntireConversation,
    SingleMessage,
    FromMessage,
    AboveMessage
}

public sealed record MemoryEntry(MemoryRecord Record, string ScopeKey, string Status, double Confidence,
    string TopicKey, string Origin, long Revision, IReadOnlyList<MemoryEvidence> Evidence);

// The complete pre-deletion row plus every evidence triple, so an undo can restore a memory field for
// field instead of re-deriving it from the ten legacy columns. A row the detach did not rewrite keeps its
// old state_version, and the restore's compare-and-set skips it (invariant I6).
public sealed record DetachedMemory(long Id, string ScopeKey, string CharacterId, string Kind, string Summary,
    string Tags, double Importance, long? SourceMessageId, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt,
    DateTimeOffset? LastUsedAt, string Status, double Confidence, string TopicKey, string Origin,
    long StateVersion, IReadOnlyList<MemoryEvidence> Evidence, long RemainingSupport);
public sealed record MemoryCandidate(string Kind, string Content, string TopicKey, long SourceMessageId,
    string Excerpt, double Importance = .6, bool Correction = false);
public sealed record CompletedMemoryTurn(MemoryScope Scope, string ConversationId, string TurnId,
    IReadOnlyList<long> InputMessageIds, IReadOnlyList<long> DeliveredMessageIds);
public sealed record WritebackReceipt(bool Accepted, long? JobId, string Reason);
public sealed record MemoryJob(long Id, CompletedMemoryTurn Turn, long Epoch, string LeaseToken,
    DateTimeOffset LeaseUntil, int Attempt, IReadOnlyList<ChatMessage> Inputs);

public sealed record ConsolidationPolicy(int QueueCapacity = 256, int PendingMessageThreshold = 8,
    int MaxAttempts = 3, int MaxBatchMessages = 16, int DelaySeconds = 1200, int LeaseSeconds = 120,
    int MaxAgeDays = 7)
{
    public void Validate()
    {
        if (QueueCapacity < 1 || PendingMessageThreshold < 1 || MaxAttempts is < 1 or > 10 ||
            MaxBatchMessages is < 1 or > 64 || DelaySeconds < 0 || LeaseSeconds < 1 || MaxAgeDays < 1)
            throw new ArgumentException("Invalid consolidation policy.");
    }
}

public interface IMemoryRepository
{
    Task<IReadOnlyList<MemoryEntry>> ListAsync(MemoryScope scope, CancellationToken token = default);
    Task<long> RevisionAsync(MemoryScope scope, CancellationToken token = default);
    Task<MemoryEntry> SaveManualAsync(MemoryScope scope, string kind, string content, string tags,
        double importance, long? id = null, CancellationToken token = default);
    Task DeleteAsync(MemoryScope scope, long id, CancellationToken token = default);
    Task ClearAsync(MemoryScope scope, CancellationToken token = default);
    Task<WritebackReceipt> EnqueueAsync(CompletedMemoryTurn turn, ConsolidationPolicy policy,
        DateTimeOffset now, CancellationToken token = default);
    Task<MemoryJob?> ClaimAsync(ConsolidationPolicy policy, DateTimeOffset now, CancellationToken token = default);
    Task<bool> CompleteAsync(MemoryJob job, IReadOnlyList<MemoryCandidate> candidates,
        DateTimeOffset now, CancellationToken token = default);
    Task FailAsync(MemoryJob job, ConsolidationPolicy policy, DateTimeOffset now, CancellationToken token = default);
}

public interface IMemoryWriteback
{
    Task<WritebackReceipt> SubmitAsync(CompletedMemoryTurn turn, CancellationToken token = default);
}
public interface IMemoryCandidateExtractor
{
    Task<IReadOnlyList<MemoryCandidate>> ExtractAsync(MemoryJob job, CancellationToken token = default);
}

public enum MemoryQueryOutcome { Ok, DegradedKeywordOnly, Empty, Unavailable, Rejected }
public sealed record MemoryQuery(MemoryScope Scope, string ConversationId, string Query,
    int MaxMemories = 6, int BudgetTokens = 480, DateTimeOffset? Now = null,
    IReadOnlyDictionary<long, double>? RelationshipRelevance = null);
public sealed record MemoryHit(MemoryEntry Memory, double Score, IReadOnlyList<string> Signals);
public sealed record RetrievalTrace(int Considered, int Relevant, int Selected, long Revision,
    bool SemanticAvailable, IReadOnlyList<string> Reasons);
public sealed record MemoryContext(MemoryScope Scope, MemoryQueryOutcome Outcome,
    IReadOnlyList<MemoryHit> Hits, RetrievalTrace Trace);
public interface IMemoryQuery
{
    Task<MemoryContext> RetrieveAsync(MemoryQuery query, CancellationToken token = default);
}
// Returns cosine similarities for the supplied current records only; stale/deleted index IDs are ignored.
public interface IMemorySemanticSignal
{
    Task<IReadOnlyDictionary<long, double>> ScoreAsync(MemoryScope scope, string query,
        IReadOnlyList<MemoryEntry> current, CancellationToken token = default);
}

public interface ITokenEstimator { int Count(string text); }
public sealed class ConservativeTokenEstimator : ITokenEstimator
{
    // UTF-8 byte bound deliberately overestimates. A model tokenizer may be injected by the host.
    public int Count(string text) => System.Text.Encoding.UTF8.GetByteCount(text);
}

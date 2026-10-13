namespace Pry.Contracts;

public sealed record ClearMemoriesRequest(string CharacterId, bool Confirm = false);
public sealed record MemoryEvidenceResponse(long MessageId, string ConversationId, string Excerpt);
public sealed record MemoryDetailsResponse(long Id, string CharacterId, string Kind, string Content,
    string Status, double Importance, double Confidence, string Origin, long Revision, string TopicKey,
    IReadOnlyList<MemoryEvidenceResponse> Evidence);

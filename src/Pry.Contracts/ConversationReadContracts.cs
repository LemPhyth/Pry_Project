namespace Pry.Contracts;

public sealed record MarkConversationReadRequest(long? ThroughMessageId);
public sealed record ConversationReadStateResponse(string ConversationId, long LastReadMessageId, long UnreadCount);
public sealed record UnreadSummaryResponse(long TotalUnreadCount, long UnreadConversationCount);

namespace Pry.Core.Models;

public sealed record ConversationReadState(string ConversationId, long LastReadMessageId, long UnreadCount);
public sealed record UnreadSummary(long TotalUnreadCount, long UnreadConversationCount);
public sealed record MarkConversationReadResult(ConversationReadState State, bool Advanced);
public sealed class InvalidReadWatermarkException() : Exception("The advancing watermark must belong to the conversation.");

using Pry.Contracts;
using Pry.Core.Memory;
using Pry.Core.Models;

namespace Pry.Api.Services;

public sealed class ConversationReadService(MemoryDatabase database, ConversationSessionService sessions,
    CompanionOperationCoordinator operations)
{
    public async Task<ConversationReadStateResponse> GetAsync(string conversationId, CancellationToken token) =>
        ToResponse(await database.GetConversationReadStateAsync(conversationId, token)
            ?? throw new ResourceNotFoundException("conversation", conversationId));

    public async Task<ConversationReadStateResponse> MarkAsync(string conversationId, MarkConversationReadRequest request,
        CancellationToken token)
    {
        if (request.ThroughMessageId is null or < 0)
            throw new ApiValidationException("throughMessageId", "必须提供非负消息 ID");
        using var operation = await operations.EnterForegroundAsync(token);
        MarkConversationReadResult result;
        try
        {
            result = await database.MarkConversationReadAsync(conversationId, request.ThroughMessageId.Value, token)
                ?? throw new ResourceNotFoundException("conversation", conversationId);
        }
        catch (InvalidReadWatermarkException)
        { throw new ApiValidationException("throughMessageId", "前进目标必须是该会话内仍存在的消息"); }
        if (result.Advanced) sessions.PublishReadWatermark(conversationId, result.State.LastReadMessageId);
        return ToResponse(result.State);
    }

    public async Task<UnreadSummaryResponse> SummaryAsync(CancellationToken token)
    {
        var value = await database.GetUnreadSummaryAsync(token);
        return new(value.TotalUnreadCount, value.UnreadConversationCount);
    }

    private static ConversationReadStateResponse ToResponse(ConversationReadState value) =>
        new(value.ConversationId, value.LastReadMessageId, value.UnreadCount);
}

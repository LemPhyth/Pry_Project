using Pry.Contracts;
using Pry.Core.Memory;

namespace Pry.Api.Services;

public sealed class CompanionActivityService(MemoryDatabase database, TimeProvider clock,
    CompanionOperationCoordinator? operations = null, ConversationSessionService? sessions = null)
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Entry> _clients = new(StringComparer.Ordinal);
    public static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(45);

    public async Task<CompanionActivityResponse> ReportAsync(string clientId,
        ReportCompanionActivityRequest request, CancellationToken token)
    {
        clientId = ValidateId(clientId);
        if (request.Sequence < 1) throw new ApiValidationException("sequence", "序号必须为正整数");
        if (request.IdleSeconds is < 0 or > 86400) throw new ApiValidationException("idleSeconds", "必须在 0–86400 范围内");
        var conversation = ContractValidation.OptionalId(request.ConversationId, "conversationId");
        if (conversation is not null && await database.GetConversationAsync(conversation, token) is null)
            throw new ResourceNotFoundException("conversation", conversation);
        request = request with { ConversationId = conversation };
        CompanionActivityResponse accepted;
        var returned = false;
        using (var state = operations is null ? null : await operations.EnterStateAsync(token))
        {
        lock (_gate)
        {
            var timestamp = clock.GetTimestamp();
            _clients.TryGetValue(clientId, out var previous);
            if (previous is not null && request.Sequence <= previous.Request.Sequence)
            {
                if (request == previous.Request) return previous.Response;
                throw new InteractionRejectedException("activity_sequence_conflict", "活动序号已过期或被其他内容使用");
            }
            if (previous is null && _clients.Count >= 64)
                throw new InteractionRejectedException("activity_capacity", "活动客户端数量已达上限，请注销不再使用的客户端");
            var elapsed = previous is null ? TimeSpan.Zero : clock.GetElapsedTime(previous.Timestamp, timestamp);
            // A lease gap, foreground activity or target switch restarts backend-observed idle time.
            var idle = previous is null || elapsed >= LeaseDuration || Busy(request) || Busy(previous.Request) ||
                previous.Request.ConversationId != request.ConversationId
                ? TimeSpan.Zero
                : TimeSpan.FromSeconds(Math.Min(request.IdleSeconds, (previous.IdleFor + elapsed).TotalSeconds));
            var response = new CompanionActivityResponse(clientId, request.Sequence, clock.GetUtcNow() + LeaseDuration);
            if (previous is null || elapsed >= LeaseDuration || Busy(request) ||
                previous.Request.ConversationId != request.ConversationId || idle < previous.IdleFor)
                operations?.Invalidate();
            _clients[clientId] = new(request, response, timestamp, idle);
            returned = previous is null || elapsed >= LeaseDuration ||
                previous.Request.ConversationId != conversation || idle < previous.IdleFor;
            accepted = response;
        }
        }
        if (conversation is not null && sessions is not null)
            await sessions.NotifyActivityAsync(conversation, request.IsTyping, returned, token);
        return accepted;
    }

    public async Task RemoveAsync(string clientId, CancellationToken token)
    {
        clientId = ValidateId(clientId);
        using var state = operations is null ? null : await operations.EnterStateAsync(token);
        lock (_gate)
            if (_clients.Remove(clientId)) operations?.Invalidate();
    }

    public CompanionActivitySnapshot Snapshot(string conversationId)
    {
        lock (_gate)
        {
            var now = clock.GetTimestamp();
            var fresh = _clients.Count > 0 && _clients.Values.All(x => clock.GetElapsedTime(x.Timestamp, now) < LeaseDuration);
            var target = _clients.Count > 0 && _clients.Values.All(x => x.Request.ConversationId == conversationId);
            var busy = _clients.Values.Any(x => Busy(x.Request));
            var idle = _clients.Count == 0 ? TimeSpan.Zero : _clients.Values.Min(x => x.IdleFor + clock.GetElapsedTime(x.Timestamp, now));
            return new(fresh, busy, target, idle);
        }
    }

    private static bool Busy(ReportCompanionActivityRequest request) => request.IsTyping || request.IsRecording ||
        request.IsTranscribing || request.IsInteractionOpen;

    private static string ValidateId(string clientId)
    {
        if (!Guid.TryParseExact(clientId, "N", out var id) && !Guid.TryParseExact(clientId, "D", out id))
            throw new ApiValidationException("clientId", "必须为 GUID");
        return id.ToString("N");
    }

    private sealed record Entry(ReportCompanionActivityRequest Request, CompanionActivityResponse Response,
        long Timestamp, TimeSpan IdleFor);
}

public sealed record CompanionActivitySnapshot(bool Fresh, bool Busy, bool TargetMatches, TimeSpan IdleFor);

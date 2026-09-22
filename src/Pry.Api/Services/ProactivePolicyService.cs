using Pry.Contracts;
using Pry.Core.Memory;
using Pry.Core.Models;

namespace Pry.Api.Services;

public sealed class ProactivePolicyService(MemoryDatabase database, BackendRuntime runtime,
    CompanionOperationCoordinator? operations = null)
{
    public async Task<ProactivePolicyResponse> GetAsync(string characterId, CancellationToken token)
    {
        ValidateCharacter(characterId);
        return ToResponse(await database.GetProactivePolicyAsync(characterId, token)
            ?? new StoredProactivePolicy(new ProactivePolicy(characterId), 0));
    }

    public async Task<ProactivePolicyResponse> SaveAsync(string characterId, SaveProactivePolicyRequest request,
        CancellationToken token)
    {
        using var operation = operations is null ? null : await operations.EnterForegroundAsync(token);
        ValidateCharacter(characterId);
        if (request.ExpectedRevision < 0 || request.ExpectedRevision == long.MaxValue)
            throw new ApiValidationException("expectedRevision", "版本必须为非负有效整数");
        Range(request.IdleSeconds, 60, 86400, "idleSeconds");
        Range(request.MinimumIntervalSeconds, 60, 604800, "minimumIntervalSeconds");
        Range(request.DailyLimit, 1, 20, "dailyLimit");
        Range(request.QuietStartMinute, 0, 1439, "quietStartMinute");
        Range(request.QuietEndMinute, 0, 1439, "quietEndMinute");
        if (request.QuietHoursEnabled && request.QuietStartMinute == request.QuietEndMinute)
            throw new ApiValidationException("quietEndMinute", "启用安静时段时起止时间不能相同");
        var zone = ContractValidation.Required(request.TimeZoneId, "timeZoneId", 128);
        try { _ = TimeZoneInfo.FindSystemTimeZoneById(zone); }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        { throw new ApiValidationException("timeZoneId", "时区不可用"); }
        var conversationId = ContractValidation.Required(request.ConversationId, "conversationId", 128);
        var room = await database.GetConversationAsync(conversationId, token)
            ?? throw new ResourceNotFoundException("conversation", conversationId);
        if (room.CharacterId != characterId)
            throw new InteractionRejectedException("target_mismatch", "目标会话必须绑定当前角色");
        var policy = new ProactivePolicy(characterId, conversationId, request.Enabled, request.IdleSeconds,
            request.MinimumIntervalSeconds, request.DailyLimit, zone, request.QuietStartMinute,
            request.QuietEndMinute, request.QuietHoursEnabled);
        StoredProactivePolicy? saved;
        try { saved = await database.SaveProactivePolicyAsync(policy, request.ExpectedRevision, token); }
        catch (ProactiveTargetMismatchException)
        { throw new InteractionRejectedException("target_mismatch", "目标会话已变化，请重新选择"); }
        return ToResponse(saved ?? throw new InteractionRejectedException("revision_conflict", "策略已变化，请重新读取后保存"));
    }

    private void ValidateCharacter(string id)
    {
        _ = ContractValidation.Required(id, "characterId", 128);
        if (!runtime.Characters.Any(x => x.Id == id)) throw new ResourceNotFoundException("character", id);
    }

    private static void Range(int value, int min, int max, string field)
    {
        if (value < min || value > max) throw new ApiValidationException(field, $"必须在 {min}–{max} 范围内");
    }

    private static ProactivePolicyResponse ToResponse(StoredProactivePolicy stored)
    {
        var p = stored.Policy;
        return new(p.CharacterId, stored.Revision, p.ConversationId, p.Enabled, p.IdleSeconds,
            p.MinimumIntervalSeconds, p.DailyLimit, p.TimeZoneId, p.QuietStartMinute, p.QuietEndMinute,
            p.QuietHoursEnabled, DeliveryAvailable: true);
    }
}

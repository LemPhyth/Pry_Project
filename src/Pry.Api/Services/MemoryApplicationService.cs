using Pry.Contracts;
using Pry.Core.Memory;
using Pry.Core.Models;

namespace Pry.Api.Services;

public sealed class MemoryApplicationService(MemoryDatabase database, MemoryEmbeddingService? embeddings = null,
    CompanionOperationCoordinator? operations = null)
{
    public async Task<IReadOnlyList<MemoryDetailsResponse>> DetailsAsync(string characterId, CancellationToken token)
    {
        var scope = MemoryScope.ForCharacter(ContractValidation.Required(characterId, "characterId", 128));
        return (await database.LongTerm.ListAsync(scope, token)).Select(x => new MemoryDetailsResponse(
            x.Record.Id, x.Record.CharacterId, x.Record.Kind, x.Record.Summary, x.Status, x.Record.Importance,
            x.Confidence, x.Origin, x.Revision, x.TopicKey,
            x.Evidence.Select(e => new MemoryEvidenceResponse(e.MessageId, e.ConversationId, e.Excerpt)).ToArray())).ToArray();
    }

    public Task ClearAsync(ClearMemoriesRequest request, CancellationToken token)
    {
        if (!request.Confirm) throw new ApiValidationException("confirm", "清空记忆需要明确确认");
        return database.LongTerm.ClearAsync(MemoryScope.ForCharacter(
            ContractValidation.Required(request.CharacterId, "characterId", 128)), token);
    }

    private static void ValidateKind(string kind)
    {
        if (kind.Equals("behavior", StringComparison.OrdinalIgnoreCase) ||
            kind.Equals("expression", StringComparison.OrdinalIgnoreCase))
            throw new ApiValidationException("kind", "行为与表达模式不属于长期记忆");
    }

    private static string ValidateTags(string? tags)
    {
        if (tags?.Length > 2000) throw new ApiValidationException("tags", "标签不能超过 2000 字符");
        return tags?.Trim() ?? "";
    }

    public Task<IReadOnlyList<MemoryRecord>> ListAsync(string characterId, string? query, CancellationToken token)
    {
        characterId = ContractValidation.Required(characterId, "characterId", 128);
        query = query?.Trim();
        return string.IsNullOrWhiteSpace(query)
            ? database.ListMemoriesAsync(characterId, null, token)
            : (embeddings is null ? database.SearchMemoriesAsync(characterId, query, 100, token) :
                embeddings.SearchMemoriesAsync(characterId, query, 100, token));
    }

    public async Task<MemoryRecord> CreateAsync(CreateMemoryRequest request, CancellationToken token)
    {
        using var operation = operations is null ? null : await operations.EnterForegroundAsync(token);
        ValidateKind(ContractValidation.Required(request.Kind, "kind", 64));
        ContractValidation.Importance(request.Importance);
        var characterId = ContractValidation.Required(request.CharacterId, "characterId", 128);
        long id;
        try
        {
            id = await database.AddMemoryAsync(characterId, ContractValidation.Required(request.Kind, "kind", 64),
                ContractValidation.Required(request.Summary, "summary", 4_000), ValidateTags(request.Tags), request.Importance, request.SourceMessageId, token);
        }
        catch (ArgumentException)
        {
            throw new ApiValidationException("sourceMessageId", "来源消息不存在或不属于此角色");
        }
        return await database.GetMemoryAsync(id, characterId, token) ?? throw new InvalidOperationException("记忆写入后无法读取");
    }

    public async Task<MemoryRecord> UpdateAsync(long id, string characterId, UpdateMemoryRequest request, CancellationToken token)
    {
        using var operation = operations is null ? null : await operations.EnterForegroundAsync(token);
        ValidateKind(ContractValidation.Required(request.Kind, "kind", 64));
        characterId = ContractValidation.Required(characterId, "characterId", 128);
        _ = await database.GetMemoryAsync(id, characterId, token) ?? throw new ResourceNotFoundException("memory", id);
        ContractValidation.Importance(request.Importance);
        try
        {
            await database.UpdateMemoryAsync(id, characterId, ContractValidation.Required(request.Kind, "kind", 64),
                ContractValidation.Required(request.Summary, "summary", 4_000), ValidateTags(request.Tags), request.Importance, token);
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteExtendedErrorCode == 2067)
        {
            throw new ResourceConflictException("memory", id, "已有相同内容的记忆");
        }
        return await database.GetMemoryAsync(id, characterId, token) ?? throw new InvalidOperationException("记忆更新后无法读取");
    }

    public async Task DeleteAsync(long id, string characterId, CancellationToken token)
    {
        using var operation = operations is null ? null : await operations.EnterForegroundAsync(token);
        characterId = ContractValidation.Required(characterId, "characterId", 128);
        await database.DeleteMemoryAsync(id, characterId, token);
    }

    public async Task<MemoryDetailsResponse> ConfirmKeepAsync(long id, string characterId, CancellationToken token)
    {
        using var operation = operations is null ? null : await operations.EnterForegroundAsync(token);
        var scope = MemoryScope.ForCharacter(ContractValidation.Required(characterId, "characterId", 128));
        var result = await database.LongTerm.ConfirmKeepAsync(scope, id, token);
        return result.Outcome switch
        {
            ConfirmKeepOutcome.Confirmed => ToDetails(result.Entry!),
            ConfirmKeepOutcome.NotFound => throw new ResourceNotFoundException("memory", id),
            ConfirmKeepOutcome.NotOrphaned => throw new MemoryConfirmRejectedException("memory_not_orphaned", id,
                "这条记忆不是\"依据已删除\"状态，无需确认保留"),
            _ => throw new MemoryConfirmRejectedException("memory_fingerprint_taken", id,
                "已有相同内容的记忆在生效，确认保留不会把它们合并")
        };
    }

    private static MemoryDetailsResponse ToDetails(MemoryEntry x) => new(
        x.Record.Id, x.Record.CharacterId, x.Record.Kind, x.Record.Summary, x.Status, x.Record.Importance,
        x.Confidence, x.Origin, x.Revision, x.TopicKey,
        x.Evidence.Select(e => new MemoryEvidenceResponse(e.MessageId, e.ConversationId, e.Excerpt)).ToArray());
}

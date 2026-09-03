using Pry.Api.Contracts;
using Pry.Core.Memory;
using Pry.Core.Models;

namespace Pry.Api.Services;

public sealed class MemoryApplicationService(MemoryDatabase database)
{
    public Task<IReadOnlyList<MemoryRecord>> ListAsync(string characterId, string? query, CancellationToken token) =>
        database.ListMemoriesAsync(ContractValidation.Required(characterId, "characterId", 128), query?.Trim(), token);

    public async Task<MemoryRecord> CreateAsync(CreateMemoryRequest request, CancellationToken token)
    {
        ContractValidation.Importance(request.Importance);
        var characterId = ContractValidation.Required(request.CharacterId, "characterId", 128);
        var id = await database.AddMemoryAsync(characterId, ContractValidation.Required(request.Kind, "kind", 64),
            ContractValidation.Required(request.Summary, "summary", 4_000), request.Tags?.Trim() ?? "", request.Importance, request.SourceMessageId, token);
        return await database.GetMemoryAsync(id, characterId, token) ?? throw new InvalidOperationException("记忆写入后无法读取");
    }

    public async Task<MemoryRecord> UpdateAsync(long id, string characterId, UpdateMemoryRequest request, CancellationToken token)
    {
        characterId = ContractValidation.Required(characterId, "characterId", 128);
        _ = await database.GetMemoryAsync(id, characterId, token) ?? throw new ResourceNotFoundException("memory", id);
        ContractValidation.Importance(request.Importance);
        await database.UpdateMemoryAsync(id, characterId, ContractValidation.Required(request.Kind, "kind", 64),
            ContractValidation.Required(request.Summary, "summary", 4_000), request.Tags?.Trim() ?? "", request.Importance, token);
        return await database.GetMemoryAsync(id, characterId, token) ?? throw new InvalidOperationException("记忆更新后无法读取");
    }

    public async Task DeleteAsync(long id, string characterId, CancellationToken token)
    {
        characterId = ContractValidation.Required(characterId, "characterId", 128);
        _ = await database.GetMemoryAsync(id, characterId, token) ?? throw new ResourceNotFoundException("memory", id);
        await database.DeleteMemoryAsync(id, characterId, token);
    }
}

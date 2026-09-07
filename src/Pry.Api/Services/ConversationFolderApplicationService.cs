using Pry.Contracts;
using Pry.Core.Memory;
using Pry.Core.Models;

namespace Pry.Api.Services;

public sealed class ConversationFolderApplicationService(MemoryDatabase database)
{
    public Task<IReadOnlyList<ConversationFolder>> ListAsync(CancellationToken token) =>
        database.ListConversationFoldersAsync(token);

    public async Task<ConversationFolder> CreateAsync(string? requestedName, CancellationToken token)
    {
        var name = ContractValidation.Required(requestedName, "name", 100);
        var id = await database.CreateConversationFolderAsync(name, token);
        return new ConversationFolder(id, name, DateTimeOffset.UtcNow);
    }

    public async Task RenameAsync(string id, string? requestedName, CancellationToken token)
    {
        if (!await database.ConversationFolderExistsAsync(id, token))
            throw new ResourceNotFoundException("folder", id);
        await database.RenameConversationFolderAsync(id, ContractValidation.Required(requestedName, "name", 100), token);
    }

    public async Task DeleteAsync(string id, CancellationToken token)
    {
        if (!await database.ConversationFolderExistsAsync(id, token))
            throw new ResourceNotFoundException("folder", id);
        await database.DeleteConversationFolderAsync(id, token);
    }
}

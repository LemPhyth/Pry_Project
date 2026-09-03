using Microsoft.AspNetCore.Mvc;
using Pry.Api.Contracts;
using Pry.Core.Memory;
using Pry.Core.Models;

namespace Pry.Api.Controllers;

[ApiController, Route("api/v1/conversation-folders")]
public sealed class FoldersController(MemoryDatabase database) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<ConversationFolder>> List(CancellationToken token) => database.ListConversationFoldersAsync(token);
    [HttpPost] public async Task<ActionResult<ConversationFolder>> Create(CreateFolderRequest request, CancellationToken token)
    {
        var name = ContractValidation.Required(request.Name, "name", 100);
        var id = await database.CreateConversationFolderAsync(name, token);
        var result = new ConversationFolder(id, name, DateTimeOffset.UtcNow);
        return Created($"/api/v1/conversation-folders/{id}", result);
    }
    [HttpPatch("{id}")] public async Task<IActionResult> Rename(string id, RenameFolderRequest request, CancellationToken token)
    {
        if (!await database.ConversationFolderExistsAsync(id, token)) throw new ResourceNotFoundException("folder", id);
        await database.RenameConversationFolderAsync(id, ContractValidation.Required(request.Name, "name", 100), token);
        return NoContent();
    }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(string id, CancellationToken token)
    {
        if (!await database.ConversationFolderExistsAsync(id, token)) throw new ResourceNotFoundException("folder", id);
        await database.DeleteConversationFolderAsync(id, token); return NoContent();
    }
}

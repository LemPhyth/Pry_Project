using Microsoft.AspNetCore.Mvc;
using Pry.Contracts;
using Pry.Api.Services;
using Pry.Core.Models;

namespace Pry.Api.Controllers;

[ApiController, Route("api/v1/conversation-folders")]
public sealed class FoldersController(ConversationFolderApplicationService service) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<ConversationFolder>> List(CancellationToken token) => service.ListAsync(token);
    [HttpPost] public async Task<ActionResult<ConversationFolder>> Create(CreateFolderRequest request, CancellationToken token)
    {
        var result = await service.CreateAsync(request.Name, token);
        return Created($"/api/v1/conversation-folders/{result.Id}", result);
    }
    [HttpPatch("{id}")] public async Task<IActionResult> Rename(string id, RenameFolderRequest request, CancellationToken token)
    {
        await service.RenameAsync(id, request.Name, token);
        return NoContent();
    }
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(string id, CancellationToken token)
    {
        await service.DeleteAsync(id, token); return NoContent();
    }
}

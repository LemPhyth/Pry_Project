using Microsoft.AspNetCore.Mvc;
using Pry.Api.Contracts;
using Pry.Api.Services;
using Pry.Core.Models;

namespace Pry.Api.Controllers;

[ApiController, Route("api/v1/memories")]
public sealed class MemoriesController(MemoryApplicationService service) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<MemoryRecord>> List([FromQuery] string characterId, [FromQuery] string? query, CancellationToken token) => service.ListAsync(characterId, query, token);
    [HttpPost] public async Task<ActionResult<MemoryRecord>> Create(CreateMemoryRequest request, CancellationToken token)
    {
        var result = await service.CreateAsync(request, token); return Created($"/api/v1/memories/{result.Id}?characterId={Uri.EscapeDataString(result.CharacterId)}", result);
    }
    [HttpPut("{id:long}")] public Task<MemoryRecord> Update(long id, [FromQuery] string characterId, UpdateMemoryRequest request, CancellationToken token) => service.UpdateAsync(id, characterId, request, token);
    [HttpDelete("{id:long}")] public async Task<IActionResult> Delete(long id, [FromQuery] string characterId, CancellationToken token) { await service.DeleteAsync(id, characterId, token); return NoContent(); }
}

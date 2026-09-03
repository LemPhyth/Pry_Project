using Microsoft.AspNetCore.Mvc;
using Pry.Api.Contracts;
using Pry.Api.Services;

namespace Pry.Api.Controllers;

[ApiController, Route("api/v1/stickers")]
public sealed class StickersController(StickerApplicationService service) : ControllerBase
{
    [HttpGet] public IReadOnlyList<StickerResponse> List() => service.List();
    [HttpPost] public async Task<ActionResult<StickerResponse>> Import(ImportStickerRequest request, CancellationToken token)
    {
        var result = await service.ImportAsync(request, token); return Created($"/api/v1/stickers/{result.Id}", result);
    }
    [HttpPut("{id}")] public Task<StickerResponse> Update(string id, UpdateStickerRequest request, CancellationToken token) => service.UpdateAsync(id, request, token);
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(string id, CancellationToken token) { await service.DeleteAsync(id, token); return NoContent(); }
    [HttpGet("{id}/content")]
    public IActionResult Download(string id)
    {
        var sticker = service.Get(id);
        var contentType = Path.GetExtension(sticker.FilePath).ToLowerInvariant() switch { ".png" => "image/png", ".gif" => "image/gif", ".webp" => "image/webp", _ => "image/jpeg" };
        return File(new FileStream(sticker.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read), contentType, enableRangeProcessing: true);
    }
}

using Microsoft.AspNetCore.Mvc;
using Pry.Contracts;
using Pry.Api.Services;

namespace Pry.Api.Controllers;

[ApiController, Route("api/v1")]
public sealed class ConfigurationController(ConfigurationApplicationService service) : ControllerBase
{
    [HttpGet("characters")] public IReadOnlyList<CharacterSummaryResponse> Characters() => service.ListCharacters();
    [HttpGet("characters/{id}")] public CharacterResponse Character(string id) => service.GetCharacter(id);
    [HttpPost("characters")] public async Task<ActionResult<CharacterResponse>> CreateCharacter(SaveCharacterRequest request, CancellationToken token)
    {
        var result = await service.CreateCharacterAsync(request, token); return Created($"/api/v1/characters/{result.Id}", result);
    }
    [HttpPut("characters/{id}")] public Task<CharacterResponse> UpdateCharacter(string id, SaveCharacterRequest request, CancellationToken token) => service.UpdateCharacterAsync(id, request, token);
    [HttpDelete("characters/{id}")] public async Task<IActionResult> DeleteCharacter(string id, CancellationToken token)
    {
        await service.DeleteCharacterAsync(id, token); return NoContent();
    }
    [HttpGet("characters/{id}/avatar")]
    public IActionResult Avatar(string id)
    {
        var path = service.GetAvatarPath(id);
        var contentType = Path.GetExtension(path).ToLowerInvariant() switch { ".png" => "image/png", ".gif" => "image/gif", ".webp" => "image/webp", ".bmp" => "image/bmp", _ => "image/jpeg" };
        return File(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), contentType, enableRangeProcessing: true);
    }
    [HttpGet("preferences")] public ClientPreferencesResponse Preferences() => service.GetPreferences();
    [HttpPatch("preferences")] public Task<ClientPreferencesResponse> UpdatePreferences(UpdateClientPreferencesRequest request, CancellationToken token) => service.UpdatePreferencesAsync(request, token);
    [HttpGet("models")] public IReadOnlyList<ModelProfileResponse> Models() => service.GetModels();
    [HttpPut("models/selection")] public Task<IReadOnlyList<ModelProfileResponse>> SelectModels(UpdateModelSelectionRequest request, CancellationToken token) => service.UpdateModelSelectionAsync(request, token);
    [HttpPost("models/custom")] public async Task<ActionResult<ModelProfileResponse>> CreateCustomModel(SaveCustomModelRequest request, CancellationToken token)
    {
        var result = await service.CreateCustomModelAsync(request, token); return Created($"/api/v1/models/{result.Id}", result);
    }
    [HttpPut("models/custom/{id}")] public Task<ModelProfileResponse> UpdateCustomModel(string id, SaveCustomModelRequest request, CancellationToken token) => service.UpdateCustomModelAsync(id, request, token);
    [HttpDelete("models/custom/{id}")] public async Task<IActionResult> DeleteCustomModel(string id, CancellationToken token) { await service.DeleteCustomModelAsync(id, token); return NoContent(); }
    [HttpPut("appearance/media")] public Task<ClientPreferencesResponse> UpdateAppearance(UpdateAppearanceMediaRequest request, CancellationToken token) => service.UpdateAppearanceMediaAsync(request, token);
    [HttpGet("appearance/{kind}")]
    public IActionResult Appearance(string kind)
    {
        var path = service.GetAppearancePath(kind);
        var contentType = Path.GetExtension(path).ToLowerInvariant() switch { ".png" => "image/png", ".gif" => "image/gif", ".webp" => "image/webp", ".bmp" => "image/bmp", _ => "image/jpeg" };
        return File(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), contentType, enableRangeProcessing: true);
    }
}

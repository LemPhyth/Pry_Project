using Microsoft.AspNetCore.Mvc;
using Pry.Api.Services;
using Pry.Contracts;

namespace Pry.Api.Controllers;

[ApiController, Route("api/v1")]
public sealed class CharacterInteractionsController(CharacterInteractionProfileService profiles,
    ConversationSessionService sessions) : ControllerBase
{
    [HttpGet("characters/{characterId}/interaction-profile")]
    public Task<InteractionProfileResponse> GetProfile(string characterId, CancellationToken token) =>
        profiles.GetAsync(characterId, token);

    [HttpPut("characters/{characterId}/interaction-profile")]
    public Task<InteractionProfileResponse> SaveProfile(string characterId, SaveInteractionProfileRequest request,
        CancellationToken token) => profiles.SaveAsync(characterId, request, token);

    [HttpPost("conversations/{conversationId}/interactions")]
    public async Task<ActionResult<InteractionCommandResponse>> Execute(string conversationId,
        ExecuteInteractionRequest request, CancellationToken token)
    {
        var result = await sessions.ExecuteInteractionAsync(conversationId, request, token);
        return Accepted($"/api/v1/conversations/{conversationId}/messages", result);
    }
}

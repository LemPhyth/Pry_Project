using Microsoft.AspNetCore.Mvc;
using Pry.Api.Services;
using Pry.Contracts;

namespace Pry.Api.Controllers;

[ApiController, Route("api/v1")]
public sealed class ProactivePolicyController(ProactivePolicyService policies, CompanionActivityService activity,
    ProactiveScheduler scheduler) : ControllerBase
{
    [HttpGet("characters/{characterId}/proactive-policy")]
    public Task<ProactivePolicyResponse> Get(string characterId, CancellationToken token) => policies.GetAsync(characterId, token);

    [HttpGet("characters/{characterId}/proactive-status")]
    public Task<ProactiveStatusResponse> Status(string characterId, CancellationToken token) => scheduler.GetStatusAsync(characterId, token);

    [HttpPut("characters/{characterId}/proactive-policy")]
    public Task<ProactivePolicyResponse> Save(string characterId, SaveProactivePolicyRequest request,
        CancellationToken token) => policies.SaveAsync(characterId, request, token);

    [HttpPut("companion/activity/{clientId}")]
    public Task<CompanionActivityResponse> Report(string clientId, ReportCompanionActivityRequest request,
        CancellationToken token) => activity.ReportAsync(clientId, request, token);

    [HttpDelete("companion/activity/{clientId}")]
    public async Task<IActionResult> Remove(string clientId, CancellationToken token)
    { await activity.RemoveAsync(clientId, token); return NoContent(); }
}

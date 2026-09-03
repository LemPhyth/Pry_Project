using Microsoft.AspNetCore.Mvc;
using Pry.Api.Contracts;
using Pry.Api.Services;
using Pry.Core.Models;

namespace Pry.Api.Controllers;

[ApiController, Route("api/v1/conversations")]
public sealed class ConversationsController(ConversationApplicationService service) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<ConversationRoom>> List([FromQuery] int limit = 100, CancellationToken token = default) => service.ListAsync(limit, token);
    [HttpGet("{id}")] public Task<ConversationRoom> Get(string id, CancellationToken token) => service.GetAsync(id, token);
    [HttpPost] public async Task<ActionResult<ConversationRoom>> Create(CreateConversationRequest request, CancellationToken token)
    {
        var result = await service.CreateAsync(request, token);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }
    [HttpPatch("{id}")] public Task<ConversationRoom> Update(string id, UpdateConversationRequest request, CancellationToken token) => service.UpdateAsync(id, request, token);
    [HttpDelete("{id}")] public async Task<IActionResult> Delete(string id, CancellationToken token) { await service.DeleteAsync(id, token); return NoContent(); }
    [HttpGet("{id}/messages")] public Task<IReadOnlyList<ChatMessage>> Messages(string id, [FromQuery] int limit = 200, CancellationToken token = default) => service.MessagesAsync(id, limit, token);
    [HttpPost("{id}/messages")] public async Task<ActionResult<ChatMessage>> AddMessage(string id, CreateMessageRequest request, CancellationToken token)
    {
        var result = await service.AddMessageAsync(id, request, token);
        return Created($"/api/v1/conversations/{id}/messages/{result.Id}", result);
    }
}

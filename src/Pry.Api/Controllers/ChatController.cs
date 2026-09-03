using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Pry.Api.Contracts;
using Pry.Api.Services;

namespace Pry.Api.Controllers;

[ApiController, Route("api/v1")]
public sealed class ChatController(ConversationSessionService sessions, BackendRuntime runtime) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet("runtime")]
    public RuntimeStatusResponse RuntimeStatus() => runtime.Status;

    [HttpPost("conversations/{id}/turns")]
    public async Task<ActionResult<SubmitTurnResponse>> Submit(string id, SubmitTurnRequest request, CancellationToken token)
    {
        var result = await sessions.SubmitAsync(id, request, token);
        return Accepted($"/api/v1/conversations/{id}/events", result);
    }

    [HttpPost("conversations/{id}/turns/cancel")]
    public async Task<IActionResult> Cancel(string id, CancellationToken token)
    {
        await sessions.CancelAsync(id, token); return Accepted();
    }

    [HttpGet("conversations/{id}/events")]
    public async Task Events(string id, [FromQuery] long after = 0, CancellationToken token = default)
    {
        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        await foreach (var item in sessions.EventsAsync(id, Math.Max(0, after), token))
        {
            await Response.WriteAsync($"id: {item.Sequence}\n", token);
            await Response.WriteAsync($"event: {item.Type}\n", token);
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(item, JsonOptions)}\n\n", token);
            await Response.Body.FlushAsync(token);
        }
    }
}

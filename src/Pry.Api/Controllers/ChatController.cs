using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Pry.Contracts;
using Pry.Api.Services;
using Pry.Core.Models;

namespace Pry.Api.Controllers;

[ApiController, Route("api/v1")]
public sealed class ChatController(ConversationSessionService sessions, BackendRuntime runtime) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet("runtime")]
    public RuntimeStatusResponse RuntimeStatus() => runtime.Status;

    [HttpGet("runtime/compute-devices")]
    public Task<IReadOnlyList<ComputeDeviceResponse>> ComputeDevices(CancellationToken token) =>
        runtime.ListComputeDevicesAsync(token);

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

    [HttpDelete("conversations/{id}/messages/{messageId:long}")]
    public Task<ConversationMutationResponse> DeleteMessage(string id, long messageId, CancellationToken token) =>
        sessions.DeleteMessageAsync(id, messageId, token);

    [HttpPost("conversations/{id}/messages/{messageId:long}/regenerate")]
    public async Task<IActionResult> Regenerate(string id, long messageId, CancellationToken token)
    {
        await sessions.RegenerateAsync(id, messageId, token); return Accepted();
    }

    [HttpPost("conversations/{id}/mutations/undo")]
    public async Task<IActionResult> Undo(string id, CancellationToken token)
    {
        await sessions.UndoAsync(id, token); return NoContent();
    }

    [HttpPost("conversations/{id}/listening-signals")]
    public async Task<ActionResult<ChatMessage>> ListeningSignal(string id, CancellationToken token) =>
        Ok(await sessions.SendListeningSignalAsync(id, token));

    [HttpGet("conversations/{id}/events")]
    public async Task Events(string id, [FromQuery] long after = 0, CancellationToken token = default)
    {
        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        await foreach (var item in sessions.EventsAsync(id, after, token))
        {
            await Response.WriteAsync($"id: {item.Sequence}\n", token);
            await Response.WriteAsync($"event: {item.Type}\n", token);
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(item, JsonOptions)}\n\n", token);
            await Response.Body.FlushAsync(token);
        }
    }
}

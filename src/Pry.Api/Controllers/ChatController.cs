using Microsoft.AspNetCore.Mvc;
using Pry.Contracts;
using Pry.Api.Services;
using Pry.Core.Models;

namespace Pry.Api.Controllers;

[ApiController, Route("api/v1")]
public sealed class ChatController(ConversationSessionService sessions, BackendRuntime runtime,
    IConfiguration configuration, ExpressionFeedbackService feedback) : ControllerBase
{
    [HttpGet("runtime")]
    public RuntimeStatusResponse RuntimeStatus() => runtime.Status;

    [HttpGet("runtime/compute-devices")]
    public Task<IReadOnlyList<ComputeDeviceResponse>> ComputeDevices(CancellationToken token) =>
        runtime.ListComputeDevicesAsync(token);

    [HttpPost("runtime/retry")]
    public async Task<RuntimeStatusResponse> RetryRuntime(CancellationToken token)
    {
        await sessions.ReconfigureAsync(runtime.ReloadAsync, token);
        return runtime.Status;
    }

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

    [HttpPost("conversations/{id}/messages/{messageId:long}/expression-feedback")]
    public Task<ExpressionFeedbackResponse> ExpressionFeedback(string id, long messageId,
        ExpressionFeedbackRequest request, CancellationToken token) =>
        feedback.ReactAsync(id, messageId, request, token);

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
        // 心跳只允许在"密于一秒、疏于客户端存活窗口一半"之间调节：更密会让注释帧变成忙等，
        // 更疏会让安静会话被客户端误判成断连，而客户端窗口不会跟着单个服务端配置缩小。
        var configuredHeartbeat = configuration.GetValue("Pry:EventHeartbeatMs", PryProtocol.HeartbeatIntervalMs);
        var heartbeat = TimeSpan.FromMilliseconds(
            Math.Clamp(configuredHeartbeat, 1_000, PryProtocol.LivenessTimeoutMs / 2));
        await foreach (var frame in SseEventStream.FramesAsync(sessions.EventsAsync(id, after, token), heartbeat, token))
        {
            await Response.WriteAsync(frame, token);
            await Response.Body.FlushAsync(token);
        }
    }
}

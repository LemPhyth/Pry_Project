using Microsoft.AspNetCore.Mvc;
using Pry.Api.Services;
using Pry.Contracts;

namespace Pry.Api.Controllers;

/// <summary>RT-002 的只读诊断入口：读它不会创建、唤醒或改变任何运行时。</summary>
[ApiController, Route("api/v1/conversations")]
public sealed class ConversationRuntimeController(ConversationSessionService sessions) : ControllerBase
{
    [HttpGet("{id}/runtime-state")]
    public Task<ConversationRuntimeStateResponse> State(string id, CancellationToken token) =>
        sessions.GetRuntimeStateAsync(id, token);

    [HttpGet("{id}/runtime-trace")]
    public Task<ConversationRuntimeTraceResponse> Trace(string id, [FromQuery] int limit, CancellationToken token) =>
        sessions.GetRuntimeTraceAsync(id, limit, token);
}

using Microsoft.AspNetCore.Mvc;
using Pry.Api.Services;
using Pry.Contracts;

namespace Pry.Api.Controllers;

[ApiController, Route("api/v1/conversations")]
public sealed class ConversationReadController(ConversationReadService service) : ControllerBase
{
    [HttpGet("{id}/read-state")]
    public Task<ConversationReadStateResponse> Get(string id, CancellationToken token) => service.GetAsync(id, token);
    [HttpPut("{id}/read-state")]
    public Task<ConversationReadStateResponse> Mark(string id, MarkConversationReadRequest request,
        CancellationToken token) => service.MarkAsync(id, request, token);
    [HttpGet("unread-summary")]
    public Task<UnreadSummaryResponse> Summary(CancellationToken token) => service.SummaryAsync(token);
}

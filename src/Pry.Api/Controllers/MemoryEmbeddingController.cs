using Microsoft.AspNetCore.Mvc;
using Pry.Api.Services;
using Pry.Contracts;

namespace Pry.Api.Controllers;

[ApiController, Route("api/v1/memory/embedding")]
public sealed class MemoryEmbeddingController(ConfigurationApplicationService configuration,
    MemoryEmbeddingService embeddings) : ControllerBase
{
    [HttpGet]
    public Task<EmbeddingStatusResponse> Status(CancellationToken token) => embeddings.GetStatusAsync(token);

    [HttpPut]
    public Task<EmbeddingStatusResponse> Update(UpdateEmbeddingSettingsRequest request, CancellationToken token) =>
        configuration.UpdateEmbeddingSelectionAsync(request, token);

    [HttpPost("reindex")]
    public Task<EmbeddingStatusResponse> Reindex([FromQuery] bool clear = false, CancellationToken token = default) =>
        embeddings.RequestReindexAsync(clear, token);
}

using Microsoft.AspNetCore.Mvc;
using Pry.Contracts;
using Pry.Api.Services;

namespace Pry.Api.Controllers;

[ApiController, Route("api/v1/speech")]
public sealed class SpeechController(SpeechApplicationService service, ConfigurationApplicationService configuration) : ControllerBase
{
    [HttpGet("models")] public IReadOnlyList<SpeechModelResponse> Models() => service.ListModels();
    [HttpPost("models/custom")] public async Task<ActionResult<SpeechModelResponse>> CreateModel(SaveSpeechModelRequest request, CancellationToken token)
    {
        var result = await configuration.CreateSpeechModelAsync(request, token);
        return Created($"/api/v1/speech/models/{result.Id}", result);
    }
    [HttpPut("models/custom/{id}")] public Task<SpeechModelResponse> UpdateModel(string id, SaveSpeechModelRequest request, CancellationToken token) =>
        configuration.UpdateSpeechModelAsync(id, request, token);
    [HttpDelete("models/custom/{id}")] public async Task<IActionResult> DeleteModel(string id, CancellationToken token)
    {
        await configuration.DeleteSpeechModelAsync(id, token); return NoContent();
    }
    [HttpPost("transcriptions")] public Task<TranscribeSpeechResponse> Transcribe(TranscribeSpeechRequest request,
        CancellationToken token) => service.TranscribeAsync(request, token);
}

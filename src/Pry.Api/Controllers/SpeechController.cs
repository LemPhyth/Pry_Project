using Microsoft.AspNetCore.Mvc;
using Pry.Api.Contracts;
using Pry.Api.Services;

namespace Pry.Api.Controllers;

[ApiController, Route("api/v1/speech")]
public sealed class SpeechController(SpeechApplicationService service) : ControllerBase
{
    [HttpGet("models")] public IReadOnlyList<SpeechModelResponse> Models() => service.ListModels();
    [HttpPost("transcriptions")] public Task<TranscribeSpeechResponse> Transcribe(TranscribeSpeechRequest request,
        CancellationToken token) => service.TranscribeAsync(request, token);
}

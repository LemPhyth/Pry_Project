using Pry.Contracts;
using Pry.Core.Abstractions;
using Pry.Core.Inference;

namespace Pry.Api.Services;

public sealed class SpeechApplicationService(BackendRuntime runtime, MediaAssetStore media) : IDisposable
{
    private readonly SemaphoreSlim _recognitionGate = new(1, 1);
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromMinutes(3) };

    public IReadOnlyList<SpeechModelResponse> ListModels() => runtime.SpeechProfiles.Select(profile =>
    {
        var resolved = runtime.ResolveSpeechProfile(profile.Id);
        ISpeechRecognizer recognizer = CreateRecognizer(resolved);
        return new SpeechModelResponse(profile.Id, profile.DisplayName, profile.Provider, profile.ModelName,
            profile.Language, profile.SampleRate, profile.Id == runtime.ActiveSpeechModelId, recognizer.IsAvailable,
            runtime.Preferences.CustomSpeechModels.Any(x => x.Id == profile.Id));
    }).ToArray();

    public async Task<TranscribeSpeechResponse> TranscribeAsync(TranscribeSpeechRequest request, CancellationToken token)
    {
        var asset = await media.ResolveAsync(ContractValidation.Required(request.MediaId, "mediaId", 128), token);
        if (asset.Metadata.Kind != "Audio") throw new ApiValidationException("mediaId", "语音识别需要 WAV 音频资源");
        var profile = runtime.ResolveSpeechProfile(request.ModelId);
        var recognizer = CreateRecognizer(profile);
        if (!recognizer.IsAvailable) throw new InvalidOperationException("所选语音识别模型当前不可用。");
        await _recognitionGate.WaitAsync(token);
        try
        {
            var text = await recognizer.RecognizeAsync(asset.Path, token);
            return new TranscribeSpeechResponse(text, profile.Id);
        }
        finally { _recognitionGate.Release(); }
    }

    private ISpeechRecognizer CreateRecognizer(Pry.Core.Models.SpeechModelProfile profile) => profile.Provider switch
    {
        "sherpa-onnx" => new SenseVoiceSpeechRecognizer(profile),
        "openai-compatible" => new OpenAiSpeechRecognizer(_httpClient, profile),
        _ => throw new ApiValidationException("modelId", $"不支持语音识别类型 {profile.Provider}")
    };

    public void Dispose() { _recognitionGate.Dispose(); _httpClient.Dispose(); }
}

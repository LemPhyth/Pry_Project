using System.Net.Http.Headers;
using System.Text.Json;
using Pry.Core.Abstractions;
using Pry.Core.Models;
using SherpaOnnx;

namespace Pry.Core.Inference;

public sealed class SenseVoiceSpeechRecognizer(SpeechModelProfile profile) : ISpeechRecognizer
{
    private readonly string _model = Path.Combine(profile.ModelPath, "model.int8.onnx");
    private readonly string _tokens = Path.Combine(profile.ModelPath, "tokens.txt");
    public bool IsAvailable => File.Exists(_model) && File.Exists(_tokens);

    public Task<string> RecognizeAsync(string audioPath, CancellationToken cancellationToken = default) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsAvailable) throw new FileNotFoundException("SenseVoice 模型不完整，需要 model.int8.onnx 和 tokens.txt。", profile.ModelPath);
        var (sampleRate, samples) = ReadPcm16Wave(audioPath);
        var config = new OfflineRecognizerConfig
        {
            FeatConfig = new FeatureConfig { SampleRate = sampleRate, FeatureDim = 80 },
            DecodingMethod = "greedy_search",
            ModelConfig = new OfflineModelConfig
            {
                Tokens = _tokens,
                NumThreads = Math.Clamp(Environment.ProcessorCount / 2, 1, 8),
                Provider = "cpu",
                SenseVoice = new OfflineSenseVoiceModelConfig
                {
                    Model = _model,
                    Language = string.IsNullOrWhiteSpace(profile.Language) ? "auto" : profile.Language,
                    UseInverseTextNormalization = 1
                }
            }
        };
        using var recognizer = new OfflineRecognizer(config);
        using var stream = recognizer.CreateStream();
        stream.AcceptWaveform(sampleRate, samples);
        cancellationToken.ThrowIfCancellationRequested();
        recognizer.Decode(stream);
        return CleanSenseVoiceResult(stream.Result.Text);
    }, cancellationToken);

    private static string CleanSenseVoiceResult(string value)
    {
        var text = value.Trim();
        while (text.StartsWith('<'))
        {
            var end = text.IndexOf('>'); if (end < 0) break;
            text = text[(end + 1)..].TrimStart();
        }
        return text;
    }

    private static (int SampleRate, float[] Samples) ReadPcm16Wave(string path)
    {
        using var stream = File.OpenRead(path); using var reader = new BinaryReader(stream);
        if (new string(reader.ReadChars(4)) != "RIFF") throw new InvalidDataException("录音不是有效的 WAV 文件。");
        reader.ReadInt32(); if (new string(reader.ReadChars(4)) != "WAVE") throw new InvalidDataException("录音不是有效的 WAV 文件。");
        short format = 0, channels = 0, bits = 0; var sampleRate = 0; byte[]? data = null;
        while (stream.Position + 8 <= stream.Length)
        {
            var id = new string(reader.ReadChars(4)); var size = reader.ReadInt32();
            if (id == "fmt ")
            {
                format = reader.ReadInt16(); channels = reader.ReadInt16(); sampleRate = reader.ReadInt32();
                reader.ReadInt32(); reader.ReadInt16(); bits = reader.ReadInt16();
                if (size > 16) reader.ReadBytes(size - 16);
            }
            else if (id == "data") data = reader.ReadBytes(size);
            else reader.ReadBytes(size);
            if ((size & 1) != 0 && stream.Position < stream.Length) reader.ReadByte();
        }
        if (format != 1 || bits != 16 || channels < 1 || data is null || sampleRate <= 0)
            throw new InvalidDataException("语音识别目前需要 16-bit PCM WAV 录音。");
        var frames = data.Length / (2 * channels); var samples = new float[frames];
        for (var frame = 0; frame < frames; frame++)
        {
            var total = 0f;
            for (var channel = 0; channel < channels; channel++)
                total += BitConverter.ToInt16(data, (frame * channels + channel) * 2) / 32768f;
            samples[frame] = total / channels;
        }
        return (sampleRate, samples);
    }
}

public sealed class OpenAiSpeechRecognizer(HttpClient client, SpeechModelProfile profile) : ISpeechRecognizer
{
    public bool IsAvailable => Uri.TryCreate(profile.BaseUrl, UriKind.Absolute, out _) && !string.IsNullOrWhiteSpace(profile.ApiKey);

    public async Task<string> RecognizeAsync(string audioPath, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable) throw new InvalidOperationException("语音 API 地址或密钥未配置。请设置 PRY_SPEECH_API_KEY_<模型ID> 环境变量。");
        using var form = new MultipartFormDataContent();
        await using var file = File.OpenRead(audioPath); using var content = new StreamContent(file);
        content.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        form.Add(content, "file", Path.GetFileName(audioPath)); form.Add(new StringContent(profile.ModelName), "model");
        if (!string.IsNullOrWhiteSpace(profile.Language) && profile.Language != "auto") form.Add(new StringContent(profile.Language), "language");
        using var request = new HttpRequestMessage(HttpMethod.Post, profile.BaseUrl) { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", profile.ApiKey);
        using var response = await client.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"语音 API 返回 {(int)response.StatusCode}：{payload}");
        using var json = JsonDocument.Parse(payload);
        return json.RootElement.TryGetProperty("text", out var text) ? text.GetString()?.Trim() ?? "" : throw new InvalidDataException("语音 API 响应缺少 text 字段。");
    }
}

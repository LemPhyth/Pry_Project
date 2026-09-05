using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class SpeechModelManagerStateTests
{
    [Fact]
    public void Add_uses_server_assigned_id_without_mutating_source()
    {
        var source = Profile("temporary");
        var state = new SpeechModelManagerState([source]);

        state.Add(source, "server-id");

        Assert.Equal(["temporary", "server-id"], state.Items.Select(item => item.Id));
        Assert.Equal("temporary", source.Id);
    }

    [Fact]
    public void Update_and_remove_target_only_matching_profile()
    {
        var state = new SpeechModelManagerState([Profile("first"), Profile("second")]);
        var updated = Profile("second") with { DisplayName = "已更新" };

        Assert.True(state.Update("second", updated));
        Assert.True(state.Remove("first"));

        var remaining = Assert.Single(state.Items);
        Assert.Equal("second", remaining.Id);
        Assert.Equal("已更新", remaining.DisplayName);
    }

    [Fact]
    public void Request_mapping_preserves_all_backend_fields()
    {
        var profile = Profile("speech-id") with
        {
            DisplayName = "SenseVoice",
            Provider = "sherpa-onnx",
            ModelName = "sense-voice-zh-en-ja-ko-yue",
            ModelPath = "models/sensevoice",
            BaseUrl = "http://localhost:8000/v1/audio/transcriptions",
            Language = "zh",
            SampleRate = 16000
        };

        var request = SpeechModelManagerDialog.ToRequest(profile);

        Assert.Equal(profile.DisplayName, request.DisplayName);
        Assert.Equal(profile.Provider, request.Provider);
        Assert.Equal(profile.ModelName, request.ModelName);
        Assert.Equal(profile.ModelPath, request.LocalModelDirectory);
        Assert.Equal(profile.BaseUrl, request.BaseUrl);
        Assert.Equal(profile.Language, request.Language);
        Assert.Equal(profile.SampleRate, request.SampleRate);
    }

    private static SpeechModelProfile Profile(string id) => new()
    {
        Id = id,
        DisplayName = id,
        Provider = "sherpa-onnx",
        ModelName = "model",
        ModelPath = "path",
        BaseUrl = "",
        Language = "zh",
        SampleRate = 16000
    };
}

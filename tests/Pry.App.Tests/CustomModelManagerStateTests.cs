using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class CustomModelManagerStateTests
{
    [Fact]
    public void Hidden_paths_are_omitted_so_backend_can_preserve_them()
    {
        var request = CustomModelManagerDialog.ToRequest(Profile("model") with
        {
            ModelPath = " ", MmprojPath = ""
        });

        Assert.Null(request.LocalModelPath);
        Assert.Null(request.LocalMmprojPath);
    }

    [Fact]
    public void Local_model_does_not_require_remote_url_but_online_model_does()
    {
        Assert.True(CustomModelManagerDialog.HasRequiredFields("模型", "local-llama", "model", ""));
        Assert.False(CustomModelManagerDialog.HasRequiredFields("模型", "openai-compatible", "model", ""));
    }

    [Fact]
    public void Collection_applies_server_id_update_and_remove()
    {
        var state = new CustomModelManagerState([Profile("first")]);
        state.Add(Profile("draft"), "server");
        Assert.True(state.Update("server", Profile("server") with { DisplayName = "已更新" }));
        Assert.True(state.Remove("first"));

        var remaining = Assert.Single(state.Items);
        Assert.Equal("server", remaining.Id);
        Assert.Equal("已更新", remaining.DisplayName);
    }

    private static ModelProfile Profile(string id) => new()
    {
        Id = id, DisplayName = id, Provider = "local-llama", ModelName = "model",
        Capabilities = new ModelCapabilities(Text: true), ContextSize = 4096,
        MaxOutputTokens = 512, Temperature = .8, GpuLayers = 999,
        ComputeDevice = "auto-discrete", EnableThinking = true
    };
}

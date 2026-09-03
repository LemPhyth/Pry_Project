using Avalonia.Controls;
using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class ModelTuningSettingsEditorTests
{
    [Fact]
    public void Editor_loads_cpu_draft_and_disables_gpu_layers()
    {
        var profile = CreateProfile("one", .7, "auto-discrete");
        var saved = new ModelTuningPreferences
        {
            Temperature = 1.1,
            MaxOutputTokens = 700,
            ContextSize = 8000,
            ComputeDevice = "cpu",
            GpuLayers = 50
        };

        var editor = new ModelTuningSettingsEditor([profile], Devices(), profile.Id,
            new Dictionary<string, ModelTuningPreferences> { [profile.Id] = saved }, new SettingsUiFactory());

        var numbers = editor.Panel.Children.OfType<NumericUpDown>().ToArray();
        Assert.Equal(1.1m, numbers[0].Value);
        Assert.False(numbers[3].IsEnabled);
        Assert.Equal(0, numbers[3].Value);
        var result = editor.BuildDrafts()[profile.Id];
        Assert.Equal("cpu", result.ComputeDevice);
        Assert.Equal(0, result.GpuLayers);
    }

    [Fact]
    public void Switching_models_preserves_edits_per_model()
    {
        var first = CreateProfile("one", .7, "auto-discrete");
        var second = CreateProfile("two", .4, "cpu");
        var editor = new ModelTuningSettingsEditor([first, second], Devices(), first.Id,
            new Dictionary<string, ModelTuningPreferences>(), new SettingsUiFactory());
        var numbers = editor.Panel.Children.OfType<NumericUpDown>().ToArray();
        numbers[0].Value = 1.25m;
        var model = editor.Panel.Children.OfType<ComboBox>().First();

        model.SelectedIndex = 1;
        var result = editor.BuildDrafts();

        Assert.Equal(second.Id, editor.CurrentModelId);
        Assert.Equal(1.25, result[first.Id].Temperature);
        Assert.Equal(.4, result[second.Id].Temperature);
        Assert.Equal("cpu", result[second.Id].ComputeDevice);
    }

    private static ModelProfile CreateProfile(string id, double temperature, string device) => new()
    {
        Id = id,
        DisplayName = id,
        Temperature = temperature,
        MaxOutputTokens = 512,
        ContextSize = 4096,
        ComputeDevice = device
    };

    private static ComputeDeviceChoice[] Devices() =>
        [new("auto-discrete", "自动"), new("cpu", "CPU")];
}

using System.Reflection;
using Avalonia.Controls;
using Pry.App;
using Pry.Contracts;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class MessengerModelDraftTests
{
    [Fact]
    public void Switching_parameter_target_restores_each_models_unsaved_values()
    {
        var settings = new PryMessengerSettingsWindow();
        var first = Model("first");
        var second = Model("second");
        Call(settings, "LoadTuning", first);
        settings.FindControl<NumericUpDown>("TemperatureBox")!.Value = .35m;
        Call(settings, "CaptureTuningDraft");
        Call(settings, "LoadTuning", second);
        settings.FindControl<NumericUpDown>("TemperatureBox")!.Value = .85m;
        Call(settings, "CaptureTuningDraft");
        Call(settings, "LoadTuning", first);
        Assert.Equal(.35m, settings.FindControl<NumericUpDown>("TemperatureBox")!.Value);
        Call(settings, "LoadTuning", second);
        Assert.Equal(.85m, settings.FindControl<NumericUpDown>("TemperatureBox")!.Value);
    }

    [Fact]
    public void Unchanged_parameters_do_not_create_override_updates()
    {
        var settings = new PryMessengerSettingsWindow();
        Assert.False((bool)typeof(PryMessengerSettingsWindow)
            .GetMethod("HasTuningChanges", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(settings, null)!);
    }

    [Fact]
    public void Unavailable_model_remains_visible_but_cannot_be_chosen()
    {
        var item = (ComboBoxItem)typeof(PryMessengerSettingsWindow)
            .GetMethod("ModelItem", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [Model("missing") with { Available = false }])!;
        Assert.False(item.IsEnabled);
        Assert.Contains("文件不可用", item.Content!.ToString());
    }

    private static ModelProfileResponse Model(string id) => new(id, id, "local-llama", id,
        new ModelCapabilities(Text: true), 4096, 512, .6, 0, "auto-discrete", false, false, false, false);

    private static void Call(object instance, string name, params object[] args) =>
        instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(instance, args);
}

using Avalonia.Automation;
using Avalonia.Controls;
using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class DesktopPetSettingsEditorTests
{
    [Fact]
    public void Editor_round_trips_existing_preferences()
    {
        var source = new DesktopPetPreferences { Enabled = true, AlwaysOnTop = false, Scale = 1.4 };

        var editor = new DesktopPetSettingsEditor(source, new SettingsUiFactory());

        Assert.Equal(source, editor.BuildDraft());
        var scale = Assert.Single(editor.Panel.Children.OfType<NumericUpDown>());
        Assert.Equal("桌宠缩放", AutomationProperties.GetName(scale));
        Assert.Equal(.5m, scale.Minimum);
        Assert.Equal(3, scale.Maximum);
    }

    [Fact]
    public void BuildDraft_reads_current_controls()
    {
        var editor = new DesktopPetSettingsEditor(new DesktopPetPreferences(), new SettingsUiFactory());
        var checks = editor.Panel.Children.OfType<CheckBox>().ToArray();
        checks[0].IsChecked = true;
        checks[1].IsChecked = false;
        Assert.Single(editor.Panel.Children.OfType<NumericUpDown>()).Value = 2.2m;

        var result = editor.BuildDraft();

        Assert.True(result.Enabled);
        Assert.False(result.AlwaysOnTop);
        Assert.Equal(2.2, result.Scale, 3);
    }
}

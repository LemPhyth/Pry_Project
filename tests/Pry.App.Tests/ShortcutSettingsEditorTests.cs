using Avalonia.Automation;
using Avalonia.Controls;
using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class ShortcutSettingsEditorTests
{
    [Fact]
    public void Editor_builds_all_fields_from_existing_settings()
    {
        var source = new ShortcutSettings { Send = "Ctrl+S", OpenStickers = "Alt+E" };

        var editor = new ShortcutSettingsEditor(source, new SettingsUiFactory());
        var inputs = editor.Panel.Children.OfType<TextBox>().ToArray();

        Assert.Equal(7, inputs.Length);
        Assert.Equal("Ctrl+S", inputs[0].Text);
        Assert.Equal("发送", AutomationProperties.GetName(inputs[0]));
        Assert.Equal("Alt+E", inputs[5].Text);
        Assert.Equal(source, editor.BuildDraft());
    }

    [Fact]
    public void BuildDraft_trims_values_and_restores_defaults_for_blank_fields()
    {
        var editor = new ShortcutSettingsEditor(new ShortcutSettings(), new SettingsUiFactory());
        var inputs = editor.Panel.Children.OfType<TextBox>().ToArray();
        inputs[0].Text = "   ";
        inputs[1].Text = "  Alt+Enter  ";

        var result = editor.BuildDraft();

        Assert.Equal("Enter", result.Send);
        Assert.Equal("Alt+Enter", result.SendImmediately);
    }
}

using Avalonia.Controls;
using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class ModelSelectionEditorTests
{
    [Fact]
    public void Filters_roles_and_preserves_selection_after_refresh()
    {
        var text = Model("text", false);
        var vision = Model("vision", true);
        var editor = new ModelSelectionEditor([text, vision], [], "text", "vision", null, new SettingsUiFactory());
        var fields = editor.TextFields.Children.OfType<ComboBox>().ToArray();
        Assert.Equal(2, fields[0].ItemCount);
        Assert.Equal(1, fields[1].ItemCount);
        editor.RefreshModels([vision, text]);
        Assert.Equal("text", editor.TextModelId);
        Assert.Equal("vision", editor.VisionModelId);
    }

    [Fact]
    public void Removed_selection_falls_back_to_available_model()
    {
        var editor = new ModelSelectionEditor([Model("old", true)], [], "old", "old", null, new SettingsUiFactory());
        editor.RefreshModels([Model("new", true)]);
        Assert.Equal("new", editor.TextModelId);
        Assert.Equal("new", editor.VisionModelId);
    }

    [Fact]
    public void Empty_lists_have_disabled_controls_and_explicit_hint()
    {
        var editor = new ModelSelectionEditor([], [], null, null, null, new SettingsUiFactory());
        foreach (var field in editor.TextFields.Children.Concat(editor.SpeechFields.Children).OfType<ComboBox>())
        {
            Assert.False(field.IsEnabled);
            Assert.Equal("暂无可用模型", field.PlaceholderText);
        }
        Assert.Null(editor.TextModelId);
        Assert.Null(editor.VisionModelId);
        Assert.Null(editor.SpeechModelId);
    }

    private static ModelProfile Model(string id, bool vision) => new()
    {
        Id = id, DisplayName = id, Capabilities = new ModelCapabilities(Text: true, Vision: vision)
    };
}

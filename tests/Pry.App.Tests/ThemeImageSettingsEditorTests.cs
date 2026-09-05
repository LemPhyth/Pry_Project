using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class ThemeImageSettingsEditorTests
{
    [Fact]
    public void Import_switch_and_clear_keep_crop_state_in_draft()
    {
        var draft = new ThemeImageDraft("first.png", [],
            new Dictionary<string, ImageDisplayPreferences>(), _ => true);
        using var resources = new ThemeImageResources(_ => new TestImage());
        var changes = 0;
        var editor = Create(draft, resources);
        editor.Changed += () => changes++;
        editor.FocusX.Value = .2m;
        editor.Zoom.Value = 2m;
        editor.Import("second.png");
        Assert.Equal(.2, draft.Displays["first.png"].FocusX, 3);
        Assert.Equal(2, draft.Displays["first.png"].Zoom, 3);
        Assert.Equal("second.png", draft.SelectedPath);
        editor.FocusY.Value = .8m;
        editor.StoreCurrent();
        Assert.Equal(.8, draft.Displays["second.png"].FocusY, 3);
        editor.Clear();
        Assert.Null(draft.SelectedPath);
        Assert.Null(editor.Preview.Source);
        Assert.True(changes >= 4);
    }

    [Fact]
    public void Remove_without_selection_is_a_noop()
    {
        var draft = new ThemeImageDraft(null, [], new Dictionary<string, ImageDisplayPreferences>(), _ => true);
        using var resources = new ThemeImageResources(_ => new TestImage());
        var editor = Create(draft, resources);
        var changes = 0;
        editor.Changed += () => changes++;
        Assert.False(editor.RemoveSelected());
        Assert.Equal(0, changes);
        Assert.Equal("尚未导入图片", Assert.IsType<TextBlock>(Assert.Single(editor.Gallery.Children)).Text);
    }

    private static ThemeImageSettingsEditor Create(ThemeImageDraft draft, ThemeImageResources resources) =>
        new(draft, resources, new SettingsUiFactory(), () => Brushes.Red, (_, _) => { },
            false, "背景", "图片", "说明", "导入", "清除");

    private sealed class TestImage : IImage, IDisposable
    {
        public Size Size => new(10, 10);
        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect) { }
        public void Dispose() { }
    }
}

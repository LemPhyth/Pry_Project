using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Pry.App.Services;
using Xunit;

namespace Pry.App.Tests;

public sealed class VisualCropEditorTests
{
    [Theory]
    [InlineData(true, 90)]
    [InlineData(false, 12)]
    public void Creates_clipped_focusable_viewport(bool circular, double radius)
    {
        var image = new Image();
        var ui = new SettingsUiFactory();
        var editor = VisualCropEditor.Create(image, ui.CreateNumber(.5m, 0, 1),
            ui.CreateNumber(.5m, 0, 1), ui.CreateNumber(1, 1, 3), 180, 180, circular);
        var grid = Assert.IsType<Grid>(editor.Child);
        var viewport = Assert.IsType<Border>(grid.Children[0]);
        Assert.Same(image, viewport.Child);
        Assert.True(viewport.ClipToBounds);
        Assert.True(viewport.Focusable);
        Assert.Equal(new CornerRadius(radius), viewport.CornerRadius);
        Assert.Equal("图片取景", AutomationProperties.GetName(viewport));
        Assert.Equal(180, image.Width);
    }

    [Fact]
    public void Keyboard_updates_shared_values_and_respects_limits()
    {
        var ui = new SettingsUiFactory();
        var x = ui.CreateNumber(.5m, 0, 1);
        var y = ui.CreateNumber(.5m, 0, 1);
        var zoom = ui.CreateNumber(1, 1, 3);
        var editor = VisualCropEditor.Create(new Image(), x, y, zoom, 180, 180, true);
        var viewport = Assert.IsType<Border>(Assert.IsType<Grid>(editor.Child).Children[0]);
        bool Press(Key key)
        {
            var args = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key };
            viewport.RaiseEvent(args);
            return args.Handled;
        }
        Assert.True(Press(Key.Right));
        Assert.True(Press(Key.Up));
        Assert.True(Press(Key.Add));
        Assert.Equal(.55m, x.Value);
        Assert.Equal(.45m, y.Value);
        Assert.Equal(1.08m, zoom.Value);
        for (var i = 0; i < 50; i++) { Press(Key.Left); Press(Key.Down); Press(Key.OemPlus); }
        Assert.Equal(0, x.Value);
        Assert.Equal(1, y.Value);
        Assert.Equal(3, zoom.Value);
        for (var i = 0; i < 50; i++) Press(Key.OemMinus);
        Assert.Equal(1, zoom.Value);
        Assert.False(Press(Key.Tab));
    }
}

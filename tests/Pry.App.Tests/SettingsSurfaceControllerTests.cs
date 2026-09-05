using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class SettingsSurfaceControllerTests
{
    [Fact]
    public void Applies_background_display_opacity_dim_and_blur_hint()
    {
        var page = new StackPanel();
        var shell = new SettingsShell([new("主题", page)], "提示", Brushes.Red, () => false);
        var surface = new Grid();
        var image = new Image();
        var dim = new Border();
        var loaded = new TestImage();
        ImageDisplayPreferences? applied = null;
        IReadOnlyList<WindowTransparencyLevel> levels = [];
        var controller = new SettingsSurfaceController(value => levels = value, surface, image, dim, [], [page], shell,
            () => Brushes.Blue, (_, _) => loaded, (_, display) => applied = display, _ => true);
        var display = new ImageDisplayPreferences { FocusX = .3, FocusY = .7, Zoom = 2 };
        controller.Apply(new ThemePreferences
        {
            ThemeMode = "system", BackgroundImagePath = "background.png",
            BackgroundImageOpacity = 2, BackgroundDimOpacity = 2, BackgroundBlurRadius = 50,
            BackgroundDisplays = new Dictionary<string, ImageDisplayPreferences> { ["background.png"] = display }
        }, true);
        Assert.Same(loaded, image.Source);
        Assert.True(image.IsVisible);
        Assert.Equal(1, image.Opacity);
        Assert.True(dim.IsVisible);
        Assert.Equal(.85, dim.Opacity);
        Assert.Same(display, applied);
        Assert.Same(Brushes.Transparent, surface.Background);
        Assert.Equal(WindowTransparencyLevel.Blur, levels[0]);
    }

    [Fact]
    public void Missing_or_failed_background_never_leaves_stale_image_visible()
    {
        var page = new StackPanel();
        var shell = new SettingsShell([new("主题", page)], "提示", Brushes.Red, () => false);
        var surface = new Grid();
        var image = new Image { Source = new TestImage() };
        var dim = new Border();
        IReadOnlyList<WindowTransparencyLevel> levels = [];
        var controller = new SettingsSurfaceController(value => levels = value, surface, image, dim, [], [page], shell,
            () => Brushes.Blue, (_, _) => throw new InvalidDataException(), (_, _) => { }, path => path == "bad.png");
        controller.Apply(new ThemePreferences { ThemeMode = "dark", BackgroundImagePath = "bad.png" }, false);
        Assert.False(image.IsVisible);
        Assert.Null(image.Source);
        controller.Apply(new ThemePreferences { ThemeMode = "dark", BackgroundImagePath = "missing.png" }, false);
        Assert.False(image.IsVisible);
        Assert.False(dim.IsVisible);
        Assert.NotSame(Brushes.Transparent, surface.Background);
        Assert.Equal([WindowTransparencyLevel.Transparent], levels);
    }

    private sealed class TestImage : IImage
    {
        public Size Size => new(10, 10);
        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect) { }
    }
}

using Avalonia.Controls;
using Avalonia.Media;
using Pry.App.Services;
using Xunit;

namespace Pry.App.Tests;

public sealed class SettingsSurfaceThemeTests
{
    [Fact]
    public void Detached_pages_and_nested_labels_receive_each_theme_change()
    {
        var heading = new TextBlock { FontSize = 18 };
        var label = new TextBlock { FontSize = 12 };
        var bold = new TextBlock { FontSize = 12, FontWeight = FontWeight.SemiBold };
        var visible = new Border { Child = heading };
        var hidden = new StackPanel { Children = { new Button { Content = label }, bold } };
        var shell = new SettingsShell([new("一", visible), new("二", hidden)], "提示", Brushes.Red, () => false);

        foreach (var light in new[] { true, false })
        {
            var palette = new SettingsSurfaceTheme(light, false);
            palette.Apply([visible], [shell.Layout, visible, hidden]);
            Assert.Same(palette.Text, heading.Foreground);
            Assert.Same(palette.Text, bold.Foreground);
            Assert.Same(palette.MutedText, label.Foreground);
            Assert.Same(palette.Card, visible.Background);
            Assert.Same(palette.Border, visible.BorderBrush);
        }
        Assert.Same(visible, shell.CurrentPage);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Background_only_changes_surface_transparency(bool light)
    {
        var opaque = new SettingsSurfaceTheme(light, false);
        var transparent = new SettingsSurfaceTheme(light, true);
        Assert.Equal(255, Assert.IsAssignableFrom<ISolidColorBrush>(opaque.Card).Color.A);
        Assert.True(Assert.IsAssignableFrom<ISolidColorBrush>(transparent.Card).Color.A < 255);
        Assert.True(Assert.IsAssignableFrom<ISolidColorBrush>(transparent.Panel).Color.A < 255);
        Assert.Equal(opaque.Text.ToString(), transparent.Text.ToString());
        Assert.Equal(opaque.MutedText.ToString(), transparent.MutedText.ToString());
    }
}

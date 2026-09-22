using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Pry.App.Services;

/// <summary>Shared presentation primitives for the programmatically built workspace pages.</summary>
internal static class PryUi
{
    public static TextBlock Heading(string text) => new()
    {
        Text = text, FontSize = 18, FontWeight = FontWeight.SemiBold,
        Foreground = PryVisualTheme.Get("Text"), TextWrapping = TextWrapping.Wrap
    };

    public static TextBlock Hint(string text) => new()
    {
        Text = text, FontSize = 13, LineHeight = 20,
        Foreground = PryVisualTheme.Get("Muted"), TextWrapping = TextWrapping.Wrap
    };

    public static StackPanel Field(string text, Control input) => new()
    {
        Spacing = 8, Children = { Hint(text), input }
    };

    public static Border Section(Control content) => new()
    {
        Padding = new Thickness(24), CornerRadius = new CornerRadius(12),
        Background = PryVisualTheme.Get("Card"), Child = content
    };

    public static Border Footer(Control content) => new()
    {
        Padding = new Thickness(24, 12), Background = PryVisualTheme.Get("Panel"),
        BorderBrush = PryVisualTheme.Get("Border"), BorderThickness = new Thickness(0, 1, 0, 0), Child = content
    };

    public static WrapPanel Actions(params Control[] controls)
    {
        var panel = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var control in controls)
        {
            control.Margin = new Thickness(0, 0, 8, 8);
            panel.Children.Add(control);
        }
        return panel;
    }
}

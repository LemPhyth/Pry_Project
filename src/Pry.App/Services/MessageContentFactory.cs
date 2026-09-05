using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Pry.Core.Models;

namespace Pry.App.Services;

public static class MessageContentFactory
{
    public static MessageContentView CreateText(string content, bool isUser, ThemePreferences theme,
        IBrush userBrush, IBrush assistantBrush, IBrush userTextBrush, IBrush assistantTextBrush)
    {
        var text = new TextBlock
        {
            Text = content, TextWrapping = TextWrapping.Wrap,
            MaxWidth = Math.Clamp(theme.BubbleMaxWidth, 280, 900),
            FontSize = Math.Clamp(theme.BubbleFontSize, 11, 24),
            Foreground = isUser ? userTextBrush : assistantTextBrush
        };
        var bubble = Bubble(text, isUser ? userBrush : assistantBrush, new Thickness(13, 9));
        return new MessageContentView(bubble, bubble, text);
    }

    public static MessageContentView CreateSticker(string path)
    {
        var content = new Border
        {
            CornerRadius = new CornerRadius(14),
            Child = new Image { Source = new Bitmap(path), MaxWidth = 220, MaxHeight = 220, Stretch = Stretch.Uniform }
        };
        return new MessageContentView(content, null, null);
    }

    public static MessageContentView CreateImage(string path, IBrush bubbleBrush)
    {
        var image = new Image { Source = new Bitmap(path), MaxWidth = 360, MaxHeight = 280, Stretch = Stretch.Uniform };
        var bubble = Bubble(image, bubbleBrush, new Thickness(5));
        return new MessageContentView(bubble, bubble, null);
    }

    public static MessageContentView CreateDocument(ChatAttachment attachment, IBrush bubbleBrush)
    {
        var extension = Path.GetExtension(attachment.Name).TrimStart('.').ToUpperInvariant();
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        panel.Children.Add(new Border
        {
            Width = 42, Height = 42, CornerRadius = new CornerRadius(8), Background = Brush.Parse("#263746"),
            Child = new TextBlock { Text = extension, FontSize = 10, FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
        });
        panel.Children.Add(new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock { Text = attachment.Name, MaxWidth = 300, TextTrimming = TextTrimming.CharacterEllipsis },
                new TextBlock { Text = "文档附件", FontSize = 10, Foreground = Brush.Parse("#8EA2B5") }
            }
        });
        var bubble = Bubble(panel, bubbleBrush, new Thickness(10));
        return new MessageContentView(bubble, bubble, null);
    }

    private static Border Bubble(Control child, IBrush brush, Thickness padding) => new()
    {
        Background = brush, CornerRadius = new CornerRadius(12), Padding = padding, Child = child
    };
}

public sealed record MessageContentView(Control Content, Border? ThemedBubble, TextBlock? Text);

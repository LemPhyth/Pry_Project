using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Pry.Core.Models;

namespace Pry.App.Services;

public static class MessageRowFactory
{
    public static MessageRowView Create(string author, Control content, bool isUser,
        DateTimeOffset timestamp, ContextMenu? contextMenu, ThemePreferences theme,
        CharacterDefinition? character, IBrush accentBrush)
    {
        var message = new StackPanel
        {
            Spacing = 3,
            HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            ContextMenu = contextMenu
        };
        var meta = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 7,
            HorizontalAlignment = message.HorizontalAlignment,
            Children =
            {
                new TextBlock { Text = author, FontSize = 11, Foreground = Brush.Parse("#7F91A4") },
                new TextBlock { Text = timestamp.LocalDateTime.ToString("HH:mm"), FontSize = 10, Foreground = Brush.Parse("#586B7D") }
            }
        };
        message.Children.Add(meta);
        message.Children.Add(content);

        var avatar = CreateAvatar(isUser, theme, character, accentBrush);
        var row = new Grid
        {
            ColumnDefinitions = isUser ? new ColumnDefinitions("*,Auto") : new ColumnDefinitions("Auto,*"),
            ColumnSpacing = 8,
            ContextMenu = contextMenu
        };
        if (isUser) { Grid.SetColumn(message, 0); Grid.SetColumn(avatar, 1); }
        else { Grid.SetColumn(avatar, 0); Grid.SetColumn(message, 1); }
        row.Children.Add(message);
        row.Children.Add(avatar);
        AutomationProperties.SetName(row, $"{author} 在 {timestamp.LocalDateTime:HH:mm} 的消息");
        return new MessageRowView(row, avatar);
    }

    private static Border CreateAvatar(bool isUser, ThemePreferences theme,
        CharacterDefinition? character, IBrush accentBrush)
    {
        var path = isUser ? theme.UserAvatarPath : character?.AvatarPath ?? theme.CharacterAvatarPath;
        Control child = CreateFallback(isUser, character);
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            try
            {
                var image = new Image { Source = new Bitmap(path) };
                var display = isUser && theme.UserAvatarDisplays.TryGetValue(path, out var userDisplay)
                    ? userDisplay
                    : character?.AvatarDisplay ?? new ImageDisplayPreferences();
                ApplyImageDisplay(image, display);
                child = image;
            }
            catch { }
        }
        var size = Math.Clamp(theme.AvatarSize, 28, 76);
        var avatar = new Border
        {
            Width = size, Height = size, CornerRadius = new CornerRadius(size / 2),
            Background = isUser ? Brush.Parse("#536C82") : accentBrush,
            ClipToBounds = true, Child = child, VerticalAlignment = VerticalAlignment.Bottom
        };
        AutomationProperties.SetName(avatar, isUser ? "用户头像" : $"{character?.Name ?? "角色"}头像");
        return avatar;
    }

    private static TextBlock CreateFallback(bool isUser, CharacterDefinition? character) => new()
    {
        Text = isUser ? "你" : (character?.Name.FirstOrDefault().ToString() ?? "P"),
        FontWeight = FontWeight.Bold,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static void ApplyImageDisplay(Image image, ImageDisplayPreferences display)
    {
        image.Stretch = Stretch.UniformToFill;
        RenderOptions.SetBitmapInterpolationMode(image, BitmapInterpolationMode.HighQuality);
        image.RenderTransformOrigin = new RelativePoint(Math.Clamp(display.FocusX, 0, 1),
            Math.Clamp(display.FocusY, 0, 1), RelativeUnit.Relative);
        var zoom = Math.Clamp(display.Zoom, 1, 3);
        image.RenderTransform = new ScaleTransform(zoom, zoom);
    }
}

public sealed record MessageRowView(Grid Row, Border Avatar);

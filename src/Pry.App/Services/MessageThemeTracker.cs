using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class MessageThemeTracker
{
    private readonly List<(Border Control, bool IsUser)> _avatars = [];
    private readonly List<(Border Control, bool IsUser)> _bubbles = [];
    private readonly List<(TextBlock Control, bool IsUser)> _texts = [];

    public void Track(MessageContentView view, bool isUser)
    {
        if (view.ThemedBubble is not null) _bubbles.Add((view.ThemedBubble, isUser));
        if (view.Text is not null) _texts.Add((view.Text, isUser));
    }

    public void TrackAvatar(Border avatar, bool isUser) => _avatars.Add((avatar, isUser));

    public void Apply(ThemePreferences theme, IBrush userAvatarBrush, IBrush accentBrush,
        IBrush assistantBubbleBrush, IBrush accentTextBrush, IBrush assistantTextBrush)
    {
        var avatarSize = Math.Clamp(theme.AvatarSize, 28, 76);
        foreach (var item in _avatars)
        {
            item.Control.Width = avatarSize;
            item.Control.Height = avatarSize;
            item.Control.CornerRadius = new CornerRadius(avatarSize / 2);
            item.Control.Background = item.IsUser ? userAvatarBrush : accentBrush;
        }

        foreach (var item in _bubbles)
            item.Control.Background = item.IsUser ? accentBrush : assistantBubbleBrush;

        var fontSize = Math.Clamp(theme.BubbleFontSize, 11, 24);
        var maxWidth = Math.Clamp(theme.BubbleMaxWidth, 280, 900);
        foreach (var item in _texts)
        {
            item.Control.FontSize = fontSize;
            item.Control.MaxWidth = maxWidth;
            item.Control.Foreground = item.IsUser ? accentTextBrush : assistantTextBrush;
        }
    }

    public void Clear()
    {
        _avatars.Clear();
        _bubbles.Clear();
        _texts.Clear();
    }
}

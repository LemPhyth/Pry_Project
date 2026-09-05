using Avalonia.Controls;
using Avalonia.Media;
using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class MessageThemeTrackerTests
{
    [Fact]
    public void Apply_updates_tracked_controls_and_clamps_theme_values()
    {
        var userAvatar = new Border();
        var assistantAvatar = new Border();
        var userBubble = new Border();
        var assistantBubble = new Border();
        var userText = new TextBlock();
        var assistantText = new TextBlock();
        var tracker = new MessageThemeTracker();
        tracker.TrackAvatar(userAvatar, true);
        tracker.TrackAvatar(assistantAvatar, false);
        tracker.Track(new MessageContentView(userBubble, userBubble, userText), true);
        tracker.Track(new MessageContentView(assistantBubble, assistantBubble, assistantText), false);

        tracker.Apply(new ThemePreferences { AvatarSize = 100, BubbleFontSize = 5, BubbleMaxWidth = 1000 },
            Brushes.Gray, Brushes.Red, Brushes.Blue, Brushes.White, Brushes.Black);

        Assert.Equal(76, userAvatar.Width);
        Assert.Equal(38, userAvatar.CornerRadius.TopLeft);
        Assert.Same(Brushes.Gray, userAvatar.Background);
        Assert.Same(Brushes.Red, assistantAvatar.Background);
        Assert.Same(Brushes.Red, userBubble.Background);
        Assert.Same(Brushes.Blue, assistantBubble.Background);
        Assert.Equal(11, userText.FontSize);
        Assert.Equal(900, userText.MaxWidth);
        Assert.Same(Brushes.White, userText.Foreground);
        Assert.Same(Brushes.Black, assistantText.Foreground);
    }

    [Fact]
    public void Clear_detaches_previous_conversation_controls()
    {
        var avatar = new Border { Width = 40 };
        var tracker = new MessageThemeTracker();
        tracker.TrackAvatar(avatar, true);
        tracker.Clear();

        tracker.Apply(new ThemePreferences { AvatarSize = 60 }, Brushes.Gray, Brushes.Red,
            Brushes.Blue, Brushes.White, Brushes.Black);

        Assert.Equal(40, avatar.Width);
    }
}

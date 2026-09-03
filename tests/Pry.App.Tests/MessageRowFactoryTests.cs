using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class MessageRowFactoryTests
{
    [Fact]
    public void Assistant_row_places_avatar_first_and_uses_character_fallback()
    {
        var character = CreateCharacter("星野");
        var timestamp = new DateTimeOffset(2026, 9, 3, 14, 5, 0, TimeSpan.Zero);

        var view = MessageRowFactory.Create("星野", new TextBlock { Text = "你好" }, false,
            timestamp, null, new ThemePreferences { AvatarSize = 12 }, character, Brushes.Red);

        Assert.Equal(28, view.Avatar.Width);
        Assert.Equal(0, Grid.GetColumn(view.Avatar));
        Assert.Equal("星", Assert.IsType<TextBlock>(view.Avatar.Child).Text);
        Assert.Equal("星野头像", AutomationProperties.GetName(view.Avatar));
        Assert.Equal($"星野 在 {timestamp.LocalDateTime:HH:mm} 的消息", AutomationProperties.GetName(view.Row));
    }

    [Fact]
    public void User_row_places_avatar_last_and_shares_context_menu()
    {
        var menu = new ContextMenu();

        var view = MessageRowFactory.Create("你", new TextBlock { Text = "收到" }, true,
            DateTimeOffset.Now, menu, new ThemePreferences { AvatarSize = 100 }, null, Brushes.Red);

        Assert.Equal(76, view.Avatar.Width);
        Assert.Equal(1, Grid.GetColumn(view.Avatar));
        Assert.Equal("你", Assert.IsType<TextBlock>(view.Avatar.Child).Text);
        Assert.Same(menu, view.Row.ContextMenu);
        Assert.Equal("用户头像", AutomationProperties.GetName(view.Avatar));
    }

    private static CharacterDefinition CreateCharacter(string name) => new()
    {
        Id = "test",
        Name = name,
        Identity = "test",
        Personality = "test",
        SpeechStyle = "test"
    };
}

using Avalonia.Controls;
using Avalonia.Media;
using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class ConversationViewControllerTests
{
    [Fact]
    public void Render_replaces_rows_preserves_anchor_and_routes_stored_message_menus()
    {
        var panel = new StackPanel();
        var anchor = new Border();
        var menus = new List<(long Id, bool IsUser, string Text)>();
        var scrolls = 0;
        var controller = Create(panel, anchor,
            (id, user, text) => { menus.Add((id, user, text)); return new ContextMenu(); },
            () => scrolls++);
        controller.AddText("stale", "old", false);
        controller.Render([
            new ChatMessage(10, "room", ChatRole.User, "hello", DateTimeOffset.Now),
            new ChatMessage(11, "room", ChatRole.Assistant, "world", DateTimeOffset.Now)
        ]);
        Assert.Equal(3, panel.Children.Count);
        Assert.Same(anchor, panel.Children[^1]);
        Assert.Equal([(10, true, "hello"), (11, false, "world")], menus);
        Assert.DoesNotContain(Descendants(panel), text => text.Text == "old");
        Assert.Equal(4, scrolls);
    }

    [Fact]
    public void Missing_sticker_keeps_message_identity_on_text_fallback()
    {
        var panel = new StackPanel();
        var anchor = new Border();
        long? menuId = null;
        var controller = Create(panel, anchor,
            (id, _, _) => { menuId = id; return new ContextMenu(); }, () => { });
        controller.AddSticker("角色", "missing", false, DateTimeOffset.Now, 42);
        Assert.Equal(42, menuId);
        Assert.Contains(Descendants(panel), text => text.Text == "[表情]");
    }

    [Fact]
    public void Reset_clears_tracked_controls_before_new_rows()
    {
        var panel = new StackPanel();
        var anchor = new Border();
        var tracker = new MessageThemeTracker();
        var controller = Create(panel, anchor, (_, _, _) => null, () => { }, tracker);
        controller.AddText("你", "one", true);
        controller.Reset();
        tracker.Apply(new ThemePreferences { BubbleFontSize = 22 }, Brushes.Gray, Brushes.Red,
            Brushes.Blue, Brushes.White, Brushes.Black);
        Assert.Single(panel.Children);
        Assert.Same(anchor, panel.Children[0]);
    }

    private static ConversationViewController Create(Panel panel, Control anchor,
        Func<long, bool, string, ContextMenu?> menu, Action scroll, MessageThemeTracker? tracker = null) =>
        new(panel, anchor, tracker ?? new MessageThemeTracker(), () => new ThemePreferences(),
            () => null, () => [], menu, () => Brushes.Red, () => Brushes.Blue,
            () => Brushes.White, () => Brushes.Black, scroll);

    private static IEnumerable<TextBlock> Descendants(Control root)
    {
        if (root is TextBlock text) yield return text;
        if (root is Panel panel)
            foreach (var child in panel.Children)
                foreach (var nested in Descendants(child)) yield return nested;
        else if (root is Decorator { Child: Control decorated })
            foreach (var nested in Descendants(decorated)) yield return nested;
        else if (root is ContentControl { Content: Control content })
            foreach (var nested in Descendants(content)) yield return nested;
    }
}

using Avalonia.Controls;
using Avalonia.Media;
using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class MessageContentFactoryTests
{
    [Fact]
    public void Text_view_clamps_theme_sizing_and_exposes_tracked_controls()
    {
        var theme = new ThemePreferences { BubbleFontSize = 50, BubbleMaxWidth = 100 };

        var view = MessageContentFactory.CreateText("hello", false, theme,
            Brushes.Red, Brushes.Blue, Brushes.White, Brushes.Black);

        Assert.Same(view.Content, view.ThemedBubble);
        Assert.Equal("hello", view.Text?.Text);
        Assert.Equal(24, view.Text?.FontSize);
        Assert.Equal(280, view.Text?.MaxWidth);
        Assert.Same(Brushes.Blue, view.ThemedBubble?.Background);
    }

    [Fact]
    public void Document_view_displays_extension_and_original_name()
    {
        var attachment = new ChatAttachment("unused", ChatAttachmentKind.Docx, "计划书.docx");

        var view = MessageContentFactory.CreateDocument(attachment, Brushes.Blue);

        var outer = Assert.IsType<Border>(view.Content);
        var row = Assert.IsType<StackPanel>(outer.Child);
        var icon = Assert.IsType<Border>(row.Children[0]);
        Assert.Equal("DOCX", Assert.IsType<TextBlock>(icon.Child).Text);
        var labels = Assert.IsType<StackPanel>(row.Children[1]);
        Assert.Equal("计划书.docx", Assert.IsType<TextBlock>(labels.Children[0]).Text);
    }
}

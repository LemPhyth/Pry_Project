using Avalonia.Automation;
using Avalonia.Controls;
using Pry.App.Services;
using Xunit;

namespace Pry.App.Tests;

public sealed class SettingsUiFactoryTests
{
    [Fact]
    public void Field_labels_control_for_accessibility()
    {
        var factory = new SettingsUiFactory();
        var panel = new StackPanel();
        var input = factory.CreateTextBox("Enter");

        factory.AddField(panel, "发送快捷键", input);

        Assert.Equal(2, panel.Children.Count);
        Assert.Equal("发送快捷键", Assert.IsType<TextBlock>(panel.Children[0]).Text);
        Assert.Equal("发送快捷键", AutomationProperties.GetName(input));
        Assert.False(input.AcceptsReturn);
    }

    [Fact]
    public void Multiline_text_and_number_preserve_requested_constraints()
    {
        var factory = new SettingsUiFactory();

        var text = factory.CreateTextBox("风格", 110);
        var number = factory.CreateNumber(48, 28, 76, .5m);

        Assert.True(text.AcceptsReturn);
        Assert.Equal(110, text.MinHeight);
        Assert.Equal(28, number.Minimum);
        Assert.Equal(76, number.Maximum);
        Assert.Equal(.5m, number.Increment);
    }

    [Fact]
    public void Cards_are_tracked_for_live_theme_refresh()
    {
        var factory = new SettingsUiFactory();

        var card = factory.CreateCard("颜色主题", new CheckBox());
        var manager = factory.CreateManagerPanel("角色卡", "管理角色", new Button());

        Assert.Equal(2, factory.Cards.Count);
        Assert.Same(card, factory.Cards[0]);
        Assert.Equal("颜色主题设置", AutomationProperties.GetName(card));
        Assert.Equal(2, manager.Children.Count);
    }
}

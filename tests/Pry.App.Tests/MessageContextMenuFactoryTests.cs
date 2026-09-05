using Avalonia.Automation;
using Avalonia.Controls;
using Pry.App.Services;
using Xunit;

namespace Pry.App.Tests;

public sealed class MessageContextMenuFactoryTests
{
    [Theory]
    [InlineData(true, "从此条消息开始编辑")]
    [InlineData(false, "从此条开始重新回复")]
    public void Menu_exposes_role_specific_primary_action_and_delete(bool isUser, string expected)
    {
        var menu = MessageContextMenuFactory.Create(isUser, () => Task.CompletedTask,
            () => Task.CompletedTask);

        var items = Assert.IsType<MenuItem[]>(menu.ItemsSource);
        Assert.Equal(expected, items[0].Header);
        Assert.Equal(expected, AutomationProperties.GetName(items[0]));
        Assert.Equal("删除", items[1].Header);
        Assert.Equal("删除消息", AutomationProperties.GetName(items[1]));
    }
}

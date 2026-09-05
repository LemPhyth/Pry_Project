using Avalonia.Automation;
using Avalonia.Controls;
using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class ConversationContextMenuFactoryTests
{
    [Theory]
    [InlineData(false, "顶置")]
    [InlineData(true, "取消顶置")]
    public void Room_menu_reflects_pin_state_and_builds_folder_targets(bool isPinned, string pinLabel)
    {
        var room = CreateRoom(isPinned);
        var folders = new[] { new ConversationFolder("work", "工作", DateTimeOffset.Now) };

        var menu = ConversationContextMenuFactory.CreateRoom(room, folders,
            () => Task.CompletedTask, () => Task.CompletedTask, _ => Task.CompletedTask,
            () => Task.CompletedTask, () => Task.CompletedTask);

        var items = Assert.IsType<MenuItem[]>(menu.ItemsSource);
        Assert.Equal(pinLabel, items[0].Header);
        Assert.Equal("重命名", items[1].Header);
        Assert.Equal("移动对话到文件夹", AutomationProperties.GetName(items[2]));
        var moveItems = Assert.IsAssignableFrom<IEnumerable<MenuItem>>(items[2].ItemsSource).ToArray();
        Assert.Equal(new[] { "未分类", "工作", "新建文件夹…" }, moveItems.Select(x => x.Header));
        Assert.Equal("删除对话", AutomationProperties.GetName(items[3]));
    }

    [Fact]
    public void Folder_and_unfiled_menus_expose_expected_actions()
    {
        var folder = ConversationContextMenuFactory.CreateFolder(
            () => Task.CompletedTask, () => Task.CompletedTask);
        var unfiled = ConversationContextMenuFactory.CreateUnfiled(() => Task.CompletedTask);

        Assert.Equal(new[] { "重命名文件夹", "删除文件夹" },
            Assert.IsType<MenuItem[]>(folder.ItemsSource).Select(x => x.Header));
        Assert.Equal("新建文件夹…", Assert.Single(Assert.IsType<MenuItem[]>(unfiled.ItemsSource)).Header);
    }

    private static ConversationRoom CreateRoom(bool isPinned) => new("room", "测试", null,
        DateTimeOffset.Now, DateTimeOffset.Now, 2, IsPinned: isPinned);
}

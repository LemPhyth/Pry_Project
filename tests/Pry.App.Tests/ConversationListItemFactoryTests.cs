using Avalonia.Automation;
using Avalonia.Controls;
using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class ConversationListItemFactoryTests
{
    [Fact]
    public void Create_groups_rooms_and_hides_items_in_collapsed_folder()
    {
        var now = new DateTimeOffset(2026, 9, 3, 8, 30, 0, TimeSpan.Zero);
        var folder = new ConversationFolder("favorites", "收藏", now);
        var unfiled = new ConversationRoom("a", "日常", null, now, now, 3, IsPinned: true);
        var filed = new ConversationRoom("b", "计划", null, now, now, 5, folder.Id);

        var items = ConversationListItemFactory.Create([unfiled, filed], [folder],
            new HashSet<string> { folder.Id }, _ => Task.CompletedTask,
            _ => new ContextMenu(), () => new ContextMenu(), _ => new ContextMenu());

        Assert.Equal(3, items.Count);
        var unfiledHeader = Assert.IsType<ConversationFolderHeader>(items[0].Tag);
        Assert.Equal(ConversationListItemFactory.UnfiledFolderId, unfiledHeader.Id);
        Assert.Same(unfiled, items[1].Tag);
        Assert.Equal("日常，3 条消息，已顶置", AutomationProperties.GetName(items[1]));
        var folderHeader = Assert.IsType<ConversationFolderHeader>(items[2].Tag);
        Assert.True(folderHeader.IsCollapsed);
        Assert.Equal("收藏文件夹，已折叠", AutomationProperties.GetName(items[2]));
    }

    [Fact]
    public void Create_omits_empty_unfiled_group_but_keeps_empty_named_folders()
    {
        var folder = new ConversationFolder("empty", "空文件夹", DateTimeOffset.Now);

        var items = ConversationListItemFactory.Create([], [folder], new HashSet<string>(),
            _ => Task.CompletedTask, _ => new ContextMenu(), () => new ContextMenu(),
            _ => new ContextMenu());

        var item = Assert.Single(items);
        var header = Assert.IsType<ConversationFolderHeader>(item.Tag);
        Assert.Equal(folder.Id, header.Id);
        Assert.False(header.IsCollapsed);
        Assert.NotNull(item.ContextMenu);
    }
}

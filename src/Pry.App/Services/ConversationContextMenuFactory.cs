using Avalonia.Automation;
using Avalonia.Controls;
using Pry.Core.Models;

namespace Pry.App.Services;

public static class ConversationContextMenuFactory
{
    public static ContextMenu CreateRoom(ConversationRoom room, IReadOnlyList<ConversationFolder> folders,
        Func<Task> togglePin, Func<Task> rename, Func<string?, Task> move,
        Func<Task> createFolderAndMove, Func<Task> delete)
    {
        var pin = CreateItem(room.IsPinned ? "取消顶置" : "顶置", togglePin);
        var renameItem = CreateItem("重命名", rename);
        var moveItem = new MenuItem { Header = "移动到文件夹" };
        var moveItems = new List<MenuItem> { CreateItem("未分类", () => move(null)) };
        moveItems.AddRange(folders.Select(folder => CreateItem(folder.Name, () => move(folder.Id))));
        moveItems.Add(CreateItem("新建文件夹…", createFolderAndMove));
        moveItem.ItemsSource = moveItems;
        AutomationProperties.SetName(moveItem, "移动对话到文件夹");
        var remove = CreateItem("删除", delete, "删除对话");
        return new ContextMenu { ItemsSource = new[] { pin, renameItem, moveItem, remove } };
    }

    public static ContextMenu CreateFolder(Func<Task> rename, Func<Task> delete) =>
        new() { ItemsSource = new[] { CreateItem("重命名文件夹", rename), CreateItem("删除文件夹", delete) } };

    public static ContextMenu CreateUnfiled(Func<Task> createFolder) =>
        new() { ItemsSource = new[] { CreateItem("新建文件夹…", createFolder) } };

    private static MenuItem CreateItem(string header, Func<Task> action, string? accessibleName = null)
    {
        var item = new MenuItem { Header = header };
        item.Click += async (_, _) => await action();
        AutomationProperties.SetName(item, accessibleName ?? header.TrimEnd('…'));
        return item;
    }
}

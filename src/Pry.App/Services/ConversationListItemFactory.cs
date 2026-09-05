using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using Pry.Core.Models;

namespace Pry.App.Services;

public static class ConversationListItemFactory
{
    public const string UnfiledFolderId = "__unfiled";

    public static IReadOnlyList<ListBoxItem> Create(IReadOnlyList<ConversationRoom> rooms,
        IReadOnlyList<ConversationFolder> folders, IReadOnlySet<string> collapsedFolderIds,
        Func<string, Task> toggleFolder, Func<ConversationFolder, ContextMenu> createFolderMenu,
        Func<ContextMenu> createUnfiledMenu, Func<ConversationRoom, ContextMenu> createRoomMenu)
    {
        var items = new List<ListBoxItem>();
        AddGroup(null, "未分类");
        foreach (var folder in folders) AddGroup(folder, folder.Name);
        return items;

        void AddGroup(ConversationFolder? folder, string name)
        {
            var grouped = rooms.Where(room => room.FolderId == folder?.Id).ToArray();
            if (folder is null && grouped.Length == 0) return;

            var key = folder?.Id ?? UnfiledFolderId;
            var collapsed = collapsedFolderIds.Contains(key);
            var header = new ListBoxItem
            {
                Tag = new ConversationFolderHeader(key, name, collapsed),
                Content = $"{(collapsed ? "▸" : "▾")}  📁 {name}",
                FontWeight = FontWeight.SemiBold,
                Padding = new Thickness(10, 8),
                ContextMenu = folder is null ? createUnfiledMenu() : createFolderMenu(folder)
            };
            AutomationProperties.SetName(header, $"{name}文件夹，{(collapsed ? "已折叠" : "已展开")}");
            header.PointerPressed += async (_, args) =>
            {
                if (!args.GetCurrentPoint(header).Properties.IsLeftButtonPressed) return;
                args.Handled = true;
                await toggleFolder(key);
            };
            items.Add(header);
            if (collapsed) return;

            foreach (var room in grouped)
            {
                var title = new TextBlock
                {
                    Text = $"{(room.IsPinned ? "📌 " : "")}{room.Title}",
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    FontWeight = FontWeight.SemiBold
                };
                var detail = new TextBlock
                {
                    Text = $"{room.UpdatedAt.LocalDateTime:MM-dd HH:mm} · {room.MessageCount} 条消息",
                    Foreground = Brush.Parse("#8EA2B5"), FontSize = 11
                };
                var item = new ListBoxItem
                {
                    Tag = room, Padding = new Thickness(14, 9),
                    Content = new StackPanel { Spacing = 2, Children = { title, detail } },
                    ContextMenu = createRoomMenu(room)
                };
                AutomationProperties.SetName(item,
                    $"{room.Title}，{room.MessageCount} 条消息{(room.IsPinned ? "，已顶置" : "")}");
                items.Add(item);
            }
        }
    }
}

public sealed record ConversationFolderHeader(string Id, string Name, bool IsCollapsed);

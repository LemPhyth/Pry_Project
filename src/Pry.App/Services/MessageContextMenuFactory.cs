using Avalonia.Automation;
using Avalonia.Controls;

namespace Pry.App.Services;

public static class MessageContextMenuFactory
{
    public static ContextMenu Create(bool isUser, Func<Task> editOrRegenerate, Func<Task> delete)
    {
        var primary = new MenuItem
        {
            Header = isUser ? "从此条消息开始编辑" : "从此条开始重新回复"
        };
        primary.Click += async (_, _) => await editOrRegenerate();

        var remove = new MenuItem { Header = "删除" };
        remove.Click += async (_, _) => await delete();

        AutomationProperties.SetName(primary, primary.Header?.ToString());
        AutomationProperties.SetName(remove, "删除消息");
        return new ContextMenu { ItemsSource = new[] { primary, remove } };
    }
}

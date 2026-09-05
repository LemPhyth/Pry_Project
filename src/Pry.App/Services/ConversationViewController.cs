using Avalonia.Controls;
using Avalonia.Media;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class ConversationViewController(
    Panel messages,
    Control bottomAnchor,
    MessageThemeTracker themeTracker,
    Func<ThemePreferences> theme,
    Func<CharacterDefinition?> character,
    Func<IReadOnlyList<StickerDefinition>> stickers,
    Func<long, bool, string, ContextMenu?> createMenu,
    Func<IBrush> userBrush,
    Func<IBrush> assistantBrush,
    Func<IBrush> userTextBrush,
    Func<IBrush> assistantTextBrush,
    Action scrollToEnd)
{
    public void Reset()
    {
        messages.Children.Clear();
        themeTracker.Clear();
        messages.Children.Add(bottomAnchor);
    }

    public void Render(IEnumerable<ChatMessage> history)
    {
        Reset();
        foreach (var message in history)
        {
            var isUser = message.Role == ChatRole.User;
            var author = isUser ? "你" : character()?.Name ?? "角色";
            if (!string.IsNullOrWhiteSpace(message.ImagePath) && File.Exists(message.ImagePath))
                AddImage(author, message.ImagePath, isUser, message.CreatedAt, message.Id);
            if (!string.IsNullOrWhiteSpace(message.StickerId))
                AddSticker(author, message.StickerId, isUser, message.CreatedAt, message.Id);
            if (!string.IsNullOrWhiteSpace(message.Content))
                AddText(author, message.Content, isUser, message.CreatedAt, message.Id);
        }
        scrollToEnd();
    }

    public void AddText(string author, string content, bool isUser,
        DateTimeOffset? timestamp = null, long? messageId = null)
    {
        var value = MessageContentFactory.CreateText(content, isUser, theme(), userBrush(),
            assistantBrush(), userTextBrush(), assistantTextBrush());
        themeTracker.Track(value, isUser);
        AddRow(author, value.Content, isUser, timestamp, messageId, content);
    }

    public void AddSticker(string author, string stickerId, bool isUser,
        DateTimeOffset? timestamp = null, long? messageId = null)
    {
        var sticker = stickers().FirstOrDefault(value => value.Id == stickerId && value.Enabled);
        if (sticker is null) { AddText(author, "[表情]", isUser, timestamp, messageId); return; }
        try { AddRow(author, MessageContentFactory.CreateSticker(sticker.FilePath).Content, isUser, timestamp, messageId, ""); }
        catch { AddText(author, $"[表情：{sticker.Name}]", isUser, timestamp, messageId); }
    }

    public void AddImage(string author, string imagePath, bool isUser,
        DateTimeOffset? timestamp = null, long? messageId = null)
    {
        try
        {
            var value = MessageContentFactory.CreateImage(imagePath, isUser ? userBrush() : assistantBrush());
            themeTracker.Track(value, isUser);
            AddRow(author, value.Content, isUser, timestamp, messageId, "");
        }
        catch { AddText(author, $"[图片无法显示：{Path.GetFileName(imagePath)}]", isUser, timestamp, messageId); }
    }

    public void AddAttachment(string author, ChatAttachment attachment, bool isUser, long? messageId = null)
    {
        if (attachment.IsImage) { AddImage(author, attachment.Path, isUser, messageId: messageId); return; }
        var value = MessageContentFactory.CreateDocument(attachment, isUser ? userBrush() : assistantBrush());
        themeTracker.Track(value, isUser);
        AddRow(author, value.Content, isUser, messageId: messageId, messageText: "");
    }

    private void AddRow(string author, Control content, bool isUser, DateTimeOffset? timestamp = null,
        long? messageId = null, string messageText = "")
    {
        var menu = messageId is long id ? createMenu(id, isUser, messageText) : null;
        var value = MessageRowFactory.Create(author, content, isUser, timestamp ?? DateTimeOffset.Now,
            menu, theme(), character(), userBrush());
        themeTracker.TrackAvatar(value.Avatar, isUser);
        messages.Children.Insert(Math.Max(0, messages.Children.Count - 1), value.Row);
        scrollToEnd();
    }
}

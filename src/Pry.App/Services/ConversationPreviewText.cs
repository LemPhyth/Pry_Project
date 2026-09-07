using Pry.Core.Models;

namespace Pry.App.Services;

public static class ConversationPreviewText
{
    public static string Format(ConversationRoom room)
    {
        ArgumentNullException.ThrowIfNull(room);
        if (!string.IsNullOrWhiteSpace(room.LastMessagePreview)) return room.LastMessagePreview;
        return room.LastMessageKind?.ToLowerInvariant() switch
        {
            "image" => "图片",
            "sticker" => "表情",
            _ => "还没有消息"
        };
    }
}

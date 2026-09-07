using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class ConversationPreviewTextTests
{
    [Theory]
    [InlineData("正文", null, "正文")]
    [InlineData(null, "image", "图片")]
    [InlineData(null, "sticker", "表情")]
    [InlineData(null, null, "还没有消息")]
    public void Formats_backend_projection_without_fetching_messages(string? preview, string? kind, string expected)
    {
        var now = DateTimeOffset.UtcNow;
        var room = new ConversationRoom("room", "会话", null, now, now, 0)
        {
            LastMessagePreview = preview,
            LastMessageKind = kind
        };

        Assert.Equal(expected, ConversationPreviewText.Format(room));
    }
}

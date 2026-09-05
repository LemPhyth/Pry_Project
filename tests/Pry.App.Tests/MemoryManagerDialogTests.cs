using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class MemoryManagerDialogTests
{
    [Theory]
    [InlineData("user_fact", "事实")]
    [InlineData("preference", "偏好")]
    [InlineData("promise", "约定")]
    [InlineData("relationship", "关系")]
    [InlineData("event", "事件")]
    [InlineData("unknown", "其他")]
    public void Kind_labels_are_stable_display_text(string kind, string expected) =>
        Assert.Equal(expected, MemoryManagerDialog.KindLabel(kind));

    [Fact]
    public void Metadata_uses_all_available_timestamps()
    {
        var memory = new MemoryRecord(1, "character", "event", "内容", "", .5, null,
            new DateTimeOffset(2026, 9, 6, 1, 2, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 6, 2, 3, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 6, 3, 4, 0, TimeSpan.Zero));

        var text = MemoryManagerDialog.FormatMetadata(memory);

        Assert.Contains("创建：", text);
        Assert.Contains("更新：", text);
        Assert.Contains("最近调用：", text);
    }
}

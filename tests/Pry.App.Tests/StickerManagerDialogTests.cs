using Pry.App.Services;
using Pry.Core.Expression;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class StickerManagerDialogTests
{
    [Fact]
    public void Only_user_stickers_can_be_modified()
    {
        Assert.True(StickerManagerDialog.CanModify(Sticker(StickerSource.User)));
        Assert.False(StickerManagerDialog.CanModify(Sticker(StickerSource.BuiltIn)));
        Assert.False(StickerManagerDialog.CanModify(null));
    }

    [Fact]
    public void Tags_accept_chinese_and_ascii_commas_and_remove_empty_values()
    {
        Assert.Equal(["开心", "安慰", "倾听"], StickerManagerDialog.ParseTags(" 开心，安慰, ,倾听 "));
    }

    private static StickerDefinition Sticker(StickerSource source) => new()
    {
        Id = "sticker", Name = "表情", FilePath = "sticker.png", Source = source
    };
}

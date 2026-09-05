using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class CharacterCardListControllerTests
{
    [Fact]
    public void Entry_marks_only_current_character()
    {
        var current = Character("current", "当前角色");
        var other = Character("other", "其他角色");

        Assert.EndsWith("· 当前", CharacterCardListEntry.Create(current, "current").Label);
        Assert.DoesNotContain("· 当前", CharacterCardListEntry.Create(other, "current").Label);
    }

    [Fact]
    public void Entry_uses_card_label_fallback()
    {
        var entry = CharacterCardListEntry.Create(Character("id", "角色") with { CardName = "" }, null);

        Assert.Equal("角色-正式-v1", entry.Label);
    }

    private static CharacterDefinition Character(string id, string name) => new()
    {
        Id = id, Name = name, CardName = name + "卡", Identity = "身份",
        Personality = "人格", SpeechStyle = "语气"
    };
}

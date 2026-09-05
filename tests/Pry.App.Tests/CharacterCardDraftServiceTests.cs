using Pry.App.Services;
using Pry.Core.Models;
using Pry.Core.Prompting;
using Xunit;

namespace Pry.App.Tests;

public sealed class CharacterCardDraftServiceTests
{
    [Fact]
    public void Structured_card_requires_identity()
    {
        var result = CharacterCardDraftService.Build(Character(), Draft(CharacterPromptMode.Structured) with
        {
            Identity = " "
        });

        Assert.Null(result);
    }

    [Fact]
    public void Legacy_card_requires_prompt_and_normalizes_lines()
    {
        Assert.Null(CharacterCardDraftService.Build(Character(), Draft(CharacterPromptMode.Legacy) with
        {
            LegacyPrompt = ""
        }));

        var result = CharacterCardDraftService.Build(Character(), Draft(CharacterPromptMode.Legacy) with
        {
            LegacyPrompt = "  system  ", BehavioralRules = "第一条\r\n\n 第二条 "
        });
        Assert.NotNull(result);
        Assert.Equal("system", result.LegacySystemPrompt);
        Assert.Equal(["第一条", "第二条"], result.BehavioralRules);
    }

    [Fact]
    public void Label_falls_back_without_changing_character_data()
    {
        var character = Character() with { CardName = "" };
        Assert.Equal("角色-正式-v1", CharacterCardDraftService.Label(character));
        Assert.Equal("", character.CardName);
    }

    private static CharacterDefinition Character() => new()
    {
        Id = "character", Name = "角色", Identity = "身份", Personality = "人格", SpeechStyle = "语气"
    };

    private static CharacterCardDraft Draft(CharacterPromptMode mode) => new(
        "character", "角色-正式-v1", "角色", null, new ImageDisplayPreferences(), mode,
        "system", "你", "身份", "人格", "语气", "规则", "设定", "你好。");
}

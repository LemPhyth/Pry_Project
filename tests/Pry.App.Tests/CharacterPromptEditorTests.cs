using Pry.App.Services;
using Pry.Core.Models;
using Pry.Core.Prompting;
using Xunit;

namespace Pry.App.Tests;

public sealed class CharacterPromptEditorTests
{
    [Fact]
    public void Load_and_reset_keep_prompt_modes_and_values_consistent()
    {
        var editor = new CharacterPromptEditor();
        editor.Load(new CharacterDefinition
        {
            Id = "legacy", Name = "角色", Identity = "身份", Personality = "人格", SpeechStyle = "语气",
            UserName = "称呼", LegacySystemPrompt = "system", PromptMode = CharacterPromptMode.Legacy,
            BehavioralRules = ["规则一", "规则二"], WorldFacts = ["设定"]
        });

        Assert.Equal(CharacterPromptMode.Legacy, editor.Mode);
        Assert.Equal("system", editor.LegacyPrompt);
        Assert.Contains("规则一", editor.BehavioralRules);

        editor.Reset();
        Assert.Equal(CharacterPromptMode.Structured, editor.Mode);
        Assert.Equal("你", editor.UserName);
        Assert.Equal("", editor.LegacyPrompt);
    }
}

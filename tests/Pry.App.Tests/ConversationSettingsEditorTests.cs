using Avalonia.Automation;
using Avalonia.Controls;
using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class ConversationSettingsEditorTests
{
    [Fact]
    public void Editor_round_trips_existing_turn_settings()
    {
        var source = new TurnTakingSettings
        {
            DebounceMs = 800,
            MaxPendingMs = 9000,
            SplitReplies = false,
            AutoClassifyInterrupts = false,
            EnableListeningSignals = false,
            ListeningSignalDelayMs = 6000,
            MinReplyMessages = 2,
            MaxReplyMessages = 5,
            MaxMessageCharacters = 180,
            StyleInstruction = "简洁回复",
            TypingStyle = new TypingStyle { Speed = 1.5, Burstiness = .4, MinDelayMs = 200, MaxDelayMs = 1000 }
        };

        var editor = new ConversationSettingsEditor(source, new SettingsUiFactory());

        Assert.Equal(source, editor.BuildDraft());
        Assert.Equal(10, editor.Panel.Children.OfType<NumericUpDown>().Count());
        var style = Assert.Single(editor.Panel.Children.OfType<TextBox>());
        Assert.Equal("额外对话风格指令", AutomationProperties.GetName(style));
    }

    [Fact]
    public void BuildDraft_reads_current_values_and_trims_style()
    {
        var editor = new ConversationSettingsEditor(new TurnTakingSettings(), new SettingsUiFactory());
        var style = Assert.Single(editor.Panel.Children.OfType<TextBox>());
        style.Text = "  自然一些  ";
        var checks = editor.Panel.Children.OfType<CheckBox>().ToArray();
        checks[0].IsChecked = false;
        checks[2].IsChecked = false;

        var result = editor.BuildDraft();

        Assert.Equal("自然一些", result.StyleInstruction);
        Assert.False(result.SplitReplies);
        Assert.False(result.EnableListeningSignals);
    }
}

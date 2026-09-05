using Avalonia.Controls;
using Avalonia.Media;
using Pry.Core.Models;
using Pry.Core.Prompting;

namespace Pry.App.Services;

public sealed class CharacterPromptEditor
{
    private readonly TextBox _userName = Box();
    private readonly TextBox _identity = Box(80);
    private readonly TextBox _personality = Box(80);
    private readonly TextBox _speechStyle = Box(70);
    private readonly TextBox _rules = Box(110);
    private readonly TextBox _facts = Box(110);
    private readonly TextBox _legacyPrompt = Box(430);
    private readonly StackPanel _structuredPanel = new() { Spacing = 8 };
    private readonly StackPanel _legacyPanel = new() { Spacing = 8 };
    private readonly Button _structuredButton = new() { Content = "结构化" };
    private readonly Button _legacyButton = new() { Content = "Legacy" };

    public CharacterPromptEditor()
    {
        AddField(_structuredPanel, "对用户的称呼", _userName);
        AddField(_structuredPanel, "身份", _identity);
        AddField(_structuredPanel, "人格", _personality);
        AddField(_structuredPanel, "说话风格", _speechStyle);
        AddField(_structuredPanel, "行为规则（每行一条）", _rules);
        AddField(_structuredPanel, "世界设定（每行一条）", _facts);
        _legacyPanel.Children.Add(new TextBlock { Text = "完整 System Prompt", FontWeight = FontWeight.SemiBold });
        _legacyPanel.Children.Add(new TextBlock
        {
            Text = "内容原样作为角色设定；应用只追加记忆、当前状态和回复格式要求。",
            Foreground = Brush.Parse("#8EA2B5"), TextWrapping = TextWrapping.Wrap
        });
        _legacyPanel.Children.Add(_legacyPrompt);
        _structuredButton.Click += (_, _) => SetMode(CharacterPromptMode.Structured);
        _legacyButton.Click += (_, _) => SetMode(CharacterPromptMode.Legacy);
        View = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8,
                    Children = { _structuredButton, _legacyButton }
                },
                _structuredPanel, _legacyPanel
            }
        };
        SetMode(CharacterPromptMode.Structured);
    }

    public Control View { get; }
    public CharacterPromptMode Mode { get; private set; }
    public string? UserName => _userName.Text;
    public string? Identity => _identity.Text;
    public string? Personality => _personality.Text;
    public string? SpeechStyle => _speechStyle.Text;
    public string? BehavioralRules => _rules.Text;
    public string? WorldFacts => _facts.Text;
    public string? LegacyPrompt => _legacyPrompt.Text;

    public void Load(CharacterDefinition card)
    {
        _userName.Text = card.UserName;
        _identity.Text = card.Identity;
        _personality.Text = card.Personality;
        _speechStyle.Text = card.SpeechStyle;
        _rules.Text = string.Join(Environment.NewLine, card.BehavioralRules);
        _facts.Text = string.Join(Environment.NewLine, card.WorldFacts);
        _legacyPrompt.Text = card.LegacySystemPrompt;
        SetMode(card.PromptMode);
    }

    public void Reset()
    {
        _userName.Text = "你";
        _identity.Text = "";
        _personality.Text = "";
        _speechStyle.Text = "";
        _rules.Text = "";
        _facts.Text = "";
        _legacyPrompt.Text = "";
        SetMode(CharacterPromptMode.Structured);
    }

    public void SetMode(CharacterPromptMode value)
    {
        Mode = value;
        _structuredPanel.IsVisible = value == CharacterPromptMode.Structured;
        _legacyPanel.IsVisible = value == CharacterPromptMode.Legacy;
        _structuredButton.Classes.Set("primary", value == CharacterPromptMode.Structured);
        _legacyButton.Classes.Set("primary", value == CharacterPromptMode.Legacy);
    }

    private static TextBox Box(int height = 34) => new()
    {
        MinHeight = height, AcceptsReturn = height > 60, TextWrapping = TextWrapping.Wrap
    };

    private static void AddField(Panel panel, string label, Control control)
    {
        panel.Children.Add(new TextBlock { Text = label, FontWeight = FontWeight.SemiBold });
        panel.Children.Add(control);
    }
}

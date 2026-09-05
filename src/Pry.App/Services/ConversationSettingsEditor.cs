using Avalonia;
using Avalonia.Controls;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class ConversationSettingsEditor
{
    private readonly TextBox _style;
    private readonly NumericUpDown _minimumReplies;
    private readonly NumericUpDown _maximumReplies;
    private readonly NumericUpDown _maximumCharacters;
    private readonly CheckBox _splitReplies;
    private readonly CheckBox _autoClassifyInterrupts;
    private readonly CheckBox _listeningSignals;
    private readonly NumericUpDown _listeningDelay;
    private readonly NumericUpDown _debounce;
    private readonly NumericUpDown _maximumPending;
    private readonly NumericUpDown _typingSpeed;
    private readonly NumericUpDown _burstiness;
    private readonly NumericUpDown _minimumDelay;
    private readonly NumericUpDown _maximumDelay;

    public ConversationSettingsEditor(TurnTakingSettings value, SettingsUiFactory ui)
    {
        Panel = new StackPanel { Margin = new Thickness(28), Spacing = 14 };
        ui.AddHeader(Panel, "回复风格与轮次");
        _style = ui.CreateTextBox(value.StyleInstruction, 110);
        _minimumReplies = ui.CreateNumber(value.MinReplyMessages, 1, 6);
        _maximumReplies = ui.CreateNumber(value.MaxReplyMessages, 1, 6);
        _maximumCharacters = ui.CreateNumber(value.MaxMessageCharacters, 20, 500, 10);
        _splitReplies = new CheckBox { Content = "将回复拆成多条气泡", IsChecked = value.SplitReplies };
        _autoClassifyInterrupts = new CheckBox { Content = "自动识别附和与打断", IsChecked = value.AutoClassifyInterrupts };
        _listeningSignals = new CheckBox { Content = "输入或语音持续较久时发送“我在听”信号", IsChecked = value.EnableListeningSignals };
        _listeningDelay = ui.CreateNumber(value.ListeningSignalDelayMs, 1500, 15000, 250);
        _debounce = ui.CreateNumber(value.DebounceMs, 0, 10000, 100);
        _maximumPending = ui.CreateNumber(value.MaxPendingMs, 200, 30000, 100);
        _typingSpeed = ui.CreateNumber((decimal)value.TypingStyle.Speed, .25m, 4, .05m);
        _burstiness = ui.CreateNumber((decimal)value.TypingStyle.Burstiness, 0, 2, .05m);
        _minimumDelay = ui.CreateNumber(value.TypingStyle.MinDelayMs, 0, 10000, 50);
        _maximumDelay = ui.CreateNumber(value.TypingStyle.MaxDelayMs, 0, 30000, 100);

        ui.AddField(Panel, "额外对话风格指令", _style);
        ui.AddField(Panel, "每轮最少消息数", _minimumReplies);
        ui.AddField(Panel, "每轮最多消息数", _maximumReplies);
        ui.AddField(Panel, "单条建议最大字符数", _maximumCharacters);
        Panel.Children.Add(_splitReplies);
        Panel.Children.Add(_autoClassifyInterrupts);
        Panel.Children.Add(_listeningSignals);
        ui.AddField(Panel, "“我在听”信号等待时间（毫秒）", _listeningDelay);
        ui.AddField(Panel, "等待用户补话（毫秒）", _debounce);
        ui.AddField(Panel, "最长等待（毫秒）", _maximumPending);
        ui.AddField(Panel, "打字速度倍率（越大越快）", _typingSpeed);
        ui.AddField(Panel, "节奏随机度", _burstiness);
        ui.AddField(Panel, "单条最短延迟（毫秒）", _minimumDelay);
        ui.AddField(Panel, "单条最长延迟（毫秒）", _maximumDelay);
    }

    public StackPanel Panel { get; }

    public TurnTakingSettings BuildDraft() => new()
    {
        DebounceMs = (int)(_debounce.Value ?? 1200),
        MaxPendingMs = (int)(_maximumPending.Value ?? 5000),
        SplitReplies = _splitReplies.IsChecked == true,
        AutoClassifyInterrupts = _autoClassifyInterrupts.IsChecked == true,
        EnableListeningSignals = _listeningSignals.IsChecked == true,
        ListeningSignalDelayMs = (int)(_listeningDelay.Value ?? 4500),
        MinReplyMessages = (int)(_minimumReplies.Value ?? 1),
        MaxReplyMessages = (int)(_maximumReplies.Value ?? 4),
        MaxMessageCharacters = (int)(_maximumCharacters.Value ?? 90),
        StyleInstruction = _style.Text?.Trim() ?? "",
        TypingStyle = new TypingStyle
        {
            Speed = (double)(_typingSpeed.Value ?? 1),
            Burstiness = (double)(_burstiness.Value ?? .75m),
            MinDelayMs = (int)(_minimumDelay.Value ?? 450),
            MaxDelayMs = (int)(_maximumDelay.Value ?? 3200)
        }
    };
}

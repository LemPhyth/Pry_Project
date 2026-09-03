using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class ShortcutSettingsEditor
{
    private readonly TextBox _send;
    private readonly TextBox _sendImmediately;
    private readonly TextBox _newLine;
    private readonly TextBox _cancelReply;
    private readonly TextBox _newConversation;
    private readonly TextBox _openStickers;
    private readonly TextBox _openCharacterEditor;

    public ShortcutSettingsEditor(ShortcutSettings value, SettingsUiFactory ui)
    {
        Panel = new StackPanel { Margin = new Thickness(28), Spacing = 14 };
        ui.AddHeader(Panel, "快捷键");
        Panel.Children.Add(new TextBlock
        {
            Text = "格式示例：Enter、Ctrl+Enter、Ctrl+Shift+C",
            Foreground = Brush.Parse("#7F91A4")
        });
        _send = AddField(ui, "发送", value.Send);
        _sendImmediately = AddField(ui, "立即回复", value.SendImmediately);
        _newLine = AddField(ui, "换行", value.NewLine);
        _cancelReply = AddField(ui, "打断回复", value.CancelReply);
        _newConversation = AddField(ui, "新对话", value.NewConversation);
        _openStickers = AddField(ui, "打开表情", value.OpenStickers);
        _openCharacterEditor = AddField(ui, "角色卡", value.OpenCharacterEditor);
    }

    public StackPanel Panel { get; }

    public ShortcutSettings BuildDraft() => new()
    {
        Send = Read(_send, "Enter"),
        SendImmediately = Read(_sendImmediately, "Ctrl+Enter"),
        NewLine = Read(_newLine, "Shift+Enter"),
        CancelReply = Read(_cancelReply, "Escape"),
        NewConversation = Read(_newConversation, "Ctrl+N"),
        OpenStickers = Read(_openStickers, "Ctrl+E"),
        OpenCharacterEditor = Read(_openCharacterEditor, "Ctrl+Shift+C")
    };

    private TextBox AddField(SettingsUiFactory ui, string label, string value)
    {
        var input = ui.CreateTextBox(value);
        ui.AddField(Panel, label, input);
        return input;
    }

    private static string Read(TextBox input, string fallback) =>
        string.IsNullOrWhiteSpace(input.Text) ? fallback : input.Text.Trim();
}

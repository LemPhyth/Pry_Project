using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class DesktopPetSettingsEditor
{
    private readonly CheckBox _enabled;
    private readonly CheckBox _alwaysOnTop;
    private readonly NumericUpDown _scale;

    public DesktopPetSettingsEditor(DesktopPetPreferences value, SettingsUiFactory ui)
    {
        Panel = new StackPanel { Margin = new Thickness(28), Spacing = 14 };
        ui.AddHeader(Panel, "桌宠与托盘");
        _enabled = new CheckBox { Content = "启用桌宠模式（等待美术素材）", IsChecked = value.Enabled };
        _alwaysOnTop = new CheckBox { Content = "桌宠始终置顶", IsChecked = value.AlwaysOnTop };
        _scale = ui.CreateNumber((decimal)value.Scale, .5m, 3, .1m);
        Panel.Children.Add(_enabled);
        Panel.Children.Add(_alwaysOnTop);
        ui.AddField(Panel, "桌宠缩放", _scale);
        Panel.Children.Add(new TextBlock
        {
            Text = "托盘菜单会始终可用；桌宠渲染将在你提供美术素材后接入现有占位接口。",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush.Parse("#7F91A4")
        });
    }

    public StackPanel Panel { get; }

    public DesktopPetPreferences BuildDraft() => new()
    {
        Enabled = _enabled.IsChecked == true,
        AlwaysOnTop = _alwaysOnTop.IsChecked == true,
        Scale = Math.Clamp((double)(_scale.Value ?? 1), .5, 3)
    };
}

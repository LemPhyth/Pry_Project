using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class ThemeAppearanceEditor
{
    private readonly ComboBox _mode;
    private readonly TextBox _accent;
    private readonly CheckBox _glass;
    private readonly CheckBox _liveResize;
    private readonly NumericUpDown _avatarSize;
    private readonly NumericUpDown _fontSize;
    private readonly NumericUpDown _maxWidth;
    private readonly NumericUpDown _spacing;

    public ThemeAppearanceEditor(ThemePreferences value, SettingsUiFactory ui)
    {
        _mode = new ComboBox { ItemsSource = new[] { "跟随系统", "深色", "浅色" }, SelectedIndex = value.ThemeMode.ToLowerInvariant() switch { "dark" => 1, "light" => 2, _ => 0 } };
        _accent = ui.CreateTextBox(string.IsNullOrWhiteSpace(value.AccentColor) ? "#B148C6" : value.AccentColor);
        _glass = new CheckBox { Content = "侧边栏、顶部标题栏和底部输入区透出软件背景", IsChecked = value.UseGlassEffects };
        _liveResize = new CheckBox { Content = "拖动时实时调整侧边栏宽度", IsChecked = value.LiveSidebarResize };
        BackgroundDim = ui.CreateNumber((decimal)value.BackgroundDimOpacity, 0, .85m, .05m);
        BackgroundOpacity = ui.CreateNumber((decimal)value.BackgroundImageOpacity, 0, 1, .05m);
        BackgroundBlur = ui.CreateNumber((decimal)value.BackgroundBlurRadius, 0, 32);
        _avatarSize = ui.CreateNumber((decimal)value.AvatarSize, 28, 76);
        _fontSize = ui.CreateNumber((decimal)value.BubbleFontSize, 11, 24, .5m);
        _maxWidth = ui.CreateNumber((decimal)value.BubbleMaxWidth, 280, 900, 20);
        _spacing = ui.CreateNumber((decimal)value.BubbleSpacing, 2, 36);
        AutomationProperties.SetName(BackgroundDim, "背景遮罩浓度");
        AutomationProperties.SetName(BackgroundOpacity, "背景透明度（透出桌面）");
        AutomationProperties.SetName(BackgroundBlur, "背景模糊程度");
        Panel = new StackPanel { Margin = new Thickness(28), Spacing = 14 };
        ui.AddHeader(Panel, "外观与主题");
        var palette = new WrapPanel();
        foreach (var color in new[] { "#B148C6", "#7C5CFC", "#3B82F6", "#06A6A6", "#16A36A", "#84A322", "#E29A24", "#D95D39", "#E05278", "#6B7A90" })
        {
            var swatch = new Button { Width = 34, Height = 34, Padding = new Thickness(0), Margin = new Thickness(0, 0, 7, 7), Background = Brush.Parse(color), BorderBrush = Brushes.White, BorderThickness = new Thickness(1) };
            ToolTip.SetTip(swatch, color);
            AutomationProperties.SetName(swatch, $"强调色 {color}");
            swatch.Click += (_, _) => _accent.Text = color;
            palette.Children.Add(swatch);
        }
        var colors = new StackPanel { Spacing = 7 };
        ui.AddField(colors, "主题模式", _mode);
        ui.AddField(colors, "强调色调色板", palette);
        ui.AddField(colors, "自定义十六进制颜色", _accent);
        Panel.Children.Add(ui.CreateCard("颜色主题", colors, Hint("可跟随系统切换深浅色，也可以指定软件强调色。")));
        Panel.Children.Add(ui.CreateCard("图片素材", Hint("背景与头像取景使用独立页面，避免大预览区占满外观设置并影响滚动。"),
            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { OpenBackground, OpenAvatar } }));
        Panel.Children.Add(ui.CreateCard("界面材质", _glass));
        var sizes = new StackPanel { Spacing = 7 };
        ui.AddField(sizes, "头像尺寸", _avatarSize);
        ui.AddField(sizes, "聊天文字大小", _fontSize);
        ui.AddField(sizes, "气泡最大宽度", _maxWidth);
        ui.AddField(sizes, "气泡纵向间距", _spacing);
        Panel.Children.Add(ui.CreateCard("界面尺寸（实时预览）", _liveResize,
            Hint("关闭时拖动只显示位置指示线，松手后调整宽度；开启时侧边栏会跟随指针实时变化。"), sizes,
            Hint("调整后主聊天窗口会立即呈现效果；关闭设置而不保存会恢复原值。")));
        _mode.SelectionChanged += (_, _) => Changed?.Invoke();
        _accent.TextChanged += (_, _) => Changed?.Invoke();
        _glass.IsCheckedChanged += (_, _) => Changed?.Invoke();
        _liveResize.IsCheckedChanged += (_, _) => Changed?.Invoke();
        foreach (var number in new[] { BackgroundDim, BackgroundOpacity, BackgroundBlur, _avatarSize, _fontSize, _maxWidth, _spacing })
            number.ValueChanged += (_, _) => Changed?.Invoke();
    }

    public event Action? Changed;
    public StackPanel Panel { get; }
    public NumericUpDown BackgroundDim { get; }
    public NumericUpDown BackgroundOpacity { get; }
    public NumericUpDown BackgroundBlur { get; }
    public Button OpenBackground { get; } = new() { Content = "聊天背景、透明度与模糊…", HorizontalAlignment = HorizontalAlignment.Left };
    public Button OpenAvatar { get; } = new() { Content = "用户头像与取景…", HorizontalAlignment = HorizontalAlignment.Left };
    public string? AccentColorText => _accent.Text;
    public bool LiveSidebarResize => _liveResize.IsChecked == true;

    public ThemePreferences BuildTheme(ThemePreferences source, ThemeImageDraft background, ThemeImageDraft avatar) =>
        SettingsDraftService.BuildTheme(source, new ThemeSettingsDraft
        {
            BackgroundImagePath = background.SelectedPath, BackgroundHistory = background.History, BackgroundDisplays = background.Displays,
            UserAvatarPath = avatar.SelectedPath, UserAvatarHistory = avatar.History, UserAvatarDisplays = avatar.Displays,
            ThemeModeIndex = _mode.SelectedIndex, AccentColor = _accent.Text ?? "#B148C6",
            UseGlassEffects = _glass.IsChecked == true, LiveSidebarResize = LiveSidebarResize,
            BackgroundImageOpacity = (double)(BackgroundOpacity.Value ?? 1), BackgroundDimOpacity = (double)(BackgroundDim.Value ?? .34m),
            BackgroundBlurRadius = (double)(BackgroundBlur.Value ?? 0), AvatarSize = (double)(_avatarSize.Value ?? 48),
            BubbleFontSize = (double)(_fontSize.Value ?? 14), BubbleMaxWidth = (double)(_maxWidth.Value ?? 620), BubbleSpacing = (double)(_spacing.Value ?? 10)
        });

    private static TextBlock Hint(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap, Foreground = Brush.Parse("#7F91A4") };
}

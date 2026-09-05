using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class UserProfileDialog(
    Func<Control, Control> createSurface,
    Action<Image, ImageDisplayPreferences> applyImageDisplay,
    Func<string, string, Task> showNotice)
{
    public async Task<UserProfileEditResult?> ShowAsync(Window owner, UserProfilePreferences profile,
        ThemePreferences theme)
    {
        var displayName = new TextBox { Text = profile.DisplayName, Watermark = "你的显示名" };
        var signature = new TextBox { Text = profile.Signature, Watermark = "一句个性签名" };
        var focusX = new NumericUpDown { Value = .5m, Minimum = 0, Maximum = 1, Increment = .05m };
        var focusY = new NumericUpDown { Value = .5m, Minimum = 0, Maximum = 1, Increment = .05m };
        var zoom = new NumericUpDown { Value = 1, Minimum = 1, Maximum = 3, Increment = .05m };
        var preview = new Image { Width = 200, Height = 200, Stretch = Stretch.UniformToFill };
        string? avatarPath = theme.UserAvatarPath;
        var displays = theme.UserAvatarDisplays.ToDictionary(pair => pair.Key, pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);

        void RefreshPreview()
        {
            preview.Source = null;
            if (string.IsNullOrWhiteSpace(avatarPath) || !File.Exists(avatarPath)) return;
            try
            {
                preview.Source = new Bitmap(avatarPath);
                applyImageDisplay(preview, CurrentDisplay(focusX, focusY, zoom));
            }
            catch { }
        }
        if (avatarPath is not null && displays.TryGetValue(avatarPath, out var existingDisplay))
        {
            focusX.Value = (decimal)existingDisplay.FocusX;
            focusY.Value = (decimal)existingDisplay.FocusY;
            zoom.Value = (decimal)existingDisplay.Zoom;
        }
        focusX.ValueChanged += (_, _) => RefreshPreview();
        focusY.ValueChanged += (_, _) => RefreshPreview();
        zoom.ValueChanged += (_, _) => RefreshPreview();
        RefreshPreview();

        var choose = new Button { Content = "导入头像…" };
        var clear = new Button { Content = "使用默认头像" };
        var save = new Button
        {
            Content = "保存资料", Classes = { "primary" },
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var panel = new StackPanel { Margin = new Thickness(26), Spacing = 10 };
        panel.Children.Add(new TextBlock { Text = "用户资料", FontSize = 22, FontWeight = FontWeight.SemiBold });
        panel.Children.Add(new TextBlock
        {
            Text = "头像读取原始素材，不生成压缩副本。拖动调整取景，滚轮缩放。",
            Foreground = Brush.Parse("#8EA2B5"), TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(VisualCropEditor.Create(preview, focusX, focusY, zoom, 200, 200, true));
        panel.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8, Children = { choose, clear }
        });
        panel.Children.Add(new TextBlock { Text = "用户名" });
        panel.Children.Add(displayName);
        panel.Children.Add(new TextBlock { Text = "个性签名" });
        panel.Children.Add(signature);
        panel.Children.Add(save);

        var window = new Window
        {
            Title = "用户资料", Width = 560, Height = 650,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = createSurface(panel)
        };
        choose.Click += async (_, _) =>
        {
            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择用户头像", AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("图片") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp"] }]
            });
            var source = files.FirstOrDefault()?.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(source)) return;
            avatarPath = source;
            focusX.Value = .5m;
            focusY.Value = .5m;
            zoom.Value = 1;
            RefreshPreview();
        };
        clear.Click += (_, _) =>
        {
            avatarPath = null;
            preview.Source = null;
        };
        UserProfileEditResult? result = null;
        save.Click += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(displayName.Text))
            {
                await showNotice("无法保存", "用户名不能为空。");
                return;
            }
            result = BuildResult(profile, theme, displayName.Text, signature.Text, avatarPath,
                CurrentDisplay(focusX, focusY, zoom), displays);
            window.Close();
        };
        await window.ShowDialog(owner);
        return result;
    }

    public static UserProfileEditResult BuildResult(UserProfilePreferences originalProfile,
        ThemePreferences theme, string displayName, string? signature, string? avatarPath,
        ImageDisplayPreferences display, IReadOnlyDictionary<string, ImageDisplayPreferences> sourceDisplays)
    {
        var displays = sourceDisplays.ToDictionary(pair => pair.Key, pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);
        if (avatarPath is not null) displays[avatarPath] = display;
        var history = theme.UserAvatarHistory.Append(avatarPath)
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return new UserProfileEditResult(
            originalProfile with { DisplayName = displayName.Trim(), Signature = signature?.Trim() ?? "" },
            theme with
            {
                UserAvatarPath = avatarPath, UserAvatarHistory = history,
                UserAvatarDisplays = displays
            });
    }

    private static ImageDisplayPreferences CurrentDisplay(NumericUpDown focusX, NumericUpDown focusY,
        NumericUpDown zoom) => new()
    {
        FocusX = (double)(focusX.Value ?? .5m), FocusY = (double)(focusY.Value ?? .5m),
        Zoom = (double)(zoom.Value ?? 1)
    };
}

public sealed record UserProfileEditResult(UserProfilePreferences Profile, ThemePreferences Theme);

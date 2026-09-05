using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class CharacterAvatarEditor
{
    private readonly Action<Image, ImageDisplayPreferences> _applyImageDisplay;
    private readonly Image _preview = new() { Width = 180, Height = 180, Stretch = Stretch.UniformToFill };
    private readonly NumericUpDown _focusX = new() { Minimum = 0, Maximum = 1, Increment = .05m };
    private readonly NumericUpDown _focusY = new() { Minimum = 0, Maximum = 1, Increment = .05m };
    private readonly NumericUpDown _zoom = new() { Minimum = 1, Maximum = 3, Increment = .05m };

    public CharacterAvatarEditor(Action<Image, ImageDisplayPreferences> applyImageDisplay)
    {
        _applyImageDisplay = applyImageDisplay;
        ChooseButton = new Button { Content = "导入角色头像…" };
        ClearButton = new Button { Content = "恢复默认" };
        View = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 14,
            Children =
            {
                VisualCropEditor.Create(_preview, _focusX, _focusY, _zoom, 180, 180, true),
                new StackPanel
                {
                    Spacing = 7, VerticalAlignment = VerticalAlignment.Center,
                    Children = { ChooseButton, ClearButton }
                }
            }
        };
        _focusX.ValueChanged += (_, _) => Refresh();
        _focusY.ValueChanged += (_, _) => Refresh();
        _zoom.ValueChanged += (_, _) => Refresh();
        ClearButton.Click += (_, _) => Load(null, new ImageDisplayPreferences());
    }

    public Button ChooseButton { get; }
    public Button ClearButton { get; }
    public Control View { get; }
    public string? AvatarPath { get; private set; }
    public ImageDisplayPreferences Display { get; private set; } = new();

    public void Load(string? path, ImageDisplayPreferences display)
    {
        AvatarPath = path;
        _focusX.Value = (decimal)display.FocusX;
        _focusY.Value = (decimal)display.FocusY;
        _zoom.Value = (decimal)display.Zoom;
        Refresh();
    }

    private void Refresh()
    {
        _preview.Source = null;
        Display = new ImageDisplayPreferences
        {
            FocusX = (double)(_focusX.Value ?? .5m), FocusY = (double)(_focusY.Value ?? .5m),
            Zoom = (double)(_zoom.Value ?? 1)
        };
        if (string.IsNullOrWhiteSpace(AvatarPath) || !File.Exists(AvatarPath)) return;
        try
        {
            _preview.Source = new Bitmap(AvatarPath);
            _applyImageDisplay(_preview, Display);
        }
        catch { }
    }
}

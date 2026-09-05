using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class ThemeImageSettingsEditor
{
    private readonly ThemeImageResources _resources;
    private readonly Func<IBrush> _accent;
    private readonly Action<Image, ImageDisplayPreferences> _applyDisplay;
    private bool _loading;

    public ThemeImageSettingsEditor(ThemeImageDraft draft, ThemeImageResources resources,
        SettingsUiFactory ui, Func<IBrush> accent, Action<Image, ImageDisplayPreferences> applyDisplay,
        bool square, string heading, string cardTitle, string description, string importText,
        string clearText, params Control[] trailingControls)
    {
        Draft = draft;
        _resources = resources;
        _accent = accent;
        _applyDisplay = applyDisplay;
        Preview = new Image { Stretch = Stretch.UniformToFill };
        Gallery = new WrapPanel { Orientation = Orientation.Horizontal };
        ImportButton = new Button { Content = importText };
        ClearButton = new Button { Content = clearText };
        DeleteButton = new Button { Content = "删除所选" };
        FocusX = ui.CreateNumber(.5m, 0, 1, .05m);
        FocusY = ui.CreateNumber(.5m, 0, 1, .05m);
        Zoom = ui.CreateNumber(1, 1, 3, .05m);
        var crop = VisualCropEditor.Create(Preview, FocusX, FocusY, Zoom,
            square ? 180 : 500, square ? 180 : 280, square);
        var controls = new List<Control>
        {
            new TextBlock { Text = description, TextWrapping = TextWrapping.Wrap, Foreground = Brush.Parse("#B8CCE0") },
            crop, Gallery,
            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8,
                Children = { ImportButton, ClearButton, DeleteButton } }
        };
        controls.AddRange(trailingControls);
        Page = new StackPanel { Margin = new Thickness(28), Spacing = 14 };
        BackButton = new Button { Content = "← 返回外观与主题", HorizontalAlignment = HorizontalAlignment.Left };
        ui.AddHeader(Page, heading);
        Page.Children.Add(BackButton);
        Page.Children.Add(ui.CreateCard(cardTitle, [.. controls]));
        foreach (var number in ValueControls) number.ValueChanged += (_, _) => { if (!_loading) Changed?.Invoke(); };
        ClearButton.Click += (_, _) => Clear();
        Refresh();
        Load(Draft.SelectedPath);
    }

    public ThemeImageDraft Draft { get; }
    public StackPanel Page { get; }
    public Button BackButton { get; }
    public Button ImportButton { get; }
    public Button ClearButton { get; }
    public Button DeleteButton { get; }
    public Image Preview { get; }
    public WrapPanel Gallery { get; }
    public NumericUpDown FocusX { get; }
    public NumericUpDown FocusY { get; }
    public NumericUpDown Zoom { get; }
    public NumericUpDown[] ValueControls => [FocusX, FocusY, Zoom];
    public event Action? Changed;

    public ImageDisplayPreferences CurrentDisplay => new()
    {
        FocusX = (double)(FocusX.Value ?? .5m), FocusY = (double)(FocusY.Value ?? .5m),
        Zoom = (double)(Zoom.Value ?? 1)
    };

    public void StoreCurrent()
    {
        if (_loading || Draft.SelectedPath is null) return;
        var display = CurrentDisplay;
        Draft.StoreDisplay(display);
        _applyDisplay(Preview, display);
    }

    public void Import(string path)
    {
        Draft.Select(path, CurrentDisplay);
        Load(path);
        Refresh();
        Changed?.Invoke();
    }

    public void Clear()
    {
        Draft.ClearSelection(CurrentDisplay);
        _resources.Clear(Preview);
        Refresh();
        Changed?.Invoke();
    }

    public bool RemoveSelected()
    {
        if (Draft.SelectedPath is null) return false;
        Draft.RemoveSelected();
        _resources.Clear(Preview);
        Refresh();
        Changed?.Invoke();
        return true;
    }

    private void Select(string path)
    {
        Draft.Select(path, CurrentDisplay);
        Load(path);
        Refresh();
        Changed?.Invoke();
    }

    private void Load(string? path)
    {
        _loading = true;
        var display = path is not null && Draft.Displays.TryGetValue(path, out var saved)
            ? saved : new ImageDisplayPreferences();
        FocusX.Value = (decimal)display.FocusX;
        FocusY.Value = (decimal)display.FocusY;
        Zoom.Value = (decimal)display.Zoom;
        if (_resources.Load(Preview, path)) _applyDisplay(Preview, display);
        _loading = false;
    }

    private void Refresh() => ThemeImageGallery.Refresh(Gallery, Draft.History, Draft.SelectedPath,
        Select, Preview.Width == 180, _accent(), _resources);
}

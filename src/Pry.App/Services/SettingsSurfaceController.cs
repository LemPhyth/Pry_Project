using Avalonia.Controls;
using Avalonia.Media;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class SettingsSurfaceController
{
    private readonly Action<IReadOnlyList<WindowTransparencyLevel>> _setTransparency;
    private readonly Grid _surface;
    private readonly Image? _backgroundImage;
    private readonly Border? _backgroundDim;
    private readonly IReadOnlyList<Border> _cards;
    private readonly IReadOnlyList<Control> _roots;
    private readonly SettingsShell _shell;
    private readonly Func<IBrush> _accent;
    private readonly Func<string, double, IImage> _loadBackground;
    private readonly Action<Image, ImageDisplayPreferences> _applyDisplay;
    private readonly Func<string, bool> _exists;

    public SettingsSurfaceController(Action<IReadOnlyList<WindowTransparencyLevel>> setTransparency,
        Grid surface, Image? backgroundImage,
        Border? backgroundDim, IReadOnlyList<Border> cards, IReadOnlyList<Control> roots,
        SettingsShell shell, Func<IBrush> accent, Func<string, double, IImage> loadBackground,
        Action<Image, ImageDisplayPreferences> applyDisplay, Func<string, bool>? exists = null)
    {
        _setTransparency = setTransparency;
        _surface = surface;
        _backgroundImage = backgroundImage;
        _backgroundDim = backgroundDim;
        _cards = cards;
        _roots = roots;
        _shell = shell;
        _accent = accent;
        _loadBackground = loadBackground;
        _applyDisplay = applyDisplay;
        _exists = exists ?? File.Exists;
    }

    public void Apply(ThemePreferences draft, bool systemUsesLightTheme)
    {
        var light = draft.ThemeMode == "light" ||
            (draft.ThemeMode == "system" && systemUsesLightTheme);
        var hasBackground = !string.IsNullOrWhiteSpace(draft.BackgroundImagePath) &&
            _exists(draft.BackgroundImagePath);
        var palette = new SettingsSurfaceTheme(light, hasBackground);
        _setTransparency(draft.BackgroundBlurRadius > .5
            ? [WindowTransparencyLevel.Blur, WindowTransparencyLevel.Transparent]
            : [WindowTransparencyLevel.Transparent]);
        _surface.Background = hasBackground ? Brushes.Transparent : palette.Canvas;
        ApplyBackground(draft, hasBackground, palette.Canvas);
        palette.Apply(_cards, _roots);
        _shell.ApplyColors(palette.Panel, palette.Card, palette.Border, _accent());
    }

    private void ApplyBackground(ThemePreferences draft, bool hasBackground, IBrush canvas)
    {
        if (_backgroundImage is not null)
        {
            _backgroundImage.IsVisible = hasBackground;
            _backgroundImage.Opacity = Math.Clamp(draft.BackgroundImageOpacity, 0, 1);
            if (hasBackground)
            {
                try
                {
                    _backgroundImage.Source = _loadBackground(draft.BackgroundImagePath!,
                        Math.Clamp(draft.BackgroundBlurRadius, 0, 32));
                    var display = draft.BackgroundDisplays.TryGetValue(draft.BackgroundImagePath!, out var saved)
                        ? saved : new ImageDisplayPreferences();
                    _applyDisplay(_backgroundImage, display);
                }
                catch
                {
                    _backgroundImage.Source = null;
                    _backgroundImage.IsVisible = false;
                }
            }
            else _backgroundImage.Source = null;
        }
        if (_backgroundDim is null) return;
        _backgroundDim.IsVisible = hasBackground;
        _backgroundDim.Background = canvas;
        _backgroundDim.Opacity = Math.Clamp(draft.BackgroundDimOpacity, 0, .85);
    }
}

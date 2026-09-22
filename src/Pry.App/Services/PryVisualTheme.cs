using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Pry.Core.Models;

namespace Pry.App.Services;

/// <summary>Shared desktop palette. Mutable brushes also update programmatically built pages.</summary>
public static class PryVisualTheme
{
    private static readonly Dictionary<string, SolidColorBrush> Brushes = new();

    public static SolidColorBrush Get(string name)
    {
        if (!Brushes.TryGetValue(name, out var brush))
            Brushes[name] = brush = new SolidColorBrush(Color.Parse("#F2EDF7"));
        return brush;
    }

    public static bool IsLight(ThemePreferences theme) => theme.ThemeMode.Equals("light", StringComparison.OrdinalIgnoreCase)
        || (theme.ThemeMode.Equals("system", StringComparison.OrdinalIgnoreCase)
            && Application.Current?.ActualThemeVariant == ThemeVariant.Light);

    public static Color ReadableText(Color background)
    {
        static double Linear(byte channel) => channel / 255d <= .04045
            ? channel / 255d / 12.92 : Math.Pow((channel / 255d + .055) / 1.055, 2.4);
        var luminance = .2126 * Linear(background.R) + .7152 * Linear(background.G) + .0722 * Linear(background.B);
        return luminance > .179 ? Colors.Black : Colors.White;
    }

    public static Color Blend(Color color, Color surface, double amount) => Color.FromRgb(
        (byte)Math.Round(color.R * amount + surface.R * (1 - amount)),
        (byte)Math.Round(color.G * amount + surface.G * (1 - amount)),
        (byte)Math.Round(color.B * amount + surface.B * (1 - amount)));

    public static double BackgroundVeilOpacity(bool light, bool showBackground) =>
        showBackground ? (light ? .9 : .84) : .96;

    public static void Apply(ThemePreferences theme)
    {
        var app = Application.Current;
        if (app is not null)
            app.RequestedThemeVariant = theme.ThemeMode.ToLowerInvariant() switch
            { "light" => ThemeVariant.Light, "dark" => ThemeVariant.Dark, _ => ThemeVariant.Default };
        var light = IsLight(theme);
        void Set(string name, Color color)
        {
            Get(name).Color = color;
            if (app is not null) app.Resources["Pry" + name + "Brush"] = Get(name);
        }
        void Pair(string name, string day, string night) => Set(name, Color.Parse(light ? day : night));
        Pair("Canvas", "#F7F5FA", "#17141E");
        Pair("Panel", "#EFEBF4", "#211C2B");
        Pair("Card", "#FFFFFF", "#2B2436");
        Pair("Popup", "#FFFFFF", "#2B2436");
        Pair("Text", "#292333", "#F2EDF7");
        Pair("Muted", "#655B72", "#BDB1CA");
        Pair("Border", "#DDD5E5", "#453A52");
        Pair("ControlBorder", "#82728F", "#9382A3");
        Pair("Button", "#E8E2EE", "#342B40");
        Pair("ButtonHover", "#DDD3E6", "#443750");
        Pair("ButtonPressed", "#D1C5DD", "#51415E");
        Pair("Error", "#A13247", "#FFA7B8");
        Pair("Warning", "#795408", "#E9C56E");
        var accent = Color.TryParse(theme.AccentColor, out var custom) ? custom : Color.Parse(light ? "#70418F" : "#C5A0DF");
        accent = Color.FromRgb(accent.R, accent.G, accent.B);
        Set("Accent", accent);
        Set("AccentText", ReadableText(accent));
        var contrastDirection = ReadableText(accent) == Colors.Black ? Colors.White : Colors.Black;
        Set("AccentHover", Blend(accent, contrastDirection, .9));
        Set("AccentPressed", Blend(accent, contrastDirection, .8));
        Set("Selection", Blend(accent, Get("Panel").Color, light ? .14 : .22));
        Set("UserBubble", Blend(accent, Get("Card").Color, light ? .15 : .25));
        Set("Focus", Color.Parse(light ? "#70418F" : "#D2B2E7"));
        if (app is not null) app.Resources["SystemAccentColor"] = accent;
    }
}

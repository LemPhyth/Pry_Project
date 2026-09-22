using Avalonia.Media;
using Pry.App.Services;
using Xunit;

namespace Pry.App.Tests;

public sealed class PryVisualThemeTests
{
    [Theory]
    [InlineData(true, "#F7F5FA", "#655B72", "#000000")]
    [InlineData(false, "#17141E", "#BDB1CA", "#FFFFFF")]
    public void Message_metadata_is_readable_over_worst_case_custom_background(bool light, string canvas, string text, string image)
    {
        foreach (var showBackground in new[] { true, false })
        {
            var background = PryVisualTheme.Blend(Color.Parse(canvas), Color.Parse(image),
                PryVisualTheme.BackgroundVeilOpacity(light, showBackground));
            var first = Luminance(Color.Parse(text)); var second = Luminance(background);
            Assert.True((Math.Max(first, second) + .05) / (Math.Min(first, second) + .05) >= 4.5);
        }
    }

    [Fact]
    public void Custom_accent_text_remains_readable_in_default_hover_and_pressed_states()
    {
        // Sample the RGB cube, including bright yellow, dark purple and midtone greys.
        for (var r = 0; r <= 255; r += 17)
        for (var g = 0; g <= 255; g += 17)
        for (var b = 0; b <= 255; b += 17)
        {
            var accent = Color.FromRgb((byte)r, (byte)g, (byte)b);
            var text = PryVisualTheme.ReadableText(accent);
            var direction = text == Colors.Black ? Colors.White : Colors.Black;
            foreach (var amount in new[] { 1d, .9, .8 })
            {
                var background = PryVisualTheme.Blend(accent, direction, amount);
                var first = Luminance(text); var second = Luminance(background);
                var ratio = (Math.Max(first, second) + .05) / (Math.Min(first, second) + .05);
                Assert.True(ratio >= 4.5, $"{text} on {background}: {ratio:F2}");
            }
        }
    }

    private static double Luminance(Color color)
    {
        static double Linear(byte value) => value / 255d <= .04045 ? value / 255d / 12.92 : Math.Pow((value / 255d + .055) / 1.055, 2.4);
        return .2126 * Linear(color.R) + .7152 * Linear(color.G) + .0722 * Linear(color.B);
    }
}

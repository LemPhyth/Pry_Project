namespace Pry.App.Services;

public sealed class ChatUnderlayMaskState
{
    private double _height = -1;
    private double _headerHeight = -1;
    private double _composerHeight = -1;

    public bool TryCalculate(double height, double headerHeight, double composerHeight,
        out ChatUnderlayMaskGeometry geometry)
    {
        geometry = default;
        if (height <= 1) return false;
        if (Math.Abs(height - _height) < .5 && Math.Abs(headerHeight - _headerHeight) < .5 &&
            Math.Abs(composerHeight - _composerHeight) < .5) return false;

        _height = height;
        _headerHeight = headerHeight;
        _composerHeight = composerHeight;
        var feather = Math.Clamp(12 / height, .008, .04);
        var top = Math.Clamp(headerHeight / height, 0, .42);
        var bottom = Math.Clamp(1 - composerHeight / height, .58, 1);
        geometry = new ChatUnderlayMaskGeometry(top, Math.Min(.49, top + feather),
            Math.Max(.51, bottom - feather), bottom);
        return true;
    }
}

public readonly record struct ChatUnderlayMaskGeometry(
    double TopTransparentEnd,
    double TopFeatherEnd,
    double BottomFeatherStart,
    double BottomTransparentStart);

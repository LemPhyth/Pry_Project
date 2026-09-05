using Avalonia.Media.Imaging;
using SkiaSharp;

namespace Pry.App.Services;

public sealed class BackgroundImageCache(int capacity = 12) : IDisposable
{
    private readonly Dictionary<string, Bitmap> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _capacity = Math.Max(1, capacity);

    public string GetKey(string path, double blurRadius) =>
        $"{Path.GetFullPath(path)}|{File.GetLastWriteTimeUtc(path).Ticks}|{Math.Round(blurRadius, 1):0.0}";

    public Bitmap Get(string path, double blurRadius)
    {
        var key = GetKey(path, blurRadius);
        if (_entries.TryGetValue(key, out var cached)) return cached;

        var bitmap = Create(path, blurRadius);
        _entries[key] = bitmap;
        while (_entries.Count > _capacity)
        {
            var stale = _entries.Keys.First(x => !string.Equals(x, key, StringComparison.Ordinal));
            if (_entries.Remove(stale, out var removed)) removed.Dispose();
        }
        return bitmap;
    }

    public void Dispose()
    {
        foreach (var bitmap in _entries.Values) bitmap.Dispose();
        _entries.Clear();
    }

    private static Bitmap Create(string path, double blurRadius)
    {
        if (blurRadius < .5) return new Bitmap(path);
        using var decoded = SKBitmap.Decode(path) ?? throw new InvalidDataException("无法读取背景图片。");
        const int maxSide = 1800;
        var scale = Math.Min(1d, maxSide / (double)Math.Max(decoded.Width, decoded.Height));
        var width = Math.Max(1, (int)Math.Round(decoded.Width * scale));
        var height = Math.Max(1, (int)Math.Round(decoded.Height * scale));
        using var resized = scale < 1
            ? decoded.Resize(new SKImageInfo(width, height), SKFilterQuality.High)
            : decoded.Copy();
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        using var paint = new SKPaint
        {
            ImageFilter = SKImageFilter.CreateBlur((float)Math.Clamp(blurRadius, 0, 40),
                (float)Math.Clamp(blurRadius, 0, 40)),
            IsAntialias = true
        };
        surface.Canvas.Clear(SKColors.Transparent);
        surface.Canvas.DrawBitmap(resized, 0, 0, paint);
        surface.Canvas.Flush();
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 92);
        using var stream = new MemoryStream(data.ToArray());
        return new Bitmap(stream);
    }
}

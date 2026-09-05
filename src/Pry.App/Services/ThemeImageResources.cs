using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Pry.App.Services;

public sealed class ThemeImageResources(Func<string, IImage>? load = null) : IDisposable
{
    private readonly Func<string, IImage> _load = load ?? (path => new Bitmap(path));
    private readonly Dictionary<Image, IImage> _images = [];
    private bool _disposed;

    public bool Load(Image target, string? path)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Clear(target);
        if (string.IsNullOrWhiteSpace(path)) return false;
        IImage source;
        try { source = _load(path); }
        catch { return false; }
        _images.Add(target, source);
        target.Source = source;
        return true;
    }

    public void Clear(Image target)
    {
        target.Source = null;
        if (_images.Remove(target, out var source) && source is IDisposable disposable)
            disposable.Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var target in _images.Keys.ToArray()) Clear(target);
    }
}

using Avalonia.Threading;
using Pry.Core.Models;

namespace Pry.App.Services;

public sealed class ThemePreviewController : IDisposable
{
    private readonly Func<ThemePreferences?> _buildDraft;
    private readonly Action<ThemePreferences> _apply;
    private readonly Action _rollback;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(55) };
    private bool _committed;
    private bool _disposed;

    public ThemePreviewController(Func<ThemePreferences?> buildDraft, Action<ThemePreferences> apply, Action rollback)
    {
        _buildDraft = buildDraft;
        _apply = apply;
        _rollback = rollback;
        _timer.Tick += OnTick;
    }

    public void Request()
    {
        if (_disposed || _committed) return;
        _timer.Stop();
        _timer.Start();
    }

    public void Flush()
    {
        if (_disposed || _committed) return;
        _timer.Stop();
        if (_buildDraft() is { } draft) _apply(draft);
    }

    public void Commit()
    {
        if (_disposed) return;
        _committed = true;
        _timer.Stop();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTick;
        if (!_committed) _rollback();
    }

    private void OnTick(object? sender, EventArgs args) => Flush();
}

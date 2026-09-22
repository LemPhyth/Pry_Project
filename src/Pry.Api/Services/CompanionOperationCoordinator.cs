namespace Pry.Api.Services;

/// <summary>Serializes short state transitions, never holds a gate while generating model text.</summary>
public sealed class CompanionOperationCoordinator(TimeProvider clock)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private int _foregroundCount;
    private long _lastForeground = clock.GetTimestamp();
    private CancellationTokenSource? _generation;

    public bool ForegroundBusy => Volatile.Read(ref _foregroundCount) > 0;
    public TimeSpan IdleFor => clock.GetElapsedTime(Interlocked.Read(ref _lastForeground));

    public async Task<IDisposable> EnterStateAsync(CancellationToken token)
    {
        await _gate.WaitAsync(token);
        return new Scope(() => _gate.Release());
    }

    public async Task<IDisposable> EnterForegroundAsync(CancellationToken token)
    {
        using var state = await EnterStateAsync(token);
        Invalidate();
        Interlocked.Increment(ref _foregroundCount);
        return new Scope(() =>
        {
            Interlocked.Exchange(ref _lastForeground, clock.GetTimestamp());
            Interlocked.Decrement(ref _foregroundCount);
        });
    }

    // These three methods are called only inside EnterStateAsync's short critical section.
    public void Invalidate()
    {
        Interlocked.Exchange(ref _lastForeground, clock.GetTimestamp());
        _generation?.Cancel();
    }

    public CancellationTokenSource BeginGeneration(CancellationToken token)
    {
        if (_generation is not null) throw new InvalidOperationException("A proactive generation is already running.");
        return _generation = CancellationTokenSource.CreateLinkedTokenSource(token);
    }

    public void EndGeneration(CancellationTokenSource source)
    {
        if (ReferenceEquals(_generation, source)) _generation = null;
        source.Dispose();
    }

    private sealed class Scope(Action release) : IDisposable
    {
        private Action? _release = release;
        public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();
    }
}

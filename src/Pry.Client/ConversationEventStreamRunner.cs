using System.IO;
using Pry.Contracts;

namespace Pry.Client;

public enum PryStreamState
{
    Connected,
    Reconnecting,
    Offline
}

public sealed record ConversationStreamOptions(
    TimeSpan FirstDelay,
    TimeSpan MaximumDelay,
    int MaximumConsecutiveFailures)
{
    public static ConversationStreamOptions Default { get; } = new(
        TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(15), 8);

    public TimeSpan DelayForAttempt(int attempt)
    {
        var delay = FirstDelay;
        for (var index = 1; index < attempt && delay < MaximumDelay; index++)
        {
            var doubled = delay * 2;
            delay = doubled <= TimeSpan.Zero || doubled > MaximumDelay ? MaximumDelay : doubled;
        }
        return delay;
    }
}

/// <summary>
/// 传输韧性属于协议客户端：退避与按事件序号续传在此处完成，界面只消费连接状态。
/// 这里是全仓库唯一读 <see cref="PryBackendException.RetryForbidden"/> 的地方——禁令先生效在退避之前，
/// 两处消费点各写一份条件就等于给"漏掉一侧"留了位置（QA-P4-02）。
/// </summary>
public sealed class ConversationEventStreamRunner
{
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly Func<long, CancellationToken, IAsyncEnumerable<ConversationEvent>> _open;
    private int _consecutiveFailures;

    public ConversationEventStreamRunner(
        Func<long, CancellationToken, IAsyncEnumerable<ConversationEvent>> open,
        ConversationStreamOptions? options = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _open = open ?? throw new ArgumentNullException(nameof(open));
        _delay = delay ?? Task.Delay;
        Options = options ?? ConversationStreamOptions.Default;
    }

    public ConversationStreamOptions Options { get; }
    public long LastSequence { get; private set; } = -1;
    public PryStreamState State { get; private set; } = PryStreamState.Reconnecting;
    public bool HasConnected { get; private set; }

    /// <summary>按投递顺序等待：处理完一条才读取下一条，避免界面并发重读历史。</summary>
    public Func<ConversationEvent, Task>? EventReceived;

    public Action<PryStreamState>? StateChanged;

    /// <summary>重连后的第一条事件之前等待一次；断线期间可能已有消息落库，调用方应据此补取历史。</summary>
    public Func<Task>? Resumed;

    /// <summary>放弃重连时唯一的出口：连续失败到上限，或服务端写明重试不会变好（<see cref="PryBackendException.RetryForbidden"/>）。</summary>
    public Action<Exception>? Faulted;

    public async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var isRecovery = HasConnected;
            var progressed = false;
            try
            {
                await foreach (var item in _open(LastSequence, token).ConfigureAwait(false))
                {
                    if (!progressed)
                    {
                        progressed = true;
                        State = PryStreamState.Connected;
                        StateChanged?.Invoke(PryStreamState.Connected);
                        HasConnected = true;
                        if (isRecovery && Resumed is { } backfill) await backfill().ConfigureAwait(false);
                    }
                    LastSequence = Math.Max(LastSequence, item.Sequence);
                    _consecutiveFailures = 0;
                    if (EventReceived is { } deliver) await deliver(item).ConfigureAwait(false);
                }
                // 收到过事件后正常结束：立即从断点续订；一个事件都没收到则按失败退避，避免空转。
                if (!progressed && !await BackoffAsync(new IOException("事件流在收到任何事件前结束。"), token))
                    return;
            }
            catch (Exception error) when (error is OperationCanceledException && token.IsCancellationRequested)
            {
                return;
            }
            catch (Exception error)
            {
                if (!await BackoffAsync(error, token)) return;
            }
        }
    }

    /// <summary>返回 false 表示不再重连（禁令或失败上限），调用方据此退出循环。</summary>
    private async Task<bool> BackoffAsync(Exception error, CancellationToken token)
    {
        // 顺序即判据：禁令必须在计数与退避之前，否则"永不重试"会被演成"再等 8 轮"。
        if (error is PryBackendException { RetryForbidden: true })
        {
            State = PryStreamState.Offline;
            StateChanged?.Invoke(PryStreamState.Offline);
            Faulted?.Invoke(error);
            return false;
        }
        _consecutiveFailures++;
        if (_consecutiveFailures >= Options.MaximumConsecutiveFailures)
        {
            State = PryStreamState.Offline;
            StateChanged?.Invoke(PryStreamState.Offline);
            Faulted?.Invoke(error);
            return false;
        }
        State = PryStreamState.Reconnecting;
        StateChanged?.Invoke(PryStreamState.Reconnecting);
        await _delay(Options.DelayForAttempt(_consecutiveFailures), token).ConfigureAwait(false);
        return !token.IsCancellationRequested;
    }
}

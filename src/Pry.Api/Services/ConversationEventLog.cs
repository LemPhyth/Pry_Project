using System.Runtime.CompilerServices;
using Pry.Api.Contracts;

namespace Pry.Api.Services;

public sealed class ConversationEventLog
{
    private readonly object _gate = new();
    private readonly List<ConversationEvent> _events = [];
    private TaskCompletionSource _changed = NewSignal();
    private long _sequence;

    public ConversationEvent Publish(string type, object data)
    {
        lock (_gate)
        {
            var item = new ConversationEvent(++_sequence, type, DateTimeOffset.UtcNow, data);
            _events.Add(item);
            if (_events.Count > 200) _events.RemoveRange(0, _events.Count - 200);
            var changed = _changed; _changed = NewSignal(); changed.TrySetResult();
            return item;
        }
    }

    public async IAsyncEnumerable<ConversationEvent> ReadAsync(long afterSequence,
        [EnumeratorCancellation] CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            ConversationEvent[] available; Task wait;
            lock (_gate)
            {
                available = _events.Where(x => x.Sequence > afterSequence).ToArray();
                wait = _changed.Task;
            }
            if (available.Length == 0) { await wait.WaitAsync(token); continue; }
            foreach (var item in available) { afterSequence = item.Sequence; yield return item; }
        }
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}

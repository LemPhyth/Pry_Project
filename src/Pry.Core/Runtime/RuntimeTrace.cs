using System.Text;

namespace Pry.Core.Runtime;

/// <summary>
/// Runtime 决策轨迹。Developer Mode 与 Timing Eval 都读这一份，而不是各自再埋一遍点。
/// 只记录状态、ID、决策与原因，不记录聊天正文（默认隐私边界）。
/// </summary>
public sealed class RuntimeTrace
{
    private readonly int _capacity;
    private readonly List<RuntimeTraceEntry> _entries = [];
    private readonly object _gate = new();
    private DateTimeOffset? _firstAt;
    private int _sequence;

    public RuntimeTrace(int capacity) => _capacity = Math.Max(20, capacity);

    public RuntimeTraceEntry Record(RuntimeEventKind? via, RuntimeTraceKind kind, string action, TurnIdentity? turn,
        string reason, int revisionBefore, int revisionAfter, DateTimeOffset at)
    {
        lock (_gate)
        {
            _firstAt ??= at;
            var entry = new RuntimeTraceEntry(++_sequence, via, kind, action, turn, reason, revisionBefore,
                revisionAfter, at);
            _entries.Add(entry);
            if (_entries.Count > _capacity) _entries.RemoveRange(0, _entries.Count - _capacity);
            return entry;
        }
    }

    public int LastSequence { get { lock (_gate) return _sequence; } }

    public IReadOnlyList<RuntimeTraceEntry> Entries()
    {
        lock (_gate) return _entries.ToArray();
    }

    public IReadOnlyList<RuntimeTraceEntry> Since(int sequence)
    {
        lock (_gate) return _entries.Where(x => x.Sequence > sequence).ToArray();
    }

    /// <summary>相对时间轴，供报告与 Eval 快照使用。</summary>
    public string Render()
    {
        IReadOnlyList<RuntimeTraceEntry> snapshot;
        DateTimeOffset start;
        lock (_gate)
        {
            snapshot = _entries.ToArray();
            start = _firstAt ?? DateTimeOffset.UtcNow;
        }
        var builder = new StringBuilder();
        foreach (var entry in snapshot)
        {
            var offset = (int)(entry.At - start).TotalMilliseconds;
            _ = builder.Append($"t+{offset}ms rev{entry.RevisionBefore}→{entry.RevisionAfter} [{entry.Kind}] {entry.Action}");
            if (entry.Event is { } via) _ = builder.Append($" event={via}");
            if (entry.Turn is { } turn && !turn.IsNone) _ = builder.Append($" turn={turn.TurnId}.g{turn.GenerationId} fence={turn.FencedRevision}");
            if (!string.IsNullOrWhiteSpace(entry.Reason)) _ = builder.Append($" :: {entry.Reason}");
            _ = builder.AppendLine();
        }
        return builder.ToString();
    }
}

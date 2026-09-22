namespace Pry.Core.Runtime;

/// <summary>缓冲中的单条输入，附带到达当时的启发结果。</summary>
public sealed record BufferedInput(RuntimeUserInput Input, DateTimeOffset ArrivedAt, long? ExistingMessageId,
    bool ForceTurn)
{
    public bool LooksIncomplete { get; init; }
    public bool AsksQuestion { get; init; }
    public bool AcknowledgementOnly { get; init; }

    /// <summary>以逗号/连接词收尾且没有问号：像说到一半。</summary>
    public bool TrailingSentence => LooksIncomplete && !AsksQuestion;

    /// <summary>
    /// 由某个正在生成的 Turn "顺带听见"的轻量插话（背书式回应）。
    /// 那一轮的回复已经覆盖它，不该再为它单独花一次模型调用。
    /// </summary>
    public bool AbsorbedByReply { get; set; }
}

/// <summary>缓冲是否已经可以形成一个 Turn，以及下一次值得重新检查的时间。</summary>
public sealed record BufferAssessment(bool Complete, TimeSpan Remaining, string Reason)
{
    public static BufferAssessment Empty { get; } = new(false, TimeSpan.MaxValue, "buffer_empty");
}

/// <summary>
/// PendingInputQueue + InputBuffer：回答"用户还在说话吗"。
/// 依据时间间隔、typing 状态、标点/半句启发、是否强制立即，以及硬上限避免饿死。
/// </summary>
public sealed class InputBuffer
{
    private readonly List<BufferedInput> _items = [];
    private readonly RuntimeSettings _settings;

    public InputBuffer(RuntimeSettings settings) => _settings = settings;

    public IReadOnlyList<BufferedInput> Items => _items;
    public int Count => _items.Count;
    public bool UserTyping { get; private set; }
    public DateTimeOffset? TypingStartedAt { get; private set; }
    public DateTimeOffset? FirstArrivedAt { get; private set; }
    public DateTimeOffset? LastArrivedAt { get; private set; }
    public bool HasForcedInput { get; private set; }

    public BufferedInput Add(RuntimeUserInput input, DateTimeOffset at, bool forceTurn = false,
        long? existingMessageId = null)
    {
        var buffered = new BufferedInput(input, at, existingMessageId, forceTurn)
        {
            LooksIncomplete = InputProbe.LooksIncomplete(input.Text),
            AsksQuestion = InputProbe.AsksQuestion(input.Text),
            AcknowledgementOnly = InputProbe.IsLightweightAcknowledgement(input.Text, input.StickerId)
        };
        _items.Add(buffered);
        FirstArrivedAt ??= at;
        LastArrivedAt = at;
        if (forceTurn) HasForcedInput = true;
        return buffered;
    }

    /// <summary>把一条已落库的后继输入退回缓冲（例如打断裁决判定为合并）。</summary>
    public void InsertAt(int index, BufferedInput input)
    {
        _items.Insert(Math.Clamp(index, 0, _items.Count), input);
        FirstArrivedAt = _items[0].ArrivedAt;
        LastArrivedAt = _items[^1].ArrivedAt;
        HasForcedInput |= input.ForceTurn;
    }

    public bool Remove(BufferedInput input)
    {
        var removed = _items.Remove(input);
        if (removed) RefreshDerivedState();
        return removed;
    }

    public IReadOnlyList<BufferedInput> Drain()
    {
        var drained = _items.ToArray();
        _items.Clear();
        RefreshDerivedState();
        return drained;
    }

    /// <summary>丢掉已被上一轮回复听见的轻量插话，返回丢掉条数。</summary>
    public int RemoveAbsorbed()
    {
        var doomed = _items.Where(x => x.AbsorbedByReply).ToArray();
        foreach (var item in doomed) _items.Remove(item);
        if (doomed.Length > 0) RefreshDerivedState();
        return doomed.Length;
    }

    public void Clear()
    {
        _items.Clear();
        RefreshDerivedState();
    }

    public void NotifyTyping(bool active, DateTimeOffset at)
    {
        // A fresh typing report renews the lease; a lost stop event still expires.
        UserTyping = active;
        TypingStartedAt = active ? at : null;
        // 停止打字本身也是一次活动： debounce 从"用户最后一次做任何事"起算，
        // 否则角色会在用户刚停手的一瞬间抢话。
        if (!active && at > LastArrivedAt) LastArrivedAt = at;
    }

    /// <summary>
    /// typing 标志会自行过期：客户端崩溃或断线时不能把"正在输入"永远挂住而让 Turn 永不形成。
    /// </summary>
    public bool IsTypingAt(DateTimeOffset at) =>
        UserTyping && TypingStartedAt is { } started && at - started < TimeSpan.FromMilliseconds(_settings.TypingHoldMs);

    /// <summary>
    /// backpressure 为真表示仍有输出在投递，此时倾向让用户的话攒齐再一次处理，
    /// 而不是抢在半个气泡之间插入新一轮规划。
    /// </summary>
    public BufferAssessment Assess(DateTimeOffset at, bool backpressure = false)
    {
        if (_items.Count == 0) return BufferAssessment.Empty;
        if (HasForcedInput) return new BufferAssessment(true, TimeSpan.Zero, "forced_immediate");

        var first = FirstArrivedAt!.Value;
        var last = LastArrivedAt!.Value;
        var readyAt = last + TimeSpan.FromMilliseconds(_settings.DebounceMs * (backpressure ? 2 : 1));

        if (UserTyping)
        {
            var typingCeiling = (TypingStartedAt ?? last) + TimeSpan.FromMilliseconds(_settings.TypingHoldMs);
            if (at < typingCeiling) readyAt = typingCeiling;
        }

        if (_items[^1].TrailingSentence)
            readyAt = Max(readyAt, last + TimeSpan.FromMilliseconds(_settings.IncompleteHoldMs));

        // 硬上限优先：无论启发结果如何，缓冲不会超过 MaxBufferMs，避免连续输入把 Turn 无限推后。
        var starveGuard = first + TimeSpan.FromMilliseconds(_settings.MaxBufferMs);
        var deadline = Min(readyAt, starveGuard);
        var remaining = deadline - at;
        if (remaining > TimeSpan.Zero) return new BufferAssessment(false, remaining, "waiting");
        if (starveGuard <= readyAt) return new BufferAssessment(true, TimeSpan.Zero, "buffer_deadline");
        if (UserTyping && at >= (TypingStartedAt ?? last) + TimeSpan.FromMilliseconds(_settings.TypingHoldMs))
            return new BufferAssessment(true, TimeSpan.Zero, "typing_hold_expired");
        return new BufferAssessment(true, TimeSpan.Zero, "silence_elapsed");
    }

    public IReadOnlyList<RuntimeUserInput> Inputs() => _items.Select(x => x.Input).ToArray();

    public string Summary() => InputProbe.Summarize(Inputs());

    private void RefreshDerivedState()
    {
        FirstArrivedAt = _items.Count == 0 ? null : _items[0].ArrivedAt;
        LastArrivedAt = _items.Count == 0 ? null : _items[^1].ArrivedAt;
        HasForcedInput = _items.Any(x => x.ForceTurn);
    }

    private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;
    private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;
}

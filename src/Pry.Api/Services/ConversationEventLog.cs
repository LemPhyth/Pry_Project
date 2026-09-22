using System.Runtime.CompilerServices;
using System.Text.Json;
using Pry.Contracts;
using Pry.Core.Memory;

namespace Pry.Api.Services;

/// <summary>
/// 一个会话的事件流。可重放的那部分存在 SQLite 的 protocol_events 里，序号由那张表的
/// AUTOINCREMENT 唯一分配，所以序号跨会话全局单调、同一会话内出现空洞是正常状态，重启也不归零。
/// 本类的持久内容只有一个游标缓存，事件真相源始终在数据库；内存里保留的是"到达顺序"，
/// 它决定电平帧与持久事件的相对次序，不决定事件是否存在。
/// </summary>
public sealed class ConversationEventLog(MemoryDatabase database, string conversationId, ILogger logger)
{
    /// <summary>
    /// 到达队列容量。挤掉的一定是队首（最旧的到达项），而队首早被在线订阅者消费过；
    /// 丢锚点的后果只是相对次序退化为"先持久后电平"，事件本身仍会从数据库补齐。
    /// </summary>
    private const int ArrivalCapacity = 64;

    private readonly object _gate = new();
    private readonly Queue<Arrival> _arrivals = new();
    private TaskCompletionSource _changed = NewSignal();
    private long _sequence;
    private long _tick;

    /// <summary>本进程已知的最新持久序号；未落库的电平事件不计入。</summary>
    public long CurrentSequence
    {
        get { lock (_gate) return _sequence; }
    }

    public ConversationEvent Publish(string type, object data)
    {
        var occurredAt = DateTimeOffset.UtcNow;
        if (!MemoryDatabase.IsDurableEvent(type))
        {
            lock (_gate)
            {
                // 电平帧携带"发布时已存在的持久头部"作为锚点：它必须排在更早的事实之后。
                _arrivals.Enqueue(new Arrival(++_tick, type, data, occurredAt, _sequence, IsAnchor: false));
                Trim();
                Signal();
            }
            // 电平类没有持久序号，帧的 id 由读取方按当时的持久头部补，所以这里返回 0 是有意的。
            return new(0, type, occurredAt, data);
        }

        long sequence;
        try
        {
            // 先落库再对外可见：游标因此不可能指向一条没写成的事件，也就不会出现无人宣告的洞。
            // 这里刻意同步等待本地 SQLite（亚毫秒），改成异步会让每个发布点连同 Phase 2 Owner
            // 未提交的文件一起改签名，越界。
            sequence = database.AppendProtocolEventAsync(conversationId, type,
                JsonSerializer.Serialize(data, JsonOptions)).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            // 写不成即不曾存在：客户端从没收到过它，所以不是留洞，只是本层的一次降级，必须留日志。
            logger.LogWarning(ex, "协议事件 {Type} 未能写入会话 {ConversationId} 的持久日志", type, conversationId);
            return new(0, type, occurredAt, data);
        }

        lock (_gate)
        {
            if (sequence > _sequence) _sequence = sequence;
            // 持久事件只入队一个锚点（Data 为空），内容一律由读取方从数据库取。
            _arrivals.Enqueue(new Arrival(++_tick, type, null, occurredAt, sequence, IsAnchor: true));
            Trim();
            Signal();
        }
        return new(sequence, type, occurredAt, data);
    }

    public async IAsyncEnumerable<ConversationEvent> ReadAsync(long afterSequence,
        [EnumeratorCancellation] CancellationToken token)
    {
        long lastTick;
        lock (_gate) lastTick = _tick;
        // 订阅方先于本进程任何持久发布时，头部必须从数据库取回，否则锚点会把历史挡在电平帧后面。
        var window = await database.GetProtocolEventWindowAsync(conversationId, token);
        lock (_gate) { if (window.GlobalHead > _sequence) _sequence = window.GlobalHead; }
        var cursor = afterSequence < 0 ? window.GlobalHead : afterSequence;

        while (true)
        {
            Arrival? next;
            lock (_gate)
            {
                next = null;
                foreach (var item in _arrivals)
                    if (item.Tick > lastTick) { next = item; break; }
            }

            if (next is null)
            {
                // 先取唤醒信号再读历史：顺序反过来时，两者之间落库的事件既不在这一批里、
                // 也不会再触发这一轮的等待，订阅者就会永远睡过去（本地写入越快窗口越窄，越难复现）。
                Task wait;
                lock (_gate) wait = _changed.Task;
                var backlog = await database.ReadProtocolEventsAsync(conversationId, cursor, 1000, token);
                if (backlog.Count > 0)
                {
                    foreach (var row in backlog)
                    {
                        cursor = row.Sequence;
                        yield return ToEvent(row);
                    }
                    continue;
                }
                // 取消必须原样传播：把它吞成正常结束，上层就分不清"我取消了"与"服务端关流"，
                // 而这两条路径分别对应重连退避与正常收尾。
                await wait.WaitAsync(token);
                continue;
            }

            // 先补齐排在这个到达项之前的持久事件，才轮到它自己——两种相邻顺序都会真实出现：
            // turn.state(AgentSending) → message.created 与 message.created → turn.state(Idle)。
            var anchored = next.Sequence;
            while (true)
            {
                var rows = await database.ReadProtocolEventsAsync(conversationId, cursor, 1000, token);
                var held = false;
                foreach (var row in rows)
                {
                    if (row.Sequence > anchored) { held = true; break; }
                    cursor = row.Sequence;
                    yield return ToEvent(row);
                }
                // 一批 1000 条没读完时不消费锚点，下一轮继续补投。
                if (held || rows.Count == 0 || rows.Count < 1000) break;
            }

            lastTick = next.Tick;
            if (next.IsAnchor) continue; // 锚点只是顺序凭据，不是事件
            long head;
            lock (_gate) head = Math.Max(_sequence, cursor);
            // 电平类的 id 复用当前持久头部，因此它不推进游标，帧格式与事实类完全一致。
            yield return new ConversationEvent(head, next.Type, next.OccurredAt, next.Data!);
        }
    }

    private ConversationEvent ToEvent(ProtocolEventRecord row)
    {
        lock (_gate) { if (row.Sequence > _sequence) _sequence = row.Sequence; }
        return new ConversationEvent(row.Sequence, row.Type, row.OccurredAt, ParsePayload(row.PayloadJson));
    }

    private static object ParsePayload(string payloadJson) =>
        JsonDocument.Parse(payloadJson).RootElement.Clone();

    private void Trim()
    {
        while (_arrivals.Count > ArrivalCapacity) _arrivals.Dequeue();
    }

    private void Signal()
    {
        var changed = _changed;
        _changed = NewSignal();
        changed.TrySetResult();
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// 一次发布的到达记录。<see cref="Sequence"/> 是"必须排在本条之前的持久事件上界"：
    /// 事实类就是它自己的序号，电平类是发布时已存在的持久头部。
    /// </summary>
    private sealed record Arrival(long Tick, string Type, object? Data, DateTimeOffset OccurredAt, long Sequence,
        bool IsAnchor);
}

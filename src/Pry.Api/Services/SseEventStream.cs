using System.Runtime.CompilerServices;
using System.Text.Json;
using Pry.Contracts;

namespace Pry.Api.Services;

/// <summary>
/// 把会话事件写成 SSE 帧，并在空闲满一个心跳周期时下发注释帧。
/// 半开的连接不会抛错，只会永远没有事件；没有注释帧，客户端就无法区分"会话安静"和"连接已死"。
/// 注释帧不占游标，也不是事件，所以只能在这一层产生，不进入 ConversationEventLog。
/// </summary>
public static class SseEventStream
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async IAsyncEnumerable<string> FramesAsync(IAsyncEnumerable<ConversationEvent> events,
        TimeSpan heartbeat, [EnumeratorCancellation] CancellationToken token)
    {
        var cursor = events.GetAsyncEnumerator(token);
        // 编译器生成的异步流在自己那笔 MoveNextAsync 结束前被释放是固定抛 NotSupportedException 的非法状态，
        // 而心跳能递出就说明那笔任务还在飞，所以每一条离开路径都要先把它结算掉再 Dispose（见 #27/DEF-P1WALK-03）。
        // 代价是对内层的一条要求：它必须认这个 token，否则"等到它自己结束"没有上界。
        Task<bool>? inFlight = null;
        try
        {
            while (true)
            {
                // MoveNextAsync 不能被取消后复用，所以这里只等它，超时时改写下一条心跳，游标保持原位。
                var moved = cursor.MoveNextAsync().AsTask();
                inFlight = moved;
                var idle = Task.Delay(heartbeat, token);
                while (!moved.IsCompleted)
                {
                    await Task.WhenAny(moved, idle);
                    if (moved.IsCompleted) break;
                    token.ThrowIfCancellationRequested();
                    yield return PryProtocol.HeartbeatFrame;
                    idle = Task.Delay(heartbeat, token);
                }

                inFlight = null;
                if (!await moved) yield break;
                yield return Format(cursor.Current);
            }
        }
        finally
        {
            if (inFlight is { } pending)
            {
                // 只等它结束，不取它的结果：离开原因已经由上面抛出，Dispose 路径再抛一次只会把它顶掉。
                _ = await Task.WhenAny(pending);
            }

            await cursor.DisposeAsync();
        }
    }

    private static string Format(ConversationEvent item) =>
        $"id: {item.Sequence}\nevent: {item.Type}\ndata: {JsonSerializer.Serialize(item, JsonOptions)}\n\n";
}

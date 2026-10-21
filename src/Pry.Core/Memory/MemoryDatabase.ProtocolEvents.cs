using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Pry.Core.Memory;

/// <summary>
/// 协议事件日志（PLAT-005 M0-c）。这张表的唯一读者是传输层：它回答"客户端该从哪一格继续"，
/// 不回答"这个会话说过什么"——后者永远以 messages 为准。
/// </summary>
public sealed partial class MemoryDatabase
{
    /// <summary>
    /// 电平触发类事件：表达"现在处于哪"，不是"发生过什么"。重放一条旧的 ModelThinking 会把界面
    /// 倒回旧状态，所以它们不进日志。分箱按黑名单走，新事件类型默认可重放——
    /// 漏放一条事实比多放一次状态的代价高得多。
    /// </summary>
    private static readonly string[] VolatileEventTypes = ["session.ready", "turn.state"];

    public static bool IsDurableEvent(string type) => !VolatileEventTypes.Contains(type, StringComparer.Ordinal);

    private static async Task InitializeProtocolEventsAsync(SqliteConnection connection, CancellationToken token,
        SqliteTransaction? transaction = null)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS protocol_events(
              sequence INTEGER PRIMARY KEY AUTOINCREMENT,
              conversation_id TEXT NOT NULL,
              type TEXT NOT NULL,
              payload_json TEXT NOT NULL,
              created_at TEXT NOT NULL,
              subject_message_id INTEGER NULL,
              FOREIGN KEY(conversation_id) REFERENCES conversations(id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ix_protocol_events_conversation ON protocol_events(conversation_id, sequence);
            CREATE INDEX IF NOT EXISTS ix_protocol_events_created ON protocol_events(created_at);
            """;
        await command.ExecuteNonQueryAsync(token);
    }

    /// <summary>追加一条可重放事件，返回分配到的全局序号。序号只在这里产生，别处不得自增。</summary>
    public async Task<long> AppendProtocolEventAsync(string conversationId, string type, string payloadJson,
        CancellationToken token = default)
    {
        if (!IsDurableEvent(type))
            throw new InvalidOperationException($"事件 {type} 是电平触发类，不得写入协议日志。");
        await using var connection = await OpenAsync(token);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(token);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO protocol_events(conversation_id,type,payload_json,created_at,subject_message_id)
            VALUES($c,$t,$p,$at,$subject); SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$c", conversationId);
        command.Parameters.AddWithValue("$t", type);
        command.Parameters.AddWithValue("$p", payloadJson);
        command.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$subject", (object?)ReadSubjectMessageId(payloadJson) ?? DBNull.Value);
        var sequence = Convert.ToInt64(await command.ExecuteScalarAsync(token));
        await transaction.CommitAsync(token);
        return sequence;
    }

    /// <summary>按游标补投：只返回本会话、序号严格大于游标的行，按序号升序。</summary>
    public async Task<IReadOnlyList<ProtocolEventRecord>> ReadProtocolEventsAsync(string conversationId,
        long afterSequence, int limit, CancellationToken token = default)
    {
        var result = new List<ProtocolEventRecord>();
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT sequence,type,payload_json,created_at FROM protocol_events
            WHERE conversation_id=$c AND sequence>$after ORDER BY sequence LIMIT $limit
            """;
        command.Parameters.AddWithValue("$c", conversationId);
        command.Parameters.AddWithValue("$after", afterSequence);
        command.Parameters.AddWithValue("$limit", Math.Clamp(limit, 1, 1000));
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
            result.Add(new(reader.GetInt64(0), reader.GetString(1), reader.GetString(2),
                DateTimeOffset.Parse(reader.GetString(3))));
        return result;
    }

    /// <summary>
     /// 本会话的留存窗口。<paramref name="firstRetained"/> 为空表示从未有过事件，
     /// 此时客户端带任何游标都不算越界；全局头部序号用于"只收新事件"的订阅起点。
    /// </summary>
    public async Task<ProtocolEventWindow> GetProtocolEventWindowAsync(string conversationId,
        CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT (SELECT MIN(sequence) FROM protocol_events WHERE conversation_id=$c),
                   (SELECT MAX(sequence) FROM protocol_events WHERE conversation_id=$c),
                   (SELECT COALESCE(MAX(sequence),0) FROM protocol_events)
            """;
        command.Parameters.AddWithValue("$c", conversationId);
        await using var reader = await command.ExecuteReaderAsync(token);
        await reader.ReadAsync(token);
        return new(
            reader.IsDBNull(0) ? null : reader.GetInt64(0),
            reader.IsDBNull(1) ? null : reader.GetInt64(1),
            reader.GetInt64(2));
    }

    /// <summary>
    /// 按每会话条数 → 年龄 → 总行数的顺序裁剪。配置区间与下界夹取属传输层的策略
    /// （见 Pry.Api 的 ProtocolEventRetention），本层只把无意义值兜到 1，不在此重复那份区间。
    /// </summary>
    public async Task<int> PruneProtocolEventsAsync(int maxRetainedPerConversation, int maxAgeDays,
        int maxTotalRows, CancellationToken token = default)
    {
        var keep = Math.Max(1, maxRetainedPerConversation);
        var age = Math.Max(1, maxAgeDays);
        var total = Math.Max(1, maxTotalRows);
        await using var connection = await OpenAsync(token);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(token);
        var removed = 0;
        // 顺序即优先级：先削每会话超额，再按年龄全局扫，最后受总行数约束。
        // 用窗口函数按会话分组取"最新 keep 条之外"的行——相关子查询里带 LIMIT 在 SQLite 不受支持。
        foreach (var sql in new[]
        {
            $"""
            DELETE FROM protocol_events WHERE sequence IN (
              SELECT sequence FROM (
                SELECT sequence, ROW_NUMBER() OVER (PARTITION BY conversation_id ORDER BY sequence DESC) AS rank
                FROM protocol_events) WHERE rank > {keep})
            """,
            "DELETE FROM protocol_events WHERE created_at<$cutoff",
            $"""
            DELETE FROM protocol_events WHERE sequence IN (
              SELECT sequence FROM (
                SELECT sequence, ROW_NUMBER() OVER (ORDER BY sequence DESC) AS rank FROM protocol_events)
              WHERE rank > {total})
            """
        })
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            if (sql.Contains("$cutoff", StringComparison.Ordinal))
                command.Parameters.AddWithValue("$cutoff", DateTimeOffset.UtcNow.AddDays(-age).ToString("O"));
            removed += await command.ExecuteNonQueryAsync(token);
        }
        await transaction.CommitAsync(token);
        return removed;
    }

    /// <summary>裁剪/清理只动这张表：领域历史逐字节不变是本层的读边界，写成断言测试守住。</summary>
    public async Task<long> CountProtocolEventsAsync(string? conversationId = null, CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = conversationId is null
            ? "SELECT COUNT(*) FROM protocol_events"
            : "SELECT COUNT(*) FROM protocol_events WHERE conversation_id=$c";
        if (conversationId is not null) command.Parameters.AddWithValue("$c", conversationId);
        return Convert.ToInt64(await command.ExecuteScalarAsync(token) ?? 0L);
    }

    /// <summary>
     /// 删除消息时遮蔽其事件行，必须在删除事务内调用：否则"删除消息"会在传输层留下一份明文副本。
     /// </summary>
    private static async Task MaskProtocolEventsAsync(SqliteConnection connection, SqliteTransaction transaction,
        string conversationId, string comparison, long messageId, CancellationToken token)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            DELETE FROM protocol_events
            WHERE conversation_id=$c AND subject_message_id IN
              (SELECT id FROM messages WHERE conversation_id=$c AND id{comparison}$id)
            """;
        command.Parameters.AddWithValue("$c", conversationId);
        command.Parameters.AddWithValue("$id", messageId);
        await command.ExecuteNonQueryAsync(token);
    }

    /// <summary>
     /// 遮蔽范围由 payload 里的 messageId 决定，而不是由发布方自觉——发布点分散在多个所有者手里，
     /// 任何一处忘记传参就会让删除语义留洞。
    /// </summary>
    private static long? ReadSubjectMessageId(string payloadJson)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("messageId", out var messageId)) return null;
            return messageId.ValueKind switch
            {
                JsonValueKind.Number => messageId.GetInt64(),
                JsonValueKind.String => long.TryParse(messageId.GetString(), out var parsed) ? parsed : null,
                _ => null
            };
        }
        catch (JsonException) { return null; }
    }
}

/// <summary>一条已持久化的协议事件。载荷保持原样 JSON，由传输层解析，Core 不理解其语义。</summary>
public sealed record ProtocolEventRecord(long Sequence, string Type, string PayloadJson, DateTimeOffset OccurredAt);

/// <summary>本会话的留存窗口与全局头部序号。</summary>
public sealed record ProtocolEventWindow(long? FirstRetained, long? Latest, long GlobalHead);

/// <summary>
/// 协议日志的保留区间。数值语义见 docs/protocol-versioning-and-websocket-migration.md §4.1 第 4 条：
/// 只走环境变量/测试注入，不进 appsettings.json；下界夹取是硬要求。
/// </summary>
public static class ProtocolEventRetentionPolicy
{
    public const int DefaultPerConversation = 500;
    public const int MinPerConversation = 50;
    public const int MaxPerConversationLimit = 5_000;
    public const int DefaultMaxAgeDays = 7;
    public const int DefaultTotalRows = 20_000;
    public const int MinTotalRows = 500;
    public const int MaxTotalRowsLimit = 200_000;
}

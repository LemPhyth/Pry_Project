using Microsoft.Data.Sqlite;
using Pry.Core.Models;

namespace Pry.Core.Memory;

public sealed partial class MemoryDatabase
{
    private static async Task InitializeReadStateAsync(SqliteConnection connection, CancellationToken token,
        SqliteTransaction? transaction = null)
    {
        await EnsureColumnAsync(connection, "conversations", "last_read_message_id", "INTEGER NOT NULL DEFAULT 0 CHECK(last_read_message_id >= 0)", token, transaction);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "CREATE INDEX IF NOT EXISTS ix_messages_conversation_role_id ON messages(conversation_id,role,id)";
        await command.ExecuteNonQueryAsync(token);
    }

    public async Task<ConversationReadState?> GetConversationReadStateAsync(string conversationId, CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        return await ReadConversationReadStateAsync(connection, null, conversationId, token);
    }

    private static async Task<ConversationReadState?> ReadConversationReadStateAsync(SqliteConnection connection,
        SqliteTransaction? transaction, string conversationId, CancellationToken token)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT c.last_read_message_id,
              (SELECT COUNT(*) FROM messages m WHERE m.conversation_id=c.id AND m.role='assistant' AND m.id>c.last_read_message_id)
            FROM conversations c WHERE c.id=$id
            """;
        command.Parameters.AddWithValue("$id", conversationId);
        await using var reader = await command.ExecuteReaderAsync(token);
        return await reader.ReadAsync(token) ? new(conversationId, reader.GetInt64(0), reader.GetInt64(1)) : null;
    }

    public async Task<MarkConversationReadResult?> MarkConversationReadAsync(string conversationId, long throughMessageId,
        CancellationToken token = default)
    {
        if (throughMessageId < 0) throw new ArgumentOutOfRangeException(nameof(throughMessageId));
        await using var connection = await OpenAsync(token);
        await using var transaction = connection.BeginTransaction(deferred: false);
        var current = await ReadConversationReadStateAsync(connection, transaction, conversationId, token);
        if (current is null) return null;
        if (throughMessageId <= current.LastReadMessageId) return new(current, false);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM messages WHERE id=$message AND conversation_id=$id)";
        command.Parameters.AddWithValue("$id", conversationId); command.Parameters.AddWithValue("$message", throughMessageId);
        if (Convert.ToInt64(await command.ExecuteScalarAsync(token)) != 1) throw new InvalidReadWatermarkException();
        command.CommandText = "UPDATE conversations SET last_read_message_id=$message WHERE id=$id";
        await command.ExecuteNonQueryAsync(token);
        var updated = (await ReadConversationReadStateAsync(connection, transaction, conversationId, token))!;
        await transaction.CommitAsync(token);
        return new(updated, true);
    }

    public async Task<UnreadSummary> GetUnreadSummaryAsync(CancellationToken token = default)
    {
        await using var connection = await OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COALESCE(SUM(unread_count),0), COALESCE(SUM(CASE WHEN unread_count>0 THEN 1 ELSE 0 END),0)
            FROM (SELECT (SELECT COUNT(*) FROM messages m WHERE m.conversation_id=c.id
                         AND m.role='assistant' AND m.id>c.last_read_message_id) AS unread_count FROM conversations c)
            """;
        await using var reader = await command.ExecuteReaderAsync(token);
        await reader.ReadAsync(token);
        return new(reader.GetInt64(0), reader.GetInt64(1));
    }
}

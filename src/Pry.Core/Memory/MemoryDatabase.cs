using Microsoft.Data.Sqlite;
using Pry.Core.Models;

namespace Pry.Core.Memory;

public sealed class MemoryDatabase(string databasePath)
{
    private readonly string _connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = databasePath,
        Mode = SqliteOpenMode.ReadWriteCreate,
        Cache = SqliteCacheMode.Shared
    }.ToString();

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(databasePath))!);
        await using var connection = await OpenAsync(cancellationToken);
        var sql = """
            PRAGMA journal_mode=WAL;
            PRAGMA foreign_keys=ON;
            CREATE TABLE IF NOT EXISTS schema_info(version INTEGER NOT NULL);
            INSERT INTO schema_info(version) SELECT 1 WHERE NOT EXISTS(SELECT 1 FROM schema_info);
            CREATE TABLE IF NOT EXISTS messages(
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              conversation_id TEXT NOT NULL,
              role TEXT NOT NULL,
              content TEXT NOT NULL,
              image_path TEXT NULL,
              sticker_id TEXT NULL,
              created_at TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_messages_conversation ON messages(conversation_id, id DESC);
            CREATE TABLE IF NOT EXISTS conversations(
              id TEXT PRIMARY KEY,
              title TEXT NOT NULL DEFAULT '新对话',
              character_id TEXT NULL,
              created_at TEXT NOT NULL,
              updated_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS conversation_folders(
              id TEXT PRIMARY KEY,
              name TEXT NOT NULL,
              created_at TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_conversations_updated ON conversations(updated_at DESC);
            INSERT OR IGNORE INTO conversations(id,title,created_at,updated_at)
            SELECT m.conversation_id,
              COALESCE(NULLIF((SELECT u.content FROM messages u WHERE u.conversation_id=m.conversation_id AND u.role='user' AND u.content<>'' ORDER BY u.id LIMIT 1),''),'旧对话'),
              MIN(m.created_at),MAX(m.created_at)
            FROM messages m GROUP BY m.conversation_id;
            CREATE TABLE IF NOT EXISTS memories(
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              character_id TEXT NOT NULL,
              kind TEXT NOT NULL,
              summary TEXT NOT NULL,
              tags TEXT NOT NULL DEFAULT '',
              importance REAL NOT NULL DEFAULT 0.5,
              source_message_id INTEGER NULL,
              created_at TEXT NOT NULL,
              FOREIGN KEY(source_message_id) REFERENCES messages(id) ON DELETE SET NULL
            );
            CREATE INDEX IF NOT EXISTS ix_memories_character ON memories(character_id, importance DESC);
            CREATE TABLE IF NOT EXISTS reply_plans(
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              conversation_id TEXT NOT NULL,
              created_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS planned_messages(
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              reply_plan_id INTEGER NOT NULL,
              sequence INTEGER NOT NULL,
              type TEXT NOT NULL,
              content TEXT NULL,
              sticker_id TEXT NULL,
              state TEXT NOT NULL,
              delivered_at TEXT NULL,
              FOREIGN KEY(reply_plan_id) REFERENCES reply_plans(id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ix_planned_messages_plan ON planned_messages(reply_plan_id, sequence);
            CREATE VIRTUAL TABLE IF NOT EXISTS memory_fts USING fts5(summary, tags, content='memories', content_rowid='id', tokenize='unicode61');
            CREATE TRIGGER IF NOT EXISTS memories_ai AFTER INSERT ON memories BEGIN
              INSERT INTO memory_fts(rowid, summary, tags) VALUES (new.id, new.summary, new.tags);
            END;
            CREATE TRIGGER IF NOT EXISTS memories_ad AFTER DELETE ON memories BEGIN
              INSERT INTO memory_fts(memory_fts, rowid, summary, tags) VALUES('delete', old.id, old.summary, old.tags);
            END;
            CREATE TRIGGER IF NOT EXISTS memories_au AFTER UPDATE OF summary, tags ON memories BEGIN
              INSERT INTO memory_fts(memory_fts, rowid, summary, tags) VALUES('delete', old.id, old.summary, old.tags);
              INSERT INTO memory_fts(rowid, summary, tags) VALUES (new.id, new.summary, new.tags);
            END;
            """;
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
        await EnsureColumnAsync(connection, "messages", "sticker_id", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "memories", "updated_at", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "memories", "last_used_at", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "conversations", "folder_id", "TEXT NULL", cancellationToken);
        await EnsureColumnAsync(connection, "conversations", "is_pinned", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await UpgradeMemoryIndexAsync(connection, cancellationToken);
    }

    public async Task<long> AddMessageAsync(string conversationId, ChatRole role, string content,
        string? imagePath, CancellationToken cancellationToken = default, string? stickerId = null)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO messages(conversation_id, role, content, image_path, sticker_id, created_at) VALUES($c,$r,$t,$i,$s,$at); SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("$c", conversationId);
        command.Parameters.AddWithValue("$r", role.ToString().ToLowerInvariant());
        command.Parameters.AddWithValue("$t", content);
        command.Parameters.AddWithValue("$i", (object?)imagePath ?? DBNull.Value);
        command.Parameters.AddWithValue("$s", (object?)stickerId ?? DBNull.Value);
        command.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O"));
        var id = (long)(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
        await using var room = connection.CreateCommand();
        room.Transaction = transaction;
        room.CommandText = """
            INSERT INTO conversations(id,title,created_at,updated_at) VALUES($c,$title,$at,$at)
            ON CONFLICT(id) DO UPDATE SET
              updated_at=excluded.updated_at,
              title=CASE WHEN conversations.title IN ('新对话','旧对话') AND $r='user' AND $t<>'' THEN $title ELSE conversations.title END
            """;
        room.Parameters.AddWithValue("$c", conversationId); room.Parameters.AddWithValue("$r", role.ToString().ToLowerInvariant());
        room.Parameters.AddWithValue("$t", content); room.Parameters.AddWithValue("$title", ConversationTitle(content));
        room.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O"));
        await room.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return id;
    }

    public async Task EnsureConversationAsync(string conversationId, string? characterId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO conversations(id,title,character_id,created_at,updated_at) VALUES($id,'新对话',$character,$at,$at)
            ON CONFLICT(id) DO UPDATE SET character_id=COALESCE(conversations.character_id,excluded.character_id)
            """;
        command.Parameters.AddWithValue("$id", conversationId);
        command.Parameters.AddWithValue("$character", (object?)characterId ?? DBNull.Value);
        command.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationRoom>> ListConversationsAsync(int count = 100,
        CancellationToken cancellationToken = default)
    {
        var result = new List<ConversationRoom>();
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.id,c.title,c.character_id,c.created_at,c.updated_at,COUNT(m.id),c.folder_id,c.is_pinned
            FROM conversations c LEFT JOIN messages m ON m.conversation_id=c.id
            GROUP BY c.id ORDER BY c.is_pinned DESC,c.updated_at DESC LIMIT $n
            """;
        command.Parameters.AddWithValue("$n", count);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new ConversationRoom(reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2),
                DateTimeOffset.Parse(reader.GetString(3)), DateTimeOffset.Parse(reader.GetString(4)), Convert.ToInt32(reader.GetInt64(5)),
                reader.IsDBNull(6) ? null : reader.GetString(6), reader.GetInt64(7) != 0));
        return result;
    }

    public async Task<ConversationRoom?> GetConversationAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.id,c.title,c.character_id,c.created_at,c.updated_at,COUNT(m.id),c.folder_id,c.is_pinned
            FROM conversations c LEFT JOIN messages m ON m.conversation_id=c.id
            WHERE c.id=$id GROUP BY c.id
            """;
        command.Parameters.AddWithValue("$id", conversationId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new ConversationRoom(reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2),
                DateTimeOffset.Parse(reader.GetString(3)), DateTimeOffset.Parse(reader.GetString(4)), Convert.ToInt32(reader.GetInt64(5)),
                reader.IsDBNull(6) ? null : reader.GetString(6), reader.GetInt64(7) != 0)
            : null;
    }

    public async Task<bool> CharacterHasReferencesAsync(string characterId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM conversations WHERE character_id=$id UNION ALL SELECT 1 FROM memories WHERE character_id=$id)";
        command.Parameters.AddWithValue("$id", characterId);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    public async Task<bool> ConversationFolderExistsAsync(string folderId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken); await using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM conversation_folders WHERE id=$id)";
        command.Parameters.AddWithValue("$id", folderId);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    public async Task<IReadOnlyList<ConversationFolder>> ListConversationFoldersAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<ConversationFolder>();
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id,name,created_at FROM conversation_folders ORDER BY name COLLATE NOCASE";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(new ConversationFolder(reader.GetString(0), reader.GetString(1), DateTimeOffset.Parse(reader.GetString(2))));
        return result;
    }

    public async Task<string> CreateConversationFolderAsync(string name, CancellationToken cancellationToken = default)
    {
        var id = $"folder-{Guid.NewGuid():N}";
        await using var connection = await OpenAsync(cancellationToken); await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO conversation_folders(id,name,created_at) VALUES($id,$name,$at)";
        command.Parameters.AddWithValue("$id", id); command.Parameters.AddWithValue("$name", name.Trim()); command.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken); return id;
    }

    public async Task RenameConversationFolderAsync(string folderId, string name, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken); await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE conversation_folders SET name=$name WHERE id=$id";
        command.Parameters.AddWithValue("$id", folderId); command.Parameters.AddWithValue("$name", name.Trim());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteConversationFolderAsync(string folderId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken); await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using (var move = connection.CreateCommand())
        {
            move.Transaction = transaction; move.CommandText = "UPDATE conversations SET folder_id=NULL WHERE folder_id=$id"; move.Parameters.AddWithValue("$id", folderId);
            await move.ExecuteNonQueryAsync(cancellationToken);
        }
        await using (var remove = connection.CreateCommand())
        {
            remove.Transaction = transaction; remove.CommandText = "DELETE FROM conversation_folders WHERE id=$id"; remove.Parameters.AddWithValue("$id", folderId);
            await remove.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RenameConversationAsync(string conversationId, string title, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken); await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE conversations SET title=$title,updated_at=$at WHERE id=$id";
        command.Parameters.AddWithValue("$id", conversationId); command.Parameters.AddWithValue("$title", ConversationTitle(title)); command.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SetConversationPinnedAsync(string conversationId, bool pinned, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken); await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE conversations SET is_pinned=$p WHERE id=$id"; command.Parameters.AddWithValue("$id", conversationId); command.Parameters.AddWithValue("$p", pinned ? 1 : 0);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task MoveConversationToFolderAsync(string conversationId, string? folderId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken); await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE conversations SET folder_id=$folder WHERE id=$id"; command.Parameters.AddWithValue("$id", conversationId); command.Parameters.AddWithValue("$folder", (object?)folderId ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteConversationAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken); await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        foreach (var sql in new[]
        {
            "DELETE FROM memories WHERE source_message_id IN (SELECT id FROM messages WHERE conversation_id=$id)",
            "DELETE FROM reply_plans WHERE conversation_id=$id",
            "DELETE FROM messages WHERE conversation_id=$id",
            "DELETE FROM conversations WHERE id=$id"
        })
        {
            await using var command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = sql; command.Parameters.AddWithValue("$id", conversationId); await command.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<ChatMessage?> GetPreviousUserMessageAsync(string conversationId, long beforeMessageId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken); await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id,role,content,created_at,image_path,sticker_id FROM messages WHERE conversation_id=$c AND id<$id AND role='user' ORDER BY id DESC LIMIT 1";
        command.Parameters.AddWithValue("$c", conversationId); command.Parameters.AddWithValue("$id", beforeMessageId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken); if (!await reader.ReadAsync(cancellationToken)) return null;
        return ReadChatMessage(reader, conversationId);
    }

    public Task<ConversationMutationSnapshot?> DeleteMessageAsync(string conversationId, long messageId, CancellationToken cancellationToken = default) =>
        DeleteMessagesCoreAsync(conversationId, messageId, false, cancellationToken);

    public Task<ConversationMutationSnapshot?> DeleteMessageAndFollowingAsync(string conversationId, long messageId, CancellationToken cancellationToken = default) =>
        DeleteMessagesCoreAsync(conversationId, messageId, true, cancellationToken);

    public async Task<IReadOnlyList<ChatMessage>> GetRecentMessagesAsync(string conversationId, int count,
        CancellationToken cancellationToken = default)
    {
        var result = new List<ChatMessage>();
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id, role, content, created_at, image_path, sticker_id FROM messages WHERE conversation_id=$c ORDER BY id DESC LIMIT $n";
        command.Parameters.AddWithValue("$c", conversationId);
        command.Parameters.AddWithValue("$n", count);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new ChatMessage(reader.GetInt64(0), conversationId, Enum.Parse<ChatRole>(reader.GetString(1), true),
                reader.GetString(2), DateTimeOffset.Parse(reader.GetString(3)), reader.IsDBNull(4) ? null : reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5)));
        result.Reverse();
        return result;
    }

    public async Task<long> AddMemoryAsync(string characterId, string kind, string summary, string tags,
        double importance, long? sourceMessageId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO memories(character_id, kind, summary, tags, importance, source_message_id, created_at, updated_at) VALUES($c,$k,$s,$t,$i,$m,$at,$at); SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("$c", characterId);
        command.Parameters.AddWithValue("$k", kind);
        command.Parameters.AddWithValue("$s", summary);
        command.Parameters.AddWithValue("$t", tags);
        command.Parameters.AddWithValue("$i", Math.Clamp(importance, 0, 1));
        command.Parameters.AddWithValue("$m", (object?)sourceMessageId ?? DBNull.Value);
        command.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O"));
        return (long)(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
    }

    public async Task<IReadOnlyList<MemoryRecord>> ListMemoriesAsync(string characterId, string? query = null,
        CancellationToken cancellationToken = default)
    {
        var result = new List<MemoryRecord>();
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id,character_id,kind,summary,tags,importance,source_message_id,created_at,updated_at,last_used_at
            FROM memories
            WHERE character_id=$c AND ($q='' OR summary LIKE $like OR tags LIKE $like OR kind LIKE $like)
            ORDER BY importance DESC, id DESC
            """;
        command.Parameters.AddWithValue("$c", characterId);
        command.Parameters.AddWithValue("$q", query?.Trim() ?? "");
        command.Parameters.AddWithValue("$like", $"%{query?.Trim() ?? ""}%");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(ReadMemory(reader));
        return result;
    }

    public async Task<MemoryRecord?> GetMemoryAsync(long id, string characterId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken); await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id,character_id,kind,summary,tags,importance,source_message_id,created_at,updated_at,last_used_at FROM memories WHERE id=$id AND character_id=$c";
        command.Parameters.AddWithValue("$id", id); command.Parameters.AddWithValue("$c", characterId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadMemory(reader) : null;
    }

    public async Task<ChatMessage?> GetMessageAsync(string conversationId, long id, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken); await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id,role,content,created_at,image_path,sticker_id FROM messages WHERE conversation_id=$c AND id=$id";
        command.Parameters.AddWithValue("$c", conversationId); command.Parameters.AddWithValue("$id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadChatMessage(reader, conversationId) : null;
    }

    public async Task UpdateMemoryAsync(long id, string characterId, string kind, string summary, string tags,
        double importance, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE memories SET kind=$k,summary=$s,tags=$t,importance=$i,updated_at=$at WHERE id=$id AND character_id=$c";
        command.Parameters.AddWithValue("$id", id); command.Parameters.AddWithValue("$c", characterId);
        command.Parameters.AddWithValue("$k", kind); command.Parameters.AddWithValue("$s", summary);
        command.Parameters.AddWithValue("$t", tags); command.Parameters.AddWithValue("$i", Math.Clamp(importance, 0, 1));
        command.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteMemoryAsync(long id, string characterId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM memories WHERE id=$id AND character_id=$c";
        command.Parameters.AddWithValue("$id", id); command.Parameters.AddWithValue("$c", characterId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<long> CreateReplyPlanAsync(string conversationId, IReadOnlyList<PlannedReplyMessage> messages,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using var plan = connection.CreateCommand();
        plan.Transaction = transaction;
        plan.CommandText = "INSERT INTO reply_plans(conversation_id, created_at) VALUES($c,$at); SELECT last_insert_rowid();";
        plan.Parameters.AddWithValue("$c", conversationId);
        plan.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O"));
        var planId = (long)(await plan.ExecuteScalarAsync(cancellationToken) ?? 0L);
        foreach (var message in messages)
        {
            await using var item = connection.CreateCommand();
            item.Transaction = transaction;
            item.CommandText = "INSERT INTO planned_messages(reply_plan_id, sequence, type, content, sticker_id, state) VALUES($p,$s,$t,$c,$i,'pending')";
            item.Parameters.AddWithValue("$p", planId); item.Parameters.AddWithValue("$s", message.Sequence);
            item.Parameters.AddWithValue("$t", message.Type.ToString().ToLowerInvariant());
            item.Parameters.AddWithValue("$c", (object?)message.Content ?? DBNull.Value);
            item.Parameters.AddWithValue("$i", (object?)message.StickerId ?? DBNull.Value);
            await item.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return planId;
    }

    public async Task<long> DeliverPlannedMessageAsync(long planId, PlannedReplyMessage message,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        var deliveredAt = DateTimeOffset.UtcNow;
        await using (var update = connection.CreateCommand())
        {
            update.Transaction = transaction;
            update.CommandText = "UPDATE planned_messages SET state='delivered', delivered_at=$at WHERE reply_plan_id=$p AND sequence=$s AND state='pending'";
            update.Parameters.AddWithValue("$at", deliveredAt.ToString("O")); update.Parameters.AddWithValue("$p", planId); update.Parameters.AddWithValue("$s", message.Sequence);
            if (await update.ExecuteNonQueryAsync(cancellationToken) != 1) throw new InvalidOperationException("计划消息状态不允许送达。");
        }
        long messageId;
        await using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = "INSERT INTO messages(conversation_id, role, content, sticker_id, created_at) SELECT conversation_id,'assistant',$c,$i,$at FROM reply_plans WHERE id=$p; SELECT last_insert_rowid();";
            insert.Parameters.AddWithValue("$c", message.Content ?? ""); insert.Parameters.AddWithValue("$i", (object?)message.StickerId ?? DBNull.Value);
            insert.Parameters.AddWithValue("$at", deliveredAt.ToString("O")); insert.Parameters.AddWithValue("$p", planId);
            messageId = Convert.ToInt64(await insert.ExecuteScalarAsync(cancellationToken));
        }
        await using (var room = connection.CreateCommand())
        {
            room.Transaction = transaction;
            room.CommandText = "UPDATE conversations SET updated_at=$at WHERE id=(SELECT conversation_id FROM reply_plans WHERE id=$p)";
            room.Parameters.AddWithValue("$at", deliveredAt.ToString("O")); room.Parameters.AddWithValue("$p", planId);
            await room.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return messageId;
    }

    public async Task CancelPendingPlanMessagesAsync(long planId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE planned_messages SET state='cancelled' WHERE reply_plan_id=$p AND state='pending'";
        command.Parameters.AddWithValue("$p", planId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MemoryRecord>> SearchMemoriesAsync(string characterId, string query, int count = 6,
        CancellationToken cancellationToken = default)
    {
        var terms = Tokenize(query).Take(6).ToArray();
        if (terms.Length == 0) return [];
        var result = new List<MemoryRecord>();
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        var clauses = terms.Select((_, i) => $"(m.summary LIKE $q{i} OR m.tags LIKE $q{i})");
        command.CommandText = $"SELECT m.id,m.character_id,m.kind,m.summary,m.tags,m.importance,m.source_message_id,m.created_at,m.updated_at,m.last_used_at FROM memories m WHERE m.character_id=$c AND ({string.Join(" OR ", clauses)}) ORDER BY m.importance DESC, m.id DESC LIMIT $n";
        command.Parameters.AddWithValue("$c", characterId);
        command.Parameters.AddWithValue("$n", count);
        for (var i = 0; i < terms.Length; i++) command.Parameters.AddWithValue($"$q{i}", $"%{terms[i]}%");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(ReadMemory(reader));
        if (result.Count > 0)
        {
            await reader.DisposeAsync();
            await using var touch = connection.CreateCommand();
            touch.CommandText = $"UPDATE memories SET last_used_at=$at WHERE id IN ({string.Join(',', result.Select((_, i) => $"$id{i}"))})";
            touch.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O"));
            for (var i = 0; i < result.Count; i++) touch.Parameters.AddWithValue($"$id{i}", result[i].Id);
            await touch.ExecuteNonQueryAsync(cancellationToken);
        }
        return result;
    }

    private async Task<ConversationMutationSnapshot?> DeleteMessagesCoreAsync(string conversationId, long messageId,
        bool includeFollowing, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        var op = includeFollowing ? ">=" : "=";
        ConversationRoom? roomSnapshot;
        await using (var room = connection.CreateCommand())
        {
            room.Transaction = transaction;
            room.CommandText = "SELECT c.id,c.title,c.character_id,c.created_at,c.updated_at,(SELECT COUNT(*) FROM messages m WHERE m.conversation_id=c.id),c.folder_id,c.is_pinned FROM conversations c WHERE c.id=$c";
            room.Parameters.AddWithValue("$c", conversationId);
            await using var reader = await room.ExecuteReaderAsync(cancellationToken);
            roomSnapshot = await reader.ReadAsync(cancellationToken)
                ? new ConversationRoom(reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2), DateTimeOffset.Parse(reader.GetString(3)), DateTimeOffset.Parse(reader.GetString(4)), Convert.ToInt32(reader.GetInt64(5)), reader.IsDBNull(6) ? null : reader.GetString(6), reader.GetInt64(7) != 0)
                : null;
        }
        if (roomSnapshot is null) return null;
        long maxMessageId;
        await using (var max = connection.CreateCommand())
        {
            max.Transaction = transaction; max.CommandText = "SELECT COALESCE(MAX(id),0) FROM messages WHERE conversation_id=$c"; max.Parameters.AddWithValue("$c", conversationId);
            maxMessageId = Convert.ToInt64(await max.ExecuteScalarAsync(cancellationToken));
        }
        var messages = new List<ChatMessage>();
        await using (var select = connection.CreateCommand())
        {
            select.Transaction = transaction; select.CommandText = $"SELECT id,role,content,created_at,image_path,sticker_id FROM messages WHERE conversation_id=$c AND id{op}$id ORDER BY id";
            select.Parameters.AddWithValue("$c", conversationId); select.Parameters.AddWithValue("$id", messageId);
            await using var reader = await select.ExecuteReaderAsync(cancellationToken); while (await reader.ReadAsync(cancellationToken)) messages.Add(ReadChatMessage(reader, conversationId));
        }
        if (messages.Count == 0) return null;
        var memories = new List<MemoryRecord>();
        await using (var select = connection.CreateCommand())
        {
            select.Transaction = transaction;
            select.CommandText = $"SELECT id,character_id,kind,summary,tags,importance,source_message_id,created_at,updated_at,last_used_at FROM memories WHERE source_message_id IN (SELECT id FROM messages WHERE conversation_id=$c AND id{op}$id)";
            select.Parameters.AddWithValue("$c", conversationId); select.Parameters.AddWithValue("$id", messageId);
            await using var reader = await select.ExecuteReaderAsync(cancellationToken); while (await reader.ReadAsync(cancellationToken)) memories.Add(ReadMemory(reader));
        }
        foreach (var sql in new[]
        {
            $"DELETE FROM memories WHERE source_message_id IN (SELECT id FROM messages WHERE conversation_id=$c AND id{op}$id)",
            $"DELETE FROM messages WHERE conversation_id=$c AND id{op}$id"
        })
        {
            await using var command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = sql;
            command.Parameters.AddWithValue("$c", conversationId); command.Parameters.AddWithValue("$id", messageId); await command.ExecuteNonQueryAsync(cancellationToken);
        }
        await using (var update = connection.CreateCommand())
        {
            update.Transaction = transaction;
            update.CommandText = """
                UPDATE conversations SET
                  title=COALESCE(NULLIF((SELECT content FROM messages WHERE conversation_id=$c AND role='user' AND content<>'' ORDER BY id LIMIT 1),''),'新对话'),
                  updated_at=COALESCE((SELECT MAX(created_at) FROM messages WHERE conversation_id=$c),created_at)
                WHERE id=$c
                """;
            update.Parameters.AddWithValue("$c", conversationId); await update.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return new ConversationMutationSnapshot(conversationId, roomSnapshot, messages, memories, maxMessageId);
    }

    public async Task RestoreConversationMutationAsync(ConversationMutationSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        foreach (var sql in new[]
        {
            "DELETE FROM memories WHERE source_message_id IN (SELECT id FROM messages WHERE conversation_id=$c AND id>$max)",
            "DELETE FROM messages WHERE conversation_id=$c AND id>$max"
        })
        {
            await using var command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = sql;
            command.Parameters.AddWithValue("$c", snapshot.ConversationId); command.Parameters.AddWithValue("$max", snapshot.MaxMessageIdBeforeMutation); await command.ExecuteNonQueryAsync(cancellationToken);
        }
        foreach (var message in snapshot.Messages)
        {
            await using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "INSERT OR REPLACE INTO messages(id,conversation_id,role,content,image_path,sticker_id,created_at) VALUES($id,$c,$r,$text,$image,$sticker,$at)";
            command.Parameters.AddWithValue("$id", message.Id); command.Parameters.AddWithValue("$c", message.ConversationId); command.Parameters.AddWithValue("$r", message.Role.ToString().ToLowerInvariant()); command.Parameters.AddWithValue("$text", message.Content);
            command.Parameters.AddWithValue("$image", (object?)message.ImagePath ?? DBNull.Value); command.Parameters.AddWithValue("$sticker", (object?)message.StickerId ?? DBNull.Value); command.Parameters.AddWithValue("$at", message.CreatedAt.ToString("O"));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        foreach (var memory in snapshot.Memories)
        {
            await using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "INSERT OR REPLACE INTO memories(id,character_id,kind,summary,tags,importance,source_message_id,created_at,updated_at,last_used_at) VALUES($id,$c,$k,$s,$t,$i,$source,$created,$updated,$used)";
            command.Parameters.AddWithValue("$id", memory.Id); command.Parameters.AddWithValue("$c", memory.CharacterId); command.Parameters.AddWithValue("$k", memory.Kind); command.Parameters.AddWithValue("$s", memory.Summary); command.Parameters.AddWithValue("$t", memory.Tags); command.Parameters.AddWithValue("$i", memory.Importance);
            command.Parameters.AddWithValue("$source", (object?)memory.SourceMessageId ?? DBNull.Value); command.Parameters.AddWithValue("$created", memory.CreatedAt.ToString("O")); command.Parameters.AddWithValue("$updated", (object?)memory.UpdatedAt?.ToString("O") ?? DBNull.Value); command.Parameters.AddWithValue("$used", (object?)memory.LastUsedAt?.ToString("O") ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        await using (var room = connection.CreateCommand())
        {
            room.Transaction = transaction;
            room.CommandText = "UPDATE conversations SET title=$title,character_id=$character,created_at=$created,updated_at=$updated,folder_id=$folder,is_pinned=$pinned WHERE id=$id";
            room.Parameters.AddWithValue("$id", snapshot.Room.Id); room.Parameters.AddWithValue("$title", snapshot.Room.Title); room.Parameters.AddWithValue("$character", (object?)snapshot.Room.CharacterId ?? DBNull.Value); room.Parameters.AddWithValue("$created", snapshot.Room.CreatedAt.ToString("O")); room.Parameters.AddWithValue("$updated", snapshot.Room.UpdatedAt.ToString("O")); room.Parameters.AddWithValue("$folder", (object?)snapshot.Room.FolderId ?? DBNull.Value); room.Parameters.AddWithValue("$pinned", snapshot.Room.IsPinned ? 1 : 0);
            await room.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    private static ChatMessage ReadChatMessage(SqliteDataReader reader, string conversationId) => new(
        reader.GetInt64(0), conversationId, Enum.Parse<ChatRole>(reader.GetString(1), true), reader.GetString(2), DateTimeOffset.Parse(reader.GetString(3)),
        reader.IsDBNull(4) ? null : reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5));

    private static MemoryRecord ReadMemory(SqliteDataReader reader) => new(
        reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
        reader.GetDouble(5), reader.IsDBNull(6) ? null : reader.GetInt64(6), DateTimeOffset.Parse(reader.GetString(7)),
        reader.IsDBNull(8) ? null : DateTimeOffset.Parse(reader.GetString(8)),
        reader.IsDBNull(9) ? null : DateTimeOffset.Parse(reader.GetString(9)));

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA busy_timeout=5000; PRAGMA foreign_keys=ON;";
        await command.ExecuteNonQueryAsync(cancellationToken);
        return connection;
    }

    private static async Task EnsureColumnAsync(SqliteConnection connection, string table, string column,
        string declaration, CancellationToken cancellationToken)
    {
        await using var check = connection.CreateCommand();
        check.CommandText = $"PRAGMA table_info({table})";
        await using var reader = await check.ExecuteReaderAsync(cancellationToken);
        var exists = false;
        while (await reader.ReadAsync(cancellationToken)) if (reader.GetString(1) == column) { exists = true; break; }
        await reader.DisposeAsync();
        if (exists) return;
        await using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {declaration}";
        await alter.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpgradeMemoryIndexAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var version = connection.CreateCommand();
        version.CommandText = "SELECT COALESCE(MAX(version), 1) FROM schema_info";
        var current = Convert.ToInt32(await version.ExecuteScalarAsync(cancellationToken));
        if (current >= 2) return;
        await using var migrate = connection.CreateCommand();
        migrate.CommandText = "INSERT INTO memory_fts(memory_fts) VALUES('rebuild'); UPDATE schema_info SET version=2;";
        await migrate.ExecuteNonQueryAsync(cancellationToken);
    }

    private static IEnumerable<string> Tokenize(string text) => text
        .Split([' ', '\t', '\r', '\n', '，', '。', '！', '？', ',', '.', '!', '?'], StringSplitOptions.RemoveEmptyEntries)
        .Where(x => x.Length >= 2)
        .Distinct(StringComparer.OrdinalIgnoreCase);

    private static string ConversationTitle(string content)
    {
        var value = string.IsNullOrWhiteSpace(content) ? "新对话" : content.Trim().Replace('\r', ' ').Replace('\n', ' ');
        return value.Length <= 24 ? value : value[..24] + "…";
    }
}

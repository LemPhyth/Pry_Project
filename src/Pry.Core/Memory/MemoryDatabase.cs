using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Pry.Core.Models;

namespace Pry.Core.Memory;

public sealed partial class MemoryDatabase(string databasePath, ILogger? logger = null) : Pry.Core.Abstractions.IMemoryRetriever
{
    public SqliteMemoryRepository LongTerm { get; } = new(databasePath);
    private readonly string _connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = databasePath,
        Mode = SqliteOpenMode.ReadWriteCreate,
        Cache = SqliteCacheMode.Shared
    }.ToString();

    /// <summary>宿主注入的日志器（PLAT-006）。Core 只提供注入口，不决定日志去哪里；
    /// 默认 <see cref="NullLogger"/>，所以未接线的调用方——包括全部现存测试——行为不变。</summary>
    private readonly ILogger _logger = logger ?? NullLogger.Instance;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var started = Environment.TickCount64;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(databasePath))!);
        await using var connection = await OpenAsync(cancellationToken);
        var versionBefore = await ReadSchemaVersionAsync(connection, cancellationToken);
        // 未知高版本保护必须在**任何写入之前**（HDS-001 E 条 / v4 §9.3 / AUD-05）：
        // 下面第一批语句里的 `PRAGMA journal_mode=WAL` 就是写——高版本库绝不能先被改了口径再报错。
        EnsureWritableSchemaVersion(versionBefore, "初始化");
        // PRAGMA 留在事务外：journal_mode 不允许在事务内切换，它也不是迁移步骤，只是连接形态设置。
        await using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
        await pragma.ExecuteNonQueryAsync(cancellationToken);
        // 建表/补列/版本推进从"一条直线"改为登记清单执行（HDS-001 F 条）：每步一个事务边界，
        // 失败整步回滚、版本不推进；全部步骤幂等，重跑初始化就是恢复路径。
        await MemorySchemaMigrator.RunAsync(connection, versionBefore, BuildMigrationSteps(), cancellationToken);
        // 版本、表与列名是标识符不是内容；这里不写文件名也不写任何用户数据（PLAT-006 "secrets 不进日志"）。
        _logger.LogInformation("持久层就绪：schema {VersionBefore} → {SchemaVersion}，用时 {ElapsedMilliseconds} 毫秒",
            versionBefore, SchemaVersion, Environment.TickCount64 - started);
    }

    /// <summary>
    /// 迁移登记清单（HDS-001 F 条）。步骤内容是本刀之前**已经存在**的结构动作，逐条原样收编——
    /// 本刀只搭治理（登记、事务边界、失败回滚、幂等要求），不定义任何新结构、不预占新版本号（v4 §9.1）。
    /// 闸门档步骤 <c>memory-fts-rebuild</c> 保留历史语义：仅对登记版本低于 2 的库执行一次。
    /// </summary>
    public IReadOnlyList<SchemaMigrationStep> BuildMigrationSteps() =>
    [
        new(1, "baseline.schema", "既有结构的自愈建表与列补齐", SchemaMigrationMode.SelfHealingBaseline,
            static async (context, token) =>
            {
                var (connection, transaction) = (context.Connection, context.Transaction);
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = """
                    CREATE TABLE IF NOT EXISTS schema_info(version INTEGER NOT NULL);
                    INSERT INTO schema_info(version) SELECT 1 WHERE NOT EXISTS(SELECT 1 FROM schema_info);
                    CREATE TABLE IF NOT EXISTS messages(
                      id INTEGER PRIMARY KEY AUTOINCREMENT,
                      conversation_id TEXT NOT NULL,
                      role TEXT NOT NULL,
                      content TEXT NOT NULL,
                      image_path TEXT NULL,
                      sticker_id TEXT NULL,
                      source TEXT NOT NULL DEFAULT 'legacy',
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
                    CREATE TABLE IF NOT EXISTS interaction_commands(
                      id INTEGER PRIMARY KEY AUTOINCREMENT,
                      conversation_id TEXT NOT NULL,
                      idempotency_key TEXT NOT NULL,
                      character_id TEXT NOT NULL,
                      action_id TEXT NOT NULL,
                      message_id INTEGER NULL,
                      outcome TEXT NOT NULL,
                      expression_intent TEXT NULL,
                      created_at TEXT NOT NULL,
                      UNIQUE(conversation_id,idempotency_key),
                      FOREIGN KEY(conversation_id) REFERENCES conversations(id) ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS ix_interaction_commands_cooldown
                      ON interaction_commands(conversation_id,action_id,created_at DESC);
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
                    CREATE TABLE IF NOT EXISTS memory_embeddings(
                      memory_id INTEGER NOT NULL,
                      model_id TEXT NOT NULL,
                      dimensions INTEGER NOT NULL,
                      vector BLOB NOT NULL,
                      content_fingerprint TEXT NOT NULL,
                      updated_at TEXT NOT NULL,
                      PRIMARY KEY(memory_id, model_id),
                      FOREIGN KEY(memory_id) REFERENCES memories(id) ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS ix_memory_embeddings_model ON memory_embeddings(model_id, memory_id);
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
                await command.ExecuteNonQueryAsync(token);
                await EnsureColumnAsync(connection, "messages", "sticker_id", "TEXT NULL", token, transaction);
                await EnsureColumnAsync(connection, "messages", "source", "TEXT NOT NULL DEFAULT 'legacy'", token, transaction);
                await EnsureColumnAsync(connection, "memories", "updated_at", "TEXT NULL", token, transaction);
                await EnsureColumnAsync(connection, "memories", "last_used_at", "TEXT NULL", token, transaction);
                await EnsureColumnAsync(connection, "conversations", "folder_id", "TEXT NULL", token, transaction);
                await EnsureColumnAsync(connection, "conversations", "is_pinned", "INTEGER NOT NULL DEFAULT 0", token, transaction);
            }),
        new(2, "upgrade.memory-fts-rebuild", "长期记忆全文索引重建", SchemaMigrationMode.VersionGatedUpgrade,
            static async (context, token) =>
            {
                await using var migrate = context.Connection.CreateCommand();
                migrate.Transaction = context.Transaction;
                // 历史欠账（PLAT-003 P2）：这两句曾经串跑且没有事务包裹。现在重建与版本推进同处
                // 本步骤的事务边界——要么一起生效，要么一起消失。
                migrate.CommandText = "INSERT INTO memory_fts(memory_fts) VALUES('rebuild'); UPDATE schema_info SET version=2;";
                await migrate.ExecuteNonQueryAsync(token);
            }),
        new(SchemaVersion, "repair.long-term-memory", "长期记忆仓储整备",
            SchemaMigrationMode.IdempotentRepairOwnsConnection,
            (context, token) => LongTerm.InitializeAsync(token)),
        new(SchemaVersion, "repair.proactive-policies", "主动消息策略表整备", SchemaMigrationMode.IdempotentRepair,
            static async (context, token) => await InitializeProactivePoliciesAsync(context.Connection, token, context.Transaction)),
        new(SchemaVersion, "repair.read-state", "会话读水位整备", SchemaMigrationMode.IdempotentRepair,
            static async (context, token) => await InitializeReadStateAsync(context.Connection, token, context.Transaction)),
        new(SchemaVersion, "repair.runtime-state", "对话 Runtime 整备", SchemaMigrationMode.IdempotentRepair,
            static async (context, token) => await InitializeRuntimeStateAsync(context.Connection, token, context.Transaction)),
        new(SchemaVersion, "repair.protocol-events", "协议事件账本整备", SchemaMigrationMode.IdempotentRepair,
            static async (context, token) => await InitializeProtocolEventsAsync(context.Connection, token, context.Transaction)),
        new(SchemaVersion, "repair.intelligence", "学习状态表整备", SchemaMigrationMode.IdempotentRepair,
            static async (context, token) => await InitializeIntelligenceAsync(context.Connection, token, context.Transaction)),
        new(SchemaVersion, "align.schema-version", "登记版本对齐", SchemaMigrationMode.VersionAlign,
            static async (context, token) =>
                await SetSchemaVersionAsync(context.Connection, SchemaVersion, token, context.Transaction)),
    ];

    /// <summary>
    /// 未知高版本拒写门（HDS-001 E 条）：登记版本高于当前二进制已知版本时，在任何写入发生前抛出
    /// <see cref="SchemaVersionTooNewException"/>。等号放行（同版本或可升级的旧版本照常走迁移清单）。
    /// </summary>
    private static void EnsureWritableSchemaVersion(int databaseSchemaVersion, string stage)
    {
        if (databaseSchemaVersion <= SchemaVersion) return;
        throw new SchemaVersionTooNewException(SchemaVersion, databaseSchemaVersion, stage);
    }

    /// <summary>
    /// 数据库结构的唯一写侧编号。任何断言版本的测试必须读这个常量，不得再写第二个字面量——
    /// 同一工作区里两份硬编码值一旦不同就自相矛盾，而 QA 只能判其中之一。
    /// 编号的分配权归 PLAT-003（§5.0 冻结中）：这里只是将既有推进登记为单点，不据此宣告 v8→v9 的迁移注册表方案已裁定。
    /// v9→v10 是 DEF-P3STATE-01 追加的学习状态五张表（character_state、behavior_pattern 及其守卫子表、
    /// expression_pattern 及其两张守卫子表、behavior_experience）：只追加钩子，不改写他人那几片的次序。
    /// </summary>
    public const int SchemaVersion = 10;

    public async Task<long> AddMessageAsync(string conversationId, ChatRole role, string content,
        string? imagePath, CancellationToken cancellationToken = default, string? stickerId = null,
        string? source = null, CompletedMemoryTurn? completedMemoryTurn = null, long? memoryEpoch = null)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO messages(conversation_id, role, content, image_path, sticker_id, source, created_at) VALUES($c,$r,$t,$i,$s,$source,$at); SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("$c", conversationId);
        command.Parameters.AddWithValue("$r", role.ToString().ToLowerInvariant());
        command.Parameters.AddWithValue("$t", content);
        command.Parameters.AddWithValue("$i", (object?)imagePath ?? DBNull.Value);
        command.Parameters.AddWithValue("$s", (object?)stickerId ?? DBNull.Value);
        command.Parameters.AddWithValue("$source", source ?? (role == ChatRole.User ? MessageSources.UserInput : MessageSources.ModelReply));
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
        if (role == ChatRole.Assistant && completedMemoryTurn is not null)
            await SqliteMemoryRepository.RecordCompletedAsync(connection, transaction,
                completedMemoryTurn with { DeliveredMessageIds = [id] }, memoryEpoch, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return id;
    }

    public async Task<InteractionCommandWriteResult> ExecuteInteractionCommandAsync(string conversationId,
        string characterId, string actionId, string idempotencyKey, string? responseText, string outcome,
        string? expressionIntent, int cooldownSeconds, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction(deferred: false);
        await using (var existing = connection.CreateCommand())
        {
            existing.Transaction = transaction;
            existing.CommandText = """
                SELECT character_id,action_id,message_id,outcome,created_at,expression_intent
                FROM interaction_commands WHERE conversation_id=$conversation AND idempotency_key=$key
                """;
            existing.Parameters.AddWithValue("$conversation", conversationId);
            existing.Parameters.AddWithValue("$key", idempotencyKey);
            await using var reader = await existing.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var storedCharacter = reader.GetString(0); var storedAction = reader.GetString(1);
                var result = new InteractionCommandWriteResult(
                    storedCharacter == characterId && storedAction == actionId
                        ? InteractionCommandWriteStatus.Replayed : InteractionCommandWriteStatus.IdempotencyConflict,
                    storedAction, storedCharacter, reader.IsDBNull(2) ? null : reader.GetInt64(2), reader.GetString(3),
                    DateTimeOffset.Parse(reader.GetString(4)), null, reader.IsDBNull(5) ? null : reader.GetString(5));
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
        }

        var now = DateTimeOffset.UtcNow;
        if (cooldownSeconds > 0)
        {
            await using var cooldown = connection.CreateCommand();
            cooldown.Transaction = transaction;
            cooldown.CommandText = "SELECT created_at FROM interaction_commands WHERE conversation_id=$conversation AND action_id=$action ORDER BY id DESC LIMIT 1";
            cooldown.Parameters.AddWithValue("$conversation", conversationId);
            cooldown.Parameters.AddWithValue("$action", actionId);
            var raw = await cooldown.ExecuteScalarAsync(cancellationToken);
            if (raw is string value)
            {
                var retryAt = DateTimeOffset.Parse(value).AddSeconds(cooldownSeconds);
                if (retryAt > now)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return new InteractionCommandWriteResult(InteractionCommandWriteStatus.Cooldown, actionId,
                        characterId, null, outcome, now, retryAt, expressionIntent);
                }
            }
        }

        long? messageId = null;
        if (responseText is not null)
        {
            await using var message = connection.CreateCommand();
            message.Transaction = transaction;
            message.CommandText = """
                INSERT INTO messages(conversation_id,role,content,image_path,sticker_id,source,created_at)
                VALUES($conversation,'assistant',$content,NULL,NULL,$source,$at); SELECT last_insert_rowid();
                """;
            message.Parameters.AddWithValue("$conversation", conversationId);
            message.Parameters.AddWithValue("$content", responseText);
            message.Parameters.AddWithValue("$source", MessageSources.InteractionPreset);
            message.Parameters.AddWithValue("$at", now.ToString("O"));
            messageId = Convert.ToInt64(await message.ExecuteScalarAsync(cancellationToken));
            await using var room = connection.CreateCommand();
            room.Transaction = transaction;
            room.CommandText = "UPDATE conversations SET updated_at=$at WHERE id=$conversation";
            room.Parameters.AddWithValue("$at", now.ToString("O"));
            room.Parameters.AddWithValue("$conversation", conversationId);
            await room.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO interaction_commands(conversation_id,idempotency_key,character_id,action_id,message_id,
                  outcome,expression_intent,created_at)
                VALUES($conversation,$key,$character,$action,$message,$outcome,$expression,$at)
                """;
            command.Parameters.AddWithValue("$conversation", conversationId);
            command.Parameters.AddWithValue("$key", idempotencyKey);
            command.Parameters.AddWithValue("$character", characterId);
            command.Parameters.AddWithValue("$action", actionId);
            command.Parameters.AddWithValue("$message", (object?)messageId ?? DBNull.Value);
            command.Parameters.AddWithValue("$outcome", outcome);
            command.Parameters.AddWithValue("$expression", (object?)expressionIntent ?? DBNull.Value);
            command.Parameters.AddWithValue("$at", now.ToString("O"));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return new InteractionCommandWriteResult(InteractionCommandWriteStatus.Created, actionId, characterId,
            messageId, outcome, now, null, expressionIntent);
    }

    public async Task<InteractionCommandWriteResult?> GetInteractionCommandAsync(string conversationId,
        string idempotencyKey, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT character_id,action_id,message_id,outcome,created_at,expression_intent
            FROM interaction_commands WHERE conversation_id=$conversation AND idempotency_key=$key
            """;
        command.Parameters.AddWithValue("$conversation", conversationId);
        command.Parameters.AddWithValue("$key", idempotencyKey);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new InteractionCommandWriteResult(InteractionCommandWriteStatus.Replayed, reader.GetString(1),
                reader.GetString(0), reader.IsDBNull(2) ? null : reader.GetInt64(2), reader.GetString(3),
                DateTimeOffset.Parse(reader.GetString(4)), null, reader.IsDBNull(5) ? null : reader.GetString(5))
            : null;
    }

    public async Task<IReadOnlyList<string>> GetReferencedImagePathsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new List<string>();
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT image_path FROM messages WHERE image_path IS NOT NULL AND image_path <> ''";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(reader.GetString(0));
        return result;
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
            SELECT c.id,c.title,c.character_id,c.created_at,c.updated_at,
                   (SELECT COUNT(*) FROM messages count_message WHERE count_message.conversation_id=c.id),
                   c.folder_id,c.is_pinned,
                   CASE
                     WHEN TRIM(last_message.content)<>'' THEN SUBSTR(REPLACE(REPLACE(TRIM(last_message.content), CHAR(13), ' '), CHAR(10), ' '),1,160)
                     ELSE NULL
                   END,
                   last_message.role,
                   CASE WHEN last_message.sticker_id IS NOT NULL THEN 'sticker'
                        WHEN last_message.image_path IS NOT NULL THEN 'image' ELSE 'text' END,
                   last_message.created_at, last_message.id, c.last_read_message_id,
                   (SELECT COUNT(*) FROM messages unread WHERE unread.conversation_id=c.id
                    AND unread.role='assistant' AND unread.id>c.last_read_message_id)
            FROM conversations c
            LEFT JOIN messages last_message ON last_message.id=(
                SELECT latest.id FROM messages latest WHERE latest.conversation_id=c.id ORDER BY latest.id DESC LIMIT 1)
            ORDER BY c.is_pinned DESC,c.updated_at DESC LIMIT $n
            """;
        command.Parameters.AddWithValue("$n", count);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(ReadConversationRoom(reader));
        return result;
    }

    public async Task<ConversationRoom?> GetConversationAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.id,c.title,c.character_id,c.created_at,c.updated_at,
                   (SELECT COUNT(*) FROM messages count_message WHERE count_message.conversation_id=c.id),
                   c.folder_id,c.is_pinned,
                   CASE
                     WHEN TRIM(last_message.content)<>'' THEN SUBSTR(REPLACE(REPLACE(TRIM(last_message.content), CHAR(13), ' '), CHAR(10), ' '),1,160)
                     ELSE NULL
                   END,
                   last_message.role,
                   CASE WHEN last_message.sticker_id IS NOT NULL THEN 'sticker'
                        WHEN last_message.image_path IS NOT NULL THEN 'image' ELSE 'text' END,
                   last_message.created_at, last_message.id, c.last_read_message_id,
                   (SELECT COUNT(*) FROM messages unread WHERE unread.conversation_id=c.id
                    AND unread.role='assistant' AND unread.id>c.last_read_message_id)
            FROM conversations c
            LEFT JOIN messages last_message ON last_message.id=(
                SELECT latest.id FROM messages latest WHERE latest.conversation_id=c.id ORDER BY latest.id DESC LIMIT 1)
            WHERE c.id=$id
            """;
        command.Parameters.AddWithValue("$id", conversationId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? ReadConversationRoom(reader)
            : null;
    }

    private static ConversationRoom ReadConversationRoom(SqliteDataReader reader) =>
        new(reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2),
            DateTimeOffset.Parse(reader.GetString(3)), DateTimeOffset.Parse(reader.GetString(4)),
            Convert.ToInt32(reader.GetInt64(5)), reader.IsDBNull(6) ? null : reader.GetString(6), reader.GetInt64(7) != 0)
        {
            LastMessagePreview = reader.IsDBNull(8) ? null : reader.GetString(8),
            LastMessageRole = reader.IsDBNull(9) ? null : Enum.Parse<ChatRole>(reader.GetString(9), true),
            LastMessageKind = reader.IsDBNull(10) ? null : reader.GetString(10),
            LastMessageAt = reader.IsDBNull(11) ? null : DateTimeOffset.Parse(reader.GetString(11)),
            LastMessageId = reader.IsDBNull(12) ? null : reader.GetInt64(12),
            LastReadMessageId = reader.GetInt64(13),
            UnreadCount = reader.GetInt64(14)
        };

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

    public async Task DeleteConversationAsync(string conversationId, CancellationToken cancellationToken = default,
        bool forgetExclusiveMemories = false)
    {
        await using var connection = await OpenAsync(cancellationToken); await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        // D4: dropping a conversation drops its records, not the facts it taught (MEM-006).
        // The opt-in cascade (N8) additionally retracts facts this conversation alone taught, via D1.
        await SqliteMemoryRepository.DetachMemoriesForDeletedMessagesAsync(connection, transaction,
            conversationId, DoomedMessages.EntireConversation, 0, forgetExclusiveMemories, cancellationToken);
        foreach (var sql in new[]
        {
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
        command.CommandText = "SELECT id,role,content,created_at,image_path,sticker_id,source FROM messages WHERE conversation_id=$c AND id<$id AND role='user' ORDER BY id DESC LIMIT 1";
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
        command.CommandText = "SELECT id, role, content, created_at, image_path, sticker_id, source FROM messages WHERE conversation_id=$c ORDER BY id DESC LIMIT $n";
        command.Parameters.AddWithValue("$c", conversationId);
        command.Parameters.AddWithValue("$n", count);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new ChatMessage(reader.GetInt64(0), conversationId, Enum.Parse<ChatRole>(reader.GetString(1), true),
                reader.GetString(2), DateTimeOffset.Parse(reader.GetString(3)), reader.IsDBNull(4) ? null : reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5),
                Source: reader.GetString(6)));
        result.Reverse();
        return result;
    }

    public async Task<long> AddMemoryAsync(string characterId, string kind, string summary, string tags,
        double importance, long? sourceMessageId, CancellationToken cancellationToken = default)
    {
        var saved = await LongTerm.SaveLegacyManualAsync(MemoryScope.ForCharacter(characterId), kind, summary, tags,
            importance, sourceMessageId, cancellationToken);
        return saved.Record.Id;
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
            WHERE character_id=$c AND scope_key='character:'||$c AND status='active'
              AND ($q='' OR summary LIKE $like OR tags LIKE $like OR kind LIKE $like)
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
        command.CommandText = "SELECT id,character_id,kind,summary,tags,importance,source_message_id,created_at,updated_at,last_used_at FROM memories WHERE id=$id AND character_id=$c AND scope_key='character:'||$c AND status='active'";
        command.Parameters.AddWithValue("$id", id); command.Parameters.AddWithValue("$c", characterId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadMemory(reader) : null;
    }

    public async Task<ChatMessage?> GetMessageAsync(string conversationId, long id, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken); await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id,role,content,created_at,image_path,sticker_id,source FROM messages WHERE conversation_id=$c AND id=$id";
        command.Parameters.AddWithValue("$c", conversationId); command.Parameters.AddWithValue("$id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadChatMessage(reader, conversationId) : null;
    }

    public async Task UpdateMemoryAsync(long id, string characterId, string kind, string summary, string tags,
        double importance, CancellationToken cancellationToken = default)
    {
        await LongTerm.SaveManualAsync(MemoryScope.ForCharacter(characterId), kind, summary, tags, importance,
            id, cancellationToken);
    }

    public async Task DeleteMemoryAsync(long id, string characterId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM memories WHERE id=$id AND character_id=$c AND scope_key='character:'||$c";
        command.Parameters.AddWithValue("$id", id); command.Parameters.AddWithValue("$c", characterId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<long> CreateReplyPlanAsync(string conversationId, IReadOnlyList<PlannedReplyMessage> messages,
        CancellationToken cancellationToken = default, MemoryScope? memoryScope = null,
        IReadOnlyList<long>? memoryInputIds = null)
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
        if (memoryScope is not null && memoryInputIds is { Count: > 0 })
            await SqliteMemoryRepository.RegisterPlanAsync(connection, transaction, planId,
                memoryScope, memoryInputIds, cancellationToken);
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
            insert.CommandText = "INSERT INTO messages(conversation_id, role, content, sticker_id, source, created_at) SELECT conversation_id,'assistant',$c,$i,$source,$at FROM reply_plans WHERE id=$p; SELECT last_insert_rowid();";
            insert.Parameters.AddWithValue("$c", message.Content ?? ""); insert.Parameters.AddWithValue("$i", (object?)message.StickerId ?? DBNull.Value);
            insert.Parameters.AddWithValue("$at", deliveredAt.ToString("O")); insert.Parameters.AddWithValue("$p", planId);
            insert.Parameters.AddWithValue("$source", MessageSources.ModelReply);
            messageId = Convert.ToInt64(await insert.ExecuteScalarAsync(cancellationToken));
        }
        await using (var room = connection.CreateCommand())
        {
            room.Transaction = transaction;
            room.CommandText = "UPDATE conversations SET updated_at=$at WHERE id=(SELECT conversation_id FROM reply_plans WHERE id=$p)";
            room.Parameters.AddWithValue("$at", deliveredAt.ToString("O")); room.Parameters.AddWithValue("$p", planId);
            await room.ExecuteNonQueryAsync(cancellationToken);
        }
        await SqliteMemoryRepository.RecordDeliveryAsync(connection, transaction, planId, messageId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return messageId;
    }

    public async Task CancelPendingPlanMessagesAsync(long planId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE planned_messages SET state='cancelled' WHERE reply_plan_id=$p AND state='pending';
            DELETE FROM memory_turn_sources WHERE plan_id=$p;
            DELETE FROM memory_turn_deliveries WHERE plan_id=$p;
            """;
        command.Parameters.AddWithValue("$p", planId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MemoryRecord>> SearchMemoriesAsync(string characterId, string query, int count = 6,
        CancellationToken cancellationToken = default)
    {
        var context = await new MemoryQueryService(LongTerm).RetrieveAsync(
            new MemoryQuery(MemoryScope.ForCharacter(characterId), "", query, count), cancellationToken);
        return context.Hits.Select(x => x.Memory.Record).ToArray();
    }

    public async Task<IReadOnlyList<MemoryEmbeddingSource>> ListPendingMemoryEmbeddingsAsync(string modelId,
        int count = 64, CancellationToken cancellationToken = default)
    {
        var result = new List<MemoryEmbeddingSource>();
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT m.id,m.summary,m.tags
            FROM memories m
            LEFT JOIN memory_embeddings e ON e.memory_id=m.id AND e.model_id=$model
            WHERE e.memory_id IS NULL OR e.content_fingerprint<>(m.summary || char(10) || m.tags)
            ORDER BY m.id LIMIT $count
            """;
        command.Parameters.AddWithValue("$model", modelId);
        command.Parameters.AddWithValue("$count", Math.Clamp(count, 1, 512));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new MemoryEmbeddingSource(reader.GetInt64(0), reader.GetString(1), reader.GetString(2)));
        return result;
    }

    public async Task UpsertMemoryEmbeddingAsync(MemoryEmbeddingSource source, string modelId, float[] vector,
        CancellationToken cancellationToken = default)
    {
        if (vector.Length == 0 || vector.Any(value => !float.IsFinite(value)))
            throw new ArgumentException("Embedding vector must contain finite values.", nameof(vector));
        var bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO memory_embeddings(memory_id,model_id,dimensions,vector,content_fingerprint,updated_at)
            VALUES($id,$model,$dimensions,$vector,$fingerprint,$at)
            ON CONFLICT(memory_id,model_id) DO UPDATE SET dimensions=excluded.dimensions,vector=excluded.vector,
              content_fingerprint=excluded.content_fingerprint,updated_at=excluded.updated_at
            """;
        command.Parameters.AddWithValue("$id", source.Id);
        command.Parameters.AddWithValue("$model", modelId);
        command.Parameters.AddWithValue("$dimensions", vector.Length);
        command.Parameters.Add("$vector", SqliteType.Blob).Value = bytes;
        command.Parameters.AddWithValue("$fingerprint", source.Content);
        command.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MemoryVectorRecord>> ListMemoryVectorsAsync(string characterId, string modelId,
        CancellationToken cancellationToken = default)
    {
        var result = new List<MemoryVectorRecord>();
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT m.id,m.character_id,m.kind,m.summary,m.tags,m.importance,m.source_message_id,m.created_at,
                   m.updated_at,m.last_used_at,e.dimensions,e.vector
            FROM memories m JOIN memory_embeddings e ON e.memory_id=m.id
            WHERE m.character_id=$character AND e.model_id=$model
            """;
        command.Parameters.AddWithValue("$character", characterId);
        command.Parameters.AddWithValue("$model", modelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var memory = ReadMemory(reader);
            var dimensions = reader.GetInt32(10);
            var bytes = (byte[])reader[11];
            if (bytes.Length != dimensions * sizeof(float)) continue;
            var vector = new float[dimensions];
            Buffer.BlockCopy(bytes, 0, vector, 0, bytes.Length);
            result.Add(new MemoryVectorRecord(memory, vector));
        }
        return result;
    }

    public async Task<(int Indexed, int Pending)> GetMemoryEmbeddingCountsAsync(string modelId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
              SUM(CASE WHEN e.memory_id IS NOT NULL AND e.content_fingerprint=(m.summary || char(10) || m.tags) THEN 1 ELSE 0 END),
              SUM(CASE WHEN e.memory_id IS NULL OR e.content_fingerprint<>(m.summary || char(10) || m.tags) THEN 1 ELSE 0 END)
            FROM memories m LEFT JOIN memory_embeddings e ON e.memory_id=m.id AND e.model_id=$model
            """;
        command.Parameters.AddWithValue("$model", modelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return (reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetInt64(0)),
            reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetInt64(1)));
    }

    public async Task ClearMemoryEmbeddingsAsync(string modelId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM memory_embeddings WHERE model_id=$model";
        command.Parameters.AddWithValue("$model", modelId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task TouchMemoriesAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default)
    {
        var values = ids.Distinct().Take(100).ToArray();
        if (values.Length == 0) return;
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"UPDATE memories SET last_used_at=$at WHERE id IN ({string.Join(',', values.Select((_, index) => $"$id{index}"))})";
        command.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O"));
        for (var index = 0; index < values.Length; index++) command.Parameters.AddWithValue($"$id{index}", values[index]);
        await command.ExecuteNonQueryAsync(cancellationToken);
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
            select.Transaction = transaction; select.CommandText = $"SELECT id,role,content,created_at,image_path,sticker_id,source FROM messages WHERE conversation_id=$c AND id{op}$id ORDER BY id";
            select.Parameters.AddWithValue("$c", conversationId); select.Parameters.AddWithValue("$id", messageId);
            await using var reader = await select.ExecuteReaderAsync(cancellationToken); while (await reader.ReadAsync(cancellationToken)) messages.Add(ReadChatMessage(reader, conversationId));
        }
        if (messages.Count == 0) return null;
        var detached = await SqliteMemoryRepository.DetachMemoriesForDeletedMessagesAsync(connection, transaction,
            conversationId, includeFollowing ? DoomedMessages.FromMessage : DoomedMessages.SingleMessage,
            messageId, false, cancellationToken);
        // 事件行的 payload 含对话明文，所以删除必须与领域写入同事务：先遮蔽再删消息，
        // 顺序反过来就删不到 subject_message_id 的取值范围了。
        await MaskProtocolEventsAsync(connection, transaction, conversationId, op, messageId, cancellationToken);
        await using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = $"DELETE FROM messages WHERE conversation_id=$c AND id{op}$id";
            delete.Parameters.AddWithValue("$c", conversationId); delete.Parameters.AddWithValue("$id", messageId);
            await delete.ExecuteNonQueryAsync(cancellationToken);
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
        return new ConversationMutationSnapshot(conversationId, roomSnapshot, messages, detached, maxMessageId);
    }

    public async Task RestoreConversationMutationAsync(ConversationMutationSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        // Messages that arrived after the snapshot are discarded by this undo, so they detach like any other
        // deleted record before the history itself goes back.
        await SqliteMemoryRepository.DetachMemoriesForDeletedMessagesAsync(connection, transaction,
            snapshot.ConversationId, DoomedMessages.AboveMessage, snapshot.MaxMessageIdBeforeMutation, false, cancellationToken);
        await using (var sweep = connection.CreateCommand())
        {
            sweep.Transaction = transaction;
            sweep.CommandText = "DELETE FROM messages WHERE conversation_id=$c AND id>$max";
            sweep.Parameters.AddWithValue("$c", snapshot.ConversationId); sweep.Parameters.AddWithValue("$max", snapshot.MaxMessageIdBeforeMutation);
            await sweep.ExecuteNonQueryAsync(cancellationToken);
        }
        foreach (var message in snapshot.Messages)
        {
            await using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "INSERT OR REPLACE INTO messages(id,conversation_id,role,content,image_path,sticker_id,source,created_at) VALUES($id,$c,$r,$text,$image,$sticker,$source,$at)";
            command.Parameters.AddWithValue("$id", message.Id); command.Parameters.AddWithValue("$c", message.ConversationId); command.Parameters.AddWithValue("$r", message.Role.ToString().ToLowerInvariant()); command.Parameters.AddWithValue("$text", message.Content);
            command.Parameters.AddWithValue("$image", (object?)message.ImagePath ?? DBNull.Value); command.Parameters.AddWithValue("$sticker", (object?)message.StickerId ?? DBNull.Value); command.Parameters.AddWithValue("$at", message.CreatedAt.ToString("O"));
            command.Parameters.AddWithValue("$source", message.Source);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        // Memory rows were never deleted by the mutation, so undo rewrites only the state this snapshot
        // captured. Rebuilding them from ten columns used to clear scope_key and fingerprint.
        await SqliteMemoryRepository.RestoreDetachedMemoriesAsync(connection, transaction,
            snapshot.DetachedMemories, cancellationToken);
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
        reader.IsDBNull(4) ? null : reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5),
        Source: reader.GetString(6));

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

    /// <summary>读当前结构版本；表还不存在（全新库）时按 0 处理，让日志能区分"第一次建库"和"就地升级"。</summary>
    private static async Task<int> ReadSchemaVersionAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(version), 1) FROM schema_info";
        try { return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)); }
        catch (SqliteException) { return 0; }
    }

    private static async Task EnsureColumnAsync(SqliteConnection connection, string table, string column,
        string declaration, CancellationToken cancellationToken, SqliteTransaction? transaction = null)
    {
        await using var check = connection.CreateCommand();
        check.Transaction = transaction;
        check.CommandText = $"PRAGMA table_info({table})";
        await using var reader = await check.ExecuteReaderAsync(cancellationToken);
        var exists = false;
        while (await reader.ReadAsync(cancellationToken)) if (reader.GetString(1) == column) { exists = true; break; }
        await reader.DisposeAsync();
        if (exists) return;
        await using var alter = connection.CreateCommand();
        alter.Transaction = transaction;
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {declaration}";
        await alter.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>当前数据库结构版本，供协议握手与诊断读取；SQL 不出本层。</summary>
    public async Task<int> GetSchemaVersionAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(version), 1) FROM schema_info";
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task SetSchemaVersionAsync(SqliteConnection connection, int version,
        CancellationToken cancellationToken, SqliteTransaction? transaction = null)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE schema_info SET version=$version WHERE version<$version";
        command.Parameters.AddWithValue("$version", version);
        await command.ExecuteNonQueryAsync(cancellationToken);
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

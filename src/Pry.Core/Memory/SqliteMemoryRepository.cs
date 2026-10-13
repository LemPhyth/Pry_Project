using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Pry.Core.Models;

namespace Pry.Core.Memory;

/// <summary>Memory-owned storage semantics. The platform supplies the database path and calls migration.</summary>
public sealed class SqliteMemoryRepository(string databasePath) : IMemoryRepository
{
    private readonly string _connectionString = new SqliteConnectionStringBuilder
    { DataSource = databasePath, ForeignKeys = true, DefaultTimeout = 5 }.ToString();

    private async Task<SqliteConnection> Open(CancellationToken token)
    {
        var connection = new SqliteConnection(_connectionString);
        try { await connection.OpenAsync(token); return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }

    private static SqliteCommand Command(SqliteConnection c, SqliteTransaction? tx, string sql,
        params (string Name, object? Value)[] args)
    {
        var command = c.CreateCommand(); command.Transaction = tx; command.CommandText = sql;
        foreach (var (name, value) in args) command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return command;
    }

    private static async Task<int> Execute(SqliteConnection c, SqliteTransaction? tx, string sql,
        CancellationToken token, params (string, object?)[] args)
    {
        await using var cmd = Command(c, tx, sql, args);
        return await cmd.ExecuteNonQueryAsync(token);
    }

    private static async Task<long> Number(SqliteConnection c, SqliteTransaction? tx, string sql,
        CancellationToken token, params (string, object?)[] args)
    {
        await using var cmd = Command(c, tx, sql, args);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(token) ?? 0L);
    }

    public static string Fingerprint(string kind, string content) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(kind.ToLowerInvariant() + "|" + Normalize(content))));
    private static string Normalize(string value) => Regex.Replace(value.Trim().TrimEnd('.', '。'), @"\s+", " ").ToLowerInvariant();
    private static long Stamp(DateTimeOffset now) => now.ToUnixTimeMilliseconds();

    public async Task InitializeAsync(CancellationToken token = default)
    {
        await using var c = await Open(token);
        using var tx = c.BeginTransaction(deferred: false);
        var columns = new HashSet<string>();
        await using (var cmd = Command(c, tx, "PRAGMA table_info(memories)"))
        await using (var reader = await cmd.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token)) columns.Add(reader.GetString(1));
        foreach (var (name, type) in new (string, string)[] {
            ("scope_key", "TEXT NOT NULL DEFAULT ''"), ("status", "TEXT NOT NULL DEFAULT 'active'"),
            ("confidence", "REAL NOT NULL DEFAULT 1"), ("fingerprint", "TEXT NOT NULL DEFAULT ''"),
            ("topic_key", "TEXT NOT NULL DEFAULT ''"), ("origin", "TEXT NOT NULL DEFAULT 'manual'"),
            ("state_version", "INTEGER NOT NULL DEFAULT 1"), ("supersedes_id", "INTEGER NULL") })
            if (!columns.Contains(name)) await Execute(c, tx, $"ALTER TABLE memories ADD COLUMN {name} {type}", token);
        await Execute(c, tx, """
            CREATE TABLE IF NOT EXISTS memory_schema(version INTEGER NOT NULL);
            INSERT INTO memory_schema(version) SELECT 1 WHERE NOT EXISTS(SELECT 1 FROM memory_schema);
            CREATE TABLE IF NOT EXISTS memory_scopes(
              scope_key TEXT PRIMARY KEY, character_id TEXT NOT NULL, epoch INTEGER NOT NULL DEFAULT 0,
              forget_before INTEGER NOT NULL DEFAULT 0);
            CREATE TABLE IF NOT EXISTS memory_scope_conversations(
              conversation_id TEXT PRIMARY KEY REFERENCES conversations(id) ON DELETE CASCADE,
              scope_key TEXT NOT NULL REFERENCES memory_scopes(scope_key));
            CREATE TABLE IF NOT EXISTS memory_evidence(
              memory_id INTEGER NOT NULL REFERENCES memories(id) ON DELETE CASCADE,
              message_id INTEGER NOT NULL REFERENCES messages(id) ON DELETE CASCADE,
              conversation_id TEXT NOT NULL, excerpt TEXT NOT NULL,
              PRIMARY KEY(memory_id,message_id));
            CREATE TABLE IF NOT EXISTS memory_tombstones(
              memory_id INTEGER PRIMARY KEY, scope_key TEXT NOT NULL, fingerprint TEXT NOT NULL);
            CREATE INDEX IF NOT EXISTS ix_memory_tombstone_scope ON memory_tombstones(scope_key,fingerprint);
            CREATE TABLE IF NOT EXISTS memory_candidates(
              id INTEGER PRIMARY KEY AUTOINCREMENT, scope_key TEXT NOT NULL REFERENCES memory_scopes(scope_key),
              conversation_id TEXT NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
              turn_id TEXT NOT NULL, input_hash TEXT NOT NULL, payload TEXT NOT NULL,
              epoch INTEGER NOT NULL, state TEXT NOT NULL DEFAULT 'pending', attempt INTEGER NOT NULL DEFAULT 0,
              created_at INTEGER NOT NULL, available_at INTEGER NOT NULL, lease_until INTEGER NOT NULL DEFAULT 0,
              lease_token TEXT NOT NULL DEFAULT '', reason TEXT NOT NULL DEFAULT '',
              UNIQUE(scope_key,conversation_id,turn_id));
            CREATE INDEX IF NOT EXISTS ix_memory_jobs_dispatch ON memory_candidates(state,available_at,id);
            CREATE TABLE IF NOT EXISTS memory_turn_sources(
              plan_id INTEGER PRIMARY KEY REFERENCES reply_plans(id) ON DELETE CASCADE,
              scope_key TEXT NOT NULL, character_id TEXT NOT NULL, input_ids TEXT NOT NULL, epoch INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS memory_turn_deliveries(
              plan_id INTEGER NOT NULL REFERENCES reply_plans(id) ON DELETE CASCADE,
              message_id INTEGER NOT NULL REFERENCES messages(id) ON DELETE CASCADE,
              PRIMARY KEY(plan_id,message_id));
            CREATE INDEX IF NOT EXISTS ix_memory_scope_active ON memories(scope_key,status);
            """, token);
        var version = await Number(c, tx, "SELECT MAX(version) FROM memory_schema", token);
        if (version is < 1 or > 2) throw new InvalidOperationException("Unsupported memory component schema version.");
        var legacy = new List<(long Id, string Character, string Kind, string Content)>();
        await using (var cmd = Command(c, tx, "SELECT id,character_id,kind,summary FROM memories WHERE scope_key='' OR fingerprint=''"))
        await using (var reader = await cmd.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token)) legacy.Add((reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
        foreach (var row in legacy)
        {
            await EnsureScope(c, tx, MemoryScope.ForCharacter(row.Character), token);
            await Execute(c, tx, "UPDATE memories SET scope_key=$scope,fingerprint=$hash WHERE id=$id", token,
                ("$scope", MemoryScope.ForCharacter(row.Character).Key), ("$hash", Fingerprint(row.Kind, row.Content)), ("$id", row.Id));
        }
        // Preserve legacy duplicates for management, but only one can participate in retrieval.
        await Execute(c, tx, """
            UPDATE memories SET status='archived' WHERE status='active' AND fingerprint<>'' AND id NOT IN
              (SELECT MIN(id) FROM memories WHERE status='active' GROUP BY scope_key,fingerprint);
            CREATE UNIQUE INDEX IF NOT EXISTS ux_memory_fingerprint ON memories(scope_key,fingerprint)
              WHERE status='active' AND fingerprint<>'';
            CREATE TRIGGER IF NOT EXISTS memory_deletion_fence BEFORE DELETE ON memories BEGIN
              INSERT OR IGNORE INTO memory_tombstones VALUES(old.id,old.scope_key,old.fingerprint);
              UPDATE memory_scopes SET epoch=epoch+1,forget_before=MAX(forget_before,COALESCE((SELECT MAX(id) FROM messages),0)) WHERE scope_key=old.scope_key;
              UPDATE memory_candidates SET state='abandoned',payload='',reason='memory_deleted'
                WHERE scope_key=old.scope_key AND state IN ('pending','claimed');
            END;
            CREATE TRIGGER IF NOT EXISTS memory_prevent_undo_resurrection BEFORE INSERT ON memories
              WHEN EXISTS(SELECT 1 FROM memory_tombstones WHERE memory_id=new.id) BEGIN SELECT RAISE(IGNORE); END;
            CREATE TRIGGER IF NOT EXISTS memory_manual_edit_fence AFTER UPDATE OF summary,kind,tags,importance ON memories BEGIN
              UPDATE memory_scopes SET epoch=epoch+1 WHERE scope_key=old.scope_key;
              UPDATE memory_candidates SET state='abandoned',payload='',reason='memory_edited'
                WHERE scope_key=old.scope_key AND state IN ('pending','claimed');
            END;
            """, token);
        // Version 2 has no DDL of its own: memories.status was already free text and its scope index already
        // existed. The number changes because a database can now hold 'orphaned' rows, which an older binary
        // would inject as if they were active.
        if (version == 1) await Execute(c, tx, "UPDATE memory_schema SET version=2", token);
        await using (var check = Command(c, tx, "PRAGMA foreign_key_check"))
        await using (var reader = await check.ExecuteReaderAsync(token))
            if (await reader.ReadAsync(token)) throw new InvalidOperationException("Memory migration foreign key check failed.");
        await using (var check = Command(c, tx, "PRAGMA integrity_check"))
            if (!string.Equals(await check.ExecuteScalarAsync(token) as string, "ok", StringComparison.Ordinal))
                throw new InvalidOperationException("Memory migration integrity check failed.");
        await tx.CommitAsync(token);
    }

    private static async Task EnsureScope(SqliteConnection c, SqliteTransaction? tx, MemoryScope scope, CancellationToken token)
    {
        scope.Validate();
        await Execute(c, tx, "INSERT OR IGNORE INTO memory_scopes(scope_key,character_id) VALUES($s,$c)", token,
            ("$s", scope.Key), ("$c", scope.CharacterId));
        if (await Number(c, tx, "SELECT COUNT(*) FROM memory_scopes WHERE scope_key=$s AND character_id=$c", token,
                ("$s", scope.Key), ("$c", scope.CharacterId)) != 1) throw new ArgumentException("Memory scope ownership mismatch.");
    }

    // Explicit host binding permits independent instances of the same character without guessing a user id.
    public async Task BindConversationAsync(MemoryScope scope, string conversationId, CancellationToken token = default)
    {
        await using var c = await Open(token); using var tx = c.BeginTransaction(deferred: false);
        await EnsureScope(c, tx, scope, token);
        if (await Number(c, tx, "SELECT COUNT(*) FROM conversations WHERE id=$id AND character_id=$c", token,
                ("$id", conversationId), ("$c", scope.CharacterId)) != 1) throw new ArgumentException("Conversation ownership mismatch.");
        await Execute(c, tx, "INSERT OR IGNORE INTO memory_scope_conversations VALUES($c,$s)", token,
            ("$c", conversationId), ("$s", scope.Key));
        if (await Number(c, tx, "SELECT COUNT(*) FROM memory_scope_conversations WHERE conversation_id=$c AND scope_key=$s", token,
                ("$c", conversationId), ("$s", scope.Key)) != 1) throw new ArgumentException("Conversation is already bound to another memory scope.");
        await tx.CommitAsync(token);
    }

    public async Task<long> RevisionAsync(MemoryScope scope, CancellationToken token = default)
    {
        scope.Validate(); await using var c = await Open(token);
        return await Number(c, null, "SELECT epoch FROM memory_scopes WHERE scope_key=$s AND character_id=$c", token,
            ("$s", scope.Key), ("$c", scope.CharacterId));
    }

    public async Task<IReadOnlyList<MemoryEntry>> ListAsync(MemoryScope scope, CancellationToken token = default)
    {
        scope.Validate(); await using var c = await Open(token);
        using var tx = c.BeginTransaction(deferred: true);
        var result = new List<MemoryEntry>();
        await using (var cmd = Command(c, tx, """
            SELECT id,character_id,kind,summary,tags,importance,source_message_id,created_at,updated_at,last_used_at,
              status,confidence,topic_key,origin,state_version FROM memories WHERE scope_key=$s AND character_id=$c
            ORDER BY id
            """, ("$s", scope.Key), ("$c", scope.CharacterId)))
        await using (var reader = await cmd.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token))
                result.Add(new MemoryEntry(new MemoryRecord(reader.GetInt64(0), reader.GetString(1), reader.GetString(2),
                    reader.GetString(3), reader.GetString(4), reader.GetDouble(5), reader.IsDBNull(6) ? null : reader.GetInt64(6),
                    DateTimeOffset.Parse(reader.GetString(7)), reader.IsDBNull(8) ? null : DateTimeOffset.Parse(reader.GetString(8)),
                    reader.IsDBNull(9) ? null : DateTimeOffset.Parse(reader.GetString(9))), scope.Key,
                    reader.GetString(10), reader.GetDouble(11), reader.GetString(12), reader.GetString(13), reader.GetInt64(14), []));
        var evidence = new Dictionary<long, List<MemoryEvidence>>();
        await using (var cmd = Command(c, tx, """
            SELECT e.memory_id,e.message_id,e.conversation_id,e.excerpt FROM memory_evidence e
            JOIN memories m ON m.id=e.memory_id WHERE m.scope_key=$s AND m.character_id=$c ORDER BY e.message_id
            """, ("$s", scope.Key), ("$c", scope.CharacterId)))
        await using (var reader = await cmd.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token))
            {
                var id = reader.GetInt64(0);
                if (!evidence.TryGetValue(id, out var entries)) evidence[id] = entries = [];
                entries.Add(new MemoryEvidence(reader.GetInt64(1), reader.GetString(2), reader.GetString(3)));
            }
        return result.Select(x => x with { Evidence = evidence.GetValueOrDefault(x.Record.Id) ?? [] }).ToArray();
    }

    public Task<MemoryEntry> SaveManualAsync(MemoryScope scope, string kind, string content, string tags,
        double importance, long? id = null, CancellationToken token = default) =>
        SaveManualCoreAsync(scope, kind, content, tags, importance, id, null, token);

    internal Task<MemoryEntry> SaveLegacyManualAsync(MemoryScope scope, string kind, string content, string tags,
        double importance, long? sourceMessageId, CancellationToken token) =>
        SaveManualCoreAsync(scope, kind, content, tags, importance, null, sourceMessageId, token);

    private async Task<MemoryEntry> SaveManualCoreAsync(MemoryScope scope, string kind, string content, string tags,
        double importance, long? id, long? sourceMessageId, CancellationToken token)
    {
        ValidateContent(kind, content, importance);
        if (tags.Length > 2000) throw new ArgumentException("Tags too long.");
        await using var c = await Open(token); using var tx = c.BeginTransaction(deferred: false);
        await EnsureScope(c, tx, scope, token);
        if (sourceMessageId is not null && await Number(c, tx, """
            SELECT COUNT(*) FROM messages m JOIN conversations c ON c.id=m.conversation_id
            WHERE m.id=$id AND (c.character_id=$c OR c.character_id IS NULL)
            """, token, ("$id", sourceMessageId), ("$c", scope.CharacterId)) != 1)
            throw new ArgumentException("Invalid source message.");
        var hash = Fingerprint(kind, content); var now = DateTimeOffset.UtcNow.ToString("O");
        if (id is not null)
        {
            if (await Execute(c, tx, """
                UPDATE memories SET kind=$k,summary=$text,tags=$tags,importance=$i,fingerprint=$h,origin='manual',
                  confidence=1,state_version=state_version+1,updated_at=$at
                WHERE id=$id AND scope_key=$s AND character_id=$c AND status='active'
                """, token, ("$k", kind), ("$text", content.Trim()), ("$tags", tags), ("$i", importance), ("$h", hash),
                ("$at", now), ("$id", id), ("$s", scope.Key), ("$c", scope.CharacterId)) != 1)
                throw new KeyNotFoundException("Memory not found.");
            // An edited assertion no longer claims that old excerpts prove its new content.
            await Execute(c, tx, "DELETE FROM memory_evidence WHERE memory_id=$id", token, ("$id", id));
        }
        else
        {
            id = await Number(c, tx, "SELECT id FROM memories WHERE scope_key=$s AND fingerprint=$h AND status='active'", token,
                ("$s", scope.Key), ("$h", hash));
            if (id == 0) id = await Number(c, tx, """
                INSERT INTO memories(character_id,kind,summary,tags,importance,created_at,updated_at,scope_key,fingerprint,source_message_id)
                VALUES($c,$k,$text,$tags,$i,$at,$at,$s,$h,$source) RETURNING id
                """, token, ("$c", scope.CharacterId), ("$k", kind), ("$text", content.Trim()), ("$tags", tags),
                ("$i", importance), ("$at", now), ("$s", scope.Key), ("$h", hash), ("$source", sourceMessageId));
        }
        await Fence(c, tx, scope, "manual_change", token);
        await tx.CommitAsync(token);
        return (await ListAsync(scope, token)).Single(x => x.Record.Id == id);
    }

    private static void ValidateContent(string kind, string content, double importance)
    {
        if (string.IsNullOrWhiteSpace(kind) || kind.Length > 64 || kind.Equals("behavior", StringComparison.OrdinalIgnoreCase) ||
            kind.Equals("expression", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(content) || content.Length > 4000 || !double.IsFinite(importance) || importance is < 0 or > 1)
            throw new ArgumentException("Invalid memory type, content or importance.");
    }

    private static async Task Fence(SqliteConnection c, SqliteTransaction tx, MemoryScope scope, string reason, CancellationToken token)
    {
        await Execute(c, tx, """
            UPDATE memory_scopes SET epoch=epoch+1,forget_before=MAX(forget_before,COALESCE((SELECT MAX(id) FROM messages),0)) WHERE scope_key=$s;
            UPDATE memory_candidates SET state='abandoned',payload='',reason=$reason WHERE scope_key=$s AND state IN ('pending','claimed');
            """, token, ("$s", scope.Key), ("$reason", reason));
    }

    public async Task DeleteAsync(MemoryScope scope, long id, CancellationToken token = default)
    {
        await using var c = await Open(token); using var tx = c.BeginTransaction(deferred: false);
        await EnsureScope(c, tx, scope, token);
        await Execute(c, tx, "DELETE FROM memories WHERE id=$id AND scope_key=$s AND character_id=$c", token,
            ("$id", id), ("$s", scope.Key), ("$c", scope.CharacterId));
        await tx.CommitAsync(token);
    }

    public async Task ClearAsync(MemoryScope scope, CancellationToken token = default)
    {
        await using var c = await Open(token); using var tx = c.BeginTransaction(deferred: false);
        await EnsureScope(c, tx, scope, token);
        await Fence(c, tx, scope, "scope_cleared", token); // Also fences an empty scope with queued facts.
        await Execute(c, tx, "DELETE FROM memories WHERE scope_key=$s AND character_id=$c", token,
            ("$s", scope.Key), ("$c", scope.CharacterId));
        await tx.CommitAsync(token);
    }

    // Deleting chat records is not retracting a statement, so this path must never reach
    // DELETE FROM memories: the row stays, only its evidence detaches. That keeps the tombstone
    // triggers exclusive to an explicit memory deletion or a scope clear.
    internal static async Task<IReadOnlyList<DetachedMemory>> DetachMemoriesForDeletedMessagesAsync(
        SqliteConnection c, SqliteTransaction tx, string conversationId, DoomedMessages selection,
        long messageId, CancellationToken token)
    {
        var doomed = $"SELECT id FROM messages WHERE conversation_id=$c AND {Clause(selection)}";
        var affected = $"""
            (m.source_message_id IN ({doomed}) OR
             EXISTS (SELECT 1 FROM memory_evidence e WHERE e.memory_id=m.id AND e.message_id IN ({doomed})))
            """;
        var args = selection == DoomedMessages.EntireConversation
            ? new (string, object?)[] { ("$c", conversationId) }
            : new (string, object?)[] { ("$c", conversationId), ("$m", messageId) };
        var pending = new List<DetachedMemory>();
        await using (var cmd = Command(c, tx, $"""
            SELECT m.id,m.scope_key,m.character_id,m.kind,m.summary,m.tags,m.importance,m.source_message_id,m.created_at,
              m.updated_at,m.last_used_at,m.status,m.confidence,m.topic_key,m.origin,m.state_version,
              (SELECT COUNT(*) FROM memory_evidence keep WHERE keep.memory_id=m.id AND keep.message_id NOT IN ({doomed}))
              + (SELECT CASE WHEN m.source_message_id IS NOT NULL AND m.source_message_id NOT IN ({doomed}) AND
                  NOT EXISTS (SELECT 1 FROM memory_evidence same WHERE same.memory_id=m.id AND
                    same.message_id=m.source_message_id) THEN 1 ELSE 0 END)
            FROM memories m WHERE {affected} ORDER BY m.id
            """, args))
        await using (var reader = await cmd.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token))
                pending.Add(new(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                    reader.GetString(4), reader.GetString(5), reader.GetDouble(6),
                    reader.IsDBNull(7) ? null : reader.GetInt64(7), DateTimeOffset.Parse(reader.GetString(8)),
                    reader.IsDBNull(9) ? null : DateTimeOffset.Parse(reader.GetString(9)),
                    reader.IsDBNull(10) ? null : DateTimeOffset.Parse(reader.GetString(10)), reader.GetString(11),
                    reader.GetDouble(12), reader.GetString(13), reader.GetString(14), reader.GetInt64(15), [],
                    reader.GetInt64(16)));
        if (pending.Count > 0)
        {
            var ids = string.Join(",", pending.Select(x => x.Id));
            var evidence = new Dictionary<long, List<MemoryEvidence>>();
            await using (var cmd = Command(c, tx, $"""
                SELECT memory_id,message_id,conversation_id,excerpt FROM memory_evidence
                WHERE memory_id IN ({ids}) ORDER BY memory_id,message_id
                """))
            await using (var reader = await cmd.ExecuteReaderAsync(token))
                while (await reader.ReadAsync(token))
                {
                    var key = reader.GetInt64(0);
                    if (!evidence.TryGetValue(key, out var list)) evidence[key] = list = [];
                    list.Add(new MemoryEvidence(reader.GetInt64(1), reader.GetString(2), reader.GetString(3)));
                }
            for (var i = 0; i < pending.Count; i++)
                pending[i] = pending[i] with { Evidence = evidence.GetValueOrDefault(pending[i].Id) ?? [] };
        }

        var now = DateTimeOffset.UtcNow.ToString("O");
        var rewritten = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in pending)
        {
            // Superseded and archived rows are already invisible, so demoting them would only cost fidelity.
            if (item.Status != "active") continue;
            var status = item.RemainingSupport > 0 ? "active" : "orphaned";
            // The orphan floor sits under the retrieval confidence gate, so injection needs two failures.
            var confidence = status == "orphaned" ? .4
                : item.Origin.Equals("correction", StringComparison.OrdinalIgnoreCase) ? item.Confidence
                : Math.Min(.99, .9 + .03 * (item.RemainingSupport - 1));
            if (await Execute(c, tx, """
                UPDATE memories SET status=$status,confidence=$confidence,source_message_id=NULL,updated_at=$at,
                  state_version=state_version+1
                WHERE id=$id AND scope_key=$s AND state_version=$expected
                """, token, ("$status", status), ("$confidence", confidence), ("$at", now), ("$id", item.Id),
                ("$s", item.ScopeKey), ("$expected", item.StateVersion)) == 1) rewritten.Add(item.ScopeKey);
        }

        await Execute(c, tx, $"""
            UPDATE memory_candidates SET state='abandoned',payload='',reason='evidence_detached'
            WHERE state IN ('pending','claimed') AND conversation_id=$c
              AND EXISTS (SELECT 1 FROM json_each(memory_candidates.payload,'$.InputMessageIds') j
                WHERE CAST(j.value AS INTEGER) IN ({doomed}))
            """, token, args);
        // Detaching invalidates cached context without forgetting history: epoch advances, watermark holds.
        foreach (var scope in rewritten)
            await Execute(c, tx, "UPDATE memory_scopes SET epoch=epoch+1 WHERE scope_key=$s", token, ("$s", scope));
        return pending;
    }

    private static string Clause(DoomedMessages selection) => selection switch
    {
        DoomedMessages.EntireConversation => "1",
        DoomedMessages.SingleMessage => "id=$m",
        DoomedMessages.FromMessage => "id>=$m",
        DoomedMessages.AboveMessage => "id>$m",
        _ => throw new ArgumentOutOfRangeException(nameof(selection))
    };

    internal static async Task RestoreDetachedMemoriesAsync(SqliteConnection c, SqliteTransaction tx,
        IReadOnlyList<DetachedMemory> detached, CancellationToken token)
    {
        foreach (var item in detached)
        {
            // The compare-and-set targets the version this detach wrote, so a row changed by anyone else,
            // including a second detach inside the same undo, stays untouched.
            await Execute(c, tx, """
                UPDATE memories SET status=$status,confidence=$confidence,source_message_id=$source,updated_at=$updated,
                  last_used_at=$used,state_version=state_version+1
                WHERE id=$id AND scope_key=$s AND state_version=$expected
                """, token, ("$status", item.Status), ("$confidence", item.Confidence),
                ("$source", (object?)item.SourceMessageId ?? DBNull.Value),
                ("$updated", (object?)item.UpdatedAt?.ToString("O") ?? DBNull.Value),
                ("$used", (object?)item.LastUsedAt?.ToString("O") ?? DBNull.Value),
                ("$id", item.Id), ("$s", item.ScopeKey), ("$expected", item.StateVersion + 1));
            // An explicit memory deletion in between cascades the evidence away for good; re-attaching it
            // would both resurrect a deleted row's evidence and break the foreign key.
            if (await Number(c, tx, "SELECT COUNT(*) FROM memories WHERE id=$id AND scope_key=$s", token,
                    ("$id", item.Id), ("$s", item.ScopeKey)) != 1) continue;
            await Execute(c, tx, "DELETE FROM memory_evidence WHERE memory_id=$id", token, ("$id", item.Id));
            foreach (var evidence in item.Evidence)
                await Execute(c, tx, "INSERT OR IGNORE INTO memory_evidence VALUES($m,$message,$conversation,$excerpt)", token,
                    ("$m", item.Id), ("$message", evidence.MessageId), ("$conversation", evidence.ConversationId),
                    ("$excerpt", evidence.Excerpt));
        }
    }

    private static async Task<IReadOnlyList<ChatMessage>> ReadInputs(SqliteConnection c, SqliteTransaction tx,
        CompletedMemoryTurn turn, CancellationToken token)
    {
        var result = new List<ChatMessage>();
        foreach (var id in turn.InputMessageIds)
        {
            await using var cmd = Command(c, tx, "SELECT content,created_at,image_path FROM messages WHERE id=$id AND conversation_id=$c AND role='user'",
                ("$id", id), ("$c", turn.ConversationId));
            await using var reader = await cmd.ExecuteReaderAsync(token);
            if (!await reader.ReadAsync(token)) return [];
            result.Add(new ChatMessage(id, turn.ConversationId, ChatRole.User, reader.GetString(0), DateTimeOffset.Parse(reader.GetString(1)),
                reader.IsDBNull(2) ? null : reader.GetString(2)));
        }
        return result;
    }

    public async Task<WritebackReceipt> EnqueueAsync(CompletedMemoryTurn turn, ConsolidationPolicy policy,
        DateTimeOffset now, CancellationToken token = default)
    {
        policy.Validate(); turn.Scope.Validate();
        if (string.IsNullOrWhiteSpace(turn.TurnId) || turn.TurnId.Length > 128 ||
            string.IsNullOrWhiteSpace(turn.ConversationId) || turn.ConversationId.Length > 128 ||
            turn.InputMessageIds.Count < 1 || turn.InputMessageIds.Count > policy.MaxBatchMessages ||
            turn.DeliveredMessageIds.Count is < 1 or > 16 || turn.InputMessageIds.Any(x => x <= 0) ||
            turn.InputMessageIds.Distinct().Count() != turn.InputMessageIds.Count)
            return new(false, null, "invalid_turn");
        // Only IDs are persisted; extraction re-reads authoritative messages after crashes and deletions.
        var payload = JsonSerializer.Serialize(turn);
        var hash = Fingerprint("turn", payload);
        await using var c = await Open(token); using var tx = c.BeginTransaction(deferred: false);
        await EnsureScope(c, tx, turn.Scope, token);
        await using (var replay = Command(c, tx,
            "SELECT id,input_hash FROM memory_candidates WHERE scope_key=$s AND conversation_id=$c AND turn_id=$t",
            ("$s", turn.Scope.Key), ("$c", turn.ConversationId), ("$t", turn.TurnId)))
        await using (var r = await replay.ExecuteReaderAsync(token))
            if (await r.ReadAsync(token)) return new(r.GetString(1) == hash, r.GetInt64(0), r.GetString(1) == hash ? "replayed" : "idempotency_conflict");
        if (await Number(c, tx, "SELECT COUNT(*) FROM memory_scope_conversations WHERE conversation_id=$c AND scope_key=$s", token,
                ("$c", turn.ConversationId), ("$s", turn.Scope.Key)) != 1) return new(false, null, "scope_mismatch");
        var inputs = await ReadInputs(c, tx, turn, token);
        if (inputs.Count != turn.InputMessageIds.Count) return new(false, null, "invalid_evidence");
        foreach (var id in turn.DeliveredMessageIds)
            if (await Number(c, tx, "SELECT COUNT(*) FROM messages WHERE id=$id AND conversation_id=$c AND role='assistant' AND id>$last", token,
                    ("$id", id), ("$c", turn.ConversationId), ("$last", turn.InputMessageIds.Max())) != 1)
                return new(false, null, "not_delivered");
        if (await Number(c, tx, "SELECT COUNT(*) FROM memory_candidates WHERE state IN ('pending','claimed')", token) >= policy.QueueCapacity)
            return new(false, null, "backpressure");
        var epoch = await Number(c, tx, "SELECT epoch FROM memory_scopes WHERE scope_key=$s", token, ("$s", turn.Scope.Key));
        var idNew = await Number(c, tx, """
            INSERT INTO memory_candidates(scope_key,conversation_id,turn_id,input_hash,payload,epoch,created_at,available_at)
            VALUES($s,$c,$t,$h,$p,$e,$at,$due) RETURNING id
            """, token, ("$s", turn.Scope.Key), ("$c", turn.ConversationId), ("$t", turn.TurnId), ("$h", hash),
            ("$p", payload), ("$e", epoch), ("$at", Stamp(now)), ("$due", Stamp(now.AddSeconds(policy.DelaySeconds))));
        // A bounded per-scope high water mark makes pending work eligible without a model call per message.
        if (await Number(c, tx, "SELECT COALESCE(SUM(json_array_length(payload,'$.InputMessageIds')),0) FROM memory_candidates WHERE scope_key=$s AND state='pending'", token,
                ("$s", turn.Scope.Key)) >= policy.PendingMessageThreshold || inputs.Any(ExplicitFactExtractor.IsHighSignal))
            await Execute(c, tx, "UPDATE memory_candidates SET available_at=$at WHERE scope_key=$s AND state='pending' AND attempt=0", token,
                ("$at", Stamp(now)), ("$s", turn.Scope.Key));
        await tx.CommitAsync(token);
        return new(true, idNew, "queued");
    }

    public async Task<MemoryJob?> ClaimAsync(ConsolidationPolicy policy, DateTimeOffset now, CancellationToken token = default)
    {
        policy.Validate(); await using var c = await Open(token); using var tx = c.BeginTransaction(deferred: false);
        await Execute(c, tx, """
            UPDATE memory_candidates SET state='failed',payload='',reason='retry_exhausted'
              WHERE state IN ('pending','claimed') AND attempt >= $max AND lease_until <= $at;
            UPDATE memory_candidates SET state='abandoned',payload='',reason='stale'
              WHERE state IN ('pending','claimed') AND created_at < $old;
            """, token, ("$max", policy.MaxAttempts), ("$at", Stamp(now)), ("$old", Stamp(now.AddDays(-policy.MaxAgeDays))));
        long id = 0, epoch = 0; int attempt = 0; string? payload = null;
        await using (var cmd = Command(c, tx, """
            SELECT id,payload,epoch,attempt FROM memory_candidates
            WHERE (state='pending' AND available_at <= $at) OR (state='claimed' AND lease_until <= $at)
            ORDER BY id LIMIT 1
            """, ("$at", Stamp(now))))
        await using (var r = await cmd.ExecuteReaderAsync(token))
            if (await r.ReadAsync(token)) { id = r.GetInt64(0); payload = r.GetString(1); epoch = r.GetInt64(2); attempt = r.GetInt32(3); }
        if (payload is null) { await tx.CommitAsync(token); return null; }
        var turn = JsonSerializer.Deserialize<CompletedMemoryTurn>(payload)!;
        var inputs = await ReadInputs(c, tx, turn, token);
        if (inputs.Count != turn.InputMessageIds.Count ||
            epoch != await Number(c, tx, "SELECT epoch FROM memory_scopes WHERE scope_key=$s", token, ("$s", turn.Scope.Key)))
        {
            await Execute(c, tx, "UPDATE memory_candidates SET state='abandoned',payload='',reason='stale_evidence' WHERE id=$id", token, ("$id", id));
            await tx.CommitAsync(token); return null;
        }
        var lease = Guid.NewGuid().ToString("N"); var until = now.AddSeconds(policy.LeaseSeconds);
        await Execute(c, tx, "UPDATE memory_candidates SET state='claimed',attempt=attempt+1,lease_token=$l,lease_until=$until WHERE id=$id", token,
            ("$l", lease), ("$until", Stamp(until)), ("$id", id));
        await tx.CommitAsync(token);
        return new(id, turn, epoch, lease, until, attempt + 1, inputs);
    }

    public async Task<bool> CompleteAsync(MemoryJob job, IReadOnlyList<MemoryCandidate> candidates,
        DateTimeOffset now, CancellationToken token = default)
    {
        if (candidates.Count > 32) throw new ArgumentException("Too many memory candidates.");
        await using var c = await Open(token); using var tx = c.BeginTransaction(deferred: false);
        if (await Number(c, tx, """
            SELECT COUNT(*) FROM memory_candidates j JOIN memory_scopes s ON s.scope_key=j.scope_key
            WHERE j.id=$id AND j.state='claimed' AND j.lease_token=$l AND j.lease_until>$at AND j.epoch=s.epoch AND j.epoch=$e
            """, token, ("$id", job.Id), ("$l", job.LeaseToken), ("$at", Stamp(now)), ("$e", job.Epoch)) != 1) return false;
        var inputs = await ReadInputs(c, tx, job.Turn, token);
        var watermark = await Number(c, tx, "SELECT forget_before FROM memory_scopes WHERE scope_key=$s", token,
            ("$s", job.Turn.Scope.Key));
        // Revalidate content as well as IDs, even when extraction ran outside the transaction.
        var verified = inputs.SelectMany(ExplicitFactExtractor.Extract).ToArray();
        foreach (var candidate in candidates)
        {
            if (!verified.Contains(candidate) || candidate.SourceMessageId <= watermark) continue;
            ValidateContent(candidate.Kind, candidate.Content, candidate.Importance);
            var hash = Fingerprint(candidate.Kind, candidate.Content);
            if (await Number(c, tx, "SELECT COUNT(*) FROM memory_tombstones WHERE scope_key=$s AND fingerprint=$h", token,
                    ("$s", job.Turn.Scope.Key), ("$h", hash)) > 0) continue;
            // A later assertion for the same slot wins regardless of worker completion order.
            if (await Number(c, tx, """
                SELECT COUNT(*) FROM memories m JOIN memory_evidence e ON e.memory_id=m.id
                WHERE m.scope_key=$s AND m.topic_key=$topic AND e.message_id>$source
                """, token, ("$s", job.Turn.Scope.Key), ("$topic", candidate.TopicKey), ("$source", candidate.SourceMessageId)) > 0) continue;
            long supersedes = 0;
            if (candidate.Correction)
            {
                supersedes = await Number(c, tx, "SELECT id FROM memories WHERE scope_key=$s AND topic_key=$topic AND status='active' ORDER BY id DESC LIMIT 1",
                    token, ("$s", job.Turn.Scope.Key), ("$topic", candidate.TopicKey));
                await Execute(c, tx, "UPDATE memories SET status='superseded',state_version=state_version+1 WHERE scope_key=$s AND topic_key=$topic AND fingerprint<>$h AND status='active'",
                    token, ("$s", job.Turn.Scope.Key), ("$topic", candidate.TopicKey), ("$h", hash));
            }
            // Superseded exact assertions cannot be reactivated by old evidence.
            if (!candidate.Correction && await Number(c, tx, "SELECT COUNT(*) FROM memories WHERE scope_key=$s AND fingerprint=$h AND status='superseded'",
                    token, ("$s", job.Turn.Scope.Key), ("$h", hash)) > 0) continue;
            var memoryId = await Number(c, tx, "SELECT id FROM memories WHERE scope_key=$s AND fingerprint=$h AND status='active'", token,
                ("$s", job.Turn.Scope.Key), ("$h", hash));
            if (memoryId == 0)
                memoryId = await Number(c, tx, """
                    INSERT INTO memories(character_id,kind,summary,tags,importance,source_message_id,created_at,updated_at,
                      scope_key,confidence,fingerprint,topic_key,origin,supersedes_id)
                    VALUES($c,$k,$text,'',$i,$source,$at,$at,$s,$confidence,$h,$topic,$origin,$old) RETURNING id
                    """, token, ("$c", job.Turn.Scope.CharacterId), ("$k", candidate.Kind), ("$text", candidate.Content),
                    ("$i", candidate.Importance), ("$source", candidate.SourceMessageId), ("$at", now.ToString("O")),
                    ("$s", job.Turn.Scope.Key), ("$confidence", candidate.Correction ? 1 : .9), ("$h", hash),
                    ("$topic", candidate.TopicKey), ("$origin", candidate.Correction ? "correction" : "extraction"),
                    ("$old", supersedes == 0 ? null : supersedes));
            await Execute(c, tx, "INSERT OR IGNORE INTO memory_evidence VALUES($m,$source,$c,$excerpt)", token,
                ("$m", memoryId), ("$source", candidate.SourceMessageId), ("$c", job.Turn.ConversationId), ("$excerpt", candidate.Excerpt));
            await Execute(c, tx, """
                UPDATE memories SET confidence=CASE WHEN origin='correction' THEN 1 ELSE
                  MIN(.99,.9+.03*((SELECT COUNT(*) FROM memory_evidence WHERE memory_id=$m)-1)) END WHERE id=$m
                """, token, ("$m", memoryId));
        }
        await Execute(c, tx, "UPDATE memory_candidates SET state='succeeded',payload='',reason='' WHERE id=$id", token, ("$id", job.Id));
        await tx.CommitAsync(token); return true;
    }

    public async Task FailAsync(MemoryJob job, ConsolidationPolicy policy, DateTimeOffset now, CancellationToken token = default)
    {
        await using var c = await Open(token);
        await Execute(c, null, """
            UPDATE memory_candidates SET state=$state,available_at=$due,lease_until=0,reason='extraction_failed',
              payload=CASE WHEN $state='failed' THEN '' ELSE payload END
            WHERE id=$id AND lease_token=$l AND state='claimed'
            """, token, ("$state", job.Attempt >= policy.MaxAttempts ? "failed" : "pending"),
            ("$due", Stamp(now.AddSeconds(Math.Min(600, 30 * Math.Pow(4, job.Attempt - 1))))), ("$id", job.Id), ("$l", job.LeaseToken));
    }

    internal static async Task RegisterPlanAsync(SqliteConnection c, SqliteTransaction tx, long planId,
        MemoryScope scope, IReadOnlyList<long> inputs, CancellationToken token)
    {
        if (inputs.Count == 0) return;
        await EnsureScope(c, tx, scope, token);
        await Execute(c, tx, """
            INSERT INTO memory_turn_sources SELECT $p,$s,$c,$ids,epoch FROM memory_scopes WHERE scope_key=$s
            """, token, ("$p", planId), ("$s", scope.Key), ("$c", scope.CharacterId),
            ("$ids", JsonSerializer.Serialize(inputs.TakeLast(16).ToArray())));
    }

    internal static async Task RecordDeliveryAsync(SqliteConnection c, SqliteTransaction tx, long planId,
        long deliveredId, CancellationToken token)
    {
        await Execute(c, tx, "INSERT OR IGNORE INTO memory_turn_deliveries VALUES($p,$m)", token,
            ("$p", planId), ("$m", deliveredId));
        if (await Number(c, tx, "SELECT COUNT(*) FROM planned_messages WHERE reply_plan_id=$p AND state<>'delivered'", token,
                ("$p", planId)) != 0) return;
        MemoryScope? scope = null; long[] inputs = []; long epoch = 0; string conversation = "";
        await using (var cmd = Command(c, tx, """
            SELECT s.scope_key,s.character_id,s.input_ids,s.epoch,p.conversation_id FROM memory_turn_sources s
              JOIN reply_plans p ON p.id=s.plan_id WHERE s.plan_id=$p
            """, ("$p", planId)))
        await using (var r = await cmd.ExecuteReaderAsync(token))
            if (await r.ReadAsync(token))
            {
                scope = new(r.GetString(0), r.GetString(1)); inputs = JsonSerializer.Deserialize<long[]>(r.GetString(2))!;
                epoch = r.GetInt64(3); conversation = r.GetString(4);
            }
        if (scope is null) return;
        var outputs = new List<long>();
        await using (var cmd = Command(c, tx, "SELECT message_id FROM memory_turn_deliveries WHERE plan_id=$p ORDER BY message_id", ("$p", planId)))
        await using (var r = await cmd.ExecuteReaderAsync(token))
            while (await r.ReadAsync(token)) outputs.Add(r.GetInt64(0));
        await RecordCompletedAsync(c, tx, new(scope, conversation, $"plan:{planId}", inputs, outputs), epoch, token);
        await Execute(c, tx, "DELETE FROM memory_turn_sources WHERE plan_id=$p; DELETE FROM memory_turn_deliveries WHERE plan_id=$p", token, ("$p", planId));
    }

    internal static async Task RecordCompletedAsync(SqliteConnection c, SqliteTransaction tx,
        CompletedMemoryTurn turn, long? expectedEpoch, CancellationToken token)
    {
        var policy = new ConsolidationPolicy();
        if (await Number(c, tx, "SELECT COUNT(*) FROM memory_scope_conversations WHERE conversation_id=$c AND scope_key=$s", token,
                ("$c", turn.ConversationId), ("$s", turn.Scope.Key)) != 1) return;
        var epoch = await Number(c, tx, "SELECT epoch FROM memory_scopes WHERE scope_key=$s", token, ("$s", turn.Scope.Key));
        if (expectedEpoch is not null && expectedEpoch != epoch) return;
        if (await Number(c, tx, "SELECT COUNT(*) FROM memory_candidates WHERE state IN ('pending','claimed')", token) >= policy.QueueCapacity) return;
        var inputs = await ReadInputs(c, tx, turn, token);
        if (inputs.Count == 0 || inputs.Count != turn.InputMessageIds.Count) return;
        var now = DateTimeOffset.UtcNow;
        var payload = JsonSerializer.Serialize(turn);
        await Execute(c, tx, """
            INSERT OR IGNORE INTO memory_candidates(scope_key,conversation_id,turn_id,input_hash,payload,epoch,created_at,available_at)
            VALUES($s,$c,$t,$h,$p,$e,$at,$due)
            """, token, ("$s", turn.Scope.Key), ("$c", turn.ConversationId), ("$t", turn.TurnId),
            ("$h", Fingerprint("turn", payload)), ("$p", payload), ("$e", epoch), ("$at", Stamp(now)),
            ("$due", Stamp(now.AddSeconds(policy.DelaySeconds))));
        if (inputs.Any(ExplicitFactExtractor.IsHighSignal) || await Number(c, tx,
            "SELECT COALESCE(SUM(json_array_length(payload,'$.InputMessageIds')),0) FROM memory_candidates WHERE scope_key=$s AND state='pending'", token, ("$s", turn.Scope.Key)) >= policy.PendingMessageThreshold)
            await Execute(c, tx, "UPDATE memory_candidates SET available_at=$at WHERE scope_key=$s AND state='pending' AND attempt=0", token,
                ("$s", turn.Scope.Key), ("$at", Stamp(now)));
    }
}

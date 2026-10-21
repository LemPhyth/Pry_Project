using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Pry.Core.Backup;

/// <summary>
/// 备份时刻的数据库指纹：全部是计数、集合散列与摘要，不含任何用户内容原文。
/// 备份与恢复各算一次、逐字段对照，就是 v4 §9.3 那句"核对行数、稳定 ID、引用、索引、外键、
/// 墓碑、孤立态及偏好"里数据库一侧的执行形态。
/// </summary>
public sealed record DatabaseFingerprint
{
    /// <summary>快照里登记的 schema 版本；恢复侧据此拒绝把"未来版本"的库交回旧二进制写。</summary>
    public int SchemaVersion { get; init; }

    /// <summary>逐表行数（不含 FTS 影子表）。键是表名。</summary>
    public IReadOnlyDictionary<string, long> RowCounts { get; init; } = new Dictionary<string, long>();

    /// <summary>稳定 ID 集合的 SHA-256：同一张表的主键/自然键全集折叠成一个不可逆摘要，
    /// 行数相同但 ID 换了（比如批量重生成）也会在这里露形。</summary>
    public IReadOnlyDictionary<string, string> StableIdDigests { get; init; } = new Dictionary<string, string>();

    /// <summary>memories.status 分布（active/orphaned/archived/…）——孤立态计数的载体。</summary>
    public IReadOnlyDictionary<string, long> MemoryStatusCounts { get; init; } = new Dictionary<string, long>();

    /// <summary>显式遗忘墓碑的数量与内容摘要（v4 §9.3：恢复时删除记录不能随旧快照倒退）。</summary>
    public long TombstoneCount { get; init; }

    public string TombstoneDigest { get; init; } = "";

    /// <summary>各记忆作用域的 (scope_key, epoch, forget_before) 水位摘要。</summary>
    public string ScopeWatermarkDigest { get; init; } = "";

    /// <summary>索引名全集摘要（引用完整性里"索引还在不在"这一格）。</summary>
    public string IndexDigest { get; init; } = "";

    public long ForeignKeyViolations { get; init; }

    /// <summary>库内悬挂引用计数（证据脱链、指向不存在消息的引用等）。纯计数。</summary>
    public IReadOnlyDictionary<string, long> OrphanCounters { get; init; } = new Dictionary<string, long>();
}

/// <summary>
/// SQLite 在线备份与核对的原语层（HDS-001 A/B 条）。全部走 SQL 通道：
/// <c>VACUUM INTO</c> 是 SQLite 官方的在线快照机制，产物是**自包含单文件**——
/// WAL 里尚未落盘主文件的已提交内容会一并进入快照，侧文件（<c>-wal</c>/<c>-shm</c>）天然不需要跟着拷。
/// 只复制主 <c>.db</c> 的做法在 WAL 模式下会得到缺行的残骸，v4 §9.3 点名禁止，这里从机制上不走那条路。
/// 所有连接一律 <c>Pooling=false</c>：备份/恢复是一次性动作，不能把句柄留在进程级连接池里
/// 咬住临时目录（DEF-P1WALK-04 的同族形状）。
/// </summary>
public static class SqliteBackupDiagnostics
{
    /// <summary>稳定 ID 摘要的表与键列（点名列，与持久层现有表面对齐；新表加入需要持久层任务批准）。</summary>
    private static readonly (string Table, string Column)[] StableIdTargets =
    [
        ("conversations", "id"), ("messages", "id"), ("memories", "id"), ("interaction_commands", "id"),
        ("reply_plans", "id"), ("planned_messages", "id"), ("memory_scopes", "scope_key"),
    ];

    /// <summary>对运行中的库做在线一致性快照。目标文件必须不存在（防覆盖，与备份"另存"语义一致）。</summary>
    public static async Task CreateOnlineSnapshotAsync(string sourceDatabasePath, string snapshotTargetPath,
        CancellationToken cancellationToken = default)
    {
        if (File.Exists(snapshotTargetPath))
            throw new BackupIntegrityException($"快照目标已存在，不覆盖：{snapshotTargetPath}");
        var source = new SqliteConnectionStringBuilder
        {
            DataSource = sourceDatabasePath, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false
        }.ToString();
        await using var connection = new SqliteConnection(source);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        // VACUUM INTO 的接受表达式参数（SQLite ≥3.27；本仓捆绑 e_sqlite3 2.1.13 运行时远高于此）。
        command.CommandText = "VACUUM INTO $target";
        command.Parameters.AddWithValue("$target", snapshotTargetPath);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>PRAGMA integrity_check：只有单行 "ok" 才算过，其余原文收进 diagnostics 供错误面呈现。</summary>
    public static async Task<(bool Ok, string Report)> IntegrityCheckAsync(string databasePath,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenReadAsync(databasePath, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check;";
        var lines = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) lines.Add(reader.GetString(0));
        return (lines.Count == 1 && string.Equals(lines[0], "ok", StringComparison.OrdinalIgnoreCase),
            lines.Count == 0 ? "integrity_check 无输出" : string.Join(" | ", lines));
    }

    /// <summary>读快照里登记的 schema 版本（表缺失时按 0，即"未建库"）。</summary>
    public static async Task<int> ReadSchemaVersionAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenReadAsync(databasePath, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(version), 1) FROM schema_info;";
        try { return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)); }
        catch (SqliteException) { return 0; }
    }

    /// <summary>算数据库指纹（行数/稳定 ID/墓碑/水位/索引/外键/孤立态）。表不存在就记 0 或跳过，不炸。</summary>
    public static async Task<DatabaseFingerprint> CaptureFingerprintAsync(string databasePath,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenReadAsync(databasePath, cancellationToken);
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var virtualTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var probe = connection.CreateCommand())
        {
            probe.CommandText = "SELECT name, COALESCE(sql, '') FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
            await using var reader = await probe.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var name = reader.GetString(0);
                tables.Add(name);
                if (reader.GetString(1).Contains("fts5(", StringComparison.OrdinalIgnoreCase)) virtualTables.Add(name);
            }
        }

        var rowCounts = new SortedDictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var table in tables)
        {
            // FTS5 影子表（<虚拟表名>_data/_idx/_docsize/_config/_content）与虚拟表本体是索引实现细节，
            // 不是数据真相；内容表 memory_fts 的行数由 memories 决定，计入只会让核对虚报。
            if (IsFtsShadow(table, virtualTables)) continue;
            await using var count = connection.CreateCommand();
            count.CommandText = $"SELECT COUNT(*) FROM \"{table}\";";
            rowCounts[table] = Convert.ToInt64(await count.ExecuteScalarAsync(cancellationToken) ?? 0L);
        }

        var stableIds = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (table, column) in StableIdTargets)
        {
            if (!tables.Contains(table)) continue;
            await using var digest = connection.CreateCommand();
            digest.CommandText = $"SELECT {column} FROM \"{table}\" ORDER BY 1;";
            var builder = new StringBuilder();
            await using var reader = await digest.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) builder.Append(reader.GetValue(0)).Append(';');
            stableIds[$"{table}.{column}"] = Sha256Hex(builder.ToString());
        }

        var memoryStatus = new SortedDictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        if (tables.Contains("memories"))
        {
            await using var status = connection.CreateCommand();
            status.CommandText = "SELECT COALESCE(status,'(null)') AS s, COUNT(*) FROM memories GROUP BY s ORDER BY s;";
            await using var reader = await status.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) memoryStatus[reader.GetString(0)] = reader.GetInt64(1);
        }

        long tombstones = 0;
        var tombstoneDigest = "";
        if (tables.Contains("memory_tombstones"))
        {
            await using var tomb = connection.CreateCommand();
            tomb.CommandText = "SELECT COUNT(*) FROM memory_tombstones;";
            tombstones = Convert.ToInt64(await tomb.ExecuteScalarAsync(cancellationToken) ?? 0L);
            await using var rows = connection.CreateCommand();
            rows.CommandText = "SELECT memory_id || '|' || scope_key || '|' || fingerprint FROM memory_tombstones ORDER BY memory_id;";
            var builder = new StringBuilder();
            await using var reader2 = await rows.ExecuteReaderAsync(cancellationToken);
            while (await reader2.ReadAsync(cancellationToken)) builder.Append(reader2.GetValue(0)).Append(';');
            tombstoneDigest = Sha256Hex(builder.ToString());
        }

        var scopeWatermark = "";
        if (tables.Contains("memory_scopes"))
        {
            await using var scopes = connection.CreateCommand();
            scopes.CommandText = "SELECT scope_key || '|' || epoch || '|' || forget_before FROM memory_scopes ORDER BY scope_key;";
            var builder = new StringBuilder();
            await using var reader = await scopes.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) builder.Append(reader.GetValue(0)).Append(';');
            scopeWatermark = Sha256Hex(builder.ToString());
        }

        await using var indexes = connection.CreateCommand();
        indexes.CommandText = "SELECT name FROM sqlite_master WHERE type='index' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
        var indexBuilder = new StringBuilder();
        await using (var reader = await indexes.ExecuteReaderAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken)) indexBuilder.Append(reader.GetString(0)).Append(';');

        long fkViolations;
        await using (var fk = connection.CreateCommand())
        {
            fk.CommandText = "SELECT COUNT(*) FROM pragma_foreign_key_check();";
            fkViolations = Convert.ToInt64(await fk.ExecuteScalarAsync(cancellationToken) ?? 0L);
        }

        var orphans = new SortedDictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        if (tables.Contains("memories") && tables.Contains("messages"))
        {
            await using var dangling = connection.CreateCommand();
            dangling.CommandText = """
                SELECT
                  SUM(CASE WHEN m.source_message_id IS NULL THEN 1 ELSE 0 END),
                  SUM(CASE WHEN m.source_message_id IS NOT NULL AND NOT EXISTS(SELECT 1 FROM messages x WHERE x.id=m.source_message_id) THEN 1 ELSE 0 END)
                FROM memories m;
                """;
            await using var reader = await dangling.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                orphans["memories_without_evidence"] = reader.IsDBNull(0) ? 0 : reader.GetInt64(0);
                orphans["memories_dangling_source"] = reader.IsDBNull(1) ? 0 : reader.GetInt64(1);
            }
        }
        if (tables.Contains("messages"))
        {
            await using var images = connection.CreateCommand();
            images.CommandText = "SELECT COUNT(*) FROM messages WHERE image_path IS NOT NULL AND image_path<>'';";
            orphans["messages_with_media_reference"] =
                Convert.ToInt64(await images.ExecuteScalarAsync(cancellationToken) ?? 0L);
        }
        if (tables.Contains("planned_messages"))
        {
            await using var pending = connection.CreateCommand();
            pending.CommandText = "SELECT COUNT(*) FROM planned_messages WHERE state='pending';";
            orphans["planned_messages_pending"] =
                Convert.ToInt64(await pending.ExecuteScalarAsync(cancellationToken) ?? 0L);
        }

        return new DatabaseFingerprint
        {
            SchemaVersion = await ReadSchemaVersionFromConnectionAsync(connection, cancellationToken),
            RowCounts = rowCounts,
            StableIdDigests = stableIds,
            MemoryStatusCounts = memoryStatus,
            TombstoneCount = tombstones,
            TombstoneDigest = tombstoneDigest,
            ScopeWatermarkDigest = scopeWatermark,
            IndexDigest = Sha256Hex(indexBuilder.ToString()),
            ForeignKeyViolations = fkViolations,
            OrphanCounters = orphans,
        };
    }

    /// <summary>偏好文件可解析性核对（用户偏好那一格：字节散列一致还不够，还得读得回来）。</summary>
    public static bool PreferencesParseable(string preferencesPath)
    {
        if (!File.Exists(preferencesPath)) return false;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(preferencesPath));
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException) { return false; }
    }

    private static bool IsFtsShadow(string name, HashSet<string> virtualTables) =>
        virtualTables.Any(v => name.Equals(v + "_content", StringComparison.OrdinalIgnoreCase) ||
                               name.Equals(v + "_idx", StringComparison.OrdinalIgnoreCase) ||
                               name.Equals(v + "_docsize", StringComparison.OrdinalIgnoreCase) ||
                               name.Equals(v + "_config", StringComparison.OrdinalIgnoreCase) ||
                               name.Equals(v + "_data", StringComparison.OrdinalIgnoreCase) ||
                               name.Equals(v, StringComparison.OrdinalIgnoreCase));

    private static async Task<SqliteConnection> OpenReadAsync(string databasePath, CancellationToken cancellationToken)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath, Mode = SqliteOpenMode.ReadWrite, Pooling = false
        };
        var connection = new SqliteConnection(builder.ToString());
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private static async Task<int> ReadSchemaVersionFromConnectionAsync(SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(version), 1) FROM schema_info;";
        try { return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)); }
        catch (SqliteException) { return 0; }
    }

    private static string Sha256Hex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

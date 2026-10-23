using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Pry.Core.Memory;

namespace Pry.Core.Narrative.Storage;

/// <summary>
/// 叙事落库面（HDS-005）：真实 SQLite 表上的条件写、投递段账本与崩溃/部分送达判定。
/// 它是 <see cref="NarrativeCommitWriter"/> / <see cref="DeliveryLedger"/> /
/// <see cref="OutboxOwnershipRegistry"/> 的持久化对偶——闸门次序与理由码逐条对齐既有内存契约
/// （行号出处见报告 §一），但每一项判据都从库里的行读出来，重启连接后判定结果不变：
/// 不在内存里假装校验，也不允许拒绝路径落半个字节。
/// </summary>
public sealed class NarrativeSqliteStore(string databasePath) : IDisposable
{
    private readonly string _connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = databasePath,
        Mode = SqliteOpenMode.ReadWriteCreate,
        Cache = SqliteCacheMode.Shared,
        Pooling = false,
    }.ToString();

    private SqliteConnection? _connection;

    /// <summary>实例登记的结果。<see cref="Created"/> 为 false 且理由为 104 时，既有登记原样保留。</summary>
    public sealed record InstanceRegistrationOutcome(
        bool Accepted,
        bool Created,
        CharacterInstanceId InstanceId,
        NarrativeContractReason Reason,
        string? Detail);

    /// <summary>打开（必要时创建）叙事库：先读平台账，未知高版本在任何写入之前拒绝，随后走登记清单迁移。</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(databasePath))!);
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        var versionBefore = await ReadSchemaVersionAsync(connection, cancellationToken);
        // 高版本保护必须先于任何写入：第一批语句里的 PRAGMA journal_mode=WAL 本身就是写
        // （MemoryDatabase.cs:28-34 同形，HDS-001 E 条 / v4 §9.3）。
        if (versionBefore > MemoryDatabase.SchemaVersion)
        {
            await connection.DisposeAsync();
            throw new SchemaVersionTooNewException(MemoryDatabase.SchemaVersion, versionBefore, "叙事落库面初始化");
        }

        await using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
            await pragma.ExecuteNonQueryAsync(cancellationToken);
        }

        await MemorySchemaMigrator.RunAsync(connection, versionBefore, NarrativeStorageSchema.BuildSteps(),
            cancellationToken);
        _connection = connection;
    }

    public void Dispose() => _connection?.Dispose();

    /// <summary>异步释放：连接句柄同步关闭即可，SQLite 在最后一个连接关闭时完成 WAL 检查点。</summary>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    /// <summary>当前库登记版本（schema_info 平台账）。</summary>
    public async Task<int> SchemaVersionAsync(CancellationToken cancellationToken = default)
    {
        EnsureReady();
        await using var command = Command("SELECT COALESCE(MAX(version), 0) FROM schema_info", null);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    /// <summary>
    /// 结构指纹：schema_info 版本 + sqlite_master 全部对象按 (type,name) 排序后的定义拼接取 SHA-256。
    /// 幂等初始化（重跑不重复建表、不改结构）的可比对读数。
    /// </summary>
    public async Task<string> GetSchemaFingerprintAsync(CancellationToken cancellationToken = default)
    {
        EnsureReady();
        var builder = new StringBuilder();
        await using (var version = Command("SELECT COALESCE(MAX(version), 0) FROM schema_info", null))
        await using (var reader = await version.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                builder.Append(reader.GetInt32(0).ToString(CultureInfo.InvariantCulture)).Append('|');
            }
        }

        await using var objects = Command(
            "SELECT type, name, COALESCE(sql, '') FROM sqlite_master WHERE name NOT LIKE 'sqlite_%' ORDER BY type, name",
            null);
        await using (var reader = await objects.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                builder.Append(reader.GetString(0)).Append('#').Append(reader.GetString(1)).Append('#')
                    .Append(NormalizeDdl(reader.GetString(2))).Append('\n');
            }
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()))).ToLowerInvariant();
    }

    private static string NormalizeDdl(string sql) =>
        string.Join(' ', sql.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    // —————————— 稳定身份与别名（契约 §character_instance / character_instance_alias）——————————

    /// <summary>
    /// 幂等登记：同一 User × Character 已存在时永远保留既有实例，这次提出的新 ID 被忽略并如实上报
    /// <see cref="NarrativeContractReason.IdentityProposalSuperseded"/>（104）——与 CharacterInstanceRegistry.Resolve 同形。
    /// </summary>
    public async Task<InstanceRegistrationOutcome> RegisterInstanceAsync(CharacterInstanceKey key,
        CharacterInstanceId instanceId, InstanceProvenance provenance, string? originAlias = null,
        CancellationToken cancellationToken = default)
    {
        EnsureReady();
        key.Validate();
        await using var probe = Command(
            "SELECT instance_id FROM character_instance WHERE local_user_id=$u AND character_id=$c", null);
        probe.Parameters.AddWithValue("$u", key.LocalUserId);
        probe.Parameters.AddWithValue("$c", key.CharacterId);
        await using (var reader = await probe.ExecuteReaderAsync(cancellationToken))
        {
            if (await reader.ReadAsync(cancellationToken))
            {
                var existing = reader.GetString(0);
                return string.Equals(existing, instanceId.Value, StringComparison.Ordinal)
                    ? new InstanceRegistrationOutcome(true, false, instanceId, NarrativeContractReason.None,
                        "该 User × Character 已登记，幂等返回既有实例。")
                    : new InstanceRegistrationOutcome(false, false, CharacterInstanceId.Create(existing),
                        NarrativeContractReason.IdentityProposalSuperseded,
                        $"实例 {key.LocalUserId}×{key.CharacterId} 已登记为 {existing}，本次提案被忽略且不落任何行。");
            }
        }

        await using var insert = Command(
            """
            INSERT INTO character_instance(local_user_id, character_id, instance_id, provenance,
              mapping_contract_version, origin_alias, updated_at_utc)
            VALUES($u, $c, $i, $p, $v, $a, $at);
            """, null);
        insert.Parameters.AddWithValue("$u", key.LocalUserId);
        insert.Parameters.AddWithValue("$c", key.CharacterId);
        insert.Parameters.AddWithValue("$i", instanceId.Value);
        insert.Parameters.AddWithValue("$p", (int)provenance);
        insert.Parameters.AddWithValue("$v", NarrativeContractVersions.IdentityMapping);
        insert.Parameters.AddWithValue("$a", (object?)originAlias ?? DBNull.Value);
        insert.Parameters.AddWithValue("$at", Format(DateTimeOffset.UtcNow));
        await insert.ExecuteNonQueryAsync(cancellationToken);
        return new InstanceRegistrationOutcome(true, true, instanceId, NarrativeContractReason.None, null);
    }

    /// <summary>旧作用键 → 稳定实例。同别名指向他实例、或实例已有别的别名 ⇒ 110，与内存登记处同形。</summary>
    public async Task<InstanceMigrationOutcome> BindLegacyScopeAsync(string legacyScopeKey, CharacterInstanceKey key,
        CancellationToken cancellationToken = default)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrWhiteSpace(legacyScopeKey);
        key.Validate();
        string instanceIdValue;
        await using (var lookup = Command(
            "SELECT instance_id FROM character_instance WHERE local_user_id=$u AND character_id=$c", null))
        {
            lookup.Parameters.AddWithValue("$u", key.LocalUserId);
            lookup.Parameters.AddWithValue("$c", key.CharacterId);
            await using var reader = await lookup.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return new InstanceMigrationOutcome(NarrativeContractReason.InstanceUnknown, key,
                    "别名必须先有实例登记才能绑定。");
            }

            instanceIdValue = reader.GetString(0);
        }

        await using (var bound = Command("SELECT instance_id FROM character_instance_alias WHERE alias=$a", null))
        {
            bound.Parameters.AddWithValue("$a", legacyScopeKey);
            await using var reader = await bound.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var owner = reader.GetString(0);
                return string.Equals(owner, instanceIdValue, StringComparison.Ordinal)
                    ? new InstanceMigrationOutcome(NarrativeContractReason.None, key, "别名已绑定，重复绑定视为幂等。")
                    : new InstanceMigrationOutcome(NarrativeContractReason.AliasConflict, key,
                        $"别名 {legacyScopeKey} 已经属于实例 {owner}，不能改指 {instanceIdValue}。");
            }
        }

        await using (var owned = Command(
            "SELECT alias FROM character_instance_alias WHERE instance_id=$i AND alias <> $a", null))
        {
            owned.Parameters.AddWithValue("$i", instanceIdValue);
            owned.Parameters.AddWithValue("$a", legacyScopeKey);
            await using var reader = await owned.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return new InstanceMigrationOutcome(NarrativeContractReason.AliasConflict, key,
                    $"实例 {instanceIdValue} 的别名是 {reader.GetString(0)}，一条实例只允许一个旧作用域入口。");
            }
        }

        await using var insert = Command("INSERT INTO character_instance_alias(alias, instance_id) VALUES($a, $i)", null);
        insert.Parameters.AddWithValue("$a", legacyScopeKey);
        insert.Parameters.AddWithValue("$i", instanceIdValue);
        await insert.ExecuteNonQueryAsync(cancellationToken);
        return new InstanceMigrationOutcome(NarrativeContractReason.None, key, null);
    }

    /// <summary>按旧作用键解析当前实例；未登记返回 null。</summary>
    public async Task<CharacterInstanceId?> ResolveAliasAsync(string legacyScopeKey,
        CancellationToken cancellationToken = default)
    {
        EnsureReady();
        await using var command = Command("SELECT instance_id FROM character_instance_alias WHERE alias=$a", null);
        command.Parameters.AddWithValue("$a", legacyScopeKey);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is string text ? CharacterInstanceId.Create(text) : null;
    }

    // —————————— 写入权（OutboxOwnershipRegistry 的持久化对偶，300/301/302/303/306 同码）——————————

    /// <summary>取得写入权。空位 ⇒ 代次 = 历史最高 + 1（释放不回退，fencing 成立的前提）；被他人持有 ⇒ 300。</summary>
    public async Task<OwnershipGrant> AcquireWriteOwnershipAsync(CharacterInstanceId instanceId, EngineMode mode,
        string writerId, DateTimeOffset acquiredAtUtc, CancellationToken cancellationToken = default)
    {
        EnsureReady();
        NarrativeGuard.RequireText(writerId, nameof(writerId), NarrativeContractReason.OwnershipNotHeld);
        await using var transaction = await BeginAsync(cancellationToken);
        var row = await ReadInstanceAsync(instanceId.Value, transaction, cancellationToken);
        if (row is null)
        {
            return OwnershipGrant.No(NarrativeContractReason.InstanceUnknown,
                $"实例 {instanceId.Value} 没有登记行，写入权无从授予。");
        }

        if (row.CurrentWriterId is not null)
        {
            if (string.Equals(row.CurrentWriterId, writerId, StringComparison.Ordinal))
            {
                return row.EngineMode == (int)mode
                    ? new OwnershipGrant(true, ToTicket(row, instanceId), NarrativeContractReason.None, null)
                    : OwnershipGrant.No(NarrativeContractReason.EngineModeMismatch,
                        $"同一写者 {writerId} 不能一边以 {row.EngineMode} 一边以 {mode} 持有同一实例；请先交接。");
            }

            return OwnershipGrant.No(NarrativeContractReason.OwnershipHeldElsewhere,
                $"实例 {instanceId.Value} 的写入权在 {row.CurrentWriterId}（代次 {row.OwnershipEpoch}）手上，"
                + "第二写者只能等待或走显式交接。");
        }

        var next = row.HighestOwnershipEpoch + 1;
        await using (var update = Command(
            """
            UPDATE character_instance SET current_writer_id=$w, engine_mode=$m,
              ownership_epoch=$e, highest_ownership_epoch=$e
            WHERE instance_id=$i AND current_writer_id IS NULL;
            """, transaction))
        {
            update.Parameters.AddWithValue("$w", writerId);
            update.Parameters.AddWithValue("$m", (int)mode);
            update.Parameters.AddWithValue("$e", next);
            update.Parameters.AddWithValue("$i", instanceId.Value);
            if (await update.ExecuteNonQueryAsync(cancellationToken) != 1)
            {
                return OwnershipGrant.No(NarrativeContractReason.OwnershipHeldElsewhere,
                    "授予期间写入权已被并发取得：本次不落，如实报告。");
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return new OwnershipGrant(true,
            new WriteOwnershipTicket(instanceId, mode, writerId, next, acquiredAtUtc), NarrativeContractReason.None, null);
    }

    /// <summary>显式交接：交出方凭据必须与账上一致（否则 306），新票代次 = 历史最高 + 1。不存在抢占。</summary>
    public async Task<OwnershipGrant> HandoverWriteOwnershipAsync(CharacterInstanceId instanceId, string fromWriterId,
        long fromEpoch, EngineMode toMode, string toWriterId, DateTimeOffset handedOverAtUtc,
        CancellationToken cancellationToken = default)
    {
        EnsureReady();
        NarrativeGuard.RequireText(toWriterId, nameof(toWriterId), NarrativeContractReason.OwnershipNotHeld);
        await using var transaction = await BeginAsync(cancellationToken);
        var row = await ReadInstanceAsync(instanceId.Value, transaction, cancellationToken);
        if (row is null)
        {
            return OwnershipGrant.No(NarrativeContractReason.InstanceUnknown,
                $"实例 {instanceId.Value} 没有登记行，交接无从发生。");
        }

        if (row.CurrentWriterId is null)
        {
            return OwnershipGrant.No(NarrativeContractReason.OwnershipNotHeld,
                $"实例 {instanceId.Value} 当前没有写者，交接无从发生。");
        }

        if (!string.Equals(row.CurrentWriterId, fromWriterId, StringComparison.Ordinal) || row.OwnershipEpoch != fromEpoch)
        {
            return OwnershipGrant.No(NarrativeContractReason.HandoverNotAuthorized,
                $"交出方凭据不匹配（登记的是 {row.CurrentWriterId}/代次 {row.OwnershipEpoch}）。");
        }

        if (string.Equals(fromWriterId, toWriterId, StringComparison.Ordinal))
        {
            return OwnershipGrant.No(NarrativeContractReason.HandoverNotAuthorized,
                "交接的两端是同一个写者：这只是换模式，走 Release + Acquire。");
        }

        var next = row.HighestOwnershipEpoch + 1;
        await using (var update = Command(
            """
            UPDATE character_instance SET current_writer_id=$w, engine_mode=$m,
              ownership_epoch=$e, highest_ownership_epoch=$e
            WHERE instance_id=$i AND current_writer_id=$from AND ownership_epoch=$fromEpoch;
            """, transaction))
        {
            update.Parameters.AddWithValue("$w", toWriterId);
            update.Parameters.AddWithValue("$m", (int)toMode);
            update.Parameters.AddWithValue("$e", next);
            update.Parameters.AddWithValue("$i", instanceId.Value);
            update.Parameters.AddWithValue("$from", fromWriterId);
            update.Parameters.AddWithValue("$fromEpoch", fromEpoch);
            if (await update.ExecuteNonQueryAsync(cancellationToken) != 1)
            {
                return OwnershipGrant.No(NarrativeContractReason.HandoverNotAuthorized,
                    "交接落库时代次已变：并发接管，本次不落。");
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return new OwnershipGrant(true,
            new WriteOwnershipTicket(instanceId, toMode, toWriterId, next, handedOverAtUtc),
            NarrativeContractReason.None, null);
    }

    /// <summary>释放写入权。票不匹配（300/302）释放不了别人的所有权；释放后历史最高代次不回退。</summary>
    public async Task<OwnershipGrant> ReleaseWriteOwnershipAsync(CharacterInstanceId instanceId, string writerId,
        long ownershipEpoch, CancellationToken cancellationToken = default)
    {
        EnsureReady();
        await using var transaction = await BeginAsync(cancellationToken);
        var row = await ReadInstanceAsync(instanceId.Value, transaction, cancellationToken);
        if (row?.CurrentWriterId is null)
        {
            return OwnershipGrant.No(NarrativeContractReason.OwnershipNotHeld,
                $"实例 {instanceId.Value} 没有登记中的写者。");
        }

        if (!string.Equals(row.CurrentWriterId, writerId, StringComparison.Ordinal))
        {
            return OwnershipGrant.No(NarrativeContractReason.OwnershipHeldElsewhere,
                $"持有者是 {row.CurrentWriterId}，{writerId} 无权释放它。");
        }

        if (row.OwnershipEpoch != ownershipEpoch)
        {
            return OwnershipGrant.No(NarrativeContractReason.OwnershipFenced,
                $"这张票的代次 {ownershipEpoch} 已落后于登记的 {row.OwnershipEpoch}。");
        }

        await using var update = Command(
            "UPDATE character_instance SET current_writer_id=NULL, engine_mode=NULL WHERE instance_id=$i", transaction);
        update.Parameters.AddWithValue("$i", instanceId.Value);
        await update.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new OwnershipGrant(true, null, NarrativeContractReason.None,
            "写入权已释放；历史最高代次保留，后续票只增不减。");
    }

    // —————————— 条件提交：真实表上的同事务原子写 ——————————

    /// <summary>
    /// 事务 A 的条件写的落库版。闸门次序逐条对齐 NarrativeCommitWriter.TryCommit
    /// （NarrativeCommitWriter.cs:85-183：身份 108 → 所有权 300/301/302 → 契约版本 212 → 内容散列 213 →
    /// 删除 epoch 504 → 重放 502/505 → 故事版本 501/500 → 水位 204 → 批次上界 203 → 消费凭据 215 →
    /// 绑定 + NarrativeCommitValidator 首拒），差别只在每一项都从库里的行读出判定。
    /// 通过后在同一条事务里写 narrative_entries、narrative_input_receipts、实例水位、
    /// narrative_intents 与可选的待发投递段；任何一步失败（含 SQLite 约束异常）整笔回滚、原样上抛。
    /// 拒绝路径不落任何字节。
    /// </summary>
    public async Task<ConditionalCommitOutcome> TryCommitAtomicAsync(NarrativeRequest request, NarrativeCommit commit,
        IReadOnlyList<DeliverySegment>? pendingSegments = null, CancellationToken cancellationToken = default)
    {
        EnsureReady();
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(commit);
        await using var transaction = await BeginAsync(cancellationToken);

        var row = await ReadInstanceAsync(commit.InstanceId.Value, transaction, cancellationToken);
        if (row is null)
        {
            return ConditionalCommitOutcome.No(NarrativeContractReason.InstanceUnknown,
                $"实例 {commit.InstanceId.Value} 没有在落库登记表里，提交无处归属。", null);
        }

        var (authorized, ownershipReason) = await AuthorizeWriterAsync(row, commit.CommittedByWriter,
            commit.OwnershipEpoch, cancellationToken);
        if (!authorized)
        {
            return ConditionalCommitOutcome.No(ownershipReason,
                ownershipReason == NarrativeContractReason.OwnershipFenced
                    ? $"写者 {commit.CommittedByWriter} 带来的代次 {commit.OwnershipEpoch} 落后于当前 "
                      + $"{row.HighestOwnershipEpoch}：所有权已交接，迟到提交只能被拒。"
                    : $"写者 {commit.CommittedByWriter} 不持有实例 {commit.InstanceId.Value} 的写入权。",
                ToState(row));
        }

        if (!NarrativeContractVersions.IsSupportedNarrativeVersion(commit.ContractVersion))
        {
            return ConditionalCommitOutcome.No(NarrativeContractReason.ContractVersionUnsupported,
                $"提交携带契约版本 {commit.ContractVersion}，本落库面只认 "
                + $"{NarrativeContractVersions.NarrativeDocument}。", ToState(row));
        }

        if (string.IsNullOrWhiteSpace(commit.ContentHash))
        {
            return ConditionalCommitOutcome.No(NarrativeContractReason.ContentHashMissing,
                "没有内容散列的提交在恢复时无法自证没被改写，一律拒绝。", ToState(row));
        }

        var storedDeletionEpoch = await ReadDeletionEpochAsync(commit.ConversationId, transaction, cancellationToken);
        if (storedDeletionEpoch > commit.DeletionEpoch)
        {
            return ConditionalCommitOutcome.No(NarrativeContractReason.DeletionEpochAdvanced,
                $"会话 {commit.ConversationId} 的删除 epoch 已提升到 {storedDeletionEpoch}，"
                + $"这条提交停留在 {commit.DeletionEpoch}：迟到结果不得复活已删除的作用域。", ToState(row));
        }

        var appliedHash = await ReadAppliedContentHashAsync(commit.CommitId, transaction, cancellationToken);
        if (appliedHash is not null)
        {
            return string.Equals(appliedHash, commit.ContentHash, StringComparison.Ordinal)
                ? ConditionalCommitOutcome.No(NarrativeContractReason.CommitAlreadyApplied,
                    "这条提交已经生效过，重放不再推进水位。", ToState(row))
                : ConditionalCommitOutcome.No(NarrativeContractReason.DuplicateCommitId,
                    $"提交 {commit.CommitId} 的 ID 被复用到不同内容上。", ToState(row));
        }

        var state = ToState(row) ?? new NarrativeInstanceState(commit.InstanceId, request.StoryVersion,
            request.ProcessedThroughAtIssue, commit.OwnershipEpoch, null, 0, request.IssuedAtUtc);

        if (commit.ExpectedStoryVersion != state.StoryVersion)
        {
            return ConditionalCommitOutcome.No(
                commit.ExpectedStoryVersion < state.StoryVersion
                    ? NarrativeContractReason.LateCommitRejected
                    : NarrativeContractReason.StoryVersionConflict,
                $"提交建立在故事版本 {commit.ExpectedStoryVersion} 上，而账上已经是 {state.StoryVersion}。"
                + (commit.ExpectedStoryVersion < state.StoryVersion ? "这是迟到提交。" : "这是版本超前，同样不落。"),
                state);
        }

        if (!commit.ProcessedThrough.AdvancesPast(state.ProcessedThrough))
        {
            return ConditionalCommitOutcome.No(NarrativeContractReason.WatermarkNotAdvancing,
                $"消费水位停留在 {state.ProcessedThrough.Seq}，提交声称 {commit.ProcessedThrough.Seq}："
                + "水位不前进的提交等于重复消费。", state);
        }

        if (!commit.ProcessedThrough.WithinBatch(request.BatchHighestSeq))
        {
            return ConditionalCommitOutcome.No(NarrativeContractReason.ProcessedBeyondBatch,
                $"提交把水位报到 {commit.ProcessedThrough.Seq}，而宿主签发的批次上界是 {request.BatchHighestSeq}。",
                state);
        }

        var receivedSeqs = request.InputSeqs;
        foreach (var seq in commit.ConsumedInputSeqs)
        {
            if (!receivedSeqs.Contains(seq))
            {
                return ConditionalCommitOutcome.No(NarrativeContractReason.ProcessedInputUnreceived,
                    $"提交声称消费了序号 {seq}，宿主在这一批次里没有收到过它。", state);
            }
        }

        var bound = commit with
        {
            Actions = [.. commit.Actions.Select(a =>
                a with { Binding = a.Binding ?? NarrativeProseBinder.Bind(commit.NarrativeProse, a.ProseQuote) })],
        };

        var rejections = NarrativeCommitValidator.Validate(bound, request);
        if (rejections.Count > 0)
        {
            return ConditionalCommitOutcome.No(rejections[0].Reason,
                string.Join("；", rejections.Select(r => r.Detail)), state);
        }

        var segmentFailures = ValidatePreregisteredSegments(bound, request.MaxOutboundSegments, pendingSegments);
        if (segmentFailures is not null)
        {
            return ConditionalCommitOutcome.No(segmentFailures.Reason, segmentFailures.Detail, state);
        }

        // —— 通过全部闸门：以下写入全部发生在同一条事务里 ——
        await using (var entry = Command(
            """
            INSERT INTO narrative_entries(commit_id, instance_id, conversation_id, batch_id, narrative_prose,
              actions_json, processed_through_seq, perceived_input_seqs, consumed_input_seqs,
              expected_story_version, deletion_epoch, ownership_epoch, contract_version, content_hash,
              committed_by_writer, committed_at_utc, proposal_id, max_outbound_segments)
            VALUES($commitId, $instanceId, $conversationId, $batchId, $prose, $actions, $wm, $perceived, $consumed,
              $expected, $deletionEpoch, $ownershipEpoch, $contractVersion, $contentHash, $writer, $at, $proposal, $max);
            """, transaction))
        {
            entry.Parameters.AddWithValue("$commitId", bound.CommitId);
            entry.Parameters.AddWithValue("$instanceId", bound.InstanceId.Value);
            entry.Parameters.AddWithValue("$conversationId", bound.ConversationId);
            entry.Parameters.AddWithValue("$batchId", bound.BatchId);
            entry.Parameters.AddWithValue("$prose", bound.NarrativeProse);
            entry.Parameters.AddWithValue("$actions", JsonSerializer.Serialize(bound.Actions));
            entry.Parameters.AddWithValue("$wm", bound.ProcessedThrough.Seq);
            entry.Parameters.AddWithValue("$perceived", JoinSeqs(bound.PerceivedInputRefs.Refs.Select(r => r.Seq)));
            entry.Parameters.AddWithValue("$consumed", JoinSeqs(bound.ConsumedInputSeqs));
            entry.Parameters.AddWithValue("$expected", bound.ExpectedStoryVersion);
            entry.Parameters.AddWithValue("$deletionEpoch", bound.DeletionEpoch);
            entry.Parameters.AddWithValue("$ownershipEpoch", bound.OwnershipEpoch);
            entry.Parameters.AddWithValue("$contractVersion", bound.ContractVersion);
            entry.Parameters.AddWithValue("$contentHash", bound.ContentHash);
            entry.Parameters.AddWithValue("$writer", bound.CommittedByWriter);
            entry.Parameters.AddWithValue("$at", Format(bound.CommittedAtUtc));
            entry.Parameters.AddWithValue("$proposal", bound.ProposalId);
            entry.Parameters.AddWithValue("$max", request.MaxOutboundSegments);
            await entry.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var receipt = Command(
            """
            INSERT INTO narrative_input_receipts(instance_id, commit_id, batch_id, processed_through_seq,
              consumed_input_seqs, received_at_utc)
            VALUES($instanceId, $commitId, $batchId, $wm, $consumed, $at);
            """, transaction))
        {
            receipt.Parameters.AddWithValue("$instanceId", bound.InstanceId.Value);
            receipt.Parameters.AddWithValue("$commitId", bound.CommitId);
            receipt.Parameters.AddWithValue("$batchId", bound.BatchId);
            receipt.Parameters.AddWithValue("$wm", bound.ProcessedThrough.Seq);
            receipt.Parameters.AddWithValue("$consumed", JoinSeqs(bound.ConsumedInputSeqs));
            receipt.Parameters.AddWithValue("$at", Format(request.IssuedAtUtc));
            await receipt.ExecuteNonQueryAsync(cancellationToken);
        }

        var applied = state with
        {
            StoryVersion = state.StoryVersion + 1,
            ProcessedThrough = commit.ProcessedThrough,
            OwnershipEpoch = commit.OwnershipEpoch,
            LastCommitId = commit.CommitId,
            AppliedCommitCount = state.AppliedCommitCount + 1,
            UpdatedAtUtc = commit.CommittedAtUtc,
        };
        await using (var advance = Command(
            """
            UPDATE character_instance SET story_version=$story, processed_through_seq=$wm,
              applied_commit_count=$count, last_commit_id=$last, updated_at_utc=$at
            WHERE instance_id=$i AND (story_version IS NULL OR story_version=$expected);
            """, transaction))
        {
            advance.Parameters.AddWithValue("$story", applied.StoryVersion);
            advance.Parameters.AddWithValue("$wm", applied.ProcessedThrough.Seq);
            advance.Parameters.AddWithValue("$count", applied.AppliedCommitCount);
            advance.Parameters.AddWithValue("$last", applied.LastCommitId);
            advance.Parameters.AddWithValue("$at", Format(applied.UpdatedAtUtc));
            advance.Parameters.AddWithValue("$i", applied.InstanceId.Value);
            advance.Parameters.AddWithValue("$expected", state.StoryVersion);
            if (await advance.ExecuteNonQueryAsync(cancellationToken) != 1)
            {
                throw new InvalidOperationException(
                    "条件提交落库时实例状态已变化：并发接管，本次提交整体回滚，绝不半写。");
            }
        }

        foreach (var intent in bound.Intents)
        {
            await using var command = Command(
                """
                INSERT INTO narrative_intents(intent_id, instance_id, topic, status, source_commit_id,
                  earliest_due_at_utc, latest_due_at_utc, proactive_send_permitted)
                VALUES($id, $instanceId, $topic, $status, $source, $earliest, $latest, $proactive);
                """, transaction);
            command.Parameters.AddWithValue("$id", intent.IntentId);
            command.Parameters.AddWithValue("$instanceId", intent.InstanceId.Value);
            command.Parameters.AddWithValue("$topic", intent.Topic);
            command.Parameters.AddWithValue("$status", (int)intent.Status);
            command.Parameters.AddWithValue("$source", intent.SourceCommitId);
            command.Parameters.AddWithValue("$earliest", (object?)intent.EarliestDueAtUtc?.ToString("O") ?? DBNull.Value);
            command.Parameters.AddWithValue("$latest", (object?)intent.LatestDueAtUtc?.ToString("O") ?? DBNull.Value);
            command.Parameters.AddWithValue("$proactive", intent.ProactiveSendPermitted ? 1 : 0);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var segment in pendingSegments ?? [])
        {
            await using var command = Command(
                """
                INSERT INTO narrative_outbox(commit_id, action_id, segment_index, instance_id, conversation_id,
                  state, text, sticker_id, ownership_epoch, cancel_reason, delivered_message_id)
                VALUES($commitId, $actionId, $index, $instanceId, $conversationId, 0, $text, $sticker, $epoch, NULL, NULL);
                """, transaction);
            command.Parameters.AddWithValue("$commitId", segment.CommitId);
            command.Parameters.AddWithValue("$actionId", segment.ActionId);
            command.Parameters.AddWithValue("$index", segment.SegmentIndex);
            command.Parameters.AddWithValue("$instanceId", segment.InstanceId.Value);
            command.Parameters.AddWithValue("$conversationId", segment.ConversationId);
            command.Parameters.AddWithValue("$text", segment.Text);
            command.Parameters.AddWithValue("$sticker", (object?)segment.StickerId ?? DBNull.Value);
            command.Parameters.AddWithValue("$epoch", segment.OwnershipEpoch);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return ConditionalCommitOutcome.Yes(bound, applied);
    }

    /// <summary>会话删除/清空：删除 epoch 只升不降（v4:277），之后较低 epoch 的提交在 504 永久无资格。</summary>
    public async Task InvalidateConversationAsync(string conversationId, long deletionEpoch, DateTimeOffset atUtc,
        CancellationToken cancellationToken = default)
    {
        EnsureReady();
        NarrativeGuard.RequireText(conversationId, nameof(conversationId), NarrativeContractReason.ConversationInvalidated);
        ArgumentOutOfRangeException.ThrowIfNegative(deletionEpoch);
        await using var command = Command(
            """
            INSERT INTO narrative_conversation_deletion_epoch(conversation_id, deletion_epoch, invalidated_at_utc)
            VALUES($c, $e, $at)
            ON CONFLICT(conversation_id) DO UPDATE SET
              deletion_epoch = MAX(narrative_conversation_deletion_epoch.deletion_epoch, $e),
              invalidated_at_utc = excluded.invalidated_at_utc;
            """, null);
        command.Parameters.AddWithValue("$c", conversationId);
        command.Parameters.AddWithValue("$e", deletionEpoch);
        command.Parameters.AddWithValue("$at", Format(atUtc));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    // —————————— 投递段：真实账本上的送达证据（DeliveryLedger 的落库对偶）——————————

    /// <summary>
    /// 追加待发段。检查次序对齐 DeliveryLedger.TryAppend（DeliveryLedger.cs:66-166）：
    /// 所有权 → 提交已生效（401）→ 会话作用域（216）→ 动作存在（402）→ 非即时（404）→ 唯一键（400）→
    /// 负序号（403）→ 计划/宿主双上限（404）→ 连号（403）→ 载荷（408）→ 绑定（205/206）→
    /// 逐字拼回（407，末段封口必须整段还原）。通过后强制 Pending、送达证据为空。
    /// </summary>
    public async Task<SegmentOutcome> AppendDeliverySegmentAsync(DeliverySegment segment, string writerId,
        CancellationToken cancellationToken = default)
    {
        EnsureReady();
        ArgumentNullException.ThrowIfNull(segment);
        var key = segment.Key;

        var row = await ReadInstanceAsync(segment.InstanceId.Value, null, cancellationToken);
        var (authorized, ownershipReason) = await AuthorizeWriterAsync(row, writerId, segment.OwnershipEpoch,
            cancellationToken);
        if (!authorized)
        {
            return SegmentOutcome.No(ownershipReason,
                $"写者 {writerId} 在实例 {segment.InstanceId.Value} 上没有当前代次的票"
                + $"（带来的是 {segment.OwnershipEpoch}），无权追加投递段。");
        }

        var commit = await ReadCommitRowAsync(key.CommitId, null, cancellationToken);
        if (commit is null)
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentCommitUnknown,
                $"提交 {key.CommitId} 没有生效过，不能产生投递段。");
        }

        if (!string.Equals(commit.ConversationId, segment.ConversationId, StringComparison.Ordinal))
        {
            return SegmentOutcome.No(NarrativeContractReason.ScopeEscalation, "投递段落在提交作用域之外的会话上。");
        }

        var actions = DeserializeActions(commit.ActionsJson);
        var action = actions.FirstOrDefault(a => string.Equals(a.ActionId, key.ActionId, StringComparison.Ordinal));
        if (action is null)
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentActionUnknown,
                $"提交 {key.CommitId} 里没有动作 {key.ActionId}。");
        }

        if (action.Mode != CommunicationMode.Immediate)
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentCountExceeded,
                $"动作 {key.ActionId} 是 {action.Mode}，不产生投递段。");
        }

        var existing = await ReadActionSequenceAsync(key, cancellationToken);
        var duplicate = existing.FirstOrDefault(s => s.SegmentIndex == key.SegmentIndex);
        if (duplicate is not null)
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentKeyDuplicate,
                $"({key.CommitId}, {key.ActionId}, {key.SegmentIndex}) 已经在账本里，状态是 {duplicate.State}。"
                + "同键二次写入如果不撞墙，就会静默覆盖已发生的投递证据。", duplicate);
        }

        var failure = ValidateSegmentCore(commit.NarrativeProse, action, commit.MaxOutboundSegments, existing, segment);
        if (failure is not null)
        {
            return SegmentOutcome.No(failure.Reason, failure.Detail);
        }

        var pending = segment with { State = DeliverySegmentState.Pending, DeliveredMessageId = null };
        await using (var insert = Command(
            """
            INSERT INTO narrative_outbox(commit_id, action_id, segment_index, instance_id, conversation_id,
              state, text, sticker_id, ownership_epoch, cancel_reason, delivered_message_id)
            VALUES($commitId, $actionId, $index, $instanceId, $conversationId, 0, $text, $sticker, $epoch, NULL, NULL);
            """, null))
        {
            insert.Parameters.AddWithValue("$commitId", key.CommitId);
            insert.Parameters.AddWithValue("$actionId", key.ActionId);
            insert.Parameters.AddWithValue("$index", key.SegmentIndex);
            insert.Parameters.AddWithValue("$instanceId", segment.InstanceId.Value);
            insert.Parameters.AddWithValue("$conversationId", segment.ConversationId);
            insert.Parameters.AddWithValue("$text", pending.Text);
            insert.Parameters.AddWithValue("$sticker", (object?)pending.StickerId ?? DBNull.Value);
            insert.Parameters.AddWithValue("$epoch", pending.OwnershipEpoch);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        return SegmentOutcome.Ok(pending);
    }

    /// <summary>Pending → Delivered：送达必须带聊天历史证据（410），前一段没落地不许跳（405），终态不可翻（406）。</summary>
    public async Task<SegmentOutcome> MarkSegmentDeliveredAsync(DeliverySegmentKey key, string? deliveredMessageId,
        long ownershipEpoch, string writerId, CancellationToken cancellationToken = default)
    {
        EnsureReady();
        var (gate, current) = await PreTransitionAsync(key, ownershipEpoch, writerId, cancellationToken);
        if (gate is not null)
        {
            return gate;
        }

        if (string.IsNullOrWhiteSpace(deliveredMessageId))
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentDeliveryEvidenceMissing,
                "没有聊天历史标识就不能把一段标成已送达：投递证据必须由真实落库支撑。", current);
        }

        var sequence = await ReadActionSequenceAsync(key, cancellationToken);
        var predecessor = sequence.FirstOrDefault(s => s.SegmentIndex == key.SegmentIndex - 1);
        if (predecessor is not null && predecessor.State != DeliverySegmentState.Delivered)
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentPredecessorPending,
                $"第 {key.SegmentIndex} 段之前还有第 {predecessor.SegmentIndex} 段是 {predecessor.State}："
                + "投递顺序不能被打乱。", current);
        }

        return await TransiteAsync(key, DeliverySegmentState.Delivered, deliveredMessageId, null,
            cancellationToken);
    }

    /// <summary>Pending → Cancelled：取消必须写清原因，已送达的段落不可撤。</summary>
    public async Task<SegmentOutcome> MarkSegmentCancelledAsync(DeliverySegmentKey key, string cancelReason,
        long ownershipEpoch, string writerId, CancellationToken cancellationToken = default)
    {
        EnsureReady();
        var (gate, current) = await PreTransitionAsync(key, ownershipEpoch, writerId, cancellationToken);
        if (gate is not null)
        {
            return gate;
        }

        if (string.IsNullOrWhiteSpace(cancelReason))
        {
            return SegmentOutcome.No(NarrativeContractReason.SegmentTerminalImmutable,
                "取消必须写清原因，否则恢复时无从判断这一段为什么没发。", current);
        }

        return await TransiteAsync(key, DeliverySegmentState.Cancelled, null, cancelReason, cancellationToken);
    }

    // —————————— 只读视图（判定重启后依旧成立靠的就是这几条从库里读的路径）——————————

    /// <summary>实例叙事状态；从未有过生效提交时与 NarrativeCommitWriter.StateOf 同为 null。</summary>
    public async Task<NarrativeInstanceState?> StateOfAsync(CharacterInstanceId instanceId,
        CancellationToken cancellationToken = default)
    {
        EnsureReady();
        var row = await ReadInstanceAsync(instanceId.Value, null, cancellationToken);
        return ToState(row);
    }

    /// <summary>按唯一键取投递段当前真相。</summary>
    public async Task<DeliverySegment?> FindSegmentAsync(DeliverySegmentKey key,
        CancellationToken cancellationToken = default)
    {
        EnsureReady();
        await using var command = Command(SegmentSelect + " WHERE commit_id=$c AND action_id=$a AND segment_index=$i", null);
        command.Parameters.AddWithValue("$c", key.CommitId);
        command.Parameters.AddWithValue("$a", key.ActionId);
        command.Parameters.AddWithValue("$i", key.SegmentIndex);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadSegment(reader) : null;
    }

    /// <summary>某提交尚未送达的段（崩溃恢复后"只补未发的、绝不重发已发的"的清单来源）。</summary>
    public async Task<IReadOnlyList<DeliverySegment>> ListPendingSegmentsAsync(string commitId,
        CancellationToken cancellationToken = default)
    {
        EnsureReady();
        await using var command = Command(SegmentSelect + " WHERE commit_id=$c AND state=0 ORDER BY action_id, segment_index", null);
        command.Parameters.AddWithValue("$c", commitId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var segments = new List<DeliverySegment>();
        while (await reader.ReadAsync(cancellationToken))
        {
            segments.Add(ReadSegment(reader));
        }

        return segments;
    }

    private const string SegmentSelect =
        "SELECT instance_id, conversation_id, commit_id, action_id, segment_index, state, text, sticker_id,"
        + " ownership_epoch, cancel_reason, delivered_message_id FROM narrative_outbox";

    private async Task<SegmentOutcome> TransiteAsync(DeliverySegmentKey key, DeliverySegmentState target,
        string? deliveredMessageId, string? cancelReason, CancellationToken cancellationToken)
    {
        await using var update = Command(
            """
            UPDATE narrative_outbox SET state=$target,
              delivered_message_id=$delivered, cancel_reason=$cancel
            WHERE commit_id=$c AND action_id=$a AND segment_index=$i AND state=0;
            """, null);
        update.Parameters.AddWithValue("$target", (int)target);
        update.Parameters.AddWithValue("$delivered", (object?)deliveredMessageId ?? DBNull.Value);
        update.Parameters.AddWithValue("$cancel", (object?)cancelReason ?? DBNull.Value);
        update.Parameters.AddWithValue("$c", key.CommitId);
        update.Parameters.AddWithValue("$a", key.ActionId);
        update.Parameters.AddWithValue("$i", key.SegmentIndex);
        if (await update.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            var raced = await FindSegmentAsync(key, cancellationToken);
            return SegmentOutcome.No(NarrativeContractReason.SegmentTerminalImmutable,
                $"这一段在判定与落库之间已变为 {raced?.State.ToString() ?? "消失"} 终态：迟到结果不得翻转它。", raced);
        }

        var updated = await FindSegmentAsync(key, cancellationToken);
        return updated is null
            ? SegmentOutcome.No(NarrativeContractReason.SegmentNotFound, "段在状态转换后从账上消失。")
            : SegmentOutcome.Ok(updated);
    }

    private async Task<(SegmentOutcome? Gate, DeliverySegment? Current)> PreTransitionAsync(DeliverySegmentKey key,
        long ownershipEpoch, string writerId, CancellationToken cancellationToken)
    {
        var commit = await ReadCommitRowAsync(key.CommitId, null, cancellationToken);
        if (commit is null)
        {
            return (SegmentOutcome.No(NarrativeContractReason.SegmentCommitUnknown,
                $"提交 {key.CommitId} 不在账上，状态转换无所附属。"), null);
        }

        var row = await ReadInstanceAsync(commit.InstanceId, null, cancellationToken);
        var (authorized, ownershipReason) = await AuthorizeWriterAsync(row, writerId, ownershipEpoch, cancellationToken);
        if (!authorized)
        {
            return (SegmentOutcome.No(ownershipReason,
                $"写者 {writerId} 的代次 {ownershipEpoch} 已不具备写终态的资格。"), null);
        }

        var current = await FindSegmentAsync(key, cancellationToken);
        if (current is null)
        {
            return (SegmentOutcome.No(NarrativeContractReason.SegmentNotFound,
                $"账上没有 ({key.CommitId}, {key.ActionId}, {key.SegmentIndex}) 这一段。"), null);
        }

        if (current.IsTerminal)
        {
            return (SegmentOutcome.No(NarrativeContractReason.SegmentTerminalImmutable,
                $"这一段已经是 {current.State} 终态，迟到结果不得翻转它。", current), current);
        }

        return (null, current);
    }

    private async Task<(bool Authorized, NarrativeContractReason Reason)> AuthorizeWriterAsync(InstanceRow? row,
        string writerId, long ownershipEpoch, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (row?.CurrentWriterId is null)
        {
            return (false, NarrativeContractReason.OwnershipNotHeld);
        }

        if (!string.Equals(row.CurrentWriterId, writerId, StringComparison.Ordinal))
        {
            return (false, NarrativeContractReason.OwnershipHeldElsewhere);
        }

        if (row.OwnershipEpoch != ownershipEpoch)
        {
            return (false, ownershipEpoch < row.OwnershipEpoch
                ? NarrativeContractReason.OwnershipFenced
                : NarrativeContractReason.OwnershipNotHeld);
        }

        return (true, NarrativeContractReason.None);
    }

    private static SegmentRejection? ValidatePreregisteredSegments(NarrativeCommit bound, int maxOutboundSegments,
        IReadOnlyList<DeliverySegment>? segments)
    {
        if (segments is null || segments.Count == 0)
        {
            return null;
        }

        var actions = bound.Actions.ToDictionary(a => a.ActionId, StringComparer.Ordinal);
        var perAction = new Dictionary<string, List<DeliverySegment>>(StringComparer.Ordinal);
        foreach (var segment in segments)
        {
            if (!segment.InstanceId.Equals(bound.InstanceId))
            {
                return new SegmentRejection(NarrativeContractReason.ScopeEscalation,
                    $"预登记段 {segment.ActionId}#{segment.SegmentIndex} 落在提交作用域之外的实例。");
            }

            if (!string.Equals(segment.ConversationId, bound.ConversationId, StringComparison.Ordinal))
            {
                return new SegmentRejection(NarrativeContractReason.ScopeEscalation,
                    $"预登记段 {segment.ActionId}#{segment.SegmentIndex} 落在提交作用域之外的会话。");
            }

            if (segment.OwnershipEpoch != bound.OwnershipEpoch)
            {
                return new SegmentRejection(NarrativeContractReason.OwnershipFenced,
                    $"预登记段 {segment.ActionId}#{segment.SegmentIndex} 的代次 {segment.OwnershipEpoch} "
                    + $"与提交票 {bound.OwnershipEpoch} 不一致。");
            }

            if (!actions.TryGetValue(segment.ActionId, out var action))
            {
                return new SegmentRejection(NarrativeContractReason.SegmentActionUnknown,
                    $"预登记段引用了提交里没有的动作 {segment.ActionId}。");
            }

            if (action.Mode != CommunicationMode.Immediate)
            {
                return new SegmentRejection(NarrativeContractReason.SegmentCountExceeded,
                    $"动作 {segment.ActionId} 是 {action.Mode}，不产生投递段。");
            }

            if (!perAction.TryGetValue(segment.ActionId, out var list))
            {
                list = [];
                perAction[segment.ActionId] = list;
            }

            if (list.Any(s => s.SegmentIndex == segment.SegmentIndex))
            {
                return new SegmentRejection(NarrativeContractReason.SegmentKeyDuplicate,
                    $"动作 {segment.ActionId} 在第 {segment.SegmentIndex} 段预登记了重复键：同键二次写入即覆盖投递证据。");
            }

            var failure = ValidateSegmentCore(bound.NarrativeProse, action, maxOutboundSegments, list, segment);
            if (failure is not null)
            {
                return failure;
            }

            list.Add(segment);
        }

        return null;
    }

    private static SegmentRejection? ValidateSegmentCore(string narrativeProse, CommunicationAction action,
        int maxSegmentsPerAction, IReadOnlyList<DeliverySegment> priorSegments, DeliverySegment candidate)
    {
        if (candidate.SegmentIndex < 0)
        {
            return new SegmentRejection(NarrativeContractReason.SegmentIndexNotContiguous, "段序号不能为负。");
        }

        var ceiling = Math.Min(action.PlannedSegmentCount, maxSegmentsPerAction);
        if (candidate.SegmentIndex >= ceiling)
        {
            return new SegmentRejection(NarrativeContractReason.SegmentCountExceeded,
                $"动作 {action.ActionId} 计划 {action.PlannedSegmentCount} 段、宿主上限 {maxSegmentsPerAction}，"
                + $"第 {candidate.SegmentIndex} 段不在范围内。");
        }

        if (candidate.SegmentIndex != priorSegments.Count)
        {
            return new SegmentRejection(NarrativeContractReason.SegmentIndexNotContiguous,
                $"动作 {action.ActionId} 已有 {priorSegments.Count} 段，第 {candidate.SegmentIndex} 段跳号了。");
        }

        if (string.IsNullOrEmpty(candidate.Text) && string.IsNullOrEmpty(candidate.StickerId))
        {
            return new SegmentRejection(NarrativeContractReason.SegmentStickerPayloadInvalid,
                "一段既没有正文也没有贴纸标识，无法作为送达证据。");
        }

        var binding = action.Binding;
        if (binding is null || !binding.Bound)
        {
            return new SegmentRejection(binding?.Failure ?? NarrativeContractReason.ActionBindingMissing,
                $"动作 {action.ActionId} 没有可用绑定，投递段无从取得正文。");
        }

        var boundSlice = narrativeProse.Substring(binding.ResolvedOffset!.Value, binding.QuotedText.Length);
        var accumulated = string.Concat(priorSegments.Select(s => s.Text)) + candidate.Text;
        if (!boundSlice.StartsWith(accumulated, StringComparison.Ordinal))
        {
            return new SegmentRejection(NarrativeContractReason.SegmentTextNotFromNarrative,
                $"第 {candidate.SegmentIndex} 段的正文拼回绑定原文时对不上：投递文本必须逐字来自叙事切片。");
        }

        if (priorSegments.Count + 1 == action.PlannedSegmentCount && accumulated.Length != boundSlice.Length)
        {
            return new SegmentRejection(NarrativeContractReason.SegmentTextNotFromNarrative,
                "最后一段封口后必须逐字还原绑定原文，缺字即视为第二次改写。");
        }

        return null;
    }

    private sealed record SegmentRejection(NarrativeContractReason Reason, string Detail);

    private async Task<IReadOnlyList<DeliverySegment>> ReadActionSequenceAsync(DeliverySegmentKey key,
        CancellationToken cancellationToken)
    {
        await using var command = Command(
            SegmentSelect + " WHERE commit_id=$c AND action_id=$a ORDER BY segment_index", null);
        command.Parameters.AddWithValue("$c", key.CommitId);
        command.Parameters.AddWithValue("$a", key.ActionId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var segments = new List<DeliverySegment>();
        while (await reader.ReadAsync(cancellationToken))
        {
            segments.Add(ReadSegment(reader));
        }

        return segments;
    }

    private static DeliverySegment ReadSegment(SqliteDataReader reader) => new()
    {
        InstanceId = CharacterInstanceId.Create(reader.GetString(0)),
        ConversationId = reader.GetString(1),
        CommitId = reader.GetString(2),
        ActionId = reader.GetString(3),
        SegmentIndex = reader.GetInt32(4),
        State = (DeliverySegmentState)reader.GetInt32(5),
        Text = reader.GetString(6),
        StickerId = reader.IsDBNull(7) ? null : reader.GetString(7),
        OwnershipEpoch = reader.GetInt64(8),
        CancelReason = reader.IsDBNull(9) ? null : reader.GetString(9),
        DeliveredMessageId = reader.IsDBNull(10) ? null : reader.GetString(10),
    };

    private sealed record CommitRow(string InstanceId, string CommitId, string ConversationId, string NarrativeProse,
        string ActionsJson, int MaxOutboundSegments);

    private async Task<CommitRow?> ReadCommitRowAsync(string commitId, SqliteTransaction? transaction,
        CancellationToken cancellationToken)
    {
        await using var command = Command(
            "SELECT instance_id, commit_id, conversation_id, narrative_prose, actions_json, max_outbound_segments"
            + " FROM narrative_entries WHERE commit_id=$c", transaction);
        command.Parameters.AddWithValue("$c", commitId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new CommitRow(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.GetString(4), reader.GetInt32(5))
            : null;
    }

    private static List<CommunicationAction> DeserializeActions(string actionsJson) =>
        JsonSerializer.Deserialize<List<CommunicationAction>>(actionsJson) ?? [];

    private sealed record InstanceRow(string InstanceId, string? CurrentWriterId, int? EngineMode, long OwnershipEpoch,
        long HighestOwnershipEpoch, long? StoryVersion, long ProcessedThroughSeq, int AppliedCommitCount,
        string? LastCommitId, string? UpdatedAtUtc);

    private async Task<InstanceRow?> ReadInstanceAsync(string instanceIdValue, SqliteTransaction? transaction,
        CancellationToken cancellationToken)
    {
        await using var command = Command(
            """
            SELECT instance_id, current_writer_id, engine_mode, ownership_epoch, highest_ownership_epoch,
              story_version, processed_through_seq, applied_commit_count, last_commit_id, updated_at_utc
            FROM character_instance WHERE instance_id=$i;
            """, transaction);
        command.Parameters.AddWithValue("$i", instanceIdValue);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new InstanceRow(
            reader.GetString(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetInt32(2),
            reader.GetInt64(3),
            reader.GetInt64(4),
            reader.IsDBNull(5) ? null : reader.GetInt64(5),
            reader.GetInt64(6),
            reader.GetInt32(7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            reader.IsDBNull(9) ? null : reader.GetString(9));
    }

    private static NarrativeInstanceState? ToState(InstanceRow? row) => row?.StoryVersion is null
        ? null
        : new NarrativeInstanceState(CharacterInstanceId.Create(row.InstanceId), row.StoryVersion.Value,
            new ProcessedThroughSeq(row.ProcessedThroughSeq), row.OwnershipEpoch, row.LastCommitId,
            row.AppliedCommitCount,
            Parse(row.UpdatedAtUtc ?? DateTimeOffset.MinValue.ToString("O")));

    private static WriteOwnershipTicket ToTicket(InstanceRow row, CharacterInstanceId instanceId) =>
        new(instanceId, (EngineMode)(row.EngineMode ?? (int)EngineMode.Legacy), row.CurrentWriterId!,
            row.OwnershipEpoch, Parse(row.UpdatedAtUtc ?? DateTimeOffset.UtcNow.ToString("O")));

    private async Task<long> ReadDeletionEpochAsync(string conversationId, SqliteTransaction? transaction,
        CancellationToken cancellationToken)
    {
        await using var command = Command(
            "SELECT deletion_epoch FROM narrative_conversation_deletion_epoch WHERE conversation_id=$c", transaction);
        command.Parameters.AddWithValue("$c", conversationId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is long or int ? Convert.ToInt64(value) : 0L;
    }

    private async Task<string?> ReadAppliedContentHashAsync(string commitId, SqliteTransaction? transaction,
        CancellationToken cancellationToken)
    {
        await using var command = Command("SELECT content_hash FROM narrative_entries WHERE commit_id=$c", transaction);
        command.Parameters.AddWithValue("$c", commitId);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value as string;
    }

    private async Task<SqliteTransaction> BeginAsync(CancellationToken cancellationToken) =>
        (SqliteTransaction)await _connection!.BeginTransactionAsync(cancellationToken);

    private SqliteCommand Command(string sql, SqliteTransaction? transaction)
    {
        var command = _connection!.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        return command;
    }

    private static async Task<int> ReadSchemaVersionAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(version), 1) FROM schema_info";
        try
        {
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        }
        catch (SqliteException)
        {
            return 0;
        }
    }

    private void EnsureReady()
    {
        if (_connection is null)
        {
            throw new InvalidOperationException("NarrativeSqliteStore 尚未初始化：先调用 InitializeAsync。");
        }
    }

    private static string JoinSeqs(IEnumerable<long> seqs) => string.Join(',', seqs);

    private static string Format(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset Parse(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}

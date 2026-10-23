using Microsoft.Data.Sqlite;
using Pry.Core.Memory;
using Pry.Core.Narrative;
using Pry.Core.Narrative.Storage;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// HDS-005 落库面（工单 §二）：真实 SQLite 表上的建表与迁移、同事务原子提交、崩溃矩阵、
/// 部分送达矩阵、失权世代与删除后不复活。每一条判据都在**重启连接之后**从库里的行判定，
/// 不在内存里假装——初始化、条件写、投递账本的闸门次序与理由码逐条对齐
/// NarrativeCommitWriter / DeliveryLedger / OutboxOwnershipRegistry 的既有形状（行号见报告 §一）。
/// 全部库文件都是本类自建的临时目录，绝不碰 %LOCALAPPDATA%\PryCompanion。
/// </summary>
public sealed class NarrativeSqliteCommitDeliveryTests : IAsyncLifetime
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"pry-narrative-sqlite-{Guid.NewGuid():N}");
    private string _databasePath = null!;

    private const string Prose = "她把笔放回盒里，看见这条消息，决定拒绝：画具还没收完呢。照片？不给。";
    private const string BoundQuote = "画具还没收完呢。照片？不给。";
    private const string SegmentOneText = "画具还没收完呢。";
    private const string SegmentTwoText = "照片？不给。";
    private const string WriterA = "host-a";
    private const string Conversation = "room-1";

    private static readonly CharacterInstanceId Alpha = CharacterInstanceId.Create("ci-instance-alpha");
    private static readonly CharacterInstanceKey AlphaKey = new() { LocalUserId = "local-user-1", CharacterId = "character-pry" };
    private static readonly DateTimeOffset BaseTime = new(2026, 10, 23, 0, 0, 0, TimeSpan.Zero);

    public ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        _databasePath = Path.Combine(_root, "narrative.db");
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        SqliteTestPools.Clear(_databasePath);
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }

        return ValueTask.CompletedTask;
    }

    // —————————— A：表存在性 / 幂等 / 半路失败 / 未知高版本 ——————————

    [Fact]
    public async Task A1_Initialize_creates_all_narrative_tables_on_a_real_database()
    {
        await using var store = await OpenStoreAsync();
        foreach (var table in new[]
        {
            "character_instance", "character_instance_alias", "narrative_entries", "narrative_input_receipts",
            "narrative_intents", "narrative_outbox", "narrative_conversation_deletion_epoch",
        })
        {
            Assert.Equal(1, await ScalarAsync(
                $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{table}'"));
        }

        Assert.Equal(MemoryDatabase.SchemaVersion, await store.SchemaVersionAsync(Token));
    }

    [Fact]
    public async Task A2_repeated_initialization_is_idempotent_on_the_same_file()
    {
        var store = await OpenStoreAsync();
        var fingerprint = await store.GetSchemaFingerprintAsync(Token);
        store.Dispose();

        await using var reopened = await OpenStoreAsync();
        Assert.Equal(fingerprint, await reopened.GetSchemaFingerprintAsync(Token));
        Assert.Equal(MemoryDatabase.SchemaVersion, await reopened.SchemaVersionAsync(Token));
        Assert.Equal(1, await ScalarAsync("SELECT COUNT(*) FROM schema_info"));
    }

    [Fact]
    public async Task A3_failed_migration_step_leaves_no_partial_structure_and_rerun_completes()
    {
        await using (var connection = new SqliteConnection($"Data Source={_databasePath};Pooling=False"))
        {
            await connection.OpenAsync(Token);
            var steps = new List<SchemaMigrationStep>
            {
                new(1, "probe.baseline", "叙事落库面基线", SchemaMigrationMode.SelfHealingBaseline,
                    static async (context, token) =>
                    {
                        await using var command = context.Connection.CreateCommand();
                        command.Transaction = context.Transaction;
                        command.CommandText = NarrativeStorageSchema.BaselineDdl;
                        await command.ExecuteNonQueryAsync(token);
                    }),
                new(MemoryDatabase.SchemaVersion, "probe.failing", "半途必炸的探测步骤",
                    SchemaMigrationMode.VersionGatedUpgrade, static async (context, token) =>
                    {
                        await using var created = context.Connection.CreateCommand();
                        created.Transaction = context.Transaction;
                        created.CommandText = "CREATE TABLE narrative_probe_marker (marker TEXT)";
                        await created.ExecuteNonQueryAsync(token);
                        await using var boom = context.Connection.CreateCommand();
                        boom.Transaction = context.Transaction;
                        boom.CommandText = "INSERT INTO narrative_probe_no_such_table VALUES (1)";
                        await boom.ExecuteNonQueryAsync(token);
                    }),
                new(MemoryDatabase.SchemaVersion, "probe.align", "版本对齐收尾",
                    SchemaMigrationMode.VersionAlign, static async (context, token) =>
                    {
                        await using var command = context.Connection.CreateCommand();
                        command.Transaction = context.Transaction;
                        command.CommandText = "UPDATE schema_info SET version=$version WHERE version<$version";
                        command.Parameters.AddWithValue("$version", MemoryDatabase.SchemaVersion);
                        await command.ExecuteNonQueryAsync(token);
                    }),
            };

            var error = await Assert.ThrowsAsync<MemoryMigrationException>(async () =>
                await MemorySchemaMigrator.RunAsync(connection, 0, steps, Token));
            Assert.Equal("probe.failing", error.StepId);
        }

        // 失败步骤整步消失：探测表不在，登记版本也没有推进；已提交的基线不受牵连。
        Assert.Equal(0, await ScalarAsync(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='narrative_probe_marker'"));
        Assert.Equal(1, await ScalarAsync("SELECT COALESCE(MAX(version), 0) FROM schema_info"));
        Assert.Equal(1, await ScalarAsync(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='narrative_entries'"));

        // 重跑初始化就是恢复路径。
        await using var store = await OpenStoreAsync();
        Assert.Equal(MemoryDatabase.SchemaVersion, await store.SchemaVersionAsync(Token));
    }

    [Fact]
    public async Task A4_database_registered_above_the_binary_is_refused_before_any_write()
    {
        await using (var seeding = new SqliteConnection($"Data Source={_databasePath};Pooling=False"))
        {
            await seeding.OpenAsync(Token);
            await using var command = seeding.CreateCommand();
            command.CommandText = """
                CREATE TABLE schema_info(version INTEGER NOT NULL);
                INSERT INTO schema_info(version) VALUES($v);
                """;
            command.Parameters.AddWithValue("$v", MemoryDatabase.SchemaVersion + 2);
            await command.ExecuteNonQueryAsync(Token);
        }

        var before = DatabaseSha256(_databasePath);
        var store = new NarrativeSqliteStore(_databasePath);
        await Assert.ThrowsAsync<SchemaVersionTooNewException>(async () => await store.InitializeAsync(Token));
        store.Dispose();

        Assert.Equal(before, DatabaseSha256(_databasePath));
        Assert.False(File.Exists(_databasePath + "-wal"));
        Assert.Equal(MemoryDatabase.SchemaVersion + 2, await ScalarAsync("SELECT MAX(version) FROM schema_info"));
        Assert.Equal(0, await ScalarAsync(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='narrative_entries'"));
    }

    // —————————— B：同事务原子提交 ——————————

    [Fact]
    public async Task B1_commit_lands_entry_receipt_watermark_intents_and_segments_atomically()
    {
        var store = await OpenStoreAsync();
        var grant = await SetupInstanceAndWriterAsync(store);
        var outcome = await store.TryCommitAtomicAsync(BuildRequest(), BuildCommit("commit-1", "sha256:commit-one",
            102, [101, 102], 0, grant.Ticket!.OwnershipEpoch, intents: true),
            [PendingSegment("commit-1", 0, SegmentOneText, grant.Ticket!.OwnershipEpoch)], Token);
        Assert.True(outcome.Accepted);
        store.Dispose();

        // 重启连接后逐项核对：叙事行、回执行、实例水位、意图行、预登记待发段都在库里。
        await using var reopened = await OpenStoreAsync();
        Assert.Equal(1, await ScalarAsync("SELECT COUNT(*) FROM narrative_entries WHERE commit_id='commit-1'"));
        Assert.Equal(1, await ScalarAsync("SELECT COUNT(*) FROM narrative_input_receipts WHERE commit_id='commit-1'"));
        Assert.Equal(1, await ScalarAsync("SELECT COUNT(*) FROM narrative_intents WHERE intent_id='intent-later-1'"));
        Assert.Equal(1, await ScalarAsync(
            "SELECT COUNT(*) FROM narrative_outbox WHERE commit_id='commit-1' AND state=0"));

        var state = await reopened.StateOfAsync(Alpha, Token);
        Assert.NotNull(state);
        Assert.Equal(1, state.StoryVersion);
        Assert.Equal(102, state!.ProcessedThrough.Seq);
        Assert.Equal(1, state.AppliedCommitCount);
        Assert.Equal("commit-1", state.LastCommitId);

        // 动作绑定从库里回读：重启连接后继续补齐第二段，逐字判定依旧成立。
        var continuation = await reopened.AppendDeliverySegmentAsync(
            PendingSegment("commit-1", 1, SegmentTwoText, grant.Ticket!.OwnershipEpoch), WriterA, Token);
        Assert.True(continuation.Accepted);
        Assert.Equal(2, await ScalarAsync("SELECT COUNT(*) FROM narrative_outbox WHERE commit_id='commit-1'"));
    }

    [Fact]
    public async Task B2_midwrite_constraint_failure_rolls_back_the_whole_commit()
    {
        var store = await OpenStoreAsync();
        var grant = await SetupInstanceAndWriterAsync(store);
        Assert.True((await store.TryCommitAtomicAsync(BuildRequest(), BuildCommit("commit-1", "sha256:commit-one",
            102, [101, 102], 0, grant.Ticket!.OwnershipEpoch, intents: true), null, Token)).Accepted);

        // 第二条提交本身合法，但故意复用第一条的 intent_id 撞主键：事务必须整笔回滚，异常原样上抛。
        await Assert.ThrowsAsync<SqliteException>(async () => await store.TryCommitAtomicAsync(
            BuildRequest(102, [103], "batch-2", 1),
            BuildCommit("commit-2", "sha256:commit-two", 103, [103], 1, grant.Ticket!.OwnershipEpoch,
                intents: true, intentId: "intent-later-1"),
            [PendingSegment("commit-2", 0, SegmentOneText, grant.Ticket!.OwnershipEpoch)], Token));
        store.Dispose();

        await using var reopened = await OpenStoreAsync();
        Assert.Equal(1, await ScalarAsync("SELECT COUNT(*) FROM narrative_entries"));
        Assert.Equal(1, await ScalarAsync("SELECT COUNT(*) FROM narrative_input_receipts"));
        Assert.Equal(1, await ScalarAsync("SELECT COUNT(*) FROM narrative_intents"));
        Assert.Equal(0, await ScalarAsync("SELECT COUNT(*) FROM narrative_outbox WHERE commit_id='commit-2'"));
        var state = await reopened.StateOfAsync(Alpha, Token);
        Assert.NotNull(state);
        Assert.Equal(1, state!.StoryVersion);
        Assert.Equal(1, state.AppliedCommitCount);
    }

    // —————————— C：投递段的逐字与账本规则（真实库上判定） ——————————

    [Fact]
    public async Task C1_segment_text_must_come_verbatim_from_the_bound_slice()
    {
        var store = await OpenStoreAsync();
        var grant = await SetupInstanceAndWriterAsync(store);
        await CommitFirst(store, grant);

        Assert.True((await store.AppendDeliverySegmentAsync(
            PendingSegment("commit-1", 0, SegmentOneText, Epoch(grant)), WriterA, Token)).Accepted);

        var reworded = await store.AppendDeliverySegmentAsync(
            PendingSegment("commit-1", 1, "照片？不给!", Epoch(grant)), WriterA, Token);
        Assert.Equal(NarrativeContractReason.SegmentTextNotFromNarrative, reworded.Reason);

        var shortClose = await store.AppendDeliverySegmentAsync(
            PendingSegment("commit-1", 1, "照片？不给", Epoch(grant)), WriterA, Token);
        Assert.Equal(NarrativeContractReason.SegmentTextNotFromNarrative, shortClose.Reason);
        store.Dispose();

        await using var reopened = await OpenStoreAsync();
        Assert.Equal(1, await ScalarAsync("SELECT COUNT(*) FROM narrative_outbox"));

        var sealedSegment = await reopened.AppendDeliverySegmentAsync(
            PendingSegment("commit-1", 1, SegmentTwoText, Epoch(grant)), WriterA, Token);
        Assert.True(sealedSegment.Accepted);
        Assert.Equal(SegmentTwoText, (await reopened.FindSegmentAsync(
            new DeliverySegmentKey("commit-1", "act-reply-1", 1), Token))!.Text);
    }

    [Fact]
    public async Task C2_segment_index_skip_and_ceiling_are_refused_on_the_database()
    {
        var store = await OpenStoreAsync();
        var grant = await SetupInstanceAndWriterAsync(store);
        await CommitFirst(store, grant);

        var skip = await store.AppendDeliverySegmentAsync(PendingSegment("commit-1", 1, SegmentTwoText, Epoch(grant)),
            WriterA, Token);
        Assert.Equal(NarrativeContractReason.SegmentIndexNotContiguous, skip.Reason);

        Assert.True((await store.AppendDeliverySegmentAsync(
            PendingSegment("commit-1", 0, SegmentOneText, Epoch(grant)), WriterA, Token)).Accepted);
        Assert.True((await store.AppendDeliverySegmentAsync(
            PendingSegment("commit-1", 1, SegmentTwoText, Epoch(grant)), WriterA, Token)).Accepted);

        var beyondCeiling = await store.AppendDeliverySegmentAsync(
            PendingSegment("commit-1", 2, SegmentOneText, Epoch(grant)), WriterA, Token);
        Assert.Equal(NarrativeContractReason.SegmentCountExceeded, beyondCeiling.Reason);
        store.Dispose();

        await using var reopened = await OpenStoreAsync();
        Assert.Equal(2, await ScalarAsync("SELECT COUNT(*) FROM narrative_outbox"));
    }

    [Fact]
    public async Task C3_duplicate_segment_key_returns_the_first_row_and_keeps_its_text()
    {
        var store = await OpenStoreAsync();
        var grant = await SetupInstanceAndWriterAsync(store);
        await CommitFirst(store, grant);
        Assert.True((await store.AppendDeliverySegmentAsync(
            PendingSegment("commit-1", 0, SegmentOneText, Epoch(grant)), WriterA, Token)).Accepted);

        var duplicate = await store.AppendDeliverySegmentAsync(
            PendingSegment("commit-1", 0, SegmentTwoText, Epoch(grant)), WriterA, Token);
        Assert.Equal(NarrativeContractReason.SegmentKeyDuplicate, duplicate.Reason);
        Assert.NotNull(duplicate.Segment);
        Assert.Equal(SegmentOneText, duplicate.Segment!.Text);
        store.Dispose();

        await using var reopened = await OpenStoreAsync();
        Assert.Equal(1, await ScalarAsync("SELECT COUNT(*) FROM narrative_outbox"));
        Assert.Equal(SegmentOneText, (await reopened.FindSegmentAsync(
            new DeliverySegmentKey("commit-1", "act-reply-1", 0), Token))!.Text);
    }

    [Fact]
    public async Task C4_delivery_without_chat_history_evidence_stays_pending_after_reconnect()
    {
        var store = await OpenStoreAsync();
        var grant = await SetupInstanceAndWriterAsync(store);
        await CommitFirst(store, grant);
        Assert.True((await store.AppendDeliverySegmentAsync(
            PendingSegment("commit-1", 0, SegmentOneText, Epoch(grant)), WriterA, Token)).Accepted);

        var noEvidence = await store.MarkSegmentDeliveredAsync(
            new DeliverySegmentKey("commit-1", "act-reply-1", 0), null, Epoch(grant), WriterA, Token);
        Assert.Equal(NarrativeContractReason.SegmentDeliveryEvidenceMissing, noEvidence.Reason);
        store.Dispose();

        await using var reopened = await OpenStoreAsync();
        var segment = await reopened.FindSegmentAsync(new DeliverySegmentKey("commit-1", "act-reply-1", 0), Token);
        Assert.NotNull(segment);
        Assert.Equal(DeliverySegmentState.Pending, segment!.State);
        Assert.Null(segment.DeliveredMessageId);
    }

    [Fact]
    public async Task C5_terminal_state_survives_reconnect_and_late_results_cannot_flip_it()
    {
        var store = await OpenStoreAsync();
        var grant = await SetupInstanceAndWriterAsync(store);
        await CommitFirst(store, grant);
        Assert.True((await store.AppendDeliverySegmentAsync(
            PendingSegment("commit-1", 0, SegmentOneText, Epoch(grant)), WriterA, Token)).Accepted);
        Assert.True((await store.MarkSegmentDeliveredAsync(
            new DeliverySegmentKey("commit-1", "act-reply-1", 0), "msg-9", Epoch(grant), WriterA, Token)).Accepted);
        store.Dispose();

        await using var reopened = await OpenStoreAsync();
        var lateCancel = await reopened.MarkSegmentCancelledAsync(
            new DeliverySegmentKey("commit-1", "act-reply-1", 0), "迟到的撤回", Epoch(grant), WriterA, Token);
        Assert.Equal(NarrativeContractReason.SegmentTerminalImmutable, lateCancel.Reason);

        var lateDeliver = await reopened.MarkSegmentDeliveredAsync(
            new DeliverySegmentKey("commit-1", "act-reply-1", 0), "msg-10", Epoch(grant), WriterA, Token);
        Assert.Equal(NarrativeContractReason.SegmentTerminalImmutable, lateDeliver.Reason);

        var segment = await reopened.FindSegmentAsync(new DeliverySegmentKey("commit-1", "act-reply-1", 0), Token);
        Assert.Equal(DeliverySegmentState.Delivered, segment!.State);
        Assert.Equal("msg-9", segment.DeliveredMessageId);
    }

    // —————————— D：崩溃矩阵（一切判定在重开连接之后） ——————————

    [Fact]
    public async Task D1_uncommitted_work_is_gone_and_committed_work_survives_the_reopen()
    {
        var store = await OpenStoreAsync();
        var grant = await SetupInstanceAndWriterAsync(store);
        await CommitFirst(store, grant, pendingSegments:
            [PendingSegment("commit-1", 0, SegmentOneText, Epoch(grant))]);
        store.Dispose();

        await using (var reopened = await OpenStoreAsync())
        {
            // 上一段进程只提交过 commit-1；没有第二条的任何痕迹，已提交项全在。
            Assert.Equal(1, await ScalarAsync("SELECT COUNT(*) FROM narrative_entries"));
            Assert.Equal(1, await ScalarAsync("SELECT COUNT(*) FROM narrative_outbox WHERE state=0"));
            var survivorState = await reopened.StateOfAsync(Alpha, Token);
            Assert.Equal(1, survivorState!.StoryVersion);
        }

        var recovered = await OpenStoreAsync();
        var second = await recovered.TryCommitAtomicAsync(BuildRequest(102, [103], "batch-2", 1),
            BuildCommit("commit-2", "sha256:commit-two", 103, [103], 1, Epoch(grant)), null, Token);
        Assert.True(second.Accepted);
        recovered.Dispose();

        await using var third = await OpenStoreAsync();
        Assert.Equal(2, await ScalarAsync("SELECT COUNT(*) FROM narrative_entries"));
        var state = await third.StateOfAsync(Alpha, Token);
        Assert.Equal(2, state!.StoryVersion);
        Assert.Equal(2, state.AppliedCommitCount);
    }

    [Fact]
    public async Task D2_replaying_an_applied_commit_after_reconnect_is_refused_without_advancing()
    {
        var store = await OpenStoreAsync();
        var grant = await SetupInstanceAndWriterAsync(store);
        await CommitFirst(store, grant);
        store.Dispose();

        await using var reopened = await OpenStoreAsync();
        var replay = await reopened.TryCommitAtomicAsync(BuildRequest(), BuildCommit("commit-1", "sha256:commit-one",
            102, [101, 102], 0, Epoch(grant), intents: true), null, Token);
        Assert.Equal(NarrativeContractReason.CommitAlreadyApplied, replay.Reason);
        Assert.Equal(1, await ScalarAsync("SELECT COUNT(*) FROM narrative_entries"));
        Assert.Equal(1, await ScalarAsync("SELECT COUNT(*) FROM narrative_input_receipts"));
        var state = await reopened.StateOfAsync(Alpha, Token);
        Assert.Equal(1, state!.StoryVersion);
        Assert.Equal(1, state.AppliedCommitCount);
    }

    [Fact]
    public async Task D3_delivery_lost_midrun_refills_only_the_gap_after_reconnect()
    {
        var store = await OpenStoreAsync();
        var grant = await SetupInstanceAndWriterAsync(store);
        await CommitFirst(store, grant);
        var key = new DeliverySegmentKey("commit-1", "act-reply-1", 0);
        var key2 = new DeliverySegmentKey("commit-1", "act-reply-1", 1);
        Assert.True((await store.AppendDeliverySegmentAsync(PendingSegment("commit-1", 0, SegmentOneText, Epoch(grant)),
            WriterA, Token)).Accepted);
        Assert.True((await store.AppendDeliverySegmentAsync(PendingSegment("commit-1", 1, SegmentTwoText, Epoch(grant)),
            WriterA, Token)).Accepted);
        Assert.True((await store.MarkSegmentDeliveredAsync(key, "msg-5", Epoch(grant), WriterA, Token)).Accepted);
        store.Dispose();

        await using var reopened = await OpenStoreAsync();
        var pending = await reopened.ListPendingSegmentsAsync("commit-1", Token);
        Assert.Single(pending);
        Assert.Equal(1, pending[0].SegmentIndex);

        Assert.True((await reopened.MarkSegmentDeliveredAsync(key2, "msg-6", Epoch(grant), WriterA, Token)).Accepted);
        var firstSegment = await reopened.FindSegmentAsync(key, Token);
        Assert.Equal("msg-5", firstSegment!.DeliveredMessageId);
        Assert.Equal(SegmentOneText, firstSegment.Text);
    }

    // —————————— E：部分送达矩阵（前段不落地后段不许先落） ——————————

    [Fact]
    public async Task E1_later_segment_cannot_land_before_its_predecessor()
    {
        var store = await OpenStoreAsync();
        var grant = await SetupInstanceAndWriterAsync(store);
        await CommitFirst(store, grant);
        var key = new DeliverySegmentKey("commit-1", "act-reply-1", 0);
        var key2 = new DeliverySegmentKey("commit-1", "act-reply-1", 1);
        Assert.True((await store.AppendDeliverySegmentAsync(PendingSegment("commit-1", 0, SegmentOneText, Epoch(grant)),
            WriterA, Token)).Accepted);
        Assert.True((await store.AppendDeliverySegmentAsync(PendingSegment("commit-1", 1, SegmentTwoText, Epoch(grant)),
            WriterA, Token)).Accepted);

        var outOfOrder = await store.MarkSegmentDeliveredAsync(key2, "msg-x", Epoch(grant), WriterA, Token);
        Assert.Equal(NarrativeContractReason.SegmentPredecessorPending, outOfOrder.Reason);

        Assert.True((await store.MarkSegmentDeliveredAsync(key, "msg-a", Epoch(grant), WriterA, Token)).Accepted);
        Assert.True((await store.MarkSegmentDeliveredAsync(key2, "msg-b", Epoch(grant), WriterA, Token)).Accepted);
        store.Dispose();

        await using var reopened = await OpenStoreAsync();
        Assert.Equal("msg-a", (await reopened.FindSegmentAsync(key, Token))!.DeliveredMessageId);
        Assert.Equal("msg-b", (await reopened.FindSegmentAsync(key2, Token))!.DeliveredMessageId);
    }

    // —————————— F：失权世代与删除后不复活（重开连接后仍成立） ——————————

    [Fact]
    public async Task F1_fenced_epoch_cannot_commit_or_touch_the_ledger_while_new_ticket_can()
    {
        var store = await OpenStoreAsync();
        var grant = await SetupInstanceAndWriterAsync(store);
        await CommitFirst(store, grant, pendingSegments:
            [PendingSegment("commit-1", 0, SegmentOneText, Epoch(grant))]);

        var handoverToB = await store.HandoverWriteOwnershipAsync(Alpha, WriterA, Epoch(grant),
            EngineMode.NarrativeV1, "host-b", BaseTime, Token);
        Assert.True(handoverToB.Accepted);
        var handoverBackToA = await store.HandoverWriteOwnershipAsync(Alpha, "host-b",
            handoverToB.Ticket!.OwnershipEpoch, EngineMode.NarrativeV1, WriterA, BaseTime, Token);
        Assert.True(handoverBackToA.Accepted);
        var currentEpoch = handoverBackToA.Ticket!.OwnershipEpoch;
        var staleEpoch = Epoch(grant) + 1;
        store.Dispose();

        await using var reopened = await OpenStoreAsync();
        var fencedCommit = await reopened.TryCommitAtomicAsync(BuildRequest(102, [103], "batch-2", 1),
            BuildCommit("commit-late", "sha256:commit-late", 103, [103], 1, staleEpoch), null, Token);
        Assert.Equal(NarrativeContractReason.OwnershipFenced, fencedCommit.Reason);

        var otherWriter = await reopened.TryCommitAtomicAsync(BuildRequest(102, [103], "batch-2", 1),
            BuildCommit("commit-other", "sha256:commit-other", 103, [103], 1, currentEpoch, writer: "host-c"),
            null, Token);
        Assert.Equal(NarrativeContractReason.OwnershipHeldElsewhere, otherWriter.Reason);

        var fencedTerminal = await reopened.MarkSegmentDeliveredAsync(
            new DeliverySegmentKey("commit-1", "act-reply-1", 0), "msg-stolen", staleEpoch, WriterA, Token);
        Assert.Equal(NarrativeContractReason.OwnershipFenced, fencedTerminal.Reason);

        var freshCommit = await reopened.TryCommitAtomicAsync(BuildRequest(102, [103], "batch-2", 1),
            BuildCommit("commit-2", "sha256:commit-two", 103, [103], 1, currentEpoch), null, Token);
        Assert.True(freshCommit.Accepted);

        Assert.Equal(2, await ScalarAsync("SELECT COUNT(*) FROM narrative_entries"));
        var segment = await reopened.FindSegmentAsync(new DeliverySegmentKey("commit-1", "act-reply-1", 0), Token);
        Assert.Equal(DeliverySegmentState.Pending, segment!.State);
        Assert.Null(segment.DeliveredMessageId);
    }

    [Fact]
    public async Task F2_stale_and_ahead_story_versions_are_refused_on_the_persisted_state()
    {
        var store = await OpenStoreAsync();
        var grant = await SetupInstanceAndWriterAsync(store);
        await CommitFirst(store, grant);
        store.Dispose();

        await using var reopened = await OpenStoreAsync();
        var stale = await reopened.TryCommitAtomicAsync(BuildRequest(102, [103], "batch-2", 1),
            BuildCommit("commit-stale", "sha256:commit-stale", 103, [103], 0, Epoch(grant)), null, Token);
        Assert.Equal(NarrativeContractReason.LateCommitRejected, stale.Reason);

        var ahead = await reopened.TryCommitAtomicAsync(BuildRequest(102, [103], "batch-2", 1),
            BuildCommit("commit-ahead", "sha256:commit-ahead", 103, [103], 9, Epoch(grant)), null, Token);
        Assert.Equal(NarrativeContractReason.StoryVersionConflict, ahead.Reason);

        Assert.Equal(1, await ScalarAsync("SELECT COUNT(*) FROM narrative_entries"));
        var state = await reopened.StateOfAsync(Alpha, Token);
        Assert.Equal(1, state!.StoryVersion);
        Assert.Equal(1, state.AppliedCommitCount);
    }

    [Fact]
    public async Task F3_deleted_conversation_stays_deleted_across_reconnect()
    {
        var store = await OpenStoreAsync();
        var grant = await SetupInstanceAndWriterAsync(store);
        await CommitFirst(store, grant);
        await store.InvalidateConversationAsync(Conversation, 3, BaseTime.AddMinutes(5), Token);
        store.Dispose();

        await using var reopened = await OpenStoreAsync();
        var resurrect = await reopened.TryCommitAtomicAsync(BuildRequest(102, [103], "batch-2", 1),
            BuildCommit("commit-resurrect", "sha256:commit-resurrect", 103, [103], 1, Epoch(grant), deletionEpoch: 2),
            null, Token);
        Assert.Equal(NarrativeContractReason.DeletionEpochAdvanced, resurrect.Reason);
        Assert.Equal(1, await ScalarAsync("SELECT COUNT(*) FROM narrative_entries"));

        var atCurrentEpoch = await reopened.TryCommitAtomicAsync(BuildRequest(102, [103], "batch-2", 1),
            BuildCommit("commit-epoch-3", "sha256:commit-epoch-3", 103, [103], 1, Epoch(grant), deletionEpoch: 3),
            null, Token);
        Assert.True(atCurrentEpoch.Accepted);
        var state = await reopened.StateOfAsync(Alpha, Token);
        Assert.Equal(2, state!.AppliedCommitCount);
    }

    // —————————— 组装工具：全部数据都是本类自造的测试夹具 ——————————

    private async Task<NarrativeSqliteStore> OpenStoreAsync()
    {
        var store = new NarrativeSqliteStore(_databasePath);
        await store.InitializeAsync(Token);
        return store;
    }

    private static async Task<OwnershipGrant> SetupInstanceAndWriterAsync(NarrativeSqliteStore store)
    {
        var registration = await store.RegisterInstanceAsync(AlphaKey, Alpha, InstanceProvenance.HostBound,
            originAlias: null, Token);
        Assert.True(registration.Accepted);
        var grant = await store.AcquireWriteOwnershipAsync(Alpha, EngineMode.NarrativeV1, WriterA, BaseTime, Token);
        Assert.True(grant.Accepted);
        Assert.NotNull(grant.Ticket);
        return grant;
    }

    private async Task CommitFirst(NarrativeSqliteStore store, OwnershipGrant grant,
        IReadOnlyList<DeliverySegment>? pendingSegments = null)
    {
        var outcome = await store.TryCommitAtomicAsync(BuildRequest(),
            BuildCommit("commit-1", "sha256:commit-one", 102, [101, 102], 0, Epoch(grant), intents: true),
            pendingSegments, Token);
        Assert.True(outcome.Accepted, outcome.Detail);
    }

    private static long Epoch(OwnershipGrant grant) => grant.Ticket!.OwnershipEpoch;

    private static NarrativeRequest BuildRequest(long processedThroughAtIssue = 100, long[]? seqs = null,
        string batchId = "batch-1", long storyVersion = 0) => new()
    {
        InstanceId = Alpha,
        ConversationId = Conversation,
        BatchId = batchId,
        Inputs = [.. (seqs ?? [101, 102]).Select(seq => new InputRef(seq, $"msg-{seq}", Conversation))],
        Trigger = NarrativeTriggerKind.UserInput,
        StoryVersion = storyVersion,
        IssuedByWriter = WriterA,
        MaxOutboundSegments = 4,
        IssuedAtUtc = BaseTime,
        ProcessedThroughAtIssue = new ProcessedThroughSeq(processedThroughAtIssue),
    };

    private static NarrativeCommit BuildCommit(string commitId, string contentHash, long watermarkSeq,
        long[] consumed, long expectedStoryVersion, long ownershipEpoch, bool intents = false,
        string? intentId = null, string? writer = null, long deletionEpoch = 0)
    {
        var actions = new List<CommunicationAction>
        {
            new()
            {
                ActionId = "act-reply-1",
                Mode = CommunicationMode.Immediate,
                TargetConversationId = Conversation,
                ProseQuote = BoundQuote,
                PlannedSegmentCount = 2,
            },
        };
        if (intents)
        {
            actions.Add(new CommunicationAction
            {
                ActionId = "act-deferred-1",
                Mode = CommunicationMode.Deferred,
                TargetConversationId = Conversation,
                PlannedSegmentCount = 0,
                DeferredIntentId = intentId ?? "intent-later-1",
            });
        }

        actions.Add(new CommunicationAction
        {
            ActionId = "act-silence-1",
            Mode = CommunicationMode.Silence,
            TargetConversationId = Conversation,
            PlannedSegmentCount = 0,
        });

        IReadOnlyList<NarrativeIntent> commitIntents = intents
            ?
            [
                new NarrativeIntent
                {
                    IntentId = intentId ?? "intent-later-1",
                    InstanceId = Alpha,
                    Topic = "改天把画具收完",
                    Status = NarrativeIntentStatus.Pending,
                    SourceCommitId = commitId,
                    ProactiveSendPermitted = true,
                },
            ]
            : [];

        return new NarrativeCommit
        {
            CommitId = commitId,
            InstanceId = Alpha,
            ConversationId = Conversation,
            BatchId = expectedStoryVersion == 0 ? "batch-1" : "batch-2",
            NarrativeProse = Prose,
            Actions = actions,
            Intents = commitIntents,
            ProcessedThrough = new ProcessedThroughSeq(watermarkSeq),
            PerceivedInputRefs = new PerceivedInputRefs
            {
                Refs = [.. consumed.Select(seq => new InputRef(seq, $"msg-{seq}", Conversation))],
            },
            ExpectedStoryVersion = expectedStoryVersion,
            DeletionEpoch = deletionEpoch,
            OwnershipEpoch = ownershipEpoch,
            ContentHash = contentHash,
            CommittedByWriter = writer ?? WriterA,
            CommittedAtUtc = BaseTime.AddMinutes(1),
            ProposalId = $"proposal-{commitId}",
            ConsumedInputSeqs = consumed,
        };
    }

    private static DeliverySegment PendingSegment(string commitId, int index, string text, long ownershipEpoch) => new()
    {
        InstanceId = Alpha,
        ConversationId = Conversation,
        CommitId = commitId,
        ActionId = "act-reply-1",
        SegmentIndex = index,
        State = DeliverySegmentState.Pending,
        Text = text,
        OwnershipEpoch = ownershipEpoch,
    };

    private async Task<long> ScalarAsync(string sql)
    {
        await using var connection = new SqliteConnection($"Data Source={_databasePath};Pooling=False");
        await connection.OpenAsync(Token);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync(Token);
        return value is null or DBNull ? 0L : Convert.ToInt64(value);
    }

    private static string DatabaseSha256(string path) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(ReadBytesShared(path))).ToLowerInvariant();

    /// <summary>活连接仍持有句柄时 File.ReadAllBytes 会撞共享冲突；测试只旁观不写，放开共享读。</summary>
    private static byte[] ReadBytesShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}

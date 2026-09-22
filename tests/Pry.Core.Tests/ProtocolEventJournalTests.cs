using Microsoft.Data.Sqlite;
using Pry.Core.Memory;
using Pry.Core.Models;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// PLAT-005 M0-c 的持久事件日志判据。契约见 docs/protocol-versioning-and-websocket-migration.md §4.1。
/// </summary>
public sealed class ProtocolEventJournalTests
{
    [Fact]
    public async Task Schema_creates_the_journal_with_its_indexes_and_bumps_the_version()
    {
        using var fixture = new Fixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Database.InitializeAsync(ct);

        Assert.Equal(MemoryDatabase.SchemaVersion, await fixture.Database.GetSchemaVersionAsync(ct));
        await using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE tbl_name='protocol_events' ORDER BY name";
        await using var reader = await command.ExecuteReaderAsync(ct);
        var objects = new List<string>();
        while (await reader.ReadAsync(ct)) objects.Add(reader.GetString(0));
        Assert.Contains("protocol_events", objects);
        Assert.Contains("ix_protocol_events_conversation", objects);
        Assert.Contains("ix_protocol_events_created", objects);

        await using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_key_list(protocol_events)";
        var keys = new List<(string Table, string OnDelete)>();
        await using var keyReader = await pragma.ExecuteReaderAsync(ct);
        while (await keyReader.ReadAsync(ct))
            // PRAGMA foreign_key_list 的列序是 id, seq, table, from, to, on_update, on_delete, match；
            // 读错一格就会把 on_update 当成 on_delete，判据看着对其实什么都没验。
            keys.Add((keyReader.GetString(2), keyReader.GetString(6)));
        Assert.Contains(("conversations", "CASCADE"), keys);
    }

    [Fact]
    public async Task An_older_database_rolls_forward_once_and_keeps_that_version_after_reopening()
    {
        using var fixture = new Fixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Database.InitializeAsync(ct);
        // 造出"上一个版本"的库：删掉本片新增的表并把版本退回它之前，等价于老用户的升级现场。
        await using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            await connection.OpenAsync(ct);
            await using var reset = connection.CreateCommand();
            reset.CommandText = "DROP TABLE protocol_events; UPDATE schema_info SET version=$previous";
            reset.Parameters.AddWithValue("$previous", MemoryDatabase.SchemaVersion - 1);
            await reset.ExecuteNonQueryAsync(ct);
        }

        await new MemoryDatabase(fixture.Path).InitializeAsync(ct);
        Assert.Equal(MemoryDatabase.SchemaVersion, await fixture.Database.GetSchemaVersionAsync(ct));
        await fixture.Database.EnsureConversationAsync("a", "c", ct);
        var sequence = await fixture.Database.AppendProtocolEventAsync("a", "message.created",
            """{"messageId":1}""", ct);

        // 前滚必须幂等：重复初始化既不能再推版本号，也不能清掉已经升上来的数据。
        await new MemoryDatabase(fixture.Path).InitializeAsync(ct);
        Assert.Equal(MemoryDatabase.SchemaVersion, await fixture.Database.GetSchemaVersionAsync(ct));
        Assert.Equal([sequence], (await fixture.Database.ReadProtocolEventsAsync("a", 0, 100, ct)).Select(x => x.Sequence));
    }

    [Fact]
    public async Task Sequences_are_allocated_globally_monotonically_and_read_per_conversation()
    {
        using var fixture = new Fixture();
        var ct = TestContext.Current.CancellationToken;
        var database = fixture.Database;
        await database.InitializeAsync(ct);
        await database.EnsureConversationAsync("a", "c", ct);
        await database.EnsureConversationAsync("b", "c", ct);

        var a1 = await database.AppendProtocolEventAsync("a", "message.created", """{"messageId":1}""", ct);
        var b1 = await database.AppendProtocolEventAsync("b", "message.created", """{"messageId":2}""", ct);
        var a2 = await database.AppendProtocolEventAsync("a", "conversation.read", """{"lastReadMessageId":1}""", ct);

        Assert.True(a1 < b1 && b1 < a2, "序号必须跨会话单调，否则重启归零会饿死带旧游标的客户端");
        // 同会话留洞是正常状态：判据只有服务端一条，客户端不得自判丢失。
        Assert.True(a2 - a1 > 1);

        var window = await database.GetProtocolEventWindowAsync("a", ct);
        Assert.Equal(a1, window.FirstRetained);
        Assert.Equal(a2, window.Latest);
        Assert.Equal(a2, window.GlobalHead);

        var replay = await database.ReadProtocolEventsAsync("a", a1, 100, ct);
        Assert.Equal([a2], replay.Select(x => x.Sequence));
        Assert.Equal("conversation.read", Assert.Single(replay).Type);
    }

    [Fact]
    public async Task Level_triggered_events_can_never_be_stored()
    {
        using var fixture = new Fixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Database.InitializeAsync(ct);
        await fixture.Database.EnsureConversationAsync("a", "c", ct);

        Assert.False(MemoryDatabase.IsDurableEvent("turn.state"));
        Assert.False(MemoryDatabase.IsDurableEvent("session.ready"));
        // 新事件类型默认可重放：漏放一条事实比多放一次状态的代价高。
        Assert.True(MemoryDatabase.IsDurableEvent("some.future.fact"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Database.AppendProtocolEventAsync("a", "turn.state", """{"state":"Idle"}""", ct));
        Assert.Equal(0, await fixture.Database.CountProtocolEventsAsync(token: ct));
    }

    [Fact]
    public async Task Trimming_respects_every_retention_axis_and_its_lower_bound()
    {
        using var fixture = new Fixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Database.InitializeAsync(ct);
        await fixture.Database.EnsureConversationAsync("a", "c", ct);
        await fixture.Database.EnsureConversationAsync("b", "c", ct);
        for (var index = 0; index < 60; index++)
            await fixture.Database.AppendProtocolEventAsync("a", "message.created", $$"""{"messageId":{{index}}}""", ct);
        for (var index = 0; index < 4; index++)
            await fixture.Database.AppendProtocolEventAsync("b", "message.created", $$"""{"messageId":{{index}}}""", ct);

        // 每会话超额只削最旧，另一个会话不受影响。
        Assert.Equal(10, await fixture.Database.PruneProtocolEventsAsync(50, 365, 200_000, ct));
        Assert.Equal(50, await fixture.Database.CountProtocolEventsAsync("a", ct));
        Assert.Equal(4, await fixture.Database.CountProtocolEventsAsync("b", ct));

        // 再收紧到每会话 12 条。
        Assert.Equal(38, await fixture.Database.PruneProtocolEventsAsync(12, 365, 200_000, ct));
        Assert.Equal(12, await fixture.Database.CountProtocolEventsAsync("a", ct));
        Assert.Equal(4, await fixture.Database.CountProtocolEventsAsync("b", ct));

        // 年龄维度是全局的：把时间戳推到 30 天之前（模拟一个周末没开客户端），按 1 天窗口应全部出局。
        await BackdateAsync(fixture.Path, DateTimeOffset.UtcNow.AddDays(-30), ct);
        Assert.Equal(16, await fixture.Database.PruneProtocolEventsAsync(5_000, 1, 200_000, ct));
        Assert.Equal(0, await fixture.Database.CountProtocolEventsAsync(token: ct));
    }

    [Fact]
    public async Task Total_row_cap_only_keeps_the_newest_rows_across_conversations()
    {
        using var fixture = new Fixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Database.InitializeAsync(ct);
        await fixture.Database.EnsureConversationAsync("a", "c", ct);
        await fixture.Database.EnsureConversationAsync("b", "c", ct);
        long lastA = 0, lastB = 0;
        for (var index = 0; index < 6; index++)
            lastA = await fixture.Database.AppendProtocolEventAsync("a", "message.created", $$"""{"messageId":{{index}}}""", ct);
        for (var index = 0; index < 6; index++)
            lastB = await fixture.Database.AppendProtocolEventAsync("b", "message.created", $$"""{"messageId":{{100 + index}}}""", ct);

        // 总量约束按"全局最新的 N 条"留，所以另一个会话可能整段出局。
        var removed = await fixture.Database.PruneProtocolEventsAsync(5_000, 365, 5, ct);
        Assert.Equal(7, removed);
        Assert.Equal(5, await fixture.Database.CountProtocolEventsAsync(token: ct));
        Assert.Equal(0, await fixture.Database.CountProtocolEventsAsync("a", ct));
        var survivors = await fixture.Database.ReadProtocolEventsAsync("b", 0, 100, ct);
        Assert.Equal(5, survivors.Count);
        Assert.Equal(lastA + 2, survivors[0].Sequence);
        Assert.Equal(lastB, survivors[^1].Sequence);
    }

    [Fact]
    public async Task Deleting_a_message_masks_its_journal_rows_in_the_same_transaction()
    {
        using var fixture = new Fixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Database.InitializeAsync(ct);
        await fixture.Database.EnsureConversationAsync("a", "c", ct);
        var doomed = await fixture.Database.AddMessageAsync("a", ChatRole.User, "明天去医院。", null, ct);
        var kept = await fixture.Database.AddMessageAsync("a", ChatRole.User, "周五交报告。", null, ct);
        var doomedEvent = await fixture.Database.AppendProtocolEventAsync("a", "message.created",
            $$"""{"messageId":{{doomed}},"content":"明天去医院。"}""", ct);
        await fixture.Database.AppendProtocolEventAsync("a", "message.created",
            $$"""{"messageId":{{kept}},"content":"周五交报告。"}""", ct);

        _ = await fixture.Database.DeleteMessageAsync("a", doomed, ct);

        // 删除消息若不在同一事务遮蔽事件行，"删除"就会在传输层留下一份明文副本。
        var rows = await fixture.Database.ReadProtocolEventsAsync("a", 0, 100, ct);
        Assert.DoesNotContain(rows, row => row.Sequence == doomedEvent);
        Assert.DoesNotContain(rows, row => row.PayloadJson.Contains("医院", StringComparison.Ordinal));
        Assert.Equal(kept, Assert.Single(rows.Select(SubjectOf)));
    }

    [Fact]
    public async Task Masking_hides_the_whole_deleted_range_and_survives_a_rollback()
    {
        using var fixture = new Fixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Database.InitializeAsync(ct);
        await fixture.Database.EnsureConversationAsync("a", "c", ct);
        var first = await fixture.Database.AddMessageAsync("a", ChatRole.User, "第一条", null, ct);
        await fixture.Database.AddMessageAsync("a", ChatRole.Assistant, "第二条", null, ct);
        foreach (var messageId in new[] { first, first + 1 })
            await fixture.Database.AppendProtocolEventAsync("a", "message.created",
                $$"""{"messageId":{{messageId}}}""", ct);
        Assert.Equal(2, await fixture.Database.CountProtocolEventsAsync("a", ct));

        // from_message 语义要连范围一起遮蔽，否则"删除这一条及其后"只删了历史。
        _ = await fixture.Database.DeleteMessageAndFollowingAsync("a", first, ct);
        Assert.Equal(0, await fixture.Database.CountProtocolEventsAsync("a", ct));
    }

    [Fact]
    public async Task Deleting_the_conversation_cascades_the_journal_but_never_the_other_way_round()
    {
        using var fixture = new Fixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Database.InitializeAsync(ct);
        await fixture.Database.EnsureConversationAsync("a", "c", ct);
        await fixture.Database.AddMessageAsync("a", ChatRole.User, "内容", null, ct);
        await fixture.Database.AppendProtocolEventAsync("a", "message.created", """{"messageId":1}""", ct);

        await fixture.Database.DeleteConversationAsync("a", ct);
        Assert.Equal(0, await fixture.Database.CountProtocolEventsAsync(token: ct));
    }

    [Fact]
    public async Task Trimming_never_touches_the_domain_history_it_is_not_a_memory_store()
    {
        using var fixture = new Fixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Database.InitializeAsync(ct);
        await fixture.Database.EnsureConversationAsync("a", "c", ct);
        for (var index = 0; index < 10; index++)
        {
            var messageId = await fixture.Database.AddMessageAsync("a", index % 2 == 0 ? ChatRole.User : ChatRole.Assistant,
                $"消息 {index}", null, ct);
            await fixture.Database.AppendProtocolEventAsync("a", "message.created",
                $$"""{"messageId":{{messageId}},"content":"消息 {{index}}"}""", ct);
        }
        var before = await fixture.Database.GetRecentMessagesAsync("a", 50, ct);
        var memoriesBefore = await fixture.Database.ListMemoriesAsync("c", null, ct);

        Assert.True(await fixture.Database.PruneProtocolEventsAsync(50, 365, 5, ct) > 0);

        // 读边界：journal ≠ Memory。裁剪或清理不得动 messages/memories 任何一行，
        // 任何界面或 Runtime 也不得把这张表当历史读取。
        var after = await fixture.Database.GetRecentMessagesAsync("a", 50, ct);
        Assert.Equal(before.Select(x => (x.Id, x.Role, x.Content, x.CreatedAt)),
            after.Select(x => (x.Id, x.Role, x.Content, x.CreatedAt)));
        Assert.Equal(memoriesBefore.Count, (await fixture.Database.ListMemoriesAsync("c", null, ct)).Count);
        Assert.Equal(5, await fixture.Database.CountProtocolEventsAsync(token: ct));
    }

    [Fact]
    public async Task Subject_message_reference_is_taken_from_the_payload_so_publishers_cannot_opt_out()
    {
        using var fixture = new Fixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Database.InitializeAsync(ct);
        await fixture.Database.EnsureConversationAsync("a", "c", ct);

        await fixture.Database.AppendProtocolEventAsync("a", "turn.cancelled", "{}", ct);
        await fixture.Database.AppendProtocolEventAsync("a", "message.created", """{"messageId":"42"}""", ct);
        await fixture.Database.AppendProtocolEventAsync("a", "conversation.read", """{"lastReadMessageId":9}""", ct);

        await using var connection = new SqliteConnection($"Data Source={fixture.Path}");
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT type, subject_message_id FROM protocol_events ORDER BY sequence";
        await using var reader = await command.ExecuteReaderAsync(ct);
        var subjects = new List<(string Type, bool Null, long Value)>();
        while (await reader.ReadAsync(ct))
            subjects.Add((reader.GetString(0), reader.IsDBNull(1),
                reader.IsDBNull(1) ? 0L : reader.GetInt64(1)));
        Assert.Equal([("turn.cancelled", true, 0L), ("message.created", false, 42L), ("conversation.read", true, 0L)],
            subjects);
    }

    [Fact]
    public async Task Writing_an_event_for_an_unknown_conversation_fails_loud()
    {
        using var fixture = new Fixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Database.InitializeAsync(ct);

        // 外键生效才意味着这张表不会积累无人认领的历史；静默写入会让删除语义失去落点。
        await Assert.ThrowsAnyAsync<SqliteException>(() =>
            fixture.Database.AppendProtocolEventAsync("ghost", "message.created", """{"messageId":1}""", ct));
    }

    private static long? SubjectOf(ProtocolEventRecord row)
    {
        using var document = System.Text.Json.JsonDocument.Parse(row.PayloadJson);
        return document.RootElement.TryGetProperty("messageId", out var value) ? value.GetInt64() : null;
    }

    /// <summary>年龄判据没法靠等待来测，所以直接把时间戳推到过去。</summary>
    private static async Task BackdateAsync(string path, DateTimeOffset createdAt, CancellationToken token)
    {
        await using var connection = new SqliteConnection($"Data Source={path}");
        await connection.OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE protocol_events SET created_at=$at";
        command.Parameters.AddWithValue("$at", createdAt.ToString("O"));
        await command.ExecuteNonQueryAsync(token);
    }

    private sealed class Fixture : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            $"pry-journal-{Guid.NewGuid():N}", "memory.db");
        public MemoryDatabase Database { get; }
        public Fixture() => Database = new(Path);
        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            var root = System.IO.Path.GetDirectoryName(Path)!;
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}

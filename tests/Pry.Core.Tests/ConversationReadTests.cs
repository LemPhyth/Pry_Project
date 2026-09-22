using Microsoft.Data.Sqlite;
using Pry.Core.Memory;
using Pry.Core.Models;
using Xunit;

namespace Pry.Core.Tests;

public sealed class ConversationReadTests
{
    [Fact]
    public async Task Read_watermark_survives_reopen_and_does_not_mark_newer_messages_read()
    {
        using var f = new Fixture(); var ct = TestContext.Current.CancellationToken;
        await f.Database.InitializeAsync(ct);
        await f.Database.EnsureConversationAsync("room", "role", ct);
        await f.Database.AddMessageAsync("room", ChatRole.User, "用户", null, ct);
        var first = await f.Database.AddMessageAsync("room", ChatRole.Assistant, "主动", null, ct, source: MessageSources.Proactive);
        var second = await f.Database.AddMessageAsync("room", ChatRole.Assistant, "互动", null, ct, source: MessageSources.InteractionPreset);
        var before = (await f.Database.GetConversationAsync("room", ct))!;
        Assert.Equal(2, before.UnreadCount); Assert.Equal(second, before.LastMessageId);
        Assert.Equal(0, before.LastReadMessageId);
        var marked = (await f.Database.MarkConversationReadAsync("room", first, ct))!;
        Assert.True(marked.Advanced); Assert.Equal(1, marked.State.UnreadCount);
        var reopened = new MemoryDatabase(f.Path); await reopened.InitializeAsync(ct);
        Assert.Equal(marked.State, await reopened.GetConversationReadStateAsync("room", ct));
        Assert.Equal(before.UpdatedAt, (await reopened.GetConversationAsync("room", ct))!.UpdatedAt);
        Assert.False((await reopened.MarkConversationReadAsync("room", 0, ct))!.Advanced);
        Assert.False((await reopened.MarkConversationReadAsync("room", first, ct))!.Advanced);
        Assert.Equal(first, Assert.Single(await reopened.ListConversationsAsync(100, ct)).LastReadMessageId);
        Assert.Equal(new UnreadSummary(1, 1), await reopened.GetUnreadSummaryAsync(ct));
    }

    [Fact]
    public async Task Deletion_undo_and_conversation_deletion_preserve_the_defined_read_semantics()
    {
        using var f = new Fixture(); var ct = TestContext.Current.CancellationToken;
        await f.Database.InitializeAsync(ct);
        await f.Database.EnsureConversationAsync("room", "role", ct);
        var read = await f.Database.AddMessageAsync("room", ChatRole.Assistant, "已读", null, ct);
        await f.Database.MarkConversationReadAsync("room", read, ct);
        var snapshot = (await f.Database.DeleteMessageAsync("room", read, ct))!;
        Assert.False((await f.Database.MarkConversationReadAsync("room", read, ct))!.Advanced);
        await f.Database.RestoreConversationMutationAsync(snapshot, ct);
        Assert.Equal(0, (await f.Database.GetConversationReadStateAsync("room", ct))!.UnreadCount);
        var unread = await f.Database.AddMessageAsync("room", ChatRole.Assistant, "未读", null, ct);
        var deleted = (await f.Database.DeleteMessageAsync("room", unread, ct))!;
        Assert.Equal(0, (await f.Database.GetConversationReadStateAsync("room", ct))!.UnreadCount);
        await Assert.ThrowsAsync<InvalidReadWatermarkException>(() => f.Database.MarkConversationReadAsync("room", unread, ct));
        await f.Database.RestoreConversationMutationAsync(deleted, ct);
        Assert.Equal(1, (await f.Database.GetConversationReadStateAsync("room", ct))!.UnreadCount);
        await f.Database.DeleteConversationAsync("room", ct);
        Assert.Null(await f.Database.GetConversationReadStateAsync("room", ct));
        Assert.Equal(new UnreadSummary(0, 0), await f.Database.GetUnreadSummaryAsync(ct));
    }

    [Fact]
    public async Task Concurrent_read_requests_are_monotonic_and_reject_cross_room_or_future_advancement()
    {
        using var f = new Fixture(); var ct = TestContext.Current.CancellationToken;
        await f.Database.InitializeAsync(ct);
        var low = await f.Database.AddMessageAsync("room", ChatRole.Assistant, "一", null, ct);
        var high = await f.Database.AddMessageAsync("room", ChatRole.Assistant, "二", null, ct);
        await Task.WhenAll(new[] { low, high, low }.Select(id => Task.Run(() => f.Database.MarkConversationReadAsync("room", id, ct), ct)));
        Assert.Equal(high, (await f.Database.GetConversationReadStateAsync("room", ct))!.LastReadMessageId);
        var other = await f.Database.AddMessageAsync("other", ChatRole.Assistant, "其他", null, ct);
        await Assert.ThrowsAsync<InvalidReadWatermarkException>(() => f.Database.MarkConversationReadAsync("room", other, ct));
        await Assert.ThrowsAsync<InvalidReadWatermarkException>(() => f.Database.MarkConversationReadAsync("room", long.MaxValue, ct));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => f.Database.MarkConversationReadAsync("room", -1, ct));
        Assert.Null(await f.Database.MarkConversationReadAsync("missing", 0, ct));
        await Task.WhenAll(Task.Run(() => f.Database.MarkConversationReadAsync("room", high, ct), ct),
            Task.Run(() => f.Database.AddMessageAsync("room", ChatRole.Assistant, "并发新消息", null, ct), ct));
        Assert.Equal(1, (await f.Database.GetConversationReadStateAsync("room", ct))!.UnreadCount);
        Assert.Equal(new UnreadSummary(2, 2), await f.Database.GetUnreadSummaryAsync(ct));
    }

    [Fact]
    public async Task Upgrade_defaults_old_messages_to_unread_and_failed_write_rolls_back()
    {
        using var f = new Fixture(); var ct = TestContext.Current.CancellationToken;
        await f.Database.InitializeAsync(ct);
        var id = await f.Database.AddMessageAsync("room", ChatRole.Assistant, "旧消息", null, ct);
        await using (var connection = new SqliteConnection($"Data Source={f.Path}"))
        {
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = "ALTER TABLE conversations DROP COLUMN last_read_message_id; UPDATE schema_info SET version=6;";
            await command.ExecuteNonQueryAsync(ct);
        }
        await f.Database.InitializeAsync(ct); await f.Database.InitializeAsync(ct);
        Assert.Equal(1, (await f.Database.GetConversationReadStateAsync("room", ct))!.UnreadCount);
        await using (var connection = new SqliteConnection($"Data Source={f.Path}"))
        {
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE TRIGGER fail_read BEFORE UPDATE OF last_read_message_id ON conversations BEGIN SELECT RAISE(ABORT,'test'); END;";
            await command.ExecuteNonQueryAsync(ct);
            await Assert.ThrowsAsync<SqliteException>(() => f.Database.MarkConversationReadAsync("room", id, ct));
            Assert.Equal(0, (await f.Database.GetConversationReadStateAsync("room", ct))!.LastReadMessageId);
            command.CommandText = "EXPLAIN QUERY PLAN SELECT COUNT(*) FROM messages WHERE conversation_id='room' AND role='assistant' AND id>0";
            await using var reader = await command.ExecuteReaderAsync(ct);
            var plan = new List<string>(); while (await reader.ReadAsync(ct)) plan.Add(reader.GetString(3));
            Assert.Contains(plan, x => x.Contains("ix_messages_conversation_role_id", StringComparison.Ordinal));
        }
    }

    private sealed class Fixture : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"pry-read-{Guid.NewGuid():N}", "memory.db");
        public MemoryDatabase Database { get; }
        public Fixture() => Database = new(Path);
        public void Dispose() { SqliteConnection.ClearAllPools(); Directory.Delete(System.IO.Path.GetDirectoryName(Path)!, true); }
    }
}

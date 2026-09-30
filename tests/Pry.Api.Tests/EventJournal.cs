using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Pry.Api.Services;
using Pry.Core.Memory;

namespace Pry.Api.Tests;

/// <summary>
/// 带真实 SQLite 的协议日志夹具。事件序号只有落库才有意义，所以这里不作假：
/// 内存桩会让"全局单调""跨实例存活""删除遮蔽"三类判据全部退化成自说自话。
/// </summary>
public sealed class EventJournal(string root, MemoryDatabase database, string conversationId) : IDisposable
{
    public MemoryDatabase Database { get; } = database;

    public string ConversationId { get; } = conversationId;

    public static async Task<EventJournal> CreateAsync(CancellationToken token, string conversationId = "room")
    {
        var root = Path.Combine(Path.GetTempPath(), $"pry-journal-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var database = new MemoryDatabase(Path.Combine(root, "memory.db"));
        await database.InitializeAsync(token);
        await database.EnsureConversationAsync(conversationId, "character", token);
        return new EventJournal(root, database, conversationId);
    }

    public ConversationEventLog CreateLog(string? overrideConversation = null) =>
        new(Database, overrideConversation ?? ConversationId, NullLogger.Instance);

    public void Dispose()
    {
        SqliteTestPools.Clear(Path.Combine(root, "memory.db"));
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}

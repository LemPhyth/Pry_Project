using Microsoft.Data.Sqlite;
using Pry.Core.Memory;
using Xunit;

namespace Pry.Core.Tests;

// QA-TIP1019-02：检索地板的行为判据。锚点＝MemoryQueryService.cs:22 的 `x.Confidence >= .5`（与状态闸同一行，本族只钉置信度那半）。
// 断言面钉在 MemoryQueryService 的返回（Hits），不钉端到端：同链 ContextBuilder 有第二道同值地板（防御纵深），
// 端到端断言在单点突变下会拿到假绿（工单 §7 F6／§8 补正二）。判据跑真行为——把两行 active 记忆喂进检索看返回哪些行，
// 不用源码文本门（§3.2 明令禁止）。
public sealed class MemoryRetrievalFloorTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "pry-memory-floor-tests-" + Guid.NewGuid().ToString("N"));
    private MemoryDatabase _db = null!;
    private SqliteMemoryRepository Repo => _db.LongTerm;
    private readonly MemoryScope _scope = new("instance:floor-alice", "pry");
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;
    private CancellationToken Token => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_directory);
        _db = new MemoryDatabase(Path.Combine(_directory, "memory.db"));
        await _db.InitializeAsync(Token);
    }

    public ValueTask DisposeAsync()
    {
        SqliteTestPools.Clear(Path.Combine(_directory, "memory.db"));
        Directory.Delete(_directory, true);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Active_memory_at_confidence_floor_is_retrieved_while_active_memory_below_floor_is_not()
    {
        // 同一角色、同一作用域的两行 active 手动记忆：置信度字面量一行落在地板 `.5`、一行落在地板下 `.45`。
        // 两行共用同一查询词与同一重要性，唯一差异就是置信度——突变 `>= .5`→`>= .4` 时 `.45` 行必须混进 Hits 把本用例压红。
        var atFloor = await Repo.SaveManualAsync(_scope, "event", "用户喜欢后摇专辑", "地板判据上", 1, token: Token);
        var belowFloor = await Repo.SaveManualAsync(_scope, "event", "用户喜欢后摇演出", "地板判据下", 1, token: Token);
        await using (var c = new SqliteConnection($"Data Source={Path.Combine(_directory, "memory.db")}"))
        {
            await c.OpenAsync(Token);
            using var cmd = c.CreateCommand();
            cmd.CommandText = "UPDATE memories SET confidence=.5 WHERE id=$at; UPDATE memories SET confidence=.45 WHERE id=$below";
            cmd.Parameters.AddWithValue("$at", atFloor.Record.Id);
            cmd.Parameters.AddWithValue("$below", belowFloor.Record.Id);
            await cmd.ExecuteNonQueryAsync(Token);
        }

        // 夹具自检：两行都必须是 Status="active"，地板只拦置信度，不许状态那半替本判据背书。
        var rows = await Repo.ListAsync(_scope, Token);
        Assert.Equal(2, rows.Count(x => x.Status == "active"));
        Assert.Equal(.5, Assert.Single(rows, x => x.Record.Id == atFloor.Record.Id).Confidence, 5);
        Assert.Equal(.45, Assert.Single(rows, x => x.Record.Id == belowFloor.Record.Id).Confidence, 5);

        var hits = (await new MemoryQueryService(Repo).RetrieveAsync(
            new MemoryQuery(_scope, "floor-conv", "聊聊后摇", Now: _now), Token)).Hits;

        // 行为判据本体：只有落在 `.5` 的那行进检索结果，`.45` 那行即使关键词与重要性同样合格也必须被地板拦下。
        var hit = Assert.Single(hits);
        Assert.Equal("用户喜欢后摇专辑", hit.Memory.Record.Summary);
        Assert.Equal(.5, hit.Memory.Confidence, 5);
        Assert.DoesNotContain("用户喜欢后摇演出", hits.Select(x => x.Memory.Record.Summary));
    }
}

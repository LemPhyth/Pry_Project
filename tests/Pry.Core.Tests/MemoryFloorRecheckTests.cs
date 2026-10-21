using Microsoft.Data.Sqlite;
using Pry.Core.Memory;
using Pry.Core.Models;
using Pry.Core.Prompting;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// QA-TIP1019-02 的第二道门（ContextBuilder.cs 组装前重读资格核对 <c>x.Status == "active" &amp;&amp; x.Confidence &gt;= .5</c>）。
/// 第一道门（MemoryQueryService 检索地板）已由 <c>MemoryRetrievalFloorTests</c> 钉住，本文件刻意**不碰它**——
/// 各道门各有各的判据，才不会互相顶包。这里钉的是查询之后的时间窗：缓存 hit 里合格的那条记忆，
/// 在组装前被降到地板下 ⇒ 第二道门是唯一的拦截处，提示词里不许出现它，dropped 记账给出 stale_or_deleted。
/// 两条对照：贴地板 <c>.5</c> 的必须留在提示词里（拦截只针对地板下），<c>.45</c> 的必须被拦。
/// 断言面全部落在 <see cref="ContextBuilder"/> 产出的提示词与 dropped 原因上；不是 grep 型源码文本门。
/// </summary>
public sealed class MemoryFloorRecheckTests : IAsyncLifetime
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "pry-memory-floor-recheck-" + Guid.NewGuid().ToString("N"));
    private MemoryDatabase _db = null!;
    private SqliteMemoryRepository Repo => _db.LongTerm;
    private readonly MemoryScope _scope = new("instance:recheck-alice", "pry");
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
    public async Task Hit_passed_at_query_time_but_demoted_below_floor_before_assembly_is_dropped_by_the_recheck()
    {
        var demoted = await Repo.SaveManualAsync(_scope, "event", "用户上周去了动物园", "复核-查询后降级", 1, token: Token);
        var atFloor = await Repo.SaveManualAsync(_scope, "event", "用户喜欢后摇专辑", "复核-贴地板", 1, token: Token);
        var belowFloor = await Repo.SaveManualAsync(_scope, "event", "用户家里养了一只猫", "复核-地板下", 1, token: Token);

        // 查询时刻：降级条在地板上（.6，属于"查询时合格"），对照条分别在 .5 与 .45。
        await SetConfidence((demoted.Record.Id, .6), (atFloor.Record.Id, .5), (belowFloor.Record.Id, .45));

        // 夹具自检一：三行必须同为 active——状态那半不许替本判据背书，唯一的变量是置信度。
        var atQueryTime = await Repo.ListAsync(_scope, Token);
        Assert.Equal(3, atQueryTime.Count(x => x.Status == "active"));
        Assert.Equal(.6, Assert.Single(atQueryTime, x => x.Record.Id == demoted.Record.Id).Confidence, 5);
        Assert.Equal(.5, Assert.Single(atQueryTime, x => x.Record.Id == atFloor.Record.Id).Confidence, 5);
        Assert.Equal(.45, Assert.Single(atQueryTime, x => x.Record.Id == belowFloor.Record.Id).Confidence, 5);

        // 查询产物的快照：三条都"曾在 hits 里"。本判据不经 MemoryQueryService 重放——
        // 缓存 hit 带着旧快照走到组装，正是第二道门存在的理由。
        var revision = await Repo.RevisionAsync(_scope, Token);
        var hits = atQueryTime
            .Select(entry => new MemoryHit(entry, .6, ["fixture-cached-hit"]))
            .ToList();
        var memory = new MemoryContext(_scope, MemoryQueryOutcome.Ok, hits,
            new RetrievalTrace(3, 3, hits.Count, revision, false, ["fixture-cached"]));

        // 组装前：demoted 被降到地板下（.45），hits 里的快照仍是合格的那份。
        await SetConfidence((demoted.Record.Id, .45));
        var recheck = await Repo.ListAsync(_scope, Token);
        var fell = Assert.Single(recheck, x => x.Record.Id == demoted.Record.Id);
        Assert.Equal(.45, fell.Confidence, 5);
        Assert.Equal("active", fell.Status); // 降级没有动状态——被拦只能是因为置信度，不许让状态背书。

        var current = new ChatMessage(1, "recheck-conv", ChatRole.User, "聊聊后摇演出吧", DateTimeOffset.UtcNow);
        var request = new ContextRequest(_scope, "recheck-conv", "你是普莱。", "", current, [current],
            memory, new ContextBudget());

        var context = await new ContextBuilder(Repo).BuildAsync(request, Token);

        // 行为判据本体：贴地板的留在提示词里；查询后降级的与地板下的都不许进提示词。
        Assert.Contains("用户喜欢后摇专辑", context.SystemPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain("用户上周去了动物园", context.SystemPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain("用户家里养了一只猫", context.SystemPrompt, StringComparison.Ordinal);

        // dropped 记账：两条降级/地板下各记一次 stale_or_deleted；不许是别的成因冒充拦截。
        Assert.Equal(2, context.Dropped.Count(d =>
            d.Section == "RelevantMemories" && d.Reason == "stale_or_deleted"));
        Assert.DoesNotContain(context.Dropped, d => d.Reason == "memory_changed_during_assembly");
        Assert.DoesNotContain(context.Dropped, d => d.Reason == "scope_or_unavailable");
        Assert.DoesNotContain(context.Dropped, d => d.Reason == "repository_unavailable");
    }

    private async Task SetConfidence(params (long Id, double Confidence)[] updates)
    {
        await using var connection = new SqliteConnection($"Data Source={Path.Combine(_directory, "memory.db")}");
        await connection.OpenAsync(Token);
        foreach (var (id, confidence) in updates)
        {
            await using var command = connection.CreateCommand();
            // 只动 confidence：不在 memory_manual_edit_fence 的列清单里，scope epoch 不推进，
            // 拦截必须来自地板复核本身，而不是"版本变了"那类旁路。
            command.CommandText = "UPDATE memories SET confidence=$c WHERE id=$id";
            command.Parameters.AddWithValue("$c", confidence);
            command.Parameters.AddWithValue("$id", id);
            await command.ExecuteNonQueryAsync(Token);
        }
    }
}

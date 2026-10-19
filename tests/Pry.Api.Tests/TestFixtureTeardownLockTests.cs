using System.Diagnostics;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// 工装收尾那两个方向的判据（合流树门禁 run1 那条红的账）。
/// <para>
/// <see cref="SqliteTestPools.ClearAndDeleteRoot"/> 加的是一个**有界等窗口**：池里正在归还的那句连接仍握着
/// <c>memory.db</c> 的句柄，紧随其后的删除会拿到 <see cref="IOException"/>——那条红点的不是产品行为，是"删除
/// 排在句柄归还之前"这一件时序上的事。窗口本身两头都可能做错，所以两边各立一条：
/// </para>
/// ① 锁在窗口内放开 ⇒ 目录必须删干净（不然窗口等于没加）；
/// ② 锁始终不放开 ⇒ **必须照旧抛**，而且不许把目录"删一半当没事"（不然重试就把删除失败变成了隐形）。
/// 第三条是本刀迁移面的源码门：已迁的 fixture 只准走这一个收尾口。
/// </summary>
public sealed class TestFixtureTeardownLockTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"pry-teardown-lock-{Guid.NewGuid():N}");
    private readonly string _databasePath;

    public TestFixtureTeardownLockTests() =>
        _databasePath = Path.Combine(_root, "memory.db");

    public ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllBytes(_databasePath, [1, 2, 3]);
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        // 本文件判的就是"删不掉"，收尾自己不许再把门禁弄红：这里刻意不做断言，只尽力清。
        try
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }
        catch (Exception)
        {
            File.AppendAllText(Path.Combine(Path.GetTempPath(), "pry-teardown-lock-leftover.txt"),
                $"{DateTimeOffset.UtcNow:O} {_root}{Environment.NewLine}");
        }

        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task A_handle_released_inside_the_window_still_leaves_no_temporary_directory()
    {
        using var held = File.Open(_databasePath, FileMode.Open, FileAccess.Read, FileShare.None);
        var token = TestContext.Current.CancellationToken;
        var release = Task.Run(async () =>
        {
            await Task.Delay(150, token);
            held.Dispose();
        }, token);

        var started = Stopwatch.StartNew();
        SqliteTestPools.ClearAndDeleteRoot(_databasePath, _root);
        started.Stop();
        await release;

        Assert.False(Directory.Exists(_root));
        // 非空转的另一半：这条必须真的等过，否则"窗口"可能是句柄压根没按住就撞对了。
        Assert.True(started.ElapsedMilliseconds >= 50,
            $"窗口一次都没等就删掉了，说明句柄没有真的按住（{started.ElapsedMilliseconds}ms）。");
    }

    [Fact]
    public void A_handle_that_never_releases_still_fails_the_teardown_instead_of_hiding_it()
    {
        using var held = File.Open(_databasePath, FileMode.Open, FileAccess.Read, FileShare.None);

        Exception? failure = null;
        try
        {
            SqliteTestPools.ClearAndDeleteRoot(_databasePath, _root, attempts: 2);
        }
        catch (Exception error)
        {
            failure = error;
        }

        // 重试只等句柄归还，不许把删除失败变成隐形：窗口用尽必须原样抛。
        Assert.NotNull(failure);
        Assert.True(failure is IOException or UnauthorizedAccessException,
            $"抛的不是「占用」那一族：{failure!.GetType().Name}: {failure.Message}");
        // 也不许"删一半当作删掉了"：被按住的文件必须还在原地。
        Assert.True(Directory.Exists(_root));
        Assert.True(File.Exists(_databasePath));
    }

    [Fact]
    public void The_migrated_fixture_shuts_its_store_down_through_the_single_teardown_door()
    {
        var source = Read("tests", "Pry.Api.Tests", "ExpressionDrainChannelTests.cs");
        Assert.Equal(1, Occurrences(source, "SqliteTestPools.ClearAndDeleteRoot("));
        // 绕过收尾口自己删目录＝回到没有等窗口的旧形状。
        Assert.Equal(0, Occurrences(source, "Directory.Delete("));
    }

    [Fact]
    public void The_scanner_sees_the_shapes_it_claims_to_see()
    {
        // 每个判定先喂一段含该形状的样本文本，确认计数真的认得它（同 MemoryManagementSurfaceTests 的口径）。
        Assert.Equal(1, Occurrences("if (Directory.Exists(root)) Directory.Delete(root, recursive: true);",
            "Directory.Delete("));
        Assert.Equal(2, Occurrences("SqliteTestPools.Clear(path);\nSqliteTestPools.ClearAndDeleteRoot(a, b);",
            "SqliteTestPools.Clear"));
        Assert.Equal(0, Occurrences("Directory.CreateDirectory(root);\nSqliteTestPools.Clear(path);",
            "Directory.Delete("));
        // 门的范围是"已迁的 fixture"，不是全仓：工装本体里那一句合法删除必须还在，且只有一句。
        Assert.Equal(1, Occurrences(Read("tests", "Pry.Api.Tests", "SqlitePoolCleanup.cs"), "Directory.Delete("));
    }

    private static int Occurrences(string text, string token)
    {
        var count = 0;
        for (var index = text.IndexOf(token, StringComparison.Ordinal); index >= 0;
             index = text.IndexOf(token, index + 1, StringComparison.Ordinal))
            count++;
        return count;
    }

    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine([RepositoryRoot(), .. parts]));

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null;
             directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Pry.slnx"))) return directory.FullName;
        throw new InvalidOperationException("从测试程序集出发找不到仓库根，无法核对源码约束。");
    }
}

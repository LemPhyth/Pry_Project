using Microsoft.Data.Sqlite;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// <c>QA-TIP1019-05</c> 的判据面：两份同名工装助手必须清同一组池键。
///
/// 缺陷形状＝<c>Pry.Core.Tests</c> 那份 <c>SqliteTestPools.Clear</c> 清**三**条串（宿主、只带
/// <c>Data Source=</c> 的直连、长期记忆仓储），而 <c>Pry.Api.Tests</c> 这份只清**两**条，漏掉直连那一条。
/// <see cref="SqliteConnection.ClearPool(SqliteConnection)"/> 的键就是连接串本身，所以对一条不存在的串清池
/// 是一声不响的 no-op：库文件带着池里那条 idle 句柄留在原地，紧随其后的 <c>Directory.Delete</c> 抛
/// <see cref="IOException"/>。这一格今天靠 <c>MemoryManagementEndpointTests.DemoteAsync</c> 自己写
/// <c>Pooling=false</c> 绕开——**一个用例绕开工装，就是工装缺了一格**，本刀把那条规避撤掉。
///
/// <para>
/// 与同日"等窗口"那一刀（<c>ClearAndDeleteRoot</c>）的分工写死在这里：窗口治**时机**（清池返回时句柄还在
/// 归还途中），本文件治**缺失**（那条串压根没被清，等多久都不会放开）。缺键时窗口用尽照原样抛，
/// 症状看起来一模一样而归因完全不同，所以两头必须各有抓手，不许用一条判据顶两条。
/// </para>
/// </summary>
public sealed class SqlitePoolKeySymmetryTests
{
    /// <summary>缺键时句柄永不放开；正常归还只要毫秒。2 秒是有界结论的边界，不是重试预算。</summary>
    private static readonly TimeSpan ReleaseWindow = TimeSpan.FromSeconds(2);

    [Fact]
    public void A_bare_data_source_connection_is_reclaimed_by_the_helper()
    {
        var root = Path.Combine(Path.GetTempPath(), $"pry-poolkey-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var database = Path.Combine(root, "memory.db");
        try
        {
            // 用例直连库做突变用的就是这一条串（产品里没有第二个使用者，所以它历史上一直没人清）。
            // WAL 让 -wal / -shm 两个侧文件一起出现：真实故障占用的是这三个，不是只有 memory.db。
            using (var connection = new SqliteConnection($"Data Source={database}"))
            {
                connection.Open();
                Execute(connection, "PRAGMA journal_mode=WAL");
                Execute(connection, "CREATE TABLE probe (v TEXT)");
            } // Dispose 只把连接交给池：sqlite3 句柄留在池里，而串就是那张表的键。

            SqliteTestPools.Clear(database);
            Assert.True(ReleasedWithin(database),
                $"清池之后 {ReleaseWindow.TotalSeconds:0} 秒内这条库仍被句柄占用＝有一条池键没被工装入口回收" +
                "（症状与『句柄还在归还途中』相同，区别是这里等多久都不会放开）。库：" + database);
        }
        finally
        {
            // 红例也已经红了，兜底把这一形状清掉再删目录：不把句柄留给下一场，也不在 temp 里堆删不掉的库。
            SqliteConnection.ClearPool(new SqliteConnection($"Data Source={database}"));
            ReleasedWithin(database);
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Both_test_assemblies_clear_the_same_set_of_pool_shapes()
    {
        var root = RepositoryRoot();
        var api = Path.Combine(root, "tests", "Pry.Api.Tests", "SqlitePoolCleanup.cs");
        var core = Path.Combine(root, "tests", "Pry.Core.Tests", "SqlitePoolCleanup.cs");
        Assert.True(File.Exists(api), $"工装助手不在了：{api}");
        Assert.True(File.Exists(core), $"工装助手不在了：{core}");

        var apiShapes = ShapesIn(File.ReadAllText(api));
        var coreShapes = ShapesIn(File.ReadAllText(core));

        // 逐字相等＝两份双胞胎不许各自漂移；条数齐全＝不许两边一起漏同一条（那种漂移这套集合比对看不出来）。
        Assert.Equal(string.Join(" | ", coreShapes), string.Join(" | ", apiShapes));
        Assert.Equal(3, apiShapes.Count);
    }

    [Fact]
    public void The_classifier_names_each_shape_it_claims_to_see_and_stays_silent_for_a_stranger()
    {
        // 仪器自检：三条形状各自被独立写出的样张点名（不是拿判据里的字回喂自己），陌生串必须回空集，
        // 注释里出现的同名字样不算——否则"门永远绿"和"门永远红"都无从分辨。
        Assert.Equal("宿主（MemoryDatabase）", Only(ShapesIn(
            @"SqliteConnection.ClearPool(new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = p, Mode = SqliteOpenMode.ReadWriteCreate, Cache = SqliteCacheMode.Shared }.ToString()));")));
        Assert.Equal("长期记忆仓储（SqliteMemoryRepository）", Only(ShapesIn(
            @"SqliteConnection.ClearPool(new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = p, ForeignKeys = true, DefaultTimeout = 5 }.ToString()));")));
        Assert.Equal("直连突变（只带 Data Source）", Only(ShapesIn(
            @"SqliteConnection.ClearPool(new SqliteConnection($""Data Source={p}""));")));

        // 另一条不相干的串：同样是 "Data Source="，但没有那条形状的形状记号 ⇒ 必须什么都看不见。
        Assert.Empty(ShapesIn(
            @"SqliteConnection.ClearPool(new SqliteConnection(""Data Source=other.db;Mode=ReadOnly""));"));
        // 被注释掉的一行不算数（语言噪声门）。
        Assert.Empty(ShapesIn(
            "// SqliteConnection.ClearPool(new SqliteConnection($\"Data Source={p}\"));"));
    }

    private static readonly string[] ShapeNames =
        ["宿主（MemoryDatabase）", "长期记忆仓储（SqliteMemoryRepository）", "直连突变（只带 Data Source）"];

    /// <summary>
    /// 从一份源码文本里取出"清的是哪几条池键"。分类只看串的形状记号，不看行号也不看条数，
    /// 所以同一份里调换顺序或重复清某一条都会被如实读出来。
    /// </summary>
    private static SortedSet<string> ShapesIn(string source)
    {
        var code = string.Join("\n", source.Replace('\r', '\n').Split('\n')
            .Where(line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal)));

        var shapes = new SortedSet<string>(StringComparer.Ordinal);
        var cursor = 0;
        while ((cursor = code.IndexOf("SqliteConnection.ClearPool(", cursor, StringComparison.Ordinal)) >= 0)
        {
            var tail = code.IndexOf("));", cursor, StringComparison.Ordinal);
            var block = code[cursor..(tail < 0 ? Math.Min(code.Length, cursor + 240) : tail)];
            if (block.Contains("SqliteOpenMode.ReadWriteCreate", StringComparison.Ordinal))
                shapes.Add(ShapeNames[0]);
            else if (block.Contains("ForeignKeys = true", StringComparison.Ordinal))
                shapes.Add(ShapeNames[1]);
            else if (block.Contains("Data Source={", StringComparison.Ordinal)) shapes.Add(ShapeNames[2]);

            cursor = tail < 0 ? cursor + 1 : tail + 3;
        }

        return shapes;
    }

    private static string Only(SortedSet<string> shapes)
    {
        Assert.Single(shapes);
        return shapes.First();
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteScalar();
    }

    private static bool ReleasedWithin(string database)
    {
        var deadline = DateTimeOffset.UtcNow + ReleaseWindow;
        while (true)
        {
            if (Free(database)) return true;
            if (DateTimeOffset.UtcNow >= deadline) return false;
            Thread.Sleep(25);
        }
    }

    /// <summary>独占得开＝这条库与它的两个 WAL 侧文件都不再被任何句柄持有。</summary>
    private static bool Free(string database)
    {
        foreach (var path in new[] { database, $"{database}-wal", $"{database}-shm" })
        {
            if (!File.Exists(path)) continue;
            try
            {
                using (File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                }
            }
            catch (IOException)
            {
                return false;
            }
        }

        return true;
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null;
             directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Pry.slnx"))) return directory.FullName;
        throw new InvalidOperationException("从测试程序集出发找不到仓库根，无法核对源码约束。");
    }
}

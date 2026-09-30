using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// 测试收尾只准清自己那一份 SQLite 池（DEF-P1WALK-04）。
/// 进程级清池（<c>SqliteConnection</c> 上的 <c>ClearAllPools()</c>）会把此刻别的测试类正在使用的 <c>sqlite3</c>
/// 句柄一起 dispose 掉，而 <c>Pry.Core.Tests</c> 与 <c>Pry.Api.Tests</c> 都没关 collection 并行。
/// 于是红例落在**没调用它的那些用例**上（<see cref="ObjectDisposedException"/> 或临时目录删不掉的
/// <see cref="IOException"/>），异常里没有一行指向真正的肇事者——这条缺陷的形态是"每个 fixture 都这么写"，
/// 逐个人工 review 抓不完，所以判据只能是一条源码门。
/// </summary>
public sealed class ProcessWidePoolResetBoundaryTests
{
    /// <summary>被禁的调用形状。写成拼接而不是字面量：本文件只要出现完整的字面量，
    /// 下面那条扫描就会先抓到门自己，而"把判据抄进探针"正是让扫描门变成常绿装饰的那条路。</summary>
    private const string ProcessWideResetCall = "SqliteConnection." + "ClearAllPools" + "()";

    [Fact]
    public void No_test_source_resets_the_process_wide_sqlite_pool()
    {
        var tests = Path.Combine(RepositoryRoot(), "tests");
        Assert.True(Directory.Exists(tests), $"找不到测试源码目录 {tests}，本门禁的结论无效。");

        var offenders = new List<string>();
        var scanned = 0;
        foreach (var file in Directory.EnumerateFiles(tests, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(tests, file);
            // 构建产物里的源码副本属于上一次编译，拿它判今天的事实只会产出一条谁都修不动的红。
            var parts = relative.Split(Path.DirectorySeparatorChar);
            if (parts.Contains("bin") || parts.Contains("obj")) continue;
            scanned++;
            var lines = File.ReadAllLines(file);
            for (var index = 0; index < lines.Length; index++)
                if (IsProcessWideReset(lines[index]))
                    offenders.Add($"{relative}:{index + 1} 只清自己那一份池——用 SqliteConnection.ClearPool(connection)，" +
                                  "或本程序集里的 SqliteTestPools.Clear(databasePath)。");
        }

        // 读到 0 个文件的扫描门永远绿，所以"确实扫到了这一族的量"本身也要判：登记时违规面是
        // 21 个文件／28 处，此刻 tests/ 下的测试源码共 115 份。
        Assert.True(scanned >= 100, $"只扫到 {scanned} 份测试源码，本门禁的结论无效。");
        Assert.Empty(offenders);
    }

    /// <summary>
    /// 正向探针：一条扫描门如果在绿灯里看不见它要禁的形状，它就能永远常绿。这里先把"看得见"钉住，
    /// 再确认修完后的形状与"注释里提到这个词"都不被抓——否则这条门会把所有人挡在门外，
    /// 而仓里已经有两处注释在解释为什么不用它。
    /// </summary>
    [Fact]
    public void The_scanner_sees_the_shape_it_claims_to_forbid()
    {
        // 探针把被禁形状**另拼一遍**、不复用判据常量：本轮实测过，两者同源时改坏常量会同时改绿探针，
        // 门就在绿灯里失效。拼起来写（而不是字面量）是因为本文件也在扫描范围内，门不该先抓到他自己。
        const string violation = "SqliteConnection.ClearAll" + "Pools()";
        const string qualifiedViolation = "Microsoft.Data.Sqlite." + "SqliteConnection.ClearAll" + "Pools()";
        Assert.Equal(violation, ProcessWideResetCall);
        Assert.True(IsProcessWideReset($"            {violation};"));
        Assert.True(IsProcessWideReset($"        finally {{ {qualifiedViolation}; if (Directory.Exists(d)) }}"));

        // 修完后的形状与"注释里提到裸词"都不该被抓，否则这条门会把仓库里所有人挡在门外——
        // 仓里已经有两处注释在解释为什么不用它，按子串数会比真实调用点多两处。
        Assert.False(IsProcessWideReset("            SqliteTestPools.Clear(Path.Combine(directory, \"memory.db\"));"));
        Assert.False(IsProcessWideReset("            SqliteConnection.ClearPool(connection);"));
        Assert.False(IsProcessWideReset(
            "        // 只清自己这条连接串对应的池（DEF-P1WALK-04：进程级 ClearAllPools 会拆掉并行用例的句柄）。"));
    }

    /// <summary>判据本体：只认调用形状，不认裸词——台账记过计数口径：按子串 <c>ClearAllPools</c> 数会比
    /// 真实调用点多两处，那两处是解释"为什么不用它"的注释。</summary>
    private static bool IsProcessWideReset(string line) =>
        line.Contains(ProcessWideResetCall, StringComparison.Ordinal);

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Pry.slnx"))) return directory.FullName;
        throw new InvalidOperationException("从测试程序集出发找不到仓库根，无法核对源码约束。");
    }
}

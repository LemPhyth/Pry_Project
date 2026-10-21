using System.Text.RegularExpressions;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// HDS-011c-E2｜资源限额总盘自身的守卫（本刀零产品代码）。
///
/// <c>docs/resource-limit-ledger.md</c> 是"这台机器上到底有多少把尺"的登记处。
/// 一张只靠人自觉维护的表一定会烂：新加的限额没人登记，删掉的限额还留在表上，
/// 行号指到别处去——三种烂法都不会让任何在册判据变红，于是"有总盘"变成"总盘只是装饰"。
/// 这条守卫把表钉回真相源：扫 <c>src/**</c> 里名字含 <c>Limit</c>／<c>MaxLength</c>／<c>Timeout</c>／<c>Tokens</c>
/// 的 <c>const</c>／<c>static readonly</c> 声明，每一个都必须在表里有一行；
/// 反过来每一行的 <c>文件:行号</c> 都必须还指得到那处声明。漏一格即红，行号漂了也即红。
///
/// <para>
/// 手法与 <see cref="UpstreamProvenanceAndDependencyShapeTests"/>、<see cref="ReleaseAuditabilityShapeTests"/> 同族：
/// 从仓库根往下读文件、自带"确实扫到了多少"的自检、反证走测试内嵌样张（**绝不为了过门去改文档或源码**）。
/// 判的是"登记"这一层，不判数值定得对不对（那是 Eval 标定与作者裁决的格）。
/// </para>
/// </summary>
public sealed class ResourceLimitLedgerCoverageTests
{
    private const string LedgerRelativePath = "docs/resource-limit-ledger.md";

    /// <summary>工单点名的四把关键字。撞上这些名字的常量必须登记，别名字不逼它（表里另有若干自愿登记的行）。</summary>
    private static readonly string[] LimitNameKeywords = ["Limit", "MaxLength", "Timeout", "Tokens"];

    private static readonly Regex DeclarationRegex = new(
        @"^\s*(?:[a-z]+\s+)*(?:const|static\s+readonly)\s+[\w.<>\[\]\?]+\s+(?<name>[A-Za-z_]\w*)\s*=",
        RegexOptions.Compiled);

    private static readonly Regex RowReferenceRegex = new(@"^(?<path>.+?):(?<line>\d+)$", RegexOptions.Compiled);

    private sealed record LimitConstant(string Name, string File, int Line);

    private sealed record LedgerRow(string Name, string Reference);

    [Fact]
    public void Every_limit_shaped_constant_has_a_row_in_the_ledger()
    {
        var constants = LimitConstants();
        var rows = LedgerRows();
        // 扫描面自证：读到 0 个常量的扫描门永远绿。登记当刻的真实读数是 18 个。
        Assert.True(constants.Count >= 18, $"只扫到 {constants.Count} 个限额常量，扫描面或声明形状变了，" +
                                          "本门禁的结论无效。");
        Assert.True(rows.Count >= constants.Count, $"总盘只有 {rows.Count} 行，扫到的限额常量有 {constants.Count} 个，" +
                                                  "表被删薄了，本门禁的结论无效。");

        Assert.Empty(MissingRows(constants, rows));
    }

    [Fact]
    public void Every_row_of_the_ledger_still_points_at_its_declaration()
    {
        Assert.Empty(UntrustedRows(LedgerRows()));
    }

    /// <summary>判据本体之一：常量 → 表行。名字既认裸名也认 <c>Class.Name</c>／<c>file.cs:Name</c> 这种限定写法。</summary>
    private static List<string> MissingRows(IReadOnlyList<LimitConstant> constants, IReadOnlyList<LedgerRow> rows) =>
        [.. constants
            .Where(constant => !rows.Any(row => Names(row.Name, constant.Name)))
            .Select(constant => $"{constant.File}:{constant.Line} 的 {constant.Name} 在 {LedgerRelativePath} 里没有行——" +
                                "新加的上限要先进总盘，再进代码。")];

    /// <summary>判据本体之二：表行 → 盘上的声明。文件不在、行号漂走、名字不在那一行，三种都算这行不可信。</summary>
    private static List<string> UntrustedRows(IReadOnlyList<LedgerRow> rows)
    {
        var problems = new List<string>();
        foreach (var row in rows)
        {
            var match = RowReferenceRegex.Match(row.Reference);
            if (!match.Success)
            {
                problems.Add($"{LedgerRelativePath} 的 {row.Name} 那一行的声明点读不出 文件:行号（写的是 \"{row.Reference}\"）。");
                continue;
            }

            var absolute = Path.Combine(RepositoryRoot(), match.Groups["path"].Value.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absolute))
            {
                problems.Add($"{LedgerRelativePath} 的 {row.Name} 指向 {row.Reference}，但文件不在——表上还挂着一条已经不存在的限额。");
                continue;
            }

            var lines = File.ReadAllLines(absolute);
            var lineNumber = int.Parse(match.Groups["line"].Value);
            if (lineNumber < 1 || lineNumber > lines.Length || !lines[lineNumber - 1].Contains(row.Name, StringComparison.Ordinal))
                problems.Add($"{LedgerRelativePath} 的 {row.Name} 记的声明点是 {row.Reference}，那一行读不到这个名字——" +
                             "限额被搬走或行号漂了，表要跟着改。");
        }

        return problems;
    }

    private static bool NamesALimit(string name) =>
        LimitNameKeywords.Any(keyword => name.Contains(keyword, StringComparison.Ordinal));

    private static bool Names(string rowName, string constantName) =>
        string.Equals(rowName, constantName, StringComparison.Ordinal) ||
        rowName.EndsWith($".{constantName}", StringComparison.Ordinal);

    /// <summary>
    /// 正向探针：两种掏空都必须红，且真实数据在同一对谓词下必须是绿——否则红的只是探针自己。
    /// 样张全部在测试内部拼，<b>不碰仓库里的文档与源码</b>。
    /// </summary>
    [Fact]
    public void The_guard_sees_a_row_being_dropped_and_a_row_drifting_off_its_line()
    {
        var constants = LimitConstants();
        var rows = LedgerRows();

        // 反向半条（先跑，免得探针把"什么都红"当成有效结论）：真实的两条读数都得是空的。
        Assert.Empty(MissingRows(constants, rows));
        Assert.Empty(UntrustedRows(rows));
        Assert.Contains(constants, constant => string.Equals(constant.Name, "PendingTurnLimit", StringComparison.Ordinal));

        // ① 把表里少写一行这件事喂进判据本体：随机挑一行删掉，那一行守的常量必须变成缺行。
        var dropped = rows.Single(row => string.Equals(row.Name, "ReceiptLimit", StringComparison.Ordinal));
        var withoutThatRow = rows.Where(row => row != dropped).ToList();
        var missing = MissingRows(constants, withoutThatRow);
        Assert.Contains(missing, problem => problem.Contains("ReceiptLimit", StringComparison.Ordinal));

        // ② 新加一把没登记的限额（表里当然没有它）：同样走本体，必须红。
        var invented = constants.Concat([new LimitConstant("ProbeQueueLimit", "src/Pry.Core/Probe.cs", 1)]).ToList();
        Assert.Contains(MissingRows(invented, rows), problem => problem.Contains("ProbeQueueLimit", StringComparison.Ordinal));
        // 关键字这一关本身也要判：撞不上四把关键字的名字根本不该进扫描结果
        //（否则这条门会挡住所有人，与工单定的口径不符）。
        Assert.True(NamesALimit("ProbeQueueLimit"));
        Assert.False(NamesALimit("ProbeWaterline"));
        Assert.All(constants, constant => Assert.True(NamesALimit(constant.Name),
            $"{constant.Name} 没撞上限额关键字却进了扫描结果，扫描口径漂了。"));

        // ③ 行号漂一格：声明还在，但那一行已经不是它了。
        var drifted = rows.Select(row => row == dropped
            ? row with { Reference = dropped.Reference.Replace(":51", ":50") }
            : row).ToList();
        Assert.Contains(UntrustedRows(drifted), problem => problem.Contains("ReceiptLimit", StringComparison.Ordinal));

        // ④ 表指的文件被搬走：拿一个内嵌假路径的行喂进去。
        Assert.Contains(UntrustedRows([new LedgerRow("GhostLimit", "src/Pry.Core/SweptAway.cs:1")]),
            problem => problem.Contains("SweptAway.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void The_ledger_is_a_tracked_document_with_the_six_columns()
    {
        var text = Read(LedgerRelativePath);
        Assert.Contains("资源限额总盘", text, StringComparison.Ordinal);
        Assert.Contains("DocxProcessingTimeoutMs", text, StringComparison.Ordinal);
        // 表头那六列是这张表能被读下去的前提；少一列（尤其是"有没有判据"）它就退化成数值清单。
        foreach (var column in new[] { "限额名", "声明点", "数值", "防的用户可见后果", "有没有判据", "判据" })
            Assert.Contains(column, text, StringComparison.Ordinal);
    }

    // ---- 扫描面 --------------------------------------------------------------------------

    /// <summary>全 <c>src/**</c>（不含构建产物）里名字撞上四把关键字的常量声明。</summary>
    private static List<LimitConstant> LimitConstants()
    {
        var source = Path.Combine(RepositoryRoot(), "src");
        Assert.True(Directory.Exists(source), $"找不到产品源码目录 {source}，本门禁的结论无效。");

        var found = new List<LimitConstant>();
        foreach (var file in Directory.EnumerateFiles(source, "*.cs", SearchOption.AllDirectories)
                     .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                                    !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                     .Order(StringComparer.Ordinal))
        {
            var lines = File.ReadAllLines(file);
            for (var index = 0; index < lines.Length; index++)
            {
                var match = DeclarationRegex.Match(lines[index]);
                if (!match.Success) continue;
                var name = match.Groups["name"].Value;
                if (!NamesALimit(name)) continue;
                found.Add(new LimitConstant(name, Relative(file), index + 1));
            }
        }

        return found;
    }

    /// <summary>总盘里所有 markdown 表的数据行，取前两列（限额名、声明点）。多张分表按同一表头一起收。</summary>
    private static List<LedgerRow> LedgerRows()
    {
        var rows = new List<LedgerRow>();
        var inTable = false;
        foreach (var raw in Read(LedgerRelativePath).Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (!line.TrimStart().StartsWith('|'))
            {
                inTable = false;
                continue;
            }

            var cells = line.Split('|').Skip(1).Take(line.Split('|').Length - 2).Select(cell => cell.Trim()).ToArray();
            if (!inTable)
            {
                inTable = cells.Length >= 2 && cells[0] == "限额名";
                continue;
            }

            if (cells.All(cell => cell.Length == 0 || cell.All(character => character is '-' or ':' or ' '))) continue;
            rows.Add(new LedgerRow(cells[0].Trim('`'), cells[1].Trim('`')));
        }

        Assert.True(rows.Count >= 18, $"{LedgerRelativePath} 只读到 {rows.Count} 行数据，表或扫描形状变了，" +
                                     "本门禁的结论无效。");
        return rows;
    }

    private static string Read(string relativePath)
    {
        var path = Path.Combine(RepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), $"找不到总盘 {path}，本门禁的结论无效。");
        return File.ReadAllText(path);
    }

    private static string Relative(string absolute) =>
        Path.GetRelativePath(RepositoryRoot(), absolute).Replace('\\', '/');

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null;
             directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Pry.slnx")) &&
                Directory.Exists(directory.FullName + Path.DirectorySeparatorChar + "src"))
                return directory.FullName;

        throw new InvalidOperationException("从测试程序集出发找不到仓库根，无法核对源码约束。");
    }
}

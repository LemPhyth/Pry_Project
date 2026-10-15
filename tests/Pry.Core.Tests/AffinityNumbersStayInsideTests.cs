using System.Reflection;
using System.Text;
using Pry.Contracts;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// 作者 2026-10-12 裁定的 A 方案：亲密与心情的数值刻度只许活在 `Pry.Core.Intelligence` 内部与
/// `character_state` 表里，永远不许出门——不进提示词、不进界面、不进角色卡文件、不进对外返回体、不进日志。
/// 提示词那一面已由 `CharacterIntelligenceTests` 的三条判据守住；这里补的是当时**没有任何判据**的另两面：
/// 对外契约类型（返回体）与日志出口。"今天干净"必须变成"过不去"。
/// </summary>
public sealed class AffinityNumbersStayInsideTests
{
    /// <summary>
    /// 内部信号的名字。刻意不收 `Interest`／`Fatigue`／`Bond`／`Mood`——它们作为英文常用词会出现在
    /// 注释与文案里，把门禁挂在噪声上就等于没有门禁；`Mood` 本身还是 A 方案允许的**文字**心情。
    /// 心情那五个量由第三条判据（拥有者不得持有日志器）整体兜住，不靠词表穷举。
    /// </summary>
    private static readonly string[] Signals =
    [
        "Familiarity", "Trust", "Closeness", "SharedContextStrength", "Valence", "Arousal",
        "Irritation", "Affinity", "Intimacy", "Favorability"
    ];

    /// <summary>正向探针：门禁必须先证明扫描器看得见它声称拦住的东西，否则它可以在全绿里空转。</summary>
    [Fact]
    public void The_scanner_sees_the_leaks_it_claims_to_see()
    {
        Assert.True(ScanFinds("public double Familiarity { get; init; }"), "扫描器看不见契约字段，本门禁的结论无效。");
        Assert.True(ScanFinds("_logger.LogWarning($\"trust={state.Trust}\");"), "扫描器看不见日志参数里的信号，也漏了大小写。");
        Assert.True(ScanFinds("sharedContextStrength = row[\"SharedContextStrength\"];"), "扫描器看不见带引号的信号名。");
        Assert.True(ScanFinds("    Closeness = stored.Closeness,"), "扫描器看不见缩进后的赋值。");
        Assert.True(ScanFinds("public int FamiliarityScore { get; init; }"), "加个后缀就绕过去，等于没有门禁。");
        Assert.True(ScanFinds("var trustPercent = Read();"), "小写开头的局部变量也得看见。");

        Assert.False(ScanFinds("public double ReplyFrequency { get; init; }"), "触发层的频率不是亲密刻度，不得误报。");
        Assert.False(ScanFinds("public double IdleForMs { get; init; }"), "空闲毫秒不是亲密刻度，不得误报。");
        Assert.False(ScanFinds("public string Mood { get; init; } = \"平静\";"), "文字心情是 A 方案明确允许的，不得误报。");
        Assert.False(ScanFinds("/// Familiarity 与 Trust 只活在内部\nConsole.WriteLine();"),
            "注释里解释这条规则不得被判成泄漏——否则下一个被改掉的就是门禁本身。");
    }

    /// <summary>第一面：对外契约类型上不得有以内部信号命名的数值成员。字符串（心情文字）不在禁令内。</summary>
    [Fact]
    public void No_wire_contract_carries_a_numeric_affinity_field()
    {
        var offenders = new List<string>();
        foreach (var type in typeof(RuntimeState).Assembly.GetExportedTypes())
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                var underlying = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                if (!IsNumeric(underlying)) continue;
                if (!Signals.Any(signal => property.Name.Contains(signal, StringComparison.OrdinalIgnoreCase))) continue;
                offenders.Add($"{type.Name}.{property.Name}:{underlying.Name}");
            }
        }

        Assert.Empty(offenders);
    }

    /// <summary>
    /// 第二面：传输层、界面层与服务端接口层不得出现内部信号的名字。这条同时兜住日志出口——
    /// 一层不能命名一个信号，就不能把它塞进日志模板或绑定到视图。拥有信号的 `Pry.Core/Intelligence`
    /// 与 `Pry.Api/Runtime`（接缝在其内消费状态）刻意不在扫描面内，它们是本该认识这些数的地方。
    /// </summary>
    [Fact]
    public void No_outward_layer_names_an_internal_affinity_signal()
    {
        var source = Path.Combine(RepositoryRoot(), "src");
        Assert.True(Directory.Exists(source), $"找不到产品源码目录 {source}，本门禁的结论无效。");

        var offenders = new List<string>();
        foreach (var layer in new[] { "Pry.Api", "Pry.Client", "Pry.App" })
        {
            var root = Path.Combine(source, layer);
            Assert.True(Directory.Exists(root), $"{layer} 目录不存在，扫描面收缩了，本门禁的结论无效。");
            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                         .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                                     !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")))
            {
                if (ScanFinds(File.ReadAllText(file))) offenders.Add(Path.GetRelativePath(source, file).Replace('\\', '/'));
            }
        }

        Assert.Empty(offenders);
    }

    /// <summary>
    /// 第三面：拥有这些信号的那三层不得持有日志器。数不出门最省事的保证不是"记得别打印"，
    /// 而是打印它的那一层根本没有可打印的通道；这条一旦有人为了调试加个 ILogger 就会红，逼他改用不露数值的表述。
    /// </summary>
    [Fact]
    public void The_layers_that_own_the_signals_have_no_way_to_log_them()
    {
        var source = Path.Combine(RepositoryRoot(), "src", "Pry.Core");
        Assert.True(Directory.Exists(source), $"找不到 {source}，本门禁的结论无效。");

        var offenders = new List<string>();
        foreach (var layer in new[] { "Intelligence", "Expression", "Runtime" })
        {
            var root = Path.Combine(source, layer);
            Assert.True(Directory.Exists(root), $"{layer} 目录不存在，扫描面收缩了，本门禁的结论无效。");
            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                var code = StripComments(File.ReadAllText(file));
                if (!code.Contains("ILogger", StringComparison.Ordinal) &&
                    !code.Contains("Microsoft.Extensions.Logging", StringComparison.Ordinal)) continue;
                offenders.Add(Path.GetRelativePath(RepositoryRoot(), file).Replace('\\', '/'));
            }
        }

        Assert.Empty(offenders);
    }

    /// <summary>
    /// 去掉整行注释后再找信号名。子串命中即算（`FamiliarityScore` 也是把刻度带出去），不做词边界豁免：
    /// 外层连"认识这个信号"都不允许，而 `trustInfo` 那种 XML 清单节点不在 `.cs` 扫描面内。
    /// </summary>
    private static bool ScanFinds(string text) => NamesASignal(StripComments(text));

    private static string StripComments(string text)
    {
        var code = new StringBuilder();
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("//", StringComparison.Ordinal)) continue;
            code.Append(line).Append('\n');
        }

        return code.ToString();
    }

    private static bool NamesASignal(string text) =>
        Signals.Any(signal => text.Contains(signal, StringComparison.OrdinalIgnoreCase));

    private static bool IsNumeric(Type type) => type == typeof(int) || type == typeof(long) || type == typeof(short) ||
        type == typeof(byte) || type == typeof(double) || type == typeof(float) || type == typeof(decimal);

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Pry.slnx")) && Directory.Exists(directory.FullName + Path.DirectorySeparatorChar + "src"))
                return directory.FullName;
        throw new InvalidOperationException("从测试程序集出发找不到仓库根，无法核对源码约束。");
    }
}

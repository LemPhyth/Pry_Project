using System.Text.RegularExpressions;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// HDS-011c-E1｜依赖与许可的**机械面**（本刀零产品代码）。
///
/// 今天的事实是：仓库里已经发生了四处跨语言照搬（<c>src/Pry.Core/Runtime/</c> 那四个 <c>Ported from</c> 头），
/// 留痕靠三份文本互相指认——文件头、<c>licenses/upstream/&lt;repo&gt;_&lt;sha&gt;/LICENSE</c>、
/// <c>licenses/THIRD-PARTY-NOTICES.md</c> 的照搬来源表；依赖侧则是每个 <c>PackageReference</c> 各自写死版本，
/// 没有任何机器看过一眼。这三处全是**人可以单方面改掉的一句话**，而改掉的后果是法律层面的：
/// 凭据目录被删＝上游 GPL 文本随包失踪；来源头被删＝表上写着"ported"而代码里已经不认账；
/// 版本浮动＝发行包还原出的依赖与 <c>THIRD-PARTY-NOTICES.md</c> 记的那份不是同一份。
///
/// <para>
/// 手法与 <see cref="ReleaseAuditabilityShapeTests"/>、<see cref="PlaceholderDefinitionStaysInCoreTests"/> 同族：
/// 从仓库根往下读真相源、每个扫描器自带"确实扫到了多少"的自检、探针用测试内嵌样张，
/// **绝不为了过门去改被扫的真相源**（<c>LICENSE</c>／<c>licenses/**</c>／<c>docs/upstream-license-compatibility.md</c>
/// 在本刀里只读不写）。名单从两份权威文本当场解析（§4.4 与其在 notices 里的镜像），
/// 两边不一致就红——削薄任一张表都别想安静过去。
/// </para>
/// </summary>
public sealed class UpstreamProvenanceAndDependencyShapeTests
{
    private const string NoticesRelativePath = "licenses/THIRD-PARTY-NOTICES.md";
    private const string CompatibilityDocRelativePath = "docs/upstream-license-compatibility.md";
    private const string UpstreamCredentialsRelativePath = "licenses/upstream";

    /// <summary>§4.4 与 AGENTS.md 第 8 条点名的"仅机制与市场调研"四家。
    /// 这四个名字只许出现在禁抄说明里：<c>src/**</c> 出现任意一处（哪怕不是来源头）即红。今天实测零命中。</summary>
    private static readonly string[] MechanismOnlyUpstreamNames =
        ["AstrBot", "Nekro-Agent", "Nekro", "Private Companion", "astrbot_plugin_private_companion", "qq-bridge"];

    /// <summary>照搬头的写法词表（§4.1 的 <c>verbatim</c>／<c>ported</c> 两档在文件头的那一句）。
    /// 只认行首注释里的这一族写法；今天仓库里用的就是 <c>Ported from</c>。</summary>
    private static readonly string[] ProvenanceHeaderVerbs = ["Ported", "Adapted", "Copied", "Derived"];

    private static readonly Regex HeaderRegex = new(
        @"^\s*//\s*(?<verb>Ported|Adapted|Copied|Derived)\s+from\s+(?<repo>[A-Za-z0-9._-]+/[A-Za-z0-9._-]+)@(?<sha>[0-9a-fA-F]{40})",
        RegexOptions.Compiled);

    private static readonly Regex RepoAtCommitRegex = new(@"[A-Za-z0-9._-]+/[A-Za-z0-9._-]+@[0-9a-fA-F]{40}",
        RegexOptions.Compiled);

    /// <summary>notices 的白名单镜像表只写仓库名不写提交，所以那里用这条单独的取法：反引号里那一段 <c>owner/repo</c>。</summary>
    private static readonly Regex QuotedRepoRegex = new(@"`(?<repo>[A-Za-z0-9._-]+/[A-Za-z0-9._-]+)`",
        RegexOptions.Compiled);

    private static readonly Regex PackageReferenceRegex = new(@"<PackageReference\b(?<attrs>[^>]*?)/?>",
        RegexOptions.Compiled);

    private static readonly Regex AttributeRegex = new(@"(?<name>Include|Update|Version)\s*=\s*""(?<value>[^""]*)""",
        RegexOptions.Compiled);

    private static readonly string[] ProductLayers = ["Pry.Core", "Pry.Api", "Pry.Client", "Pry.App", "Pry.Contracts"];

    /// <summary>一处照搬头：文件（仓库相对路径，正斜杠）、行号、上游仓库、锁定的提交。</summary>
    private sealed record ProvenanceHeader(string File, int Line, string Repo, string Commit);

    /// <summary>notices 照搬来源表的一行：Pry 文件、上游仓库、上游提交、类型（ported／mechanism）。</summary>
    private sealed record ReuseRow(string PryFile, string Repo, string Commit, string Type);

    /// <summary>一条 PackageReference：所在 csproj、行号、包名、写下的版本（没有写就是 null）。</summary>
    private sealed record PackageReference(string Project, int Line, string Id, string? Version);

    // ---- 判据 1：三处留痕齐不齐（代码头 → 凭据目录 → notices 表行）----------------------------

    [Fact]
    public void Every_provenance_header_has_its_license_copy_and_a_row_in_the_reuse_table()
    {
        var headers = ProvenanceHeaders();
        var rows = ReuseRows();
        AssertScannedSurface(headers.Count);

        var missing = new List<string>();
        foreach (var header in headers)
        {
            if (!HasCredentials(header))
                missing.Add($"{header.File}:{header.Line} 声明照搬 {header.Repo}@{Short(header.Commit)}，" +
                            $"但 {Relative(CredentialsPath(header))} 不在——§4.2 三处留痕缺了第二处（上游许可文本没随仓走）。");

            if (!TableHasRow(rows, header))
                missing.Add($"{header.File}:{header.Line} 带着 {header.Repo}@{Short(header.Commit)} 的来源头，" +
                            $"但 {NoticesRelativePath} 的照搬来源表里没有这一行——第三处留痕缺失，读表的人不知道这文件是借来的。");
        }

        Assert.Empty(missing);
    }

    /// <summary>第二处留痕：<c>licenses/upstream/&lt;repo 尾段&gt;_&lt;commit&gt;/LICENSE</c>（§4.2 第 2 条）。</summary>
    private static bool HasCredentials(ProvenanceHeader header) => File.Exists(CredentialsPath(header));

    /// <summary>第三处留痕：照搬来源表里有指向这个文件的一行。</summary>
    private static bool TableHasRow(IEnumerable<ReuseRow> rows, ProvenanceHeader header) =>
        rows.Any(row => string.Equals(row.PryFile, header.File, StringComparison.Ordinal));

    // ---- 判据 1 的反向：表上写了、代码里的痕被删了 -----------------------------------------

    [Fact]
    public void Every_row_of_the_reuse_table_still_leaves_a_trace_in_the_source_file()
    {
        var rows = ReuseRows();
        Assert.True(rows.Count >= 5, $"只从 {NoticesRelativePath} 的照搬来源表读到 {rows.Count} 行，" +
                                     "表或扫描形状变了，本门禁的结论无效。");

        var headers = ProvenanceHeaders();
        // 文件不在盘上的那一格由同一个谓词记账（探针也走它），下面只判还在盘上的那些。
        var broken = new List<string>(ReuseRowsAbsentFromDisk(rows));
        foreach (var row in rows)
        {
            var absolute = Path.Combine(RepositoryRoot(), row.PryFile.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absolute)) continue;
            broken.AddRange(TraceInSourceProblems(row, File.ReadAllText(absolute), headers));
        }

        Assert.Empty(broken);
    }

    /// <summary>
    /// 一行的"代码里还认不认账"：<c>ported</c> 档必须仍带指向同一 <c>repo@commit</c> 的来源头（§4.1 的硬要求）；
    /// <c>mechanism</c> 档按 §4.1 只要求在契约或提交说明记来源，所以不逼它写头，
    /// 但代码里连上游名字都不提＝痕被删干净了，同样红。判据本体，探针也走它。
    /// </summary>
    private static IEnumerable<string> TraceInSourceProblems(ReuseRow row, string sourceText,
        IEnumerable<ProvenanceHeader> headers)
    {
        if (string.Equals(row.Type, "ported", StringComparison.Ordinal))
        {
            var hasHeader = headers.Any(h => string.Equals(h.File, row.PryFile, StringComparison.Ordinal) &&
                                             string.Equals(h.Repo, row.Repo, StringComparison.Ordinal) &&
                                             string.Equals(h.Commit, row.Commit, StringComparison.OrdinalIgnoreCase));
            return hasHeader ? []
                : [$"{row.PryFile} 在表里登记为 ported，却没有 {row.Repo}@{Short(row.Commit)} 那一行来源头" +
                  $"（§4.1：跨语言移植必须在文件头注明 repo@commit:path）"];
        }

        var names = CitationNames(row.Repo);
        return names.Any(name => sourceText.Contains(name, StringComparison.Ordinal)) ? []
            : [$"{row.PryFile} 登记为 {row.Type}，代码里既没有来源头也不提上游任何一个名字" +
               $"（期望出现：{string.Join(" / ", names)}）"];
    }

    // ---- 判据 2：白名单外即红 --------------------------------------------------------------

    [Fact]
    public void Only_the_authorised_upstreams_are_cited_and_the_two_lists_agree()
    {
        var authorised = AuthorisedRepos();
        var forbidden = ForbiddenRepos();
        // 名单本身也要判：§4.4 现在是三家放行＋四家禁抄，形状变了说明权威表被重排，
        // 那时"白名单外即红"会因为解析出空集合而常绿——那是装饰，不是门。
        Assert.Equal(3, authorised.Count);
        Assert.Equal(4, forbidden.Count);
        Assert.True(authorised.Contains("Mai-with-u/MaiBot") && authorised.Contains("satorijs/satori") &&
                    authorised.Contains("ISEKAI-MomoiCore/HDS-Interlude"),
            $"§4.4 解析出的放行名单是 {string.Join(", ", authorised)}，与作者 2026-09-21／2026-10-20 点名的三家不符。");

        // 同一个白名单在两处各写了一份（compatibility §4.4 与 notices 的复用表）；两份必须说同一句话。
        var mirrored = NoticesAuthorisedRepos();
        Assert.Equal(new HashSet<string>(authorised, StringComparer.Ordinal),
            new HashSet<string>(mirrored, StringComparer.Ordinal));

        var reds = new List<string>();
        foreach (var header in ProvenanceHeaders())
            if (!authorised.Contains(header.Repo))
                reds.Add($"{header.File}:{header.Line} 的来源头指向 {header.Repo}——不在 §4.4 放行名单里。" +
                         $"白名单按项目点名，不按许可自动放行。");

        // 四家禁抄的名字在 src/** 一个字都不许出现（今天零命中）。它们出现在说明文字里是合法的，
        // 但那两处都在 docs/ 与 licenses/，不在产品源码里。
        foreach (var file in SourceFiles())
        {
            var text = File.ReadAllText(file);
            foreach (var name in MechanismOnlyUpstreamNames)
                if (text.Contains(name, StringComparison.OrdinalIgnoreCase))
                    reds.Add($"{Relative(file)} 出现 {name}——§4.4 明示它只有 mechanism 一档，" +
                             $"源码文本与措辞都不许进仓库。");
        }

        Assert.Empty(reds);
    }

    // ---- 判据 3：依赖版本不许浮动 ----------------------------------------------------------

    [Fact]
    public void No_package_reference_floats()
    {
        var references = PackageReferences();
        Assert.True(references.Count >= 20, $"只扫到 {references.Count} 条 PackageReference，" +
                                           "csproj 形状或扫描面变了，本门禁的结论无效。");

        var floats = references
            .Where(reference => IsFloatingVersion(reference.Version))
            .Select(reference => $"{reference.Project}:{reference.Line} 的 {reference.Id} " +
                                 $"版本写成 \"{reference.Version}\"——还原出的依赖不可复现，" +
                                 $"{NoticesRelativePath} 记的那份版本也就对不上。")
            .ToList();

        Assert.Empty(floats);
    }

    /// <summary>判据本体：没写版本、空串、通配、区间、属性间接都算浮动。</summary>
    private static bool IsFloatingVersion(string? version) =>
        string.IsNullOrWhiteSpace(version) ||
        version.Contains('*') || version.Contains('[') || version.Contains(']') ||
        version.Contains('(') || version.Contains(')') || version.Contains(',') ||
        version.Contains("$(", StringComparison.Ordinal);

    // ---- 判据 4：第三方表覆盖还原面（只判包名，不判许可兼容性）------------------------------

    [Fact]
    public void The_third_party_table_names_every_restored_package()
    {
        var components = NoticeComponents();
        Assert.True(components.Count >= 10, $"{NoticesRelativePath} 的直接依赖表只读到 {components.Count} 行，" +
                                           "扫描面收缩了，本门禁的结论无效。");

        var packageIds = RestoredPackageIds();
        Assert.True(packageIds.Length >= 14, $"全部 csproj 里只读到 {packageIds.Length} 个不同包名，" +
                                            "扫描面收缩了，本门禁的结论无效。");

        Assert.Empty(UncoveredPackages(packageIds, components));
    }

    /// <summary>
    /// 覆盖判定的本体：包 id 要么与表里某一行的组件名逐字相等，要么经 <see cref="PackageFamilyAliases"/>
    /// 显式登记过、且指向的那一行确实还在表里。<b>不做前缀匹配、不做大小写折叠、不做模糊匹配</b>——
    /// 别名是"一张 NuGet 包属于哪个上游组件"这一条人工结论的登记处，不是字符串游戏。
    /// 新包既没进表也没登记别名 ⇒ 红；表里删掉一行 ⇒ 那一族的每个包一起红。
    /// </summary>
    private static List<string> UncoveredPackages(string[] packageIds, HashSet<string> components)
    {
        var uncovered = new List<string>();
        foreach (var id in packageIds)
        {
            if (components.Contains(id)) continue;
            if (PackageFamilyAliases.TryGetValue(id, out var component) && components.Contains(component)) continue;
            uncovered.Add($"{NoticesRelativePath} 的直接依赖表覆盖不到 {id}" +
                          $"（表里逐字没有这个名字，也没登记它属于哪一行组件）——还原面与告知名单脱钩。");
        }

        return uncovered;
    }

    /// <summary>
    /// 一张 NuGet 包 id → 它在直接依赖表里那一行的组件名。今天表按"组件"列名（一个组件往往发好几个包），
    /// 而本单不许改 <c>licenses/**</c>，所以覆盖判定只能显式登记这层归属，而不是假装表是逐包写的。
    /// 新增包必须在这里补一行（或在表里补一行），否则判据 4 直接红。
    /// </summary>
    private static readonly Dictionary<string, string> PackageFamilyAliases = new(StringComparer.Ordinal)
    {
        ["Avalonia.Desktop"] = "Avalonia",
        ["Avalonia.Themes.Fluent"] = "Avalonia",
        ["Avalonia.Fonts.Inter"] = "Avalonia",
        ["org.k2fsa.sherpa.onnx"] = "sherpa-onnx",
        ["xunit.v3"] = "xUnit.net",
        ["xunit.runner.visualstudio"] = "xUnit.net",
        ["Microsoft.NET.Test.Sdk"] = "Microsoft.TestPlatform",
    };

    // ---- 正向探针：扫描器必须看得见它声称禁掉的形状 ---------------------------------------

    [Fact]
    public void The_scanners_see_the_shapes_they_claim_to_ban()
    {
        var headers = ProvenanceHeaders();
        var rows = ReuseRows();
        var authorised = AuthorisedRepos();
        var components = NoticeComponents();
        var realIds = RestoredPackageIds();

        // ① 三处留痕：凭据目录不见、表行不见，两种掏空都必须被同一对谓词读到；
        //    真实数据那半边必须读成绿，否则 ① 证明的只是"什么都红"。
        Assert.True(HasCredentials(headers[0]));
        Assert.True(TableHasRow(rows, headers[0]));
        Assert.False(HasCredentials(headers[0] with { Commit = new string('a', 40) }));
        Assert.False(TableHasRow(rows, headers[0] with { File = "src/Pry.Core/Runtime/NotInTheTable.cs" }));

        // ② 表上写着 ported、代码里的痕被删了：把真实的 mechanism 行（TurnScheduler.cs）改成 ported 必须红。
        var mechanism = rows.Single(row => string.Equals(row.Type, "mechanism", StringComparison.Ordinal));
        var mechanismText = File.ReadAllText(
            Path.Combine(RepositoryRoot(), mechanism.PryFile.Replace('/', Path.DirectorySeparatorChar)));
        Assert.NotEmpty(TraceInSourceProblems(mechanism with { Type = "ported" }, mechanismText, headers)
            .ToList());
        // 反向半条：同一份文本按它登记的 mechanism 档判必须是绿的。
        Assert.Empty(TraceInSourceProblems(mechanism, mechanismText, headers).ToList());
        // 表点名的文件不存在（改名、删除）同样要红——走判据本体那条分支。
        Assert.Contains(ReuseRowsAbsentFromDisk([mechanism with { PryFile = "src/Pry.Core/Runtime/SweptAway.cs" }])
            .ToList(), problem => problem.Contains("SweptAway.cs", StringComparison.Ordinal));
        // mechanism 档把上游名字也抹掉：内嵌样本文本必须判红。
        Assert.NotEmpty(TraceInSourceProblems(mechanism, "// 这一轮的门控顺序是我自己想的，不提任何出处。", headers)
            .ToList());

        // ③ 白名单外：把真实头的仓库换成 §4.4 明写 ❌ 的那家，必须落在放行名单之外。
        Assert.Contains(headers[0].Repo, authorised);
        Assert.DoesNotContain("AstrBotDevs/AstrBot", authorised);
        Assert.Contains("AstrBotDevs/AstrBot", ForbiddenRepos());
        // 名单被削薄时解析要立刻响，而不是悄悄变成空集合。
        Assert.NotEmpty(authorised.ToList());
        Assert.NotEmpty(components.ToList());

        // ④ 浮动版本：六种写法的样张全判红，两个真实版本全判绿。
        Assert.True(IsFloatingVersion(null));
        Assert.True(IsFloatingVersion(""));
        Assert.True(IsFloatingVersion("*"));
        Assert.True(IsFloatingVersion("[10.0.0, 11.0.0)"));
        Assert.True(IsFloatingVersion("10.0.*"));
        Assert.True(IsFloatingVersion("$(MicrosoftDataSqliteVersion)"));
        Assert.False(IsFloatingVersion("10.0.0"));
        Assert.False(IsFloatingVersion("2.1.13"));
        // 且这个谓词确实作用在真扫到的数据上：真实仓库一条都不浮，而扫到的条数不是零。
        var realReferences = PackageReferences();
        Assert.True(realReferences.Count >= 20, $"只扫到 {realReferences.Count} 条 PackageReference，探针证明不了任何事。");
        Assert.Empty(realReferences.Where(reference => IsFloatingVersion(reference.Version)).ToList());

        // ⑤ 覆盖表：新包名与"表里那一行被删掉"两种形状都要红。
        Assert.NotEmpty(UncoveredPackages([.. realIds, "Brand.New.Package"], components));
        var thinned = new HashSet<string>(components, StringComparer.Ordinal);
        Assert.True(thinned.Remove("Avalonia"));
        Assert.Contains(UncoveredPackages(realIds, thinned),
            problem => problem.Contains("Avalonia.Desktop", StringComparison.Ordinal));
        // 别名指向一个表里根本没有的组件（拼错的行名）同样不算覆盖。
        Assert.NotEmpty(UncoveredPackages(["org.k2fsa.sherpa.onnx"],
            new HashSet<string>(StringComparer.Ordinal) { "sherpa-onx" }));
    }

    /// <summary>照搬来源表点名的文件不在盘上（判据本体的另一条分支，单独成函数供探针复用）。</summary>
    private static IEnumerable<string> ReuseRowsAbsentFromDisk(IEnumerable<ReuseRow> rows) =>
        rows.Where(row => !File.Exists(Path.Combine(RepositoryRoot(),
                row.PryFile.Replace('/', Path.DirectorySeparatorChar))))
            .Select(row => $"{NoticesRelativePath} 点名 {row.PryFile}，但文件不在——表上还挂着一条已经不存在的照搬。");

    // ---- 扫描面与真相源读取 ---------------------------------------------------------------

    private static string CredentialsPath(ProvenanceHeader header) =>
        Path.Combine(RepositoryRoot(), UpstreamCredentialsRelativePath, $"{RepoTail(header.Repo)}_{header.Commit}",
            "LICENSE");

    private static void AssertScannedSurface(int headersFound)
    {
        var files = SourceFiles();
        Assert.True(files.Count >= 100, $"只扫到 {files.Count} 份产品源码，扫描面收缩了，本门禁的结论无效。");
        Assert.True(headersFound > 0, "一份照搬头都没扫到：头写法或扫描面变了，" +
                                     "此时上面的比对是常绿的，本门禁的结论无效。");
    }

    /// <summary>全 <c>src/**</c>（不缩到 Runtime 一个目录）里带照搬写法词表的来源头。</summary>
    private static List<ProvenanceHeader> ProvenanceHeaders()
    {
        var found = new List<ProvenanceHeader>();
        foreach (var file in SourceFiles())
        {
            var lines = File.ReadAllLines(file);
            for (var index = 0; index < lines.Length; index++)
            {
                var header = ParseHeaderLine(Relative(file), index + 1, lines[index]);
                if (header is not null) found.Add(header);
            }
        }

        return found;
    }

    private static ProvenanceHeader? ParseHeaderLine(string relativeFile, int lineNumber, string line)
    {
        var match = HeaderRegex.Match(line);
        if (!match.Success) return null;
        if (!ProvenanceHeaderVerbs.Contains(match.Groups["verb"].Value, StringComparer.Ordinal)) return null;
        return new ProvenanceHeader(relativeFile, lineNumber, match.Groups["repo"].Value, match.Groups["sha"].Value);
    }

    /// <summary>
    /// §4.4 与 notices 的复用表把同一个上游写了两种名字（<c>Mai-with-u/MaiBot</c> 与 <c>(MaiCore)</c>）。
    /// mechanism 档只要求代码里认账，所以认账名＝仓库尾段＋表上括号里登记的别名。
    /// </summary>
    private static string[] CitationNames(string repo)
    {
        var names = new List<string> { RepoTail(repo) };
        foreach (var cell in NoticesUpstreamCells())
        {
            var match = RepoAtCommitRegex.Match(cell);
            var head = match.Success ? cell[..match.Index] : cell;
            if (!head.Contains(RepoTail(repo), StringComparison.Ordinal)) continue;
            foreach (Match alias in Regex.Matches(cell, @"\((?<alias>[^()]{2,40})\)"))
                names.Add(alias.Groups["alias"].Value.Trim('`', ' ', '*'));
        }

        return names.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static string RepoTail(string repo) => repo.Split('/')[^1];

    private static string Short(string commit) => commit[..8];

    /// <summary>照搬来源表（| Pry file | Upstream source ... | Type | Verified |）。</summary>
    private static List<ReuseRow> ReuseRows()
    {
        var rows = new List<ReuseRow>();
        foreach (var cells in MarkdownRows(Read(NoticesRelativePath), "Pry file", "Upstream source"))
        {
            var source = RepoAtCommitRegex.Match(cells[1]);
            Assert.True(source.Success, $"{NoticesRelativePath} 照搬来源表某一行的上游列读不出 repo@commit：" +
                                        $"{cells[1]}");
            rows.Add(new ReuseRow(cells[0].Trim('`'), source.Value.Split('@')[0],
                source.Value.Split('@')[1], cells[2]));
        }

        return rows;
    }

    /// <summary>白名单镜像表（| Upstream | License | Code reuse |）的上游列，供别名解析与放行名单核对。</summary>
    private static List<string> NoticesUpstreamCells() =>
        [.. MarkdownRows(Read(NoticesRelativePath), "Upstream", "License").Select(cells => cells[0])];

    private static HashSet<string> NoticesAuthorisedRepos()
    {
        var repos = new HashSet<string>(StringComparer.Ordinal);
        foreach (var cells in MarkdownRows(Read(NoticesRelativePath), "Upstream", "License"))
        {
            if (!cells.Any(cell => cell.Contains('✅'))) continue;
            var match = QuotedRepoRegex.Match(cells[0]);
            Assert.True(match.Success, $"{NoticesRelativePath} 的白名单镜像表第一列读不出 owner/repo：{cells[0]}");
            repos.Add(match.Groups["repo"].Value);
        }

        return repos;
    }

    /// <summary>§1 实测表（5 列，第 4 列是"逐行照抄进 Pry"的 ✅／❌）里的放行名单。</summary>
    private static List<string> AuthorisedRepos() => MeasuredRepos(decision => decision.Contains('✅'));

    private static List<string> ForbiddenRepos() => MeasuredRepos(decision => decision.Contains('❌'));

    private static List<string> MeasuredRepos(Func<string, bool> decision)
    {
        var doc = Read(CompatibilityDocRelativePath);
        var repos = new List<string>();
        foreach (var cells in MarkdownRows(doc, "项目", "仓库"))
        {
            Assert.Equal(5, cells.Length);
            var match = RepoAtCommitRegex.Match(cells[1]);
            Assert.True(match.Success, $"{CompatibilityDocRelativePath} §1 的实测表某一行的仓库列读不出 repo@commit：" +
                                       $"{cells[1]}");
            if (decision(cells[3])) repos.Add(match.Value.Split('@')[0]);
        }

        return repos;
    }

    /// <summary>直接依赖表（| Component | Version | License | Project |）的组件列。</summary>
    private static HashSet<string> NoticeComponents() =>
        [.. MarkdownRows(Read(NoticesRelativePath), "Component", "Version")
            .Select(cells => cells[0].Trim('`')).Where(name => name.Length > 0)];

    private static string[] RestoredPackageIds() =>
        [.. PackageReferences().Select(reference => reference.Id).Distinct(StringComparer.Ordinal)];

    private static List<PackageReference> PackageReferences()
    {
        var found = new List<PackageReference>();
        foreach (var project in ProjectFiles())
        {
            var lines = File.ReadAllLines(project);
            for (var index = 0; index < lines.Length; index++)
            {
                foreach (Match match in PackageReferenceRegex.Matches(lines[index]))
                {
                    string? id = null;
                    string? version = null;
                    foreach (Match attribute in AttributeRegex.Matches(match.Groups["attrs"].Value))
                        switch (attribute.Groups["name"].Value)
                        {
                            case "Include" or "Update":
                                id = attribute.Groups["value"].Value;
                                break;
                            case "Version":
                                version = attribute.Groups["value"].Value;
                                break;
                        }

                    Assert.True(id is not null, $"{Relative(project)}:{index + 1} 的 PackageReference 读不出包名，" +
                                                "扫描形状要重读。");
                    found.Add(new PackageReference(Relative(project), index + 1, id!, version));
                }
            }
        }

        return found;
    }

    /// <summary>仓库里全部 csproj（含测试工程——还原面就是它们合起来的样子），返回绝对路径。</summary>
    private static List<string> ProjectFiles() => Directory
        .EnumerateFiles(RepositoryRoot(), "*.csproj", SearchOption.AllDirectories)
        .Where(file => !IsBuildArtifact(file))
        .Order(StringComparer.Ordinal)
        .ToList();

    /// <summary>
    /// 按表头前两列取某张 markdown 表的数据行（返回每行的单元格数组，已去掉首尾竖线留下的空段）。
    /// notices 里"Component"开头的表有两张（直接依赖／本地运行库与模型），所以必须两列一起认。
    /// 读不到表＝红，不做"文件不存在就跳过"那种常绿写法。
    /// </summary>
    private static List<string[]> MarkdownRows(string markdown, string firstHeaderCell,
        string secondHeaderCellPrefix)
    {
        var rows = new List<string[]>();
        var inTable = false;
        foreach (var raw in markdown.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (!line.TrimStart().StartsWith('|'))
            {
                inTable = false;
                continue;
            }

            var cells = line.Split('|').Skip(1).Take(line.Split('|').Length - 2)
                .Select(cell => cell.Trim()).ToArray();
            if (!inTable)
            {
                inTable = cells.Length > 1 && string.Equals(cells[0], firstHeaderCell, StringComparison.Ordinal) &&
                          cells[1].StartsWith(secondHeaderCellPrefix, StringComparison.Ordinal);
                continue;
            }

            if (cells.All(cell => cell.Length == 0 || cell.All(character => character is '-' or ':' or ' '))) continue;
            rows.Add(cells);
        }

        Assert.True(rows.Count > 0, $"从 markdown 里读不到以 |{firstHeaderCell}|…|{secondHeaderCellPrefix}… 开头的表" +
                                    "——真相源被重排了，本门禁的结论无效。");
        return rows;
    }

    private static List<string> SourceFiles()
    {
        var root = Path.Combine(RepositoryRoot(), "src");
        Assert.True(Directory.Exists(root), $"找不到产品源码目录 {root}，本门禁的结论无效。");

        var files = new List<string>();
        foreach (var layer in ProductLayers)
        {
            var layerRoot = Path.Combine(root, layer);
            Assert.True(Directory.Exists(layerRoot), $"{layer} 目录不存在，扫描面收缩了，本门禁的结论无效。");
            files.AddRange(Directory.EnumerateFiles(layerRoot, "*.cs", SearchOption.AllDirectories)
                .Where(file => !IsBuildArtifact(file)));
        }

        return files.Order(StringComparer.Ordinal).ToList();
    }

    private static bool IsBuildArtifact(string path) => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") ||
                                                       path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}");

    private static string Read(string relativePath)
    {
        var path = Path.Combine(RepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), $"找不到真相源 {path}，本门禁的结论无效。");
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

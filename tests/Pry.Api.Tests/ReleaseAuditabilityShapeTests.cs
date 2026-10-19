using System.Text.RegularExpressions;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// <c>QA-TIP1016-01</c>（S2，工单 <c>PLAT-003-BACKUP-RESTORE</c> §2）：整条发行路径今天机械判据为零——
/// QA 全文检索 <c>package-release</c>，在 <c>tests/</c>、永久场景目录与全部受跟踪文件里除脚本自身外零命中，
/// 意思是**把 <c>Assert-CleanWorkingTree</c> 那一行删掉，在册一条判据都不会红**，而 <c>0-4</c> 的
/// "包里的代码 == manifest 里那个 SHA"只由一次人工执行守着。
/// <para>
/// 手法与 <see cref="ProcessWidePoolResetBoundaryTests"/> 同族：扫脚本文本＋正向探针，不动一行产品代码。
/// 判的是三个承重点还在不在——脏树守卫必须**无条件**跑（挪进 <c>-SkipValidation</c> 分支就等于没有它）、
/// manifest 的 <c>sourceCommit</c> 必须由 git 当场算（写死字面量就是那条纸面承诺）、
/// 缺运行库必须先抛再落盘（改成静默跳过就发出去一个跑不起来的包）。
/// 同一字样出现在脚本注释里也会被抓，这是有意的。
/// </para>
/// </summary>
public sealed class ReleaseAuditabilityShapeTests
{
    private const string ScriptRelativePath = "scripts/package-release.ps1";

    private static string[] Lines()
    {
        var path = Path.Combine(RepositoryRoot(), ScriptRelativePath);
        Assert.True(File.Exists(path), $"找不到发行脚本 {path}，本门禁的结论无效。");
        return File.ReadAllLines(path).Select(line => line.TrimEnd('\r')).ToArray();
    }

    /// <summary>脏树守卫的调用点：整行就是那句调用。函数声明行以 <c>function</c> 开头，不会被算进来。</summary>
    private static List<int> GuardCallSites(string[] lines) =>
        [.. lines.Select((line, index) => (line, index)).Where(x => x.line.Trim() == "Assert-CleanWorkingTree")
            .Select(x => x.index)];

    private static int SkipValidationBranch(string[] lines) =>
        Array.FindIndex(lines, line => line.TrimStart().StartsWith("if (-not $SkipValidation)", StringComparison.Ordinal));

    /// <summary>判据本体之一：守卫恰有一处调用，且它落在 -SkipValidation 分支之前，也就是无条件执行。</summary>
    private static bool GuardRunsUnconditionally(string[] lines, int skip)
    {
        var sites = GuardCallSites(lines);
        return sites.Count == 1 && skip >= 0 && sites[0] < skip;
    }

    /// <summary>判据本体之二：manifest 那一格仍由 git 当场算出，不是写死的字面量。</summary>
    private static bool CommitComesFromGit(string body) =>
        Regex.IsMatch(body, @"sourceCommit\s*=\s*\$commit") && Regex.IsMatch(body, @"git\b[^\n]*rev-parse[^\n]*HEAD");

    /// <summary>判据本体之三：缺运行库要报错，而且报错必须先于任何落盘拷贝。</summary>
    private static bool RefusesMissingRuntimeBeforeCopying(string body)
    {
        var throwAt = body.IndexOf("throw", StringComparison.Ordinal);
        var copyAt = body.IndexOf("Copy-Item", StringComparison.Ordinal);
        return Regex.IsMatch(body, @"throw\s+""Required inference runtime is missing") &&
               copyAt >= 0 && throwAt >= 0 && throwAt < copyAt;
    }

    /// <summary>取函数体：从 <c>function 名字</c> 那行到下一个**顶格**的 <c>}</c>。脚本里的函数全部写在第 0 列，
    /// 而函数体内部的 <c>if</c> 块收尾是缩进的——按 Trim() 定界会在第一个内层花括号就把函数体截断。</summary>
    private static string FunctionBody(string[] lines, string name)
    {
        var start = Array.FindIndex(lines, line => Regex.IsMatch(line, @"^\s*function\s+" + Regex.Escape(name) + @"\b"));
        Assert.True(start >= 0, $"脚本里找不到函数 {name}，本门禁要守的形状已经不在那里了。");
        var end = Array.FindIndex(lines, start + 1, line => line == "}");
        Assert.True(end > start, $"函数 {name} 没有闭合，扫描无法定界。");
        return string.Join('\n', lines[start..(end + 1)]);
    }

    [Fact]
    public void The_working_tree_guard_runs_before_any_skip_switch()
    {
        var lines = Lines();
        Assert.Single(GuardCallSites(lines));
        Assert.True(SkipValidationBranch(lines) >= 0, "脚本里不再有 -SkipValidation 分支，本判据的前提要重读。");
        Assert.True(GuardRunsUnconditionally(lines, SkipValidationBranch(lines)),
            "脏树守卫不再是无条件的那一处调用：要么被挪进了 -SkipValidation 之后，要么有人另开了一处调用点。");

        var body = FunctionBody(lines, "Assert-CleanWorkingTree");
        Assert.Contains("status --porcelain", body, StringComparison.Ordinal);
        Assert.Contains("Refusing to package from a dirty working tree", body, StringComparison.Ordinal);
        Assert.Contains("rev-parse HEAD", body, StringComparison.Ordinal);
        Assert.Contains("[0-9a-fA-F]{40}", body, StringComparison.Ordinal);
    }

    [Fact]
    public void The_release_manifest_still_takes_its_commit_from_git()
    {
        Assert.True(CommitComesFromGit(FunctionBody(Lines(), "Write-PackageMetadata")),
            "manifest 的 sourceCommit 不再是 git 当场算出来的那个值：写死的 SHA 只会把纸面承诺伪装成审计。");
    }

    [Fact]
    public void A_missing_inference_runtime_is_refused_before_anything_is_copied()
    {
        Assert.True(RefusesMissingRuntimeBeforeCopying(FunctionBody(Lines(), "Copy-InferenceRuntime")),
            "缺运行库不再先报错再落盘：改成静默跳过的话，脚本会发出去一个跑不起来的包还报成功。");
    }

    /// <summary>
    /// 正向探针：扫描在绿灯里必须真的看得见这四种掏空。样本各自独立拼写，不复用判据里的形状串——
    /// 同源时改坏判据会连带改绿探针，门就在绿灯里失效。
    /// </summary>
    [Fact]
    public void The_scanner_sees_the_hollowing_it_claims_to_ban()
    {
        // ① 守卫调用整行不在了（今天删掉它，在册判据一条都不会响）。
        var removed = new[] { "  Invoke-Checked 'dotnet' @('build')", "New-Item -ItemType Directory | Out-Null" };
        Assert.False(GuardRunsUnconditionally(removed, 1));

        // ② 调用仍只有一处，但挪进了 -SkipValidation 分支：形状上仍在，语义上成了可选。
        var moved = new[]
        {
            "if (-not $SkipValidation) {",
            "    Assert-CleanWorkingTree",
            "    Invoke-Checked 'dotnet' @('build')",
            "}",
        };
        Assert.False(GuardRunsUnconditionally(moved, SkipValidationBranch(moved)),
            "探针形状没被读成'守卫可跳过'，说明判据只看存在性、没看次序。");

        // ③ manifest 里写死一个 SHA。
        const string Hardcoded = @"    sourceCommit = '0123456789abcdef0123'";
        Assert.False(CommitComesFromGit(Hardcoded));

        // ④ 缺运行库改成静默返回，或者把检查挪到拷贝之后。
        const string Silent = "    if (-not (Test-Path $source)) { return }\n    Copy-Item $source $Destination";
        Assert.False(RefusesMissingRuntimeBeforeCopying(Silent));
        const string Late = @"    Copy-Item $source $Destination
    throw ""Required inference runtime is missing: $source""";
        Assert.False(RefusesMissingRuntimeBeforeCopying(Late));

        // 反向半条：真实脚本的四处形状都得被同一段判定认出来，否则探针证明的只是探针自己。
        var lines = Lines();
        Assert.True(GuardRunsUnconditionally(lines, SkipValidationBranch(lines)));
        Assert.True(CommitComesFromGit(FunctionBody(lines, "Write-PackageMetadata")));
        Assert.True(RefusesMissingRuntimeBeforeCopying(FunctionBody(lines, "Copy-InferenceRuntime")));
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Pry.slnx"))) return directory.FullName;
        throw new InvalidOperationException("从测试程序集出发找不到仓库根，无法核对源码约束。");
    }
}

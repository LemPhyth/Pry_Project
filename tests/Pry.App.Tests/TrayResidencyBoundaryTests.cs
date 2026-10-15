using Xunit;

namespace Pry.App.Tests;

/// <summary>
/// <c>0-5</c> 的第二块判据面：「收窗 ≠ 退出」今天零守卫。产品行为已经是对的（<c>Closing</c> 只 <c>Cancel + Hide</c>、
/// <c>ShutdownMode.OnExplicitShutdown</c>、心跳与事件流都不看窗口可见性），但它完全靠"没人去动它"维持——
/// 在 <c>Closing</c> 里补一句 <c>DisposeBackendAsync</c>、或者给 15 秒心跳加一个 <c>&amp;&amp; IsVisible</c>，
/// 都不会让任何一条判据变红，而后者会静默杀掉收窗期间的主动消息与存活上报。
/// <para>
/// 手法与 <see cref="Pry.Api.Tests.DiscoveryDataDirectoryBoundaryTests"/> 同族：文本扫描＋正向探针。
/// 判的是"这个形状有没有出现在这一处"，注释里出现同样字样也会红，这是有意的。
/// </para>
/// </summary>
public sealed class TrayResidencyBoundaryTests
{
    private const string AppFile = "App.axaml.cs";
    private const string MessengerFile = "PryMessengerWindow.axaml.cs";
    private const string CompanionFile = "PryMessengerWindow.Companion.cs";
    private const string LegacyFile = "MainWindow.axaml.cs";

    /// <summary>收窗这一段里出现任何"退出才会做的事"，就是把关窗当成了退出。</summary>
    private static readonly string[] ExitOnlyTokens =
    [
        "PrepareForExitAsync", "DisposeBackendAsync", "StopCompanionExperience", "_eventCancellation",
        "RemoveCompanionActivity", "Shutdown", "CloseMainWindow", "Environment.Exit", "Kill", "HostWindow.Close",
    ];

    /// <summary>退出四步的次序不变量：收尾 → 停宿主 → 收托盘 → 关生命周期。</summary>
    private static readonly string[] ExitStepOrder =
    [
        "PrepareForExitAsync", "DisposeBackendAsync", "IsVisible = false", "Shutdown",
    ];

    private static readonly string[] VisibilityTokens = ["IsVisible", "IsActive", "WindowState"];

    [Fact]
    public void Closing_the_window_hides_it_and_starts_no_exit()
    {
        var offenders = new List<string>();
        foreach (var file in new[] { MessengerFile, LegacyFile })
        {
            if (file == LegacyFile && !File.Exists(Path.Combine(RepositoryRoot(), "src", "Pry.App", file))) continue;
            var segment = ClosingSegment(file);
            offenders.AddRange(ExitOnlyTokens.Where(segment.Contains).Select(token => $"{file}: {token}"));
            Assert.Contains(".Cancel = true", segment);
            Assert.Contains("Hide()", segment);
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void The_closing_scanner_sees_the_exit_shape_it_claims_to_ban()
    {
        const string ExitShaped = "Closing += (_, e) => { e.Cancel = true; Hide(); await DisposeBackendAsync(); }";
        const string HideOnly = "Closing += (_, e) => { if (!ShouldHideToTray(_allowClose)) return; e.Cancel = true; Hide(); }";
        Assert.Contains("DisposeBackendAsync", ExitOnlyTokens.Where(token => ExitShaped.Contains(token)));
        Assert.All(ExitOnlyTokens, token => Assert.False(HideOnly.Contains(token), token));
        // 反证的另一半：判定必须走那个决策点，不是各自内联一句 if——否则两处窗口各写一份，改一漏一。
        Assert.Contains("WindowClosePolicy", ClosingSegment(MessengerFile));
    }

    [Fact]
    public void Every_tray_exit_site_keeps_the_locked_step_order()
    {
        var source = Source(AppFile);
        var sites = Occurrences(source, "TrayExit.RunAsync(");
        // 两条点头路径（统一窗口的确认框 / 旧版确认窗）都必须走同一个 coordinator；多出一条说明有人另开了一扇退出门。
        Assert.Equal(2, sites.Count);

        foreach (var at in sites)
            Assert.True(ExitOrderViolation(source[at..]) is null,
                $"退出序列走样：{ExitOrderViolation(source[at..])}");
    }

    [Fact]
    public void The_exit_order_scanner_sees_a_scrambled_sequence()
    {
        Assert.Null(ExitOrderViolation("Run(() => PrepareForExitAsync(true), DisposeBackendAsync, () => IsVisible = false, () => Shutdown());"));
        // 托盘先收、宿主后停：图标没了但宿主还活着，或者反过来——正是"孤儿进程"那一条点名的形状。
        Assert.NotNull(ExitOrderViolation("Run(() => PrepareForExitAsync(true), () => IsVisible = false, DisposeBackendAsync, () => Shutdown());"));
        Assert.NotNull(ExitOrderViolation("Run(() => PrepareForExitAsync(true), DisposeBackendAsync, () => Shutdown());"));
        Assert.NotNull(ExitOrderViolation("Run(() => PrepareForExitAsync(true), DisposeBackendAsync, () => IsVisible = false);"));
    }

    [Fact]
    public void The_exit_never_reaches_for_a_process_kill()
    {
        // 反向约束"不许用杀进程当作退出判据"在产品侧的半边：退出只走宿主与生命周期，不写 Kill/Environment.Exit。
        Assert.DoesNotContain("Kill", Source(AppFile), StringComparison.Ordinal);
        Assert.DoesNotContain("Environment.Exit", Source(AppFile), StringComparison.Ordinal);
    }

    [Fact]
    public void Tray_residency_preconditions_stay_declared()
    {
        var source = Source(AppFile);
        // 少了 OnExplicitShutdown，收窗（＝最后一个可见窗口消失）就会自己把进程带下去，托盘形同虚设。
        Assert.Contains("ShutdownMode.OnExplicitShutdown", source, StringComparison.Ordinal);
        Assert.Contains("NativeMenuItem(\"退出…\")", source, StringComparison.Ordinal);
        var tray = BracedBlock(source, "new TrayIcon {");
        Assert.Contains("IsVisible = true", tray, StringComparison.Ordinal);
        Assert.Contains("Menu = menu", tray, StringComparison.Ordinal);
        Assert.Contains("Icon = CreateApplicationIcon()", tray, StringComparison.Ordinal);
    }

    [Fact]
    public void Only_the_exit_sequence_stops_the_companion_heartbeat()
    {
        Assert.Contains("StopCompanionExperienceAsync()",
            MethodBody(MessengerFile, "public async Task PrepareForExitAsync(bool stopModelService)"), StringComparison.Ordinal);
        // 全仓只有那一个调用点：收窗、重启前端都不许顺带把心跳停掉。
        // 数的是"定义 + 调用"共两处，不是去匹配 await 写法——突变用 `_ = Stop…()` 一样要被抓到。
        Assert.Equal(2, new[] { MessengerFile, CompanionFile }.Select(Source)
            .Sum(source => Occurrences(source, "StopCompanionExperienceAsync()").Count));
    }

    [Theory]
    [InlineData(CompanionFile, "private void InitializeCompanionExperience()")]
    [InlineData(CompanionFile, "private async Task ReportActivityAsync()")]
    [InlineData(MessengerFile, "private void StartEventStream(string roomId, long revision)")]
    [InlineData(MessengerFile, "private async Task ObserveEventsAsync(string roomId, long revision, CancellationToken token)")]
    [InlineData(MessengerFile, "private async Task RefreshRoomsWithoutSwitchAsync()")]
    public void A_hidden_window_keeps_the_stream_and_the_heartbeat_running(string file, string signature)
    {
        // 15 秒心跳与事件流续订在收窗期间仍要工作（主动消息就是这段时间发出来的）。
        // 把可见性写成"要不要跑"的门槛，就是拿收窗当退出。
        Assert.Empty(VisibilityGatedBailOuts(MethodBody(file, signature)));
    }

    [Fact]
    public void The_visibility_gate_scanner_sees_both_shapes()
    {
        Assert.NotEmpty(VisibilityGatedBailOuts("if (_preferences is null || !IsVisible) return;"));
        Assert.NotEmpty(VisibilityGatedBailOuts("        if (!IsActive) { return; }"));
        // 换行写的门槛也要抓到，否则"把条件挪到下一行"就是一条静默的绿灯通道。
        Assert.NotEmpty(VisibilityGatedBailOuts("if (_updatingTimeline || !IsVisible ||\n    WorkspacePage.IsVisible) return;"));
        // 正向对照一：上报"此刻界面开着什么"不是门槛，不该被误抓。
        Assert.Empty(VisibilityGatedBailOuts("            IsInteractionOpen: WorkspacePage.IsVisible));"));
        // 正向对照二：白名单那条路确实带门槛，说明上一条 Theory 抓的不是空规则。
        Assert.Contains("!IsVisible", MethodBody(CompanionFile, "private async Task MarkVisibleReadAsync()"), StringComparison.Ordinal);
    }

    /// <summary>
    /// 抓"可见性被写成要不要跑的门槛"。条件语句可以换行写（<c>MarkVisibleReadAsync</c> 自己就跨两行），
    /// 所以从 <c>if (</c> 起往后读到分号为止，再判这条语句是不是一次提前返回——只按单行读会漏掉换行的门槛。
    /// </summary>
    private static List<string> VisibilityGatedBailOuts(string body)
    {
        var lines = body.Split('\n').Select(line => line.Trim()).ToArray();
        var offenders = new List<string>();
        for (var index = 0; index < lines.Length; index++)
        {
            if (!lines[index].StartsWith("if (", StringComparison.Ordinal)) continue;
            if (!VisibilityTokens.Any(token => lines[index].Contains(token))) continue;
            var statement = lines[index];
            var cursor = index;
            while (cursor + 1 < lines.Length && !statement.EndsWith(";", StringComparison.Ordinal))
                statement += " " + lines[++cursor];

            if (statement.Contains("return", StringComparison.Ordinal)) offenders.Add(lines[index]);
        }

        return offenders;
    }

    private static string? ExitOrderViolation(string fromSite)
    {
        var previous = -1;
        foreach (var token in ExitStepOrder)
        {
            var at = fromSite.IndexOf(token, StringComparison.Ordinal);
            if (at < 0) return $"缺少「{token}」这一步";
            if (at < previous) return $"「{token}」排到了前一步之前";
            previous = at;
        }

        return null;
    }

    private static string ClosingSegment(string file)
    {
        var source = Source(file);
        var start = source.IndexOf("Closing +=", StringComparison.Ordinal);
        Assert.True(start >= 0, $"{file} 没有挂 Closing 处理，收窗行为无从裁定。");
        var end = source.IndexOf("};", start, StringComparison.Ordinal);
        Assert.True(end >= 0, $"{file} 的 Closing 段没有闭合，扫描器无法裁定。");
        return source[start..(end + 2)];
    }

    private static string MethodBody(string file, string signature)
    {
        var source = Source(file);
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, $"{file} 里找不到 {signature}，本门禁的结论无效。");
        return BracedBlock(source, "{", start);
    }

    /// <summary>从 <paramref name="open"/>（或其起点）之后第一个 <c>{</c> 开始做括号配对。</summary>
    private static string BracedBlock(string source, string open, int start = 0)
    {
        var at = source.IndexOf(open, start, StringComparison.Ordinal);
        Assert.True(at >= 0, $"扫描器找不到 {open} 的起点，本门禁的结论无效。");
        var depth = 0;
        for (var index = at; index < source.Length; index++)
        {
            if (source[index] == '{') depth++;
            else if (source[index] != '}') continue;
            else if (--depth == 0) return source[at..(index + 1)];
        }

        throw new InvalidOperationException($"{open} 起点的块没有闭合，扫描器无法裁定。");
    }

    private static List<int> Occurrences(string source, string token)
    {
        var found = new List<int>();
        for (var at = source.IndexOf(token, StringComparison.Ordinal); at >= 0; at = source.IndexOf(token, at + 1, StringComparison.Ordinal))
            found.Add(at);
        return found;
    }

    private static string Source(string file)
    {
        var path = Path.Combine(RepositoryRoot(), "src", "Pry.App", file);
        Assert.True(File.Exists(path), $"找不到 {file}，本门禁的结论无效。");
        return File.ReadAllText(path);
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Pry.slnx"))) return directory.FullName;
        throw new InvalidOperationException("从测试程序集出发找不到仓库根，无法核对源码约束。");
    }
}

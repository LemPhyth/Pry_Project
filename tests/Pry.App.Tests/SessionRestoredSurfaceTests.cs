using Xunit;

namespace Pry.App.Tests;

/// <summary>
/// <c>0-6</c> 的界面半边（<c>DEF-P1WALK-05</c> 判据④）：命令面在换后端之后自己会接回来，
/// 但事件流那条 <c>ConversationEventStreamRunner</c> 早在端口空窗里按 8 轮退避走到终点就返回了，
/// 此后没有任何一条路会再订阅——"重启后端后还能不能说上话"缺的就是重新订阅那一句。
/// <para>
/// 这一句只能钉在界面上：<b>令牌怎么换的、从哪份描述符重读，界面一概不知道也不许知道</b>
/// （F-1 的三条源码门钉的就是这个）。所以本门禁判的是接线形状——
/// 有且只有一处听这个通知、通知必须回 UI 线程、重订阅必须走切房间用的那同一个入口。
/// </para>
/// <para>
/// 手法与 <see cref="TrayResidencyBoundaryTests"/> 同族：文本扫描＋正向探针。扫描面是
/// <c>src/Pry.App</c> 的全部 <c>.cs</c>，不是"挑两个文件读读"——覆盖面比结论窄的门禁正是
/// <c>QA-TIP1015-01</c> 那一族踩过的位置。
/// </para>
/// </summary>
public sealed class SessionRestoredSurfaceTests
{
    private const string MessengerFile = "PryMessengerWindow.axaml.cs";
    private const string LegacyFile = "MainWindow.axaml.cs";
    private const string TurnManagerFile = "Services/RemoteConversationTurnManager.cs";

    private const string Notice = "SessionRestored";

    [Fact]
    public void Exactly_one_site_in_the_shell_listens_for_a_swapped_backend()
    {
        var sites = Assignments();
        Assert.Single(sites);
        Assert.Contains(MessengerFile, sites[0].File);
    }

    [Fact]
    public void The_notice_hands_off_to_the_ui_thread_and_re_arms_through_the_one_entry()
    {
        var (_, statement) = Assert.Single(Assignments());
        // 通知来自工作线程：房间号与修订号只有回到 UI 线程读才是事实，就地读会读到半个房间。
        Assert.True(HasUiHandoff(statement), statement);

        var handler = ReArmMethodBody();
        Assert.Contains("StartEventStream(", handler, StringComparison.Ordinal);
        // 不许另开一条订阅：自己 new 一个 Runner 就等于在界面里再造一次"补取历史与 Resumed 的次序"。
        Assert.DoesNotContain("new ConversationEventStreamRunner", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("ReadEventsAsync", handler, StringComparison.Ordinal);
    }

    [Fact]
    public void Re_arming_reuses_the_entry_that_retires_the_previous_runner()
    {
        var body = MethodBody(MessengerFile, "private void StartEventStream(string roomId, long revision)");
        // 重订阅走的是切房间那个入口，旧 Runner 才会被 Cancel 掉；不然换一次后端就多留一条流。
        Assert.Contains("_eventCancellation?.Cancel()", body, StringComparison.Ordinal);
        Assert.Contains("ObserveEventsAsync(", body, StringComparison.Ordinal);
    }

    [Fact]
    public void The_other_event_holder_does_not_take_the_re_arm_into_its_own_hands()
    {
        // 旧外壳那条链路（MainWindow + RemoteConversationTurnManager）不在当前启动路径上：
        // 它一旦也接这条通知，界面就有两处重新订阅，改一漏一的形状就回来了。
        Assert.Contains(TurnManagerFile, ScannedFiles());
        Assert.DoesNotContain(Notice, Source(TurnManagerFile), StringComparison.Ordinal);
        Assert.DoesNotContain(Notice, Source(LegacyFile), StringComparison.Ordinal);
    }

    [Fact]
    public void The_surface_scanner_sees_a_missing_handoff_and_a_leaked_one()
    {
        // 正向探针一：接线本身在扫描面上见得到，"零处"不是因为扫描器看不见这句。
        Assert.NotEmpty(Assignments());
        // 正向探针二：换行写的赋值也要抓到，否则"把这句挪到下一行"就是一条静默的绿灯通道。
        Assert.NotEmpty(StatementLines($"{Notice} =\n    () => Dispatcher.UIThread.Post(ReArmEventStream);", Notice));
        // 正向探针三：注释里提一句不算接走，否则"删掉那行"根本不会红。
        Assert.Empty(StatementLines($"// {Notice} = ReArmEventStream;", Notice));
        // 正向探针四：三种"接了但没回 UI 线程"的写法都要被抓出来，而不是只抓"完全没接"那一种。
        Assert.True(HasUiHandoff("_api.SessionRestored = () => Dispatcher.UIThread.Post(ReArmEventStream);"));
        foreach (var unwired in new[]
                 {
                     "_api.SessionRestored = ReArmEventStream;",
                     "_api.SessionRestored = () => Task.Run(ReArmEventStream);",
                     "_api.SessionRestored = () => { ReArmEventStream(); };",
                     "_api.SessionRestored = null;",
                 })
            Assert.False(HasUiHandoff(unwired), unwired);
    }

    [Fact]
    public void The_scan_surface_is_the_whole_shell_not_a_pair_of_files()
    {
        var files = ScannedFiles();
        Assert.True(files.Count >= 20, $"扫描面只剩 {files.Count} 个文件，界面层不止这么点。");
        foreach (var expected in new[] { MessengerFile, LegacyFile, TurnManagerFile })
            Assert.Contains(expected, files);
    }

    /// <summary>界面层里每一处把通知接走的语句：一条语句连同它换行的后半一起交回来。</summary>
    private static List<(string File, string Statement)> Assignments()
    {
        var sites = new List<(string, string)>();
        foreach (var file in ScannedFiles())
            sites.AddRange(StatementLines(Source(file), Notice).Select(statement => (file, statement)));
        return sites;
    }

    /// <summary>把出现处所在的那一行读到分号为止；赋值才算了数，声明与注释不算接走。</summary>
    private static List<string> StatementLines(string source, string token)
    {
        var lines = source.Split('\n');
        var found = new List<string>();
        for (var index = 0; index < lines.Length; index++)
        {
            var trimmed = lines[index].Trim();
            if (!trimmed.Contains(token, StringComparison.Ordinal)) continue;
            if (!trimmed.Contains('=') || trimmed.StartsWith("//", StringComparison.Ordinal)) continue;
            var statement = trimmed;
            var cursor = index;
            while (cursor + 1 < lines.Length && !statement.EndsWith(";", StringComparison.Ordinal))
                statement += " " + lines[++cursor].Trim();
            found.Add(statement);
        }

        return found;
    }

    /// <summary>这条赋值有没有把工作交回 UI 线程：只认那一个投递口，就地读线程状态的不算。</summary>
    private static bool HasUiHandoff(string statement) =>
        statement.Contains("Dispatcher.UIThread.Post", StringComparison.Ordinal);

    private static string ReArmMethodBody() => MethodBody(MessengerFile, "private void ReArmEventStream()");

    private static string MethodBody(string file, string signature)
    {
        var source = Source(file);
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, $"{file} 里找不到 {signature}，本门禁的结论无效。");
        return BracedBlock(source, start);
    }

    /// <summary>从签名之后第一个 <c>{</c> 开始做括号配对。</summary>
    private static string BracedBlock(string source, int start)
    {
        var at = source.IndexOf('{', start);
        Assert.True(at >= 0, $"扫描器找不到块的起点，本门禁的结论无效。");
        var depth = 0;
        for (var index = at; index < source.Length; index++)
        {
            if (source[index] == '{') depth++;
            else if (source[index] != '}') continue;
            else if (--depth == 0) return source[at..(index + 1)];
        }

        throw new InvalidOperationException("签名之后的块没有闭合，扫描器无法裁定。");
    }

    private static List<string> ScannedFiles() => Directory
        .GetFiles(Path.Combine(RepositoryRoot(), "src", "Pry.App"), "*.cs", SearchOption.AllDirectories)
        .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        .Select(path => Path.GetRelativePath(Path.Combine(RepositoryRoot(), "src", "Pry.App"), path).Replace('\\', '/'))
        .Order(StringComparer.Ordinal)
        .ToList();

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

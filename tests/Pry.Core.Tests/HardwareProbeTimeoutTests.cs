using System.Diagnostics;
using Pry.Core.Inference;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// QA-P2-04②：设备探测的子进程等待此前无界，而且它持着一把单槽 <c>SemaphoreSlim</c>——一次挂起不只卡住这一次调用，
/// 还会让之后每一次探测排队饿死。这里判的是三件事：等待有界、挂起不传染、取消仍然是取消。
/// QA-P6-01 另记一条：全树终止那一条的前置条件已改成**事件**，不再和四程序集并行的进程创建延迟共用一条预算。
/// </summary>
public sealed class HardwareProbeTimeoutTests
{
    private static readonly TimeSpan ProbeBudget = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// 全树终止一例的探测预算。它必须明显长于 <see cref="FirstBeatWindow"/>：孙进程要先写下第一行，
    /// 产品侧随后的到期杀才有"杀掉一个正在写的进程"可判。
    /// </summary>
    private static readonly TimeSpan TreeSweepBudget = TimeSpan.FromSeconds(5);

    /// <summary>等"孙进程开始写文件"这个事件的时间窗，判据是事件出现与否，不是它出现得多快。</summary>
    private static readonly TimeSpan FirstBeatWindow = TimeSpan.FromSeconds(3);

    [Fact]
    public async Task A_hanging_probe_is_cut_off_within_its_budget()
    {
        RequireWindowsProbeFixture();
        using var fixture = new ProbeFixture(hanging: true);

        var started = Stopwatch.GetTimestamp();
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            LlamaHardwareDetector.ListDevicesAsync(fixture.ExecutablePath, probeTimeout: ProbeBudget,
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.IsType<TimeoutException>(failure.InnerException);
        Assert.True(Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(10),
            $"探测没有在预算内被切断，实际等待 {Stopwatch.GetElapsedTime(started).TotalSeconds:0.#} 秒。");
    }

    [Fact]
    public async Task A_hang_does_not_poison_the_next_probe()
    {
        // 单槽 Gate 是这条判据的意义所在：没有 kill-on-timeout，下面这一次调用会永远排在挂死的那一次后面。
        RequireWindowsProbeFixture();
        using var hung = new ProbeFixture(hanging: true);
        using var good = new ProbeFixture(hanging: false);
        var ct = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            LlamaHardwareDetector.ListDevicesAsync(hung.ExecutablePath, probeTimeout: ProbeBudget, cancellationToken: ct));

        var devices = await LlamaHardwareDetector.ListDevicesAsync(good.ExecutablePath,
            probeTimeout: TimeSpan.FromSeconds(20), cancellationToken: ct);
        var device = Assert.Single(devices);
        Assert.Equal("CUDA0", device.Id);
        Assert.Equal("Fake Discrete GPU", device.Name);
        Assert.False(device.IsIntegrated);
    }

    [Fact]
    public async Task Killing_on_timeout_takes_the_whole_tree_not_just_the_shell()
    {
        // 假探测会拉起一个连续往文件里追加行的孙进程。只杀 cmd 本身的话，孙进程会变成孤儿继续写；
        // 文件停止增长才证明"到期终止"真的收干净了——否则每次探测失败都在用户机器上留一个常驻进程。
        RequireWindowsProbeFixture();
        using var fixture = new ProbeFixture(hanging: true);

        // 探测在后台跑，先在短于预算的窗口里等到"写下第一行"这个事件，再让预算到点。两条时间轴分开之后，
        // 前置条件不再与进程创建延迟抢同一条预算；等不到事件仍按"没有判据"报红，不放行。
        var probe = Assert.ThrowsAsync<InvalidOperationException>(() =>
            LlamaHardwareDetector.ListDevicesAsync(fixture.ExecutablePath, probeTimeout: TreeSweepBudget,
                cancellationToken: TestContext.Current.CancellationToken));

        var writing = await StartedWritingAsync(fixture.BeatPath);
        await probe;
        Assert.True(writing,
            $"孙进程没有在 {FirstBeatWindow.TotalSeconds:0} 秒内开始写文件，本用例没有判据可言。");

        var size = BeatSize(fixture.BeatPath);
        await Task.Delay(700, TestContext.Current.CancellationToken);
        Assert.Equal(size, BeatSize(fixture.BeatPath));
    }

    [Fact]
    public async Task Caller_cancellation_stays_cancellation_and_still_releases_the_gate()
    {
        RequireWindowsProbeFixture();
        using var hung = new ProbeFixture(hanging: true);
        using var good = new ProbeFixture(hanging: false);
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));

        // 包成超时或自定义异常，上层就分不出"用户停了"和"硬件探测坏了"，界面会为一个取消弹错误框。
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            LlamaHardwareDetector.ListDevicesAsync(hung.ExecutablePath, probeTimeout: TimeSpan.FromSeconds(30),
                cancellationToken: cancel.Token));

        var devices = await LlamaHardwareDetector.ListDevicesAsync(good.ExecutablePath,
            probeTimeout: TimeSpan.FromSeconds(20), cancellationToken: TestContext.Current.CancellationToken);
        Assert.Single(devices);
    }

    /// <summary>假探测用 cmd 批处理启动孙进程，这套判据只在 Windows 上成立。</summary>
    private static void RequireWindowsProbeFixture()
    {
        if (!OperatingSystem.IsWindows()) Assert.Skip("探测子进程用 cmd 批处理充当 llama-server，只在 Windows 上成立。");
    }

    /// <summary>只报告"第一行出现过没有"：出现即事件成立，窗口耗尽仍为 0 则由调用方判成没有判据。</summary>
    private static async Task<bool> StartedWritingAsync(string path)
    {
        for (var attempt = 0; attempt < FirstBeatWindow.TotalMilliseconds / 100; attempt++)
        {
            if (BeatSize(path) > 0) return true;
            await Task.Delay(100, TestContext.Current.CancellationToken);
        }
        return BeatSize(path) > 0;
    }

    /// <summary>只取元数据长度，不打开文件：孙进程正握着写句柄，读内容会撞上共享冲突。</summary>
    private static long BeatSize(string path)
    {
        try { return new FileInfo(path).Length; }
        catch (Exception failure) when (failure is FileNotFoundException or DirectoryNotFoundException or IOException)
        {
            return 0;
        }
    }

    /// <summary>假 llama-server：<c>hanging</c> 时永远不退出也不写标准输出，否则打印一行设备清单。</summary>
    private sealed class ProbeFixture : IDisposable
    {
        public string Directory { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            $"pry-probe-{Guid.NewGuid():N}");

        public string ExecutablePath => System.IO.Path.Combine(Directory, "fake-llama-server.cmd");
        public string BeatPath => System.IO.Path.Combine(Directory, "beat.log");

        public ProbeFixture(bool hanging)
        {
            System.IO.Directory.CreateDirectory(Directory);
            if (hanging)
            {
                // 孙进程不用 powershell：冷启动要几秒。但"cmd 起 cmd 只要毫秒级"这句在四程序集并行下不成立过
                // （QA-P6-01：250ms 预算内孙进程没写下第一行，用例变成空跑），所以这里的写法只负责"连续追加、
                // 不需要 sleep 原语"，稳定性由 FirstBeatWindow 那条**事件**等待保证，不靠启动器够快。
                File.WriteAllText(System.IO.Path.Combine(Directory, "beat.cmd"),
                    "@echo off\r\nfor /l %%i in (1,1,20000) do echo beat>>\"%~dp0beat.log\"\r\n");
                File.WriteAllText(ExecutablePath,
                    "@echo off\r\nstart \"\" /b \"%~dp0beat.cmd\"\r\nping -n 61 127.0.0.1 > nul\r\n");
            }
            else
            {
                File.WriteAllText(ExecutablePath, "@echo off\r\necho CUDA0: Fake Discrete GPU\r\n");
            }
        }

        public void Dispose()
        {
            try { System.IO.Directory.Delete(Directory, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}

using Pry.App.Services;
using Xunit;

namespace Pry.App.Tests;

/// <summary>
/// <c>0-5</c>（托盘常驻与真实退出）缺的第一块判据面：退出序列的<b>次序</b>今天零覆盖。执行基准里那四条反向约束
/// 点名的形状是"退出时必须连内嵌后端一起退，不许留孤儿进程"，而产品代码今天把四步连着写完就没人再看过——
/// 删掉任何一步都不会让一条判据变红。这里把次序本身钉成不变量，并且<b>按现状锁</b>：
/// 不重排、不把任何一步改成尽力而为、不加兜底。
/// </summary>
public sealed class WindowLifecycleTests
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Only_an_approved_exit_may_actually_close_the_window(bool exitApproved, bool expectedHide)
        => Assert.Equal(expectedHide, WindowClosePolicy.ShouldHideToTray(exitApproved));

    [Theory]
    [InlineData(null, false)]
    [InlineData(TrayExit.CancelChoice, false)]
    [InlineData("exit", true)]
    public void Cancelling_the_exit_dialog_is_not_a_nod(string? choice, bool expected)
        => Assert.Equal(expected, TrayExit.IsConfirmed(choice));

    [Fact]
    public async Task The_exit_steps_run_in_the_locked_order()
    {
        var log = new List<string>();
        await TrayExit.RunAsync(
            () => { log.Add("prepare"); return Task.CompletedTask; },
            () => { log.Add("backend"); return Task.CompletedTask; },
            () => log.Add("tray"),
            () => log.Add("shutdown"));

        // 窗口收尾在停宿主之前（还在跑的循环不能打到已释放的客户端）；托盘在 Shutdown 之前收起
        // （进程没了、图标还留在通知区是假退出）；Shutdown 排最后（宿主只能由"释放后端"那一步带走）。
        Assert.Equal(["prepare", "backend", "tray", "shutdown"], log);
    }

    [Fact]
    public async Task Each_step_waits_for_the_previous_one_to_finish()
    {
        var gate = new TaskCompletionSource();
        var log = new List<string>();
        var running = TrayExit.RunAsync(
            () => { log.Add("prepare"); return gate.Task; },
            () => { log.Add("backend"); return Task.CompletedTask; },
            () => log.Add("tray"),
            () => log.Add("shutdown"));

        // 后三步在第一步返回之前就开跑，等于把窗口收尾和停宿主并发——正是"孤儿进程"那一条要拦的形状。
        await Task.Delay(30, TestContext.Current.CancellationToken);
        Assert.Equal(["prepare"], log);
        Assert.False(running.IsCompleted);

        gate.SetResult();
        await running;
        Assert.Equal(["prepare", "backend", "tray", "shutdown"], log);
    }

    [Fact]
    public async Task A_failing_first_step_leaves_the_rest_untouched()
    {
        var log = new List<string>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => TrayExit.RunAsync(
            () => throw new InvalidOperationException("窗口收尾失败"),
            () => { log.Add("backend"); return Task.CompletedTask; },
            () => log.Add("tray"),
            () => log.Add("shutdown")));

        // 按现状锁死：今天没有任何一步被写成"尽力而为"，失败就停在失败处。
        // 这条判据不是为了赞美现状——它让"顺手加个 try/catch 让后端继续停"必须显式改判据，而不是静默发生。
        Assert.Empty(log);
    }

    [Fact]
    public async Task A_failing_backend_release_stops_before_the_tray_and_the_lifetime()
    {
        var log = new List<string>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => TrayExit.RunAsync(
            () => { log.Add("prepare"); return Task.CompletedTask; },
            () => throw new InvalidOperationException("宿主停不下来"),
            () => log.Add("tray"),
            () => log.Add("shutdown")));

        Assert.Equal(["prepare"], log);
    }
}

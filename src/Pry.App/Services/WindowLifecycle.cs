namespace Pry.App.Services;

/// <summary>
/// 关窗与退出是同一条裁定（作者 2026-10-12 裁 A：托盘常驻）的两面，放在同一处是为了让"收窗被当成退出"
/// 这种形状在同一处判掉：关掉窗口只把它收进托盘，进程与内嵌后端宿主继续跑（主动消息在此期间仍要工作），
/// 只有用户在托盘里点头才走真正的退出。
/// </summary>
public static class WindowClosePolicy
{
    /// <summary>
    /// 默认拦下关闭请求、改成收窗。<paramref name="exitApproved"/> 只可能由退出序列自己置起：
    /// 标题栏 ×、Alt+F4、外部发 <c>WM_CLOSE</c>、<c>taskkill</c> 不带 <c>/F</c> 都不算点头。
    /// </summary>
    public static bool ShouldHideToTray(bool exitApproved) => !exitApproved;
}

/// <summary>
/// "退出 Pry" 唯一的决策与次序出口。抽出来的理由不是好看：这四步在生产代码里原本是连着写的四行，
/// 删掉其中任何一步都不会让一条判据变红，而少一步的形状正是执行基准 <c>0-5</c> 反向约束要点名拦掉的
/// ——「退出时必须连内嵌后端一起退，不许留孤儿进程」。次序本身是不变量：窗口收尾在停宿主之前（还在跑的
/// 循环不能打到已释放的客户端），托盘在 <c>Shutdown</c> 之前收起（进程没了图标还留在通知区是假退出），
/// <c>Shutdown</c> 排最后（宿主只能由"释放后端"那一步带走，不能指望生命周期自己收尾）。
/// </summary>
public static class TrayExit
{
    public const string CancelChoice = "cancel";

    /// <summary>取消与"没选"都不算点头。按现状锁住：确认框今天只有"取消／退出"两个动作，非空且非取消即点头。</summary>
    public static bool IsConfirmed(string? choice) => choice is not null and not CancelChoice;

    public static async Task RunAsync(Func<Task> prepareWindowAsync, Func<Task> releaseBackendAsync,
        Action retireTrayIcon, Action shutdownLifetime)
    {
        await prepareWindowAsync();
        await releaseBackendAsync();
        retireTrayIcon();
        shutdownLifetime();
    }
}

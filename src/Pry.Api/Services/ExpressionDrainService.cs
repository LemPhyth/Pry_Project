using Pry.Api.Runtime;

namespace Pry.Api.Services;

/// <summary>
/// 表达学习的排空通道（<c>RT-004-DRAIN</c>，缺口 ④）。此前这条队列一直有人放、没人取：
/// 每一轮投递成功都往 <c>ExpressionLearner</c> 里 <c>TryEnqueue</c> 一条，而 <c>DrainAsync</c> 在
/// <c>src/</c> 内零调用者；<c>QueueCapacity = 64</c> 满了就开始丢，且丢的时候一点报错都没有。
/// 本类就是那个"取走"的动作，逐卡排一次——学习器是一卡一份（网关的 <c>BuildScopeAsync</c>），
/// 所以枚举口开在网关上，Core 不为这一刀新增公共 API。
///
/// 两个形状是这条通道的实质，不是风格：
/// ① 只在后台 tick 上跑，绝不搬进任何一轮的可见路径。抽取与写库慢的时候，用户等的是回复，
/// 不是她的口癖——<c>ExpressionService</c> 那句"Nothing here sends or delays a message"靠的正是这一点。
/// ② 一张卡排空失败或拖住，只结束它自己这一次排空：不终止进程，也不剥夺其余卡的机会。
///
/// 单次排空带 5 秒预算（照 <c>ConversationSessionService</c> 里 <c>DrainAsync</c> 那个形状）。到点取消
/// 不算丢：未提交的形状连同脏标记留在原处（<c>A_cancelled_drain_preserves_queued_work</c> 的方向），
/// 下一次 tick 接着排，不需要用户再说一遍。
/// </summary>
public sealed class ExpressionDrainService(CharacterIntelligenceGateway gateway,
    IConfiguration configuration, ILogger<ExpressionDrainService> logger) : BackgroundService
{
    /// <summary>
    /// 默认 15 秒。选这个量级的理由要能解释清楚"64 的容量为什么不会被填满"：
    /// 队列只在**投递成功**那一瞬增长，而一轮投递前面隔着成轮闸门、typing 等待与模型起头，
    /// 正常使用远达不到 15 秒内 64 轮；真达到时（脚本连发也一样）本刀有判据钉住
    /// <c>DroppedCount</c> 必须仍是 0，而不是靠这个数看起来够大。
    /// 后台学习不需要秒级响应，但重启之间的窗口越短越好，所以上限之外没有别的取舍。
    /// </summary>
    public const int DefaultIntervalMs = 15_000;

    /// <summary>下界夹住"每 tick 都是一趟库读"：配成 0 会把后台通道变成对 SQLite 的忙等。</summary>
    public const int MinimumIntervalMs = 25;

    /// <summary>上界一天：超过它，"她学到的东西什么时候能用上"就取决于进程活多久了。</summary>
    public const int MaximumIntervalMs = 600_000;

    private static readonly TimeSpan DrainBudget = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMilliseconds(Math.Clamp(
            configuration.GetValue("Pry:ExpressionDrainMs", DefaultIntervalMs),
            MinimumIntervalMs, MaximumIntervalMs));

        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var learner in gateway.ActiveLearners())
            {
                if (!await DrainOneAsync(learner, stoppingToken)) return;
            }
        }
    }

    /// <summary>返回 false 只有一种意思：宿主正在退出，循环就此收摊。</summary>
    private async Task<bool> DrainOneAsync(CharacterIntelligenceGateway.ActiveLearner learner,
        CancellationToken stoppingToken)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        budget.CancelAfter(DrainBudget);
        try
        {
            await learner.Learner.DrainAsync(budget.Token);
            return true;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            // 到点只说明这一张卡的存储慢或被占用，脏标记还在，下一轮 tick 照排。
            logger.LogWarning("角色 {Character} 的表达排空超过 5 秒预算，未提交的部分留给下一次排空",
                learner.CharacterInstanceId);
            return true;
        }
        catch (Exception error)
        {
            // 后台学习坏掉不得把宿主带下去：这里没有正文可泄露，所以只带卡 id 与异常本身。
            logger.LogWarning(error, "角色 {Character} 的表达排空失败，本轮跳过", learner.CharacterInstanceId);
            return true;
        }
    }
}

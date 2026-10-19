using Pry.Core.Abstractions;

namespace Pry.Api.Capabilities;

/// <summary>
/// 能力的执行通道（RT-007）。它接的是一份 <see cref="CapabilityRequest"/>，回的是一份
/// <see cref="CapabilityResult"/>——<b>轮身份由这里盖上去</b>，因为 Runtime 认的是坐标
/// （<c>CapabilityId</c> 加 <c>TurnIdentity</c>，见 docs/character-runtime.md），而能力自己不知道自己是被哪一轮叫起来的。
///
/// <para>四条形状是这一刀的实质：</para>
/// <list type="number">
/// <item><b>问不到就回执</b>：任何一条拒绝路径都必须产出一份结果。不回执的那一轮会一直等在
/// <c>capability_pending</c>，界面上一句话也不会出来——这正是接registry之前那句
/// "按不可用返回"存在的原因，它不能因为接了真东西就消失。</item>
/// <item><b>取消还是取消</b>：调用方令牌被取消时重新抛出 <see cref="OperationCanceledException"/>，
/// 不翻译成一次失败（PLAT-001 定的规矩，哪一层都一样）。</item>
/// <item><b>权限先于参数</b>：Ask / Deny 的轮次连参数都不看。先看参数再看权限，等于把"你给了什么"
/// 记进日志再去决定让不让做。</item>
/// <item><b>审计不带参数值</b>：只记 id、轮次、档位与结论。URL 里可以带访问令牌，而日志会落盘、会被打包。</item>
/// </list>
/// </summary>
public sealed class CapabilityDispatcher(CapabilityRegistry registry, ILogger<CapabilityDispatcher> logger)
{
    private static readonly IReadOnlyDictionary<string, string> NoArguments =
        new Dictionary<string, string>(StringComparer.Ordinal);

    public async Task<CapabilityResult> DispatchAsync(string conversationId, CapabilityRequest request,
        CancellationToken cancellationToken)
    {
        var registration = registry.Find(request.CapabilityId);
        if (registration is null) return Refuse(request, "capability_not_registered");
        if (registration.Permission == CapabilityPermission.Deny) return Refuse(request, "capability_denied");
        // Ask 今天到此为止：还没有一个用户能在界面上点"这一句可以"。它回的是"没人批准过"，
        // 不是"做失败了"——这两个形状对作者的读法完全不同，所以不能合成一条错误码。
        if (registration.Permission == CapabilityPermission.Ask)
            return Refuse(request, "capability_approval_required");

        var arguments = request.Arguments ?? NoArguments;
        var missing = registration.Descriptor.Arguments
            .FirstOrDefault(x => x.Required &&
                (!arguments.TryGetValue(x.Name, out var value) || string.IsNullOrWhiteSpace(value)));
        if (missing is not null) return Refuse(request, "capability_argument_missing");

        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(registration.Descriptor.TimeoutMs);
        CapabilityRunResult outcome;
        try
        {
            outcome = await registration.Capability.ExecuteAsync(arguments, budget.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 这一轮已经在取消路上，Runtime 会把它判成 interrupted；这里补一份"失败"反而把它说成她试过了。
            throw;
        }
        catch (OperationCanceledException)
        {
            return Refuse(request, "capability_timeout");
        }
        catch (Exception error)
        {
            // 异常原文只进日志，不进她的回复：一次外部启动失败不该把系统错误文案搬到会话里。
            logger.LogWarning(error, "能力 {CapabilityId} 执行失败（会话 {ConversationId}）", request.CapabilityId,
                conversationId);
            return Refuse(request, "capability_failed");
        }

        logger.LogInformation("能力 {CapabilityId} 于轮 t{TurnId}g{Generation} 结束：{Outcome}", request.CapabilityId,
            request.RequestedBy.TurnId, request.RequestedBy.GenerationId,
            outcome.Succeeded ? "成功" : outcome.ErrorCode);
        return new CapabilityResult(request.CapabilityId, request.RequestedBy, outcome.Succeeded, outcome.Output,
            outcome.ErrorCode);
    }

    private CapabilityResult Refuse(CapabilityRequest request, string errorCode)
    {
        logger.LogInformation("能力 {CapabilityId} 未执行：{Code}", request.CapabilityId, errorCode);
        return new CapabilityResult(request.CapabilityId, request.RequestedBy, false, null, errorCode);
    }
}

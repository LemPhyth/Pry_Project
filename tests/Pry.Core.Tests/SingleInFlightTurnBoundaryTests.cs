using Pry.Core.Intelligence;
using Pry.Core.Runtime;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// 投递归属的前提（<c>DEF-P3SEAM-02</c>）：网关抄气泡正文时只能认"该会话恰好有一轮在飞"，
/// 因为外发端口那句回调不带轮身份，而它的签名是契约 §7.1 结论 1 冻结的形状。
/// 那句话今天确实成立，靠的是 ReplyGate 里"用户还在输入 / 仍有排队输出"两道闸走在建议器**之前**，
/// 不是靠围栏只认最新一轮（围栏比的是 epoch，而成轮不抬 epoch）。
/// 所以这一条判据钉的是顺序本身：谁把那两道闸挪到建议器之后，这里立刻转红——
/// 那一刻网关看到的就不再是"唯一在飞的一轮"，而是两轮里猜一个。
/// </summary>
public sealed class SingleInFlightTurnBoundaryTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_turn_is_never_advised_while_the_previous_one_still_has_output_queued()
    {
        var advisor = new CountingAdvisor();
        var gate = new ReplyGate(advisor);

        var result = await gate.DecideAsync(Inputs(pendingOutput: 1), Token);

        Assert.Equal(ReplyGateDisposition.Defer, result.Disposition);
        Assert.Equal(0, advisor.Calls);
    }

    [Fact]
    public async Task A_turn_is_never_advised_while_the_user_is_still_typing()
    {
        var advisor = new CountingAdvisor();
        var gate = new ReplyGate(advisor);

        var result = await gate.DecideAsync(Inputs(typing: true), Token);

        Assert.Equal(ReplyGateDisposition.Defer, result.Disposition);
        Assert.Equal(0, advisor.Calls);
    }

    [Fact]
    public async Task Once_the_queue_is_clear_the_same_turn_is_advised_exactly_once()
    {
        // 反向探针：上面两条若只是因为建议器根本没被接到门上而常绿，它们守的就不是顺序。
        var advisor = new CountingAdvisor();
        var gate = new ReplyGate(advisor);

        var result = await gate.DecideAsync(Inputs(pendingOutput: 0), Token);

        Assert.Equal(ReplyGateDisposition.Allow, result.Disposition);
        Assert.Equal(1, advisor.Calls);
    }

    private static ReplyGateInputs Inputs(int pendingOutput = 0, bool typing = false) =>
        new(new TurnIdentity(11, 1, 0), true, typing, false, false, "我今天加班到十点", ["小潮"], null,
            TimeSpan.Zero, 0, RuntimePhase.Planning, 1, pendingOutput);

    private sealed class CountingAdvisor : IReplyAdvisor
    {
        public int Calls { get; private set; }

        public string Name => "counting";

        public Task<ReplyAdvice> AdviseAsync(ReplyGateContext context, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new ReplyAdvice(true, ResponseIntent.ContinueTopic, "counting_probe"));
        }
    }
}

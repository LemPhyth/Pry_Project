using Pry.Api.Services;
using Pry.Core.Inference;
using Pry.Core.Models;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// HDS-011d-F3｜钉住 <see cref="ModelPerformancePolicy.MinimumUsableTokensPerSecond"/>（12）这条边界的真实形状，
/// 以及"窗口小于 4096 就不再往下降档"的地板。数值 12／4096 一律从声明点读（本刀不改、不抄第二份），
/// 判据只走 <see cref="ModelPerformancePolicy"/> 的公开行为面；比较语义取自
/// <c>ModelPerformancePolicy.cs:71-72</c>＝<c>profile.ContextSize &gt; 4096 &amp;&amp; metrics.TokensPerSecond is &gt; 0 and &lt; MinimumUsableTokensPerSecond</c>。
/// </summary>
public sealed class ModelPerformancePolicyBoundaryTests
{
    // 门槛两侧：12.0 恰好落在常量上（`12 < 12` 为假）判不慢；11.9 落进开区间判太慢；
    // 0 被 `> 0` 那一半挡在外面判不慢——别把它当成"太慢"，那是漏测 `> 0` 那一向。
    [Fact]
    public void The_usable_speed_floor_flags_only_the_open_band_below_twelve()
    {
        var policy = new ModelPerformancePolicy();
        var wideWindow = new ModelProfile { Id = "boundary", DisplayName = "boundary", ContextSize = 8192 };

        Assert.False(policy.IsTooSlow(wideWindow,
            new LlamaServerStartupMetrics(ModelPerformancePolicy.MinimumUsableTokensPerSecond)));
        Assert.True(policy.IsTooSlow(wideWindow, new LlamaServerStartupMetrics(11.9)));
        Assert.False(policy.IsTooSlow(wideWindow, new LlamaServerStartupMetrics(0)));
    }

    // 地板：窗口恰为 4096 时同一低速也不再判慢（降档在此止步，她仍会回话），
    // 而降档候选链停在 4096、绝不给出小于 4096 的窗口——继续往 0 掉才是"她永远不回话"。
    [Fact]
    public void The_context_floor_stops_lowering_at_4096_instead_of_driving_the_window_to_zero()
    {
        var policy = new ModelPerformancePolicy();
        var slow = new LlamaServerStartupMetrics(4.5);

        Assert.False(policy.IsTooSlow(
            new ModelProfile { Id = "at-floor", DisplayName = "at-floor", ContextSize = 4096 }, slow));
        Assert.True(policy.IsTooSlow(
            new ModelProfile { Id = "above-floor", DisplayName = "above-floor", ContextSize = 8192 }, slow));

        var largeDevice = new LlamaComputeDevice("CUDA0", "NVIDIA GeForce RTX 5090", false, 32768, 30000);
        var requested = new ModelProfile
        {
            Id = "boundary", DisplayName = "boundary", ContextSize = 262144, MaxOutputTokens = 32768
        };
        var contexts = policy.CreateCandidates(requested, largeDevice).Select(profile => profile.ContextSize).ToList();

        Assert.Equal(4096, contexts.Min());
        Assert.DoesNotContain(contexts, context => context < 4096);
    }

    // 第二把闸（ModelProcessRegistry.cs:63 的 catch 过滤器）里可测的那半条：
    // IsTooSlow 在 >4096 档抛出的 context_performance_low 必须留在可降档白名单里，
    // 非容量／非性能的错误码不许让循环继续降档。闸的另一半（attemptedProfile.ContextSize>4096）
    // 只在私有 local-llama 起进程路径里跑，没有产品接缝便测不到，本刀不冒充钉住了它。
    [Fact]
    public void The_retry_gate_keeps_the_performance_code_lowerable_and_rejects_non_capacity_codes()
    {
        var policy = new ModelPerformancePolicy();

        Assert.True(policy.MayTryLowerContext(new ModelRuntimeException("context_performance_low", "速度过低", true)));
        Assert.True(policy.MayTryLowerContext(new ModelRuntimeException("memory_allocation_failed", "内存不足", true)));
        Assert.False(policy.MayTryLowerContext(new ModelRuntimeException("process_start_failed", "启动失败", true)));
    }
}

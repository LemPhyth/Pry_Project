using Pry.Api.Services;
using Pry.Core.Inference;
using Pry.Core.Models;
using Pry.Core.Prompting;
using Xunit;

namespace Pry.Api.Tests;

// HDS-011 slice: AUD-01 at the model-load / downgrade boundary (the place the negative input
// budget was actually produced before it ever reached assembly).
public sealed class ContextWindowLoadTests
{
    private static ModelProfile Default1_7B() => new()
    {
        Id = "qwen3-1.7b-local", DisplayName = "Qwen3-1.7B", ContextSize = 32768, MaxOutputTokens = 32768
    };

    [Fact]
    public void Loading_the_default_profile_at_full_context_yields_a_positive_input_budget()
    {
        var policy = new ModelPerformancePolicy();
        // Balanced GPU -> initial context equals the requested 32768, so no downgrade masks the bug.
        var balanced = new LlamaComputeDevice("CUDA0", "NVIDIA GeForce RTX 4060", false, 8192, 8100);

        var first = policy.CreateCandidates(Default1_7B(), balanced).First();

        Assert.Equal(32768, first.ContextSize);
        Assert.True(first.MaxOutputTokens <= first.ContextSize / 2);
        Assert.True(first.ContextSize - first.MaxOutputTokens - ContextBudget.DefaultSafetyTokens > 0);
    }

    [Fact]
    public void Every_downgrade_candidate_keeps_a_positive_input_budget()
    {
        var policy = new ModelPerformancePolicy();
        var extreme = new LlamaComputeDevice("CUDA0", "NVIDIA GeForce RTX 5090", false, 32768, 30000);

        foreach (var candidate in policy.CreateCandidates(new ModelProfile
        {
            Id = "m", DisplayName = "M", ContextSize = 262144, MaxOutputTokens = 262144
        }, extreme))
        {
            var input = candidate.ContextSize - candidate.MaxOutputTokens - ContextBudget.DefaultSafetyTokens;
            Assert.True(input > 0, $"context {candidate.ContextSize} leaves non-positive input budget {input}");
            Assert.True(candidate.MaxOutputTokens >= 1);
        }
    }

    [Fact]
    public void ForModelWindow_agrees_with_the_loader_on_the_default_profile()
    {
        var budget = ContextBudget.ForModelWindow(32768, 32768);
        var loaded = new ModelPerformancePolicy()
            .CreateCandidates(Default1_7B(), new LlamaComputeDevice("c", "RTX 4060", false, 8192, 8100)).First();

        Assert.Equal(loaded.MaxOutputTokens, budget.OutputTokens);
        Assert.Equal(loaded.ContextSize, budget.ContextTokens);
    }

    [Fact]
    public void A_4096_cpu_window_keeps_the_exact_half_window_output()
    {
        var policy = new ModelPerformancePolicy();
        // Window 4096, device omitted -> Cpu tier (MaximumInitialContextSize 4096) ->
        // firstContext = min(4096, 4096) = 4096 and every ladder step is >= 4096, so the downgrade
        // chain yields exactly one candidate.
        var candidates = policy.CreateCandidates(new ModelProfile
        {
            Id = "m", DisplayName = "M", ContextSize = 4096, MaxOutputTokens = 4096
        }).ToList();

        Assert.Single(candidates);

        // Context 4096: the half-window clamp yields output 2048 (the converted value, not a "<=" shape)
        // and the input budget is 4096 - 2048 - 128 = 1920.
        Assert.Equal(4096, candidates[0].ContextSize);
        Assert.Equal(2048, candidates[0].MaxOutputTokens);
        var input = candidates[0].ContextSize - candidates[0].MaxOutputTokens - ContextBudget.DefaultSafetyTokens;
        Assert.True(input == 1920, $"context 4096: expected input budget 1920, got {input}");
    }

    [Fact]
    public void A_1024_cpu_window_keeps_the_exact_input_floor_output()
    {
        var policy = new ModelPerformancePolicy();
        // Window 1024, device omitted -> Cpu tier -> firstContext = min(1024, 4096) = 1024 and the
        // whole ladder stays above it, so exactly one candidate is produced.
        var candidates = policy.CreateCandidates(new ModelProfile
        {
            Id = "m", DisplayName = "M", ContextSize = 1024, MaxOutputTokens = 1024
        }).ToList();

        Assert.Single(candidates);

        // Context 1024: ceiling 1024 - 128 - 512 = 384 trims the half-window 512 down to 384, so the
        // input budget is 1024 - 384 - 128 = 512, exactly the MinimumInputTokens floor.
        Assert.Equal(1024, candidates[0].ContextSize);
        Assert.Equal(384, candidates[0].MaxOutputTokens);
        var input = candidates[0].ContextSize - candidates[0].MaxOutputTokens - ContextBudget.DefaultSafetyTokens;
        Assert.True(input == 512, $"context 1024: expected input budget 512, got {input}");
    }
}

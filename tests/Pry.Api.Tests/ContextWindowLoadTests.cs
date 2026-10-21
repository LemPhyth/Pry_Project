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
}

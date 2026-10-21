using Pry.Core.Memory;
using Pry.Core.Models;
using Pry.Core.Prompting;
using Xunit;

namespace Pry.Core.Tests;

// HDS-011 slice: AUD-01 (negative input budget) + AUD-06 (unbudgeted Advisory bypass).
public sealed class ContextBudgetTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    // Repository is never touched when the memory scope mismatches / outcome is unavailable,
    // so these budget-only tests can run without a database.
    private sealed class UnusedRepository : IMemoryRepository
    {
        public Task<IReadOnlyList<MemoryEntry>> ListAsync(MemoryScope scope, CancellationToken token = default) =>
            throw new NotSupportedException();
        public Task<long> RevisionAsync(MemoryScope scope, CancellationToken token = default) =>
            throw new NotSupportedException();
        public Task<MemoryEntry> SaveManualAsync(MemoryScope scope, string kind, string content, string tags,
            double importance, long? id = null, CancellationToken token = default) => throw new NotSupportedException();
        public Task<ConfirmKeepResult> ConfirmKeepAsync(MemoryScope scope, long id, CancellationToken token = default) =>
            throw new NotSupportedException();
        public Task DeleteAsync(MemoryScope scope, long id, CancellationToken token = default) => throw new NotSupportedException();
        public Task ClearAsync(MemoryScope scope, CancellationToken token = default) => throw new NotSupportedException();
        public Task<WritebackReceipt> EnqueueAsync(CompletedMemoryTurn turn, ConsolidationPolicy policy,
            DateTimeOffset now, CancellationToken token = default) => throw new NotSupportedException();
        public Task<MemoryJob?> ClaimAsync(ConsolidationPolicy policy, DateTimeOffset now, CancellationToken token = default) =>
            throw new NotSupportedException();
        public Task<bool> CompleteAsync(MemoryJob job, IReadOnlyList<MemoryCandidate> candidates,
            DateTimeOffset now, CancellationToken token = default) => throw new NotSupportedException();
        public Task FailAsync(MemoryJob job, ConsolidationPolicy policy, DateTimeOffset now, CancellationToken token = default) =>
            throw new NotSupportedException();
    }

    private static readonly MemoryScope Scope = new("instance:budget-test", "pry");

    private static MemoryContext OutOfScopeMemory() =>
        new(new MemoryScope("instance:other", "someone"), MemoryQueryOutcome.Unavailable, [],
            new RetrievalTrace(0, 0, 0, 0, false, []));

    private static ContextRequest Request(ContextBudget budget, string advisory = "", string state = "",
            IReadOnlyList<ChatMessage>? recent = null) =>
        new(Scope, "c", "IDENTITY", "RULES", new ChatMessage(0, "c", ChatRole.User, "CURRENT", Now),
            recent ?? [], OutOfScopeMemory(), budget, State: state, Advisory: advisory);

    // ---- AUD-01 --------------------------------------------------------------

    [Fact]
    public void The_raw_equal_output_and_context_window_yields_a_negative_input_budget()
    {
        // The shipped bug: default 1.7B profile 32768/32768 minus 128 safety is negative.
        Assert.Equal(-128, new ContextBudget(32768, 32768).InputBudget);
    }

    [Fact]
    public void ForModelWindow_turns_an_equal_output_and_context_into_a_positive_input_budget()
    {
        var budget = ContextBudget.ForModelWindow(32768, 32768);

        // Rule is written in code, explainable from config: output never exceeds half the window,
        // and a positive input budget >= MinimumInputTokens is guaranteed.
        Assert.Equal(16384, budget.OutputTokens);
        Assert.True(budget.OutputTokens <= budget.ContextTokens / 2);
        Assert.Equal(32768 - 16384 - ContextBudget.DefaultSafetyTokens, budget.InputBudget);
        Assert.True(budget.InputBudget >= ContextBudget.MinimumInputTokens);
        // Inequality: output reservation + safety + input floor never exceeds the window.
        Assert.True(budget.OutputTokens + budget.SafetyTokens + budget.InputBudget == budget.ContextTokens);
    }

    [Theory]
    [InlineData(256, 256)]       // even half-window minus safety leaves less than the input floor
    [InlineData(500, 1)]         // tiny window, positive output reservation, still too small
    [InlineData(0, 512)]         // non-positive context
    public void TryNormalizeWindow_rejects_windows_too_small_to_hold_a_positive_input_budget(
        int context, int output)
    {
        Assert.False(ContextBudget.TryNormalizeWindow(context, output, out var ctx, out var out_, out var input));
        Assert.True(input < ContextBudget.MinimumInputTokens || ctx <= 0 || out_ < 0);
    }

    [Theory]
    [InlineData(32768, 32768)]   // the default 1.7B shape
    [InlineData(262144, 32768)]  // the 9B shape, already fine
    [InlineData(32768, 8192)]    // the 3B vision shape, already fine
    [InlineData(4096, 4096)]     // smallest still normalizable (4096 -> 2048 output, 1920 input)
    public void TryNormalizeWindow_accepts_real_model_windows(int context, int output)
    {
        Assert.True(ContextBudget.TryNormalizeWindow(context, output, out var ctx, out var out_, out var input));
        Assert.Equal(context, ctx);
        Assert.True(out_ >= 1 && out_ <= context / 2);
        Assert.True(input >= ContextBudget.MinimumInputTokens);
        Assert.True(input > 0);
    }

    [Fact]
    public async Task Assembly_of_a_non_positive_window_fails_with_a_distinguishable_budget_error()
    {
        // Even if a negative window reaches assembly directly (bypassing ForModelWindow), it must be
        // rejected as WindowTooSmall, not mistaken for "identity does not fit".
        var ex = await Assert.ThrowsAsync<ContextBudgetException>(() =>
            new ContextBuilder(new UnusedRepository()).BuildAsync(Request(new ContextBudget(32768, 32768)),
                TestContext.Current.CancellationToken));
        Assert.Equal(ContextBudgetFailure.WindowTooSmall, ex.Reason);
    }

    [Fact]
    public async Task A_real_default_model_window_survives_assembly_with_positive_budget()
    {
        var budget = ContextBudget.ForModelWindow(32768, 32768);
        var result = await new ContextBuilder(new UnusedRepository())
            .BuildAsync(Request(budget), TestContext.Current.CancellationToken);

        Assert.Equal(16256, result.InputBudget);
        Assert.True(result.InputTokens <= result.InputBudget);
        Assert.Contains("IDENTITY", result.SystemPrompt);
        Assert.Equal("CURRENT", result.Messages.Last().Content);
    }

    // ---- AUD-06 --------------------------------------------------------------

    [Fact]
    public async Task Advisory_is_counted_into_used_and_reaches_the_model_verbatim()
    {
        var budget = ContextBudget.ForModelWindow(32768, 32768);
        const string advisory = "本轮决定：先共情，再给出一个小建议，语气简短。";

        var withAdvisory = await new ContextBuilder(new UnusedRepository())
            .BuildAsync(Request(budget, advisory: advisory), TestContext.Current.CancellationToken);
        var withoutAdvisory = await new ContextBuilder(new UnusedRepository())
            .BuildAsync(Request(budget, advisory: ""), TestContext.Current.CancellationToken);

        // AUD-06 core: the advisory is now part of the accounting, so used strictly grows.
        Assert.True(withAdvisory.InputTokens > withoutAdvisory.InputTokens);
        Assert.True(withAdvisory.InputTokens <= withAdvisory.InputBudget);
        // The decision text must arrive verbatim (RT-002 seam invariant) - no truncation, no rewrite.
        Assert.Contains(advisory, withAdvisory.SystemPrompt);
        Assert.DoesNotContain("省略", withAdvisory.SystemPrompt);
        // And it is not silently dropped.
        Assert.DoesNotContain(withAdvisory.Dropped, d => d.Section == "Advisory");
    }

    [Fact]
    public async Task An_advisory_beyond_its_reserved_share_fails_loudly_and_is_ledged()
    {
        // Reverse of the bypass: oversized directives must never be silently truncated or appended.
        var budget = new ContextBudget(ContextTokens: 4096, OutputTokens: 512, SafetyTokens: 128, AdvisoryTokens: 64);
        var oversized = new string('x', 5_000); // well past the 64-byte reservation
        var ex = await Assert.ThrowsAsync<ContextBudgetException>(() =>
            new ContextBuilder(new UnusedRepository()).BuildAsync(Request(budget, advisory: oversized),
                TestContext.Current.CancellationToken));

        Assert.Equal(ContextBudgetFailure.AdvisoryDoesNotFit, ex.Reason);
        Assert.DoesNotContain(oversized, ex.Message); // no partial content smuggled through the error
    }

    [Fact]
    public async Task Advisory_reservation_is_protected_while_optional_sections_drop()
    {
        // A tight total: after reserving the mandatory advisory the optional sections can no longer
        // fit and must be dropped, but the advisory still reaches the model whole.
        var budget = new ContextBudget(ContextTokens: 800, OutputTokens: 100, SafetyTokens: 100,
            RecentTokens: 200, StateTokens: 100, BehaviorTokens: 50, ExpressionTokens: 50, CapabilityTokens: 50,
            AdvisoryTokens: 400);
        var advisory = new string('d', 200);
        var state = new string('s', 300); // exceeds StateTokens -> dropped

        var result = await new ContextBuilder(new UnusedRepository())
            .BuildAsync(Request(budget, advisory: advisory, state: state), TestContext.Current.CancellationToken);

        Assert.Contains(advisory, result.SystemPrompt);
        Assert.Contains(result.Dropped, d => d.Section == "当前状态");
        Assert.True(result.InputTokens <= result.InputBudget);
    }
}

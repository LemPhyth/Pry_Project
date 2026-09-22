using Pry.Contracts;
using Pry.Core.Memory;
using Pry.Core.Models;
using Pry.Core.Runtime;
using Pry.Core.Intelligence;

namespace Pry.Api.Services;

public sealed class ProactiveScheduler(MemoryDatabase database, BackendRuntime runtime,
    CompanionActivityService activity, CompanionOperationCoordinator operations,
    ConversationSessionService sessions, IProactiveMessageGenerator generator, TimeProvider clock,
    ILogger<ProactiveScheduler> logger) : BackgroundService
{
    private readonly SemaphoreSlim _runGate = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await RunOnceAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                // Log only type and a stable code, never model text, prompts or provider error bodies.
                logger.LogWarning("Proactive scheduler paused: {Code} ({ErrorType})", "scheduler_failure", ex.GetType().Name);
            }
        }
    }

    public async Task<ProactiveStatusResponse> GetStatusAsync(string characterId, CancellationToken token)
    {
        if (!runtime.Characters.Any(x => x.Id == characterId)) throw new ResourceNotFoundException("character", characterId);
        var stored = await database.GetProactivePolicyAsync(characterId, token)
            ?? new StoredProactivePolicy(new ProactivePolicy(characterId), 0);
        var history = await database.GetProactiveHistoryAsync(stored.Policy, clock.GetUtcNow(), token);
        var eligibility = await EvaluateAsync(stored.Policy, history, false, token);
        return new(characterId, stored.Policy.ConversationId, eligibility.Reason, history.LastOutcome,
            history.LastAttemptAt, history.NextAttemptAt, history.SentToday, history.AwaitingUserReply);
    }

    public async Task RunOnceAsync(CancellationToken token)
    {
        if (!await _runGate.WaitAsync(0, token)) return;
        try
        {
            await sessions.EvictIdleRuntimesAsync(token);
            // No generation can be active while this run gate is held at entry. Recover a previous
            // receipt whose final write failed, without replaying its output or bypassing its cooldown.
            await database.RecoverProactiveAttemptsAsync(token);
            foreach (var stored in await database.ListProactivePoliciesAsync(token))
            {
                ProactiveAttempt? attempt;
                CancellationTokenSource generation;
                using (await operations.EnterStateAsync(token))
                {
                    var history = await database.GetProactiveHistoryAsync(stored.Policy, clock.GetUtcNow(), token);
                    if (!(await EvaluateAsync(stored.Policy, history, false, token)).Eligible) continue;
                    attempt = await database.BeginProactiveAttemptAsync(stored, clock.GetUtcNow(), token);
                    if (attempt is null) continue;
                    generation = operations.BeginGeneration(token);
                }
                await GenerateAndDeliverAsync(attempt, generation);
                return;
            }
        }
        finally { _runGate.Release(); }
    }

    private async Task<ProactiveEligibility> EvaluateAsync(ProactivePolicy policy, ProactiveHistory history,
        bool completing, CancellationToken token)
    {
        var now = clock.GetUtcNow();
        var snapshot = activity.Snapshot(policy.ConversationId ?? "");
        var room = policy.ConversationId is null ? null : await database.GetConversationAsync(policy.ConversationId, token);
        var idle = snapshot.IdleFor < operations.IdleFor ? snapshot.IdleFor : operations.IdleFor;
        var eligibility = ProactivePolicyRules.Evaluate(policy, now, snapshot.Fresh, snapshot.Busy, idle,
            (!snapshot.Fresh || snapshot.TargetMatches) && room?.CharacterId == policy.CharacterId && runtime.Characters.Any(x => x.Id == policy.CharacterId),
            operations.ForegroundBusy || (completing ? sessions.HasActiveTurnsExcept(policy.ConversationId!) : sessions.HasActiveTurns),
            null, history.SentToday, history.AwaitingUserReply);
        if (!eligibility.Eligible) return eligibility;
        if (!completing && history.LastOutcome == "generating") return new("generating");
        if (!completing && history.NextAttemptAt > now) return new("minimum_interval", history.NextAttemptAt);
        return generator.IsReady ? eligibility : new("model_not_loaded");
    }

    private async Task GenerateAndDeliverAsync(ProactiveAttempt attempt, CancellationTokenSource generation)
    {
        var started = clock.GetTimestamp();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(generation.Token, timeout.Token);
        using var monitorStop = CancellationTokenSource.CreateLinkedTokenSource(linked.Token);
        var monitor = MonitorActivityAsync(attempt.Policy.Policy, generation, monitorStop.Token);
        var outcome = "failed";
        try
        {
            var captured = await sessions.CaptureProactiveAsync(attempt, linked.Token);
            var content = await generator.GenerateAsync(attempt.Policy.Policy, linked.Token);
            linked.Token.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(content)) throw new ProactiveOutputException("empty_output");
            if (content.Length > 500) throw new ProactiveOutputException("invalid_output");
            var candidate = captured.Candidate with
            {
                Plan = new ResponsePlan(ResponseIntent.ContinueTopic, [new RuntimePlannedMessage(0, content, null)],
                    Source: MessageSources.Proactive, ProactiveOrigin: true)
            };
            var report = await sessions.DeliverProactiveAsync(captured.Runtime, candidate,
                async (permission, message, ct) =>
                {
                    // Delays never hold the foreground coordination lock. Only the final review + transaction do.
                    using var state = await operations.EnterStateAsync(ct);
                    linked.Token.ThrowIfCancellationRequested();
                    var history = await database.GetProactiveHistoryAsync(attempt.Policy.Policy, clock.GetUtcNow(), ct);
                    if (!(await EvaluateAsync(attempt.Policy.Policy, history, true, ct)).Eligible) return null;
                    return await database.CompleteProactiveAttemptAsync(attempt, message.Text!, clock.GetUtcNow(), ct, permission);
                }, linked.Token);
            outcome = report.Delivered == 1 ? "sent" : linked.IsCancellationRequested ? "cancelled" : "superseded";
        }
        catch (ProactiveOutputException ex) { outcome = ex.Code; }
        catch (OperationCanceledException) { outcome = timeout.IsCancellationRequested ? "timed_out" : "cancelled"; }
        catch (Exception ex)
        {
            logger.LogWarning("Proactive attempt {AttemptId} failed ({ErrorType})", attempt.Id, ex.GetType().Name);
        }
        finally
        {
            await monitorStop.CancelAsync();
            try { await monitor; } catch (OperationCanceledException) { }
            using (await operations.EnterStateAsync(CancellationToken.None)) operations.EndGeneration(generation);
            // Completing the receipt after cancellation must not reuse the cancelled request token.
            if (outcome != "sent")
                await database.EndProactiveAttemptAsync(attempt, outcome, clock.GetUtcNow(), CancellationToken.None);
            logger.LogInformation("Proactive {AttemptId} for {CharacterId}/{ConversationId}: {Outcome}, {ElapsedMs}ms",
                attempt.Id, attempt.Policy.Policy.CharacterId, attempt.Policy.Policy.ConversationId, outcome,
                clock.GetElapsedTime(started).TotalMilliseconds);
        }
    }

    private async Task MonitorActivityAsync(ProactivePolicy policy, CancellationTokenSource generation, CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(token))
        {
            var snapshot = activity.Snapshot(policy.ConversationId!);
            if (snapshot.Fresh && !snapshot.Busy && snapshot.TargetMatches && !operations.ForegroundBusy &&
                !sessions.HasActiveTurnsExcept(policy.ConversationId!)) continue;
            await generation.CancelAsync();
            return;
        }
    }
}

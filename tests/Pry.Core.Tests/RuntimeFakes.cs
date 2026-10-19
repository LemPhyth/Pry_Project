using Pry.Core.Abstractions;
using Pry.Core.Runtime;

namespace Pry.Core.Tests;

/// <summary>
/// 在多个 ConversationRuntime 实例之间共享的"存储"。用于模拟进程重启：
/// epoch、计划与消息都必须活过 runtime 实例的生命周期。
/// </summary>
internal sealed class RuntimeLedger
{
    private long _nextPlanId = 1;
    private long _nextMessageId = 1;

    public long Epoch { get; set; }
    public List<(string Role, string Text, long PlanId)> Messages { get; } = [];
    public Dictionary<long, PlanRow> Plans { get; } = [];
    public Dictionary<string, RuntimePersistenceState> State { get; } = [];
    public int SweepCount { get; private set; }
    public bool FailNextRead { get; set; }

    public void NoteSweep() => SweepCount++;

    public int OpenPlanCount()
    {
        lock (this) return Plans.Values.Count(x => x.Status == "open");
    }

    public long NextMessageId() => Interlocked.Increment(ref _nextMessageId);

    public long NextPlanId() => Interlocked.Increment(ref _nextPlanId);

    internal sealed class PlanRow
    {
        public required TurnIdentity Identity { get; init; }
        public required long Epoch { get; init; }
        public required int PlannedCount { get; init; }
        public required string ConversationId { get; init; }
        public int NextSequence { get; set; }
        public string Status { get; set; } = "open";
    }
}

/// <summary>
/// 镜像真实存储的关键语义：投递许可与每个气泡都是条件写入，
/// epoch 一变，旧计划的下一次写入就被拒绝；重启后 open 计划必须清扫。
/// </summary>
internal sealed class FakeRuntimeStore(RuntimeLedger ledger) : IRuntimeStore
{
    public List<string> Calls { get; } = [];
    public int AppendedInputs { get; private set; }

    public Task<RuntimePersistenceState?> LoadStateAsync(string conversationId, CancellationToken cancellationToken)
    {
        lock (ledger)
        {
            Calls.Add("load");
            return Task.FromResult(ledger.State.GetValueOrDefault(conversationId));
        }
    }

    public Task SaveStateAsync(string conversationId, RuntimePersistenceState state, CancellationToken cancellationToken)
    {
        lock (ledger)
        {
            Calls.Add("save");
            ledger.State[conversationId] = state;
            return Task.CompletedTask;
        }
    }

    public Task<long> ReadEpochAsync(string conversationId, CancellationToken cancellationToken)
    {
        lock (ledger)
        {
            if (ledger.FailNextRead)
            {
                ledger.FailNextRead = false;
                Calls.Add("read_failed");
                throw new InvalidOperationException("simulated store outage");
            }
            Calls.Add("read");
            return Task.FromResult(ledger.Epoch);
        }
    }

    public Task<long> BumpEpochAsync(string conversationId, CancellationToken cancellationToken)
    {
        lock (ledger)
        {
            ledger.Epoch++;
            Calls.Add($"bump:{ledger.Epoch}");
            if (ledger.State.TryGetValue(conversationId, out var state))
                ledger.State[conversationId] = state with { Epoch = ledger.Epoch };
            return Task.FromResult(ledger.Epoch);
        }
    }

    public Task<long> AppendUserInputAsync(string conversationId, RuntimeUserInput input,
        CancellationToken cancellationToken)
    {
        lock (ledger)
        {
            AppendedInputs++;
            var id = ledger.NextMessageId();
            ledger.Messages.Add(("user", InputProbe.Summarize([input]), id));
            Calls.Add($"append:{id}");
            return Task.FromResult(id);
        }
    }

    public Task<DeliveryPermission> RequestDeliveryAsync(string conversationId, TurnIdentity turn, long fenceEpoch,
        IReadOnlyList<RuntimePlannedMessage> messages, string source, string? commitToken,
        CancellationToken cancellationToken)
    {
        lock (ledger)
        {
            if (ledger.Epoch != fenceEpoch)
                return Task.FromResult(new DeliveryPermission(false, turn, 0, conversationId, fenceEpoch, source,
                    commitToken, "epoch_mismatch"));
            var planId = ledger.NextPlanId();
            ledger.Plans[planId] = new RuntimeLedger.PlanRow
            {
                Identity = turn, Epoch = fenceEpoch, PlannedCount = messages.Count,
                ConversationId = conversationId
            };
            Calls.Add($"request:{planId}");
            return Task.FromResult(new DeliveryPermission(true, turn, planId, conversationId, fenceEpoch, source,
                commitToken));
        }
    }

    public Task<long?> CommitBubbleAsync(DeliveryPermission permission, int sequence, RuntimePlannedMessage message,
        CancellationToken cancellationToken)
    {
        lock (ledger)
        {
            if (!ledger.Plans.TryGetValue(permission.PlanId, out var row) || row.Status != "open"
                || ledger.Epoch != row.Epoch || sequence != row.NextSequence || row.NextSequence >= row.PlannedCount)
            {
                Calls.Add($"commit_refused:{permission.PlanId}#{sequence}");
                return Task.FromResult<long?>(null);
            }
            var id = ledger.NextMessageId();
            row.NextSequence = sequence + 1;
            if (row.NextSequence >= row.PlannedCount) row.Status = "closed";
            ledger.Messages.Add(("assistant", message.Text ?? message.StickerId ?? "", id));
            Calls.Add($"commit:{id}");
            return Task.FromResult<long?>(id);
        }
    }

    public Task AbandonPlanAsync(DeliveryPermission permission, int fromSequence, CancellationToken cancellationToken)
    {
        lock (ledger)
        {
            if (ledger.Plans.TryGetValue(permission.PlanId, out var row) && row.Status == "open")
            {
                row.Status = "cancelled";
                Calls.Add($"abandon:{permission.PlanId}");
            }
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyList<long>> SweepStalePlansAsync(CancellationToken cancellationToken)
    {
        lock (ledger)
        {
            ledger.NoteSweep();
            var swept = ledger.Plans.Where(x => x.Value.Status == "open").Select(x => x.Key).ToArray();
            foreach (var id in swept) ledger.Plans[id].Status = "cancelled";
            Calls.Add($"sweep:{swept.Length}");
            return Task.FromResult<IReadOnlyList<long>>(swept);
        }
    }

    public IReadOnlyList<string> AssistantTexts()
    {
        lock (ledger) return ledger.Messages.Where(x => x.Role == "assistant").Select(x => x.Text).ToArray();
    }

    public int OpenPlanCount
    {
        get { lock (ledger) return ledger.Plans.Values.Count(x => x.Status == "open"); }
    }
}

/// <summary>可编程的生成端：记录每次调用，并允许测试决定何时、是否返回结果。</summary>
internal sealed class ControlledComposer(Func<TurnRequest, int, CancellationToken, Task<ResponsePlan>> handler) : IReplyComposer
{
    private readonly List<TaskCompletionSource> _starts = [];

    public int Calls { get; private set; }
    public List<TurnRequest> Requests { get; } = [];

    public async Task<ResponsePlan> ComposeAsync(TurnRequest request, CancellationToken cancellationToken)
    {
        TaskCompletionSource started;
        int index;
        lock (_starts)
        {
            index = Calls++;
            started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _starts.Add(started);
            Requests.Add(request);
        }
        started.TrySetResult();
        return await handler(request, index, cancellationToken);
    }

    public Task StartedAsync(int invocation)
    {
        lock (_starts) return invocation < _starts.Count ? _starts[invocation].Task : Task.Delay(2000);
    }

    public static ResponsePlan Plan(params string[] messages) => new(Pry.Core.Intelligence.ResponseIntent.ContinueTopic,
        messages.Select((text, index) => new RuntimePlannedMessage(index, text, null)).ToArray());
}

internal sealed class ScriptedAdvisor(Func<ReplyGateContext, ReplyAdvice> decide) : IReplyAdvisor
{
    public List<ReplyGateContext> Contexts { get; } = [];
    public string Name => "scripted";

    public Task<ReplyAdvice> AdviseAsync(ReplyGateContext context, CancellationToken cancellationToken)
    {
        lock (Contexts) Contexts.Add(context);
        return Task.FromResult(decide(context));
    }

    public static ScriptedAdvisor AlwaysSpeak() =>
        new(_ => new ReplyAdvice(true, Pry.Core.Intelligence.ResponseIntent.ContinueTopic, "scripted_allow"));

    public static ScriptedAdvisor DoNothingFor(Func<ReplyGateContext, bool> predicate, string reason) =>
        new(context => predicate(context)
            ? new ReplyAdvice(false, Pry.Core.Intelligence.ResponseIntent.DoNothing, reason)
            : new ReplyAdvice(true, Pry.Core.Intelligence.ResponseIntent.ContinueTopic, "scripted_allow"));
}

internal sealed class ScriptedClassifier(Func<RuntimeUserInput, InterruptStrength> classify) : IInterruptClassifier
{
    public InterruptAssessment Classify(RuntimeUserInput input, RuntimeObservedState observed) =>
        new(classify(input), "scripted");

    /// <summary>"等等/不对"视为硬插话，纯语气词视为背书式回应，其余为软插话。</summary>
    public static ScriptedClassifier Heuristic() => new(input =>
    {
        var text = input.Text.Trim();
        if (text.Contains("等等") || text.Contains("不对") || text.Contains("别说了")) return InterruptStrength.Hard;
        if (text is "嗯" or "嗯嗯" or "哦" or "对" or "哈哈" or "好的") return InterruptStrength.Backchannel;
        return InterruptStrength.Soft;
    });
}

internal sealed class RecordingSink : IRuntimeOutputSink
{
    public List<(long MessageId, string Text, string Source)> Delivered { get; } = [];
    public List<(TurnIdentity Turn, RuntimeTurnOutcome Outcome, string Reason)> Turns { get; } = [];
    public List<string> Typing { get; } = [];
    public List<string> Phases { get; } = [];
    public List<CapabilityRequest> Capabilities { get; } = [];
    public List<string> Failures { get; } = [];

    public Task MessageDeliveredAsync(string conversationId, long messageId, RuntimePlannedMessage message,
        string source, CancellationToken cancellationToken)
    {
        lock (Delivered) Delivered.Add((messageId, message.Text ?? message.StickerId ?? "", source));
        return Task.CompletedTask;
    }

    public Task TypingIndicatorAsync(string conversationId, bool active, CancellationToken cancellationToken)
    {
        lock (Typing) Typing.Add(active ? "on" : "off");
        return Task.CompletedTask;
    }

    public Task PhaseChangedAsync(string conversationId, RuntimePhase phase, RuntimePhase previous,
        CancellationToken cancellationToken)
    {
        lock (Phases) Phases.Add($"{previous}->{phase}");
        return Task.CompletedTask;
    }

    public Task TurnTransitionAsync(string conversationId, TurnIdentity turn, RuntimeTurnOutcome outcome,
        string reason, CancellationToken cancellationToken)
    {
        lock (Turns) Turns.Add((turn, outcome, reason));
        return Task.CompletedTask;
    }

    public Task CapabilityRequestedAsync(string conversationId, CapabilityRequest request,
        CancellationToken cancellationToken)
    {
        lock (Capabilities) Capabilities.Add(request);
        return Task.CompletedTask;
    }

    public Task FailureAsync(string conversationId, TurnIdentity turn, string errorCode,
        CancellationToken cancellationToken)
    {
        lock (Failures) Failures.Add(errorCode);
        return Task.CompletedTask;
    }
}

internal static class RuntimeSettingsFactory
{
    /// <summary>把节奏压缩到毫秒级：断言的是行为与次数，不是真实等待时长。</summary>
    public static RuntimeSettings Fast(int debounceMs = 40, int maxBufferMs = 400) => new()
    {
        DebounceMs = debounceMs,
        MaxBufferMs = maxBufferMs,
        TypingHoldMs = debounceMs * 2,
        IncompleteHoldMs = debounceMs / 2,
        MaxBackoffMs = debounceMs * 4,
        IdleHeartbeatMs = 0,
        IdleReturnAfterMs = 60_000,
        EvictAfterIdleMs = 120_000,
        ProactiveCandidateTtlMs = 60_000,
        Typing = new TypingStyle
        {
            Speed = 40, MinDelayMs = 1, MaxDelayMs = 12, Burstiness = 0.2
        }
    };

    /// <summary>需要观察"气泡之间"的行为时，只放慢投递间隔，不放慢输入缓冲。</summary>
    public static RuntimeSettings WithBubbleDelay(int minMs, int maxMs) =>
        Fast() with { Typing = new TypingStyle { Speed = 1, MinDelayMs = minMs, MaxDelayMs = maxMs, Burstiness = 0 } };
}

internal sealed record RuntimeHarness(
    ConversationRuntime Runtime,
    RuntimeLedger Ledger,
    FakeRuntimeStore Store,
    ControlledComposer Composer,
    ScriptedAdvisor Advisor,
    RecordingSink Sink)
{
    public static async Task<RuntimeHarness> CreateAsync(ControlledComposer composer,
        ScriptedAdvisor? advisor = null, RuntimeSettings? settings = null, RuntimeLedger? ledger = null,
        string conversationId = "chat-1", bool isGroupChat = false, double replyFrequency = 1.0)
    {
        var shared = ledger ?? new RuntimeLedger();
        var store = new FakeRuntimeStore(shared);
        var sink = new RecordingSink();
        var chosen = settings ?? RuntimeSettingsFactory.Fast();
        var chosenAdvisor = advisor ?? ScriptedAdvisor.AlwaysSpeak();
        var runtime = await ConversationRuntime.CreateAsync(new ConversationRuntimeOptions(
            conversationId, "instance-1", TimeProvider.System, chosen, store, composer,
            chosenAdvisor, ScriptedClassifier.Heuristic(), sink)
        {
            CharacterNames = ["小予"],
            IsGroupChat = isGroupChat,
            ReplyFrequency = replyFrequency
        });
        return new RuntimeHarness(runtime, shared, store, composer, chosenAdvisor, sink);
    }

    public Task<bool> SettleAsync(CancellationToken cancellationToken = default) =>
        Runtime.DrainAsync(TimeSpan.FromSeconds(5), cancellationToken);
}

using Pry.Api.Capabilities;
using Pry.Core.Abstractions;
using Pry.Core.Runtime;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// 分发器判据（RT-007 第一期）。最要紧的一条是第 1 条：<b>任何一条路径都得回执</b>。
/// 等待能力的那一轮不会自己醒——少一份结果就是让界面停在那儿不再出字，
/// 而这一刀改的正是"回执从哪来"，所以它必须比任何功能判据都硬。
/// </summary>
public sealed class CapabilityDispatcherTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly TurnIdentity Turn = new(7, 1, 1);

    private static CapabilityRequest Request(string id,
        IReadOnlyDictionary<string, string>? arguments = null) =>
        new(id, arguments ?? new Dictionary<string, string>(), Turn);

    private static CapabilityRegistry Registry(params IPryPlugin[] plugins) =>
        CapabilityRegistry.Build(plugins, CapabilityConfig.Build(), Path.GetTempPath(), new CapturingLoggerFactory());

    private static CapabilityDispatcher Dispatch(CapabilityRegistry registry,
        CapturingLogger<CapabilityDispatcher>? logger = null) =>
        new(registry, logger ?? new CapturingLogger<CapabilityDispatcher>());

    [Fact]
    public async Task An_unregistered_capability_still_wakes_the_waiting_turn()
    {
        var result = await Dispatch(CapabilityRegistry.EmptyRegistry)
            .DispatchAsync("chat-1", Request("no_such_thing"), Token);

        Assert.False(result.Succeeded);
        Assert.Equal("capability_not_registered", result.ErrorCode);
        // 坐标必须逐字回原：Runtime 认的是这两样，回错了就变成一次"没人等的答案"。
        Assert.Equal("no_such_thing", result.CapabilityId);
        Assert.Equal(Turn, result.ForTurn);
    }

    [Fact]
    public async Task Ask_refuses_without_running_anything()
    {
        var capability = new StubCapability("asked", CapabilityPermission.Ask, requiredArguments: ["url"]);
        var result = await Dispatch(Registry(plugins: new StubPlugin("stub", capability)))
            .DispatchAsync("chat-1", Request("asked", new Dictionary<string, string> { ["url"] = "https://pry.test" }), Token);

        Assert.Equal("capability_approval_required", result.ErrorCode);
        Assert.Equal(0, capability.Invocations);
    }

    [Fact]
    public async Task Deny_refuses_without_running_anything()
    {
        var capability = new StubCapability("banned", CapabilityPermission.Deny);
        var result = await Dispatch(Registry(plugins: new StubPlugin("stub", capability)))
            .DispatchAsync("chat-1", Request("banned"), Token);

        Assert.Equal("capability_denied", result.ErrorCode);
        Assert.Equal(0, capability.Invocations);
    }

    [Fact]
    public async Task A_missing_required_argument_is_refused_before_the_capability_runs()
    {
        var capability = new StubCapability("needs_url", CapabilityPermission.Allow, requiredArguments: ["url"]);
        var result = await Dispatch(Registry(plugins: new StubPlugin("stub", capability)))
            .DispatchAsync("chat-1", Request("needs_url", new Dictionary<string, string> { ["url"] = "   " }), Token);

        Assert.Equal("capability_argument_missing", result.ErrorCode);
        Assert.Equal(0, capability.Invocations);
    }

    [Fact]
    public async Task An_allowed_capability_runs_and_the_result_carries_both_coordinates()
    {
        var capability = new StubCapability("goes", CapabilityPermission.Allow,
            run: (_, _) => Task.FromResult(CapabilityRunResult.Done("opened")));
        var result = await Dispatch(Registry(plugins: new StubPlugin("stub", capability)))
            .DispatchAsync("chat-1", Request("goes", new Dictionary<string, string> { ["target"] = "notes" }), Token);

        Assert.Equal(1, capability.Invocations);
        Assert.Equal("notes", capability.LastArguments!["target"]);
        Assert.True(result.Succeeded);
        Assert.Equal("opened", result.Output);
        Assert.Null(result.ErrorCode);
        Assert.Equal(Turn, result.ForTurn);
    }

    [Fact]
    public async Task A_capability_that_overruns_its_budget_becomes_a_timeout_not_a_hang()
    {
        var capability = new StubCapability("slow", CapabilityPermission.Allow, timeoutMs: 50,
            run: async (_, cancellationToken) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                return CapabilityRunResult.Done();
            });

        var result = await Dispatch(Registry(plugins: new StubPlugin("stub", capability)))
            .DispatchAsync("chat-1", Request("slow"), Token);

        Assert.Equal("capability_timeout", result.ErrorCode);
    }

    [Fact]
    public async Task Cancelling_the_turn_is_still_a_cancellation()
    {
        // PLAT-001 的规矩：取消到哪一层都还是取消。把它翻译成一次失败，就等于把用户的中断
        // 写成"她试过了但没做成"，那一轮的账就错了。
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var capability = new StubCapability("goes", CapabilityPermission.Allow,
            run: (_, _) => Task.FromException<CapabilityRunResult>(new OperationCanceledException()));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Dispatch(Registry(plugins: new StubPlugin("stub", capability)))
                .DispatchAsync("chat-1", Request("goes"), cancelled.Token));
    }

    [Fact]
    public async Task A_capability_that_throws_does_not_hand_the_system_error_text_to_her_reply()
    {
        var capability = new StubCapability("crashes", CapabilityPermission.Allow,
            run: (_, _) => Task.FromException<CapabilityRunResult>(
                new InvalidOperationException("CreateProcess 失败：winerror 2 于 https://leak.test")));

        var result = await Dispatch(Registry(plugins: new StubPlugin("stub", capability)))
            .DispatchAsync("chat-1", Request("crashes"), Token);

        Assert.False(result.Succeeded);
        Assert.Equal("capability_failed", result.ErrorCode);
        Assert.Null(result.Output);
    }

    [Fact]
    public async Task A_permission_check_comes_before_the_argument_check()
    {
        // 同一份请求，缺参数又没批准。回的是"没人批准过"而不是"参数没给全"：后者会把她的输入
        // 当作已经受理的请求去校验，等于在点头之前先记下一遍"她给了什么"。
        var capability = new StubCapability("asked", CapabilityPermission.Ask, requiredArguments: ["url"]);
        var result = await Dispatch(Registry(plugins: new StubPlugin("stub", capability)))
            .DispatchAsync("chat-1", Request("asked"), Token);

        Assert.Equal("capability_approval_required", result.ErrorCode);
        Assert.Equal(0, capability.Invocations);
    }

    [Fact]
    public async Task The_audit_line_never_carries_an_argument_value()
    {
        var logger = new CapturingLogger<CapabilityDispatcher>();
        var capability = new StubCapability("goes", CapabilityPermission.Allow, requiredArguments: ["url"]);
        var registry = Registry(plugins: new StubPlugin("stub", capability));

        var result = await Dispatch(registry, logger).DispatchAsync("chat-1",
            // 链接里带令牌是这个判据的全部理由：日志会落盘，也会被打包带走。
            Request("goes", new Dictionary<string, string> { ["url"] = "https://pry.test/cb?access_token=hunter2" }),
            Token);

        Assert.True(result.Succeeded);
        Assert.Contains(logger.Lines, x => x.Contains("goes", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Lines, x => x.Contains("hunter2", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Lines, x => x.Contains("pry.test", StringComparison.Ordinal));
    }

    [Fact]
    public async Task The_result_tier_survives_a_refusal_because_refusal_is_not_failure_of_the_run()
    {
        // 同一项能力，Ask 档与 Allow 档给出可分辨的两个码。合成一条会让"还没人批准"读起来像"做砸了"。
        var askRegistry = Registry(plugins: new StubPlugin("stub", new StubCapability("same", CapabilityPermission.Ask)));
        var allowRegistry = Registry(plugins: new StubPlugin("stub", new StubCapability("same", CapabilityPermission.Allow)));

        var ask = await Dispatch(askRegistry).DispatchAsync("chat-1", Request("same"), Token);
        var allow = await Dispatch(allowRegistry).DispatchAsync("chat-1", Request("same"), Token);

        Assert.Equal("capability_approval_required", ask.ErrorCode);
        Assert.True(allow.Succeeded);
        Assert.Null(allow.ErrorCode);
    }
}

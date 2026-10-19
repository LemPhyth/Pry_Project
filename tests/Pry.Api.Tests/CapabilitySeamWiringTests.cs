using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Pry.Api.Capabilities;
using Pry.Api.Runtime;
using Pry.Api.Services;
using Pry.Core.Abstractions;
using Pry.Core.Expression;
using Pry.Core.Intelligence;
using Pry.Core.Memory;
using Pry.Core.Models;
using Pry.Core.Runtime;
using Pry.Core.TurnTaking;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// RT-007 第一期的接缝判据：真 Runtime、真 SQLite、真分发器、真输出通道，只把"起进程"那一次系统调用换成记录器。
/// 这一组要区分的是两种都会让用户看到同一个省略号、但账完全不同的形状——
/// <b>接了分发器</b>：能力跑完，回执进 Runtime，那一轮把话说完；
/// <b>没接</b>：保持接入前那份 <c>capability_unavailable</c> 回执，那一轮同样把话说完。
/// 两条都不许停成"永远等在能力上"。Core 侧那两条能力判据（<c>ConversationRuntimeTests</c> 的两例）
/// 手工把结果喂给 Runtime，替不了这里——它测不到"谁去算出那份结果"。
/// </summary>
public sealed class CapabilitySeamWiringTests : IAsyncLifetime
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"pry-capability-seam-{Guid.NewGuid():N}");
    private MemoryDatabase _database = null!;
    private string _databasePath = null!;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        _databasePath = Path.Combine(_root, "memory.db");
        _database = new MemoryDatabase(_databasePath);
        await _database.InitializeAsync(Token);
    }

    public ValueTask DisposeAsync()
    {
        SqliteTestPools.Clear(_databasePath);
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        return ValueTask.CompletedTask;
    }

    private sealed record Rig(ConversationRuntime Runtime, RecordingLauncher Launcher);

    /// <summary>
    /// 建一个真的会话 Runtime：第一轮交付一个 <c>open_url</c> 请求，第二轮交付一句"打开了"。
    /// <paramref name="permission"/> 为 null 时不接分发器（保持接入前的宿主形状）。
    /// </summary>
    private async Task<Rig> CreateRigAsync(CapabilityPermission? permission)
    {
        var conversationId = $"conv-{Guid.NewGuid():N}";
        await _database.EnsureConversationAsync(conversationId, "card-capability", Token);

        var calls = 0;
        var composer = new DeferredReplyComposer((_, _) =>
        {
            calls++;
            return Task.FromResult(calls == 1
                ? new ResponsePlan(ResponseIntent.UseCapability, [], new CapabilityRequest("open_url",
                    new Dictionary<string, string> { ["url"] = "https://pry.test/hello" }, TurnIdentity.None))
                : new ResponsePlan(ResponseIntent.ContinueTopic, [new RuntimePlannedMessage(0, "打开了", null)]));
        });

        var store = new DatabaseRuntimeStore(_database);
        var events = new ConversationEventLog(_database, conversationId, NullLogger.Instance);
        var launcher = new RecordingLauncher();
        ConversationRuntime? built = null;
        CapabilityDispatcher? dispatcher = null;
        if (permission is not null)
        {
            // 走真注册路径：出厂档是 Ask，这里用宿主配置把 open_url 点名成判据要观察的那一档。
            var configuration = CapabilityConfig.Build(
                ($"{CapabilityRegistry.PermissionsConfigKey}:open_url",
                    permission.Value.ToString().ToLowerInvariant()));
            dispatcher = new CapabilityDispatcher(CapabilityRegistry.Build(
                [new DesktopPlugin(configuration, launcher)], configuration, _root, new CapturingLoggerFactory()),
                NullLogger<CapabilityDispatcher>.Instance);
        }
        var sink = new ConversationRuntimeEventSink(events, NullLogger.Instance, () => built, dispatcher);

        var catalog = new StickerCatalog(Path.Combine(_root, "stickers-missing.json"), Path.Combine(_root, "stickers"));
        await catalog.LoadAsync(Token);
        built = await ConversationRuntime.CreateAsync(new ConversationRuntimeOptions(conversationId,
            "card-capability", TimeProvider.System, FastSettings(), store, composer,
            new TransitionalReplyAdvisor(), new StickerInterruptClassifier(new InterruptClassifier(catalog)), sink)
        {
            CharacterNames = ["小予"]
        }, Token);
        return new Rig(built, launcher);
    }

    private static RuntimeSettings FastSettings() => new()
    {
        DebounceMs = 40, MaxBufferMs = 400, TypingHoldMs = 80, IncompleteHoldMs = 20, MaxBackoffMs = 160,
        IdleHeartbeatMs = 0, IdleReturnAfterMs = 60_000, EvictAfterIdleMs = 120_000, ProactiveCandidateTtlMs = 60_000,
        Typing = new TypingStyle { Speed = 40, MinDelayMs = 1, MaxDelayMs = 12, Burstiness = 0.2 }
    };

    private async Task<IReadOnlyList<string>> AssistantTextsAsync(string conversationId) =>
        (await _database.GetRecentMessagesAsync(conversationId, 20, Token))
        .Where(x => x.Role == ChatRole.Assistant).Select(x => x.Content).ToArray();

    [Fact]
    public async Task A_wired_capability_runs_once_and_the_waiting_turn_still_says_its_second_half()
    {
        var rig = await CreateRigAsync(CapabilityPermission.Allow);
        var conversationId = rig.Runtime.ConversationId;

        await rig.Runtime.SubmitUserInputAsync(new RuntimeUserInput("帮我打开这个链接"), true, Token);
        await rig.Runtime.DrainAsync(TimeSpan.FromSeconds(10), Token);

        Assert.Equal(["https://pry.test/hello"], rig.Launcher.OpenedUrls);
        Assert.Equal(["打开了"], await AssistantTextsAsync(conversationId));
        Assert.DoesNotContain(rig.Runtime.TraceEntries, x => x.Action == "capability_result_stale");
        await rig.Runtime.DisposeAsync();
    }

    [Fact]
    public async Task An_ask_tier_capability_launches_nothing_and_the_turn_still_completes()
    {
        // 出厂档。这一条是"零用户可见新行为"的正面证据：请求照样发出、结果照样回执，
        // 只是那份结果写的是"没人批准过"，而那一句仍然说出口。
        var rig = await CreateRigAsync(CapabilityPermission.Ask);
        var conversationId = rig.Runtime.ConversationId;

        await rig.Runtime.SubmitUserInputAsync(new RuntimeUserInput("帮我打开这个链接"), true, Token);
        await rig.Runtime.DrainAsync(TimeSpan.FromSeconds(10), Token);

        Assert.Equal(0, rig.Launcher.TotalLaunches);
        Assert.Equal(["打开了"], await AssistantTextsAsync(conversationId));
        await rig.Runtime.DisposeAsync();
    }

    [Fact]
    public async Task An_unwired_host_keeps_the_pre_rt007_unavailable_receipt()
    {
        var rig = await CreateRigAsync(null);
        var conversationId = rig.Runtime.ConversationId;

        await rig.Runtime.SubmitUserInputAsync(new RuntimeUserInput("帮我打开这个链接"), true, Token);
        await rig.Runtime.DrainAsync(TimeSpan.FromSeconds(10), Token);

        // 没接分发器时一次都不许起，也不许把那一轮挂在 capability_pending 上。
        Assert.Equal(0, rig.Launcher.TotalLaunches);
        Assert.Equal(["打开了"], await AssistantTextsAsync(conversationId));
        Assert.NotEqual(RuntimePhase.WaitingForTurn, rig.Runtime.Phase);
        await rig.Runtime.DisposeAsync();
    }

    [Fact]
    public void Nobody_can_unwire_the_seam_and_keep_a_green_suite()
    {
        // 源码门。接线只在这两处发生，行为判据在"摘掉接线"时只会变回上面那条的形状，不会红：
        // 所以这一刀留下的洞必须由扫源文件的断言守着，理由和 CharacterIntelligenceSeamTests 那条一致。
        var session = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Pry.Api", "Services",
            "ConversationSessionService.cs"));
        Assert.Contains("new ConversationRuntimeEventSink(events, logger, () => built, capabilities)", session,
            StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Pry.Api", "Program.cs"));
        Assert.Contains("CapabilityRegistry.Build(", program, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<CapabilityDispatcher>", program, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<IProcessLauncher, DesktopProcessLauncher>", program, StringComparison.Ordinal);

        // 没接时的回落不许被删：它是"任何一条路径都回执"这条例在缺分发器时的唯一执行处。
        var adapter = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Pry.Api", "Runtime",
            "RuntimeAdapters.cs"));
        Assert.Contains("capability_unavailable", adapter, StringComparison.Ordinal);
    }

    [Fact]
    public void The_outbound_channel_still_has_exactly_six_methods()
    {
        // 契约 §7.1 结论 1：只有一条外发通道，不加第七个方法。RT-007 用的就是已有的第六个。
        // 数一下而不是读注释——这条判据在有人加方法的那天才会第一次响。
        Assert.Equal(6, typeof(IRuntimeOutputSink).GetMethods().Length);
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null;
             directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Pry.slnx"))) return directory.FullName;
        throw new InvalidOperationException("从测试程序集出发找不到仓库根，无法核对源码约束。");
    }
}

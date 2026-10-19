using Pry.Api.Capabilities;
using Pry.Core.Abstractions;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// 名册判据（RT-007 第一期）。核心是一句话：<b>档位由宿主点名，读不出档位时不猜</b>。
/// 出厂默认是 Ask，所以"注册了"和"能执行"是两件事，第 1 条判据把它们分开钉。
/// </summary>
public sealed class CapabilityRegistryTests
{
    private static string TempRoot() => Path.Combine(Path.GetTempPath(), $"pry-capability-registry-{Guid.NewGuid():N}");

    [Fact]
    public void The_shipped_plugin_registers_both_desktop_capabilities_at_the_ask_tier()
    {
        var registry = CapabilityRegistry.Build([new DesktopPlugin(CapabilityConfig.Build(), new RecordingLauncher())],
            CapabilityConfig.Build(), TempRoot(), new CapturingLoggerFactory());

        Assert.Equal(["open_application", "open_url"], registry.Descriptors.Select(x => x.Id).ToArray());
        // 注册不等于放行：这两项今天都停在 Ask，而 Ask 在没有批准界面之前一律被拒。
        Assert.All(registry.Registrations, x => Assert.Equal(CapabilityPermission.Ask, x.Permission));
    }

    [Fact]
    public void The_host_can_name_one_capability_and_the_other_stays_shut()
    {
        var configuration = CapabilityConfig.Build(("Pry:Capabilities:Permissions:open_url", "allow"));
        var registry = CapabilityRegistry.Build(
            [new DesktopPlugin(configuration, new RecordingLauncher())], configuration, TempRoot(),
            new CapturingLoggerFactory());

        Assert.Equal(CapabilityPermission.Allow, registry.Find("open_url")!.Permission);
        Assert.Equal(CapabilityPermission.Ask, registry.Find("open_application")!.Permission);
    }

    [Fact]
    public void The_host_can_also_shut_one_that_declares_itself_open()
    {
        var logger = new CapturingLoggerFactory();
        var registry = CapabilityRegistry.Build([new StubPlugin("stub",
                new StubCapability("loud", CapabilityPermission.Allow))],
            CapabilityConfig.Build(("Pry:Capabilities:Permissions:loud", "deny")), TempRoot(), logger);

        Assert.Equal(CapabilityPermission.Deny, registry.Find("loud")!.Permission);
    }

    [Fact]
    public void A_permission_the_host_could_not_read_never_widens_the_tier()
    {
        var logger = new CapturingLoggerFactory();
        var registry = CapabilityRegistry.Build([new StubPlugin("stub",
                new StubCapability("open_url", CapabilityPermission.Ask))],
            // 用户手写配置时打错的形状。把它当成 allow 是在替用户点那个他没点过的按钮。
            CapabilityConfig.Build(("Pry:Capabilities:Permissions:open_url", "postive")), TempRoot(), logger);

        Assert.Equal(CapabilityPermission.Ask, registry.Find("open_url")!.Permission);
        Assert.Contains(logger.Logger.Lines, x => x.Contains("open_url", StringComparison.Ordinal));
    }

    [Fact]
    public void A_second_registration_of_the_same_id_does_not_replace_the_first()
    {
        var first = new StubCapability("dup", CapabilityPermission.Allow);
        var second = new StubCapability("dup");
        var logger = new CapturingLoggerFactory();
        var registry = CapabilityRegistry.Build([new StubPlugin("one", first), new StubPlugin("two", second)],
            CapabilityConfig.Build(), TempRoot(), logger);

        Assert.Single(registry.Descriptors);
        Assert.Same(first, registry.Find("dup")!.Capability);
        Assert.Contains(logger.Logger.Lines, x => x.Contains("丢弃", StringComparison.Ordinal));
    }

    [Fact]
    public void A_capability_without_a_positive_timeout_is_not_registered()
    {
        // TimeoutMs = 0 不是"不设限"的写法，而是一个会一直等的等待，它进不了名册。
        var registry = CapabilityRegistry.Build([new StubPlugin("stub",
                new StubCapability("forever", timeoutMs: 0))], CapabilityConfig.Build(), TempRoot(),
            new CapturingLoggerFactory());

        Assert.Null(registry.Find("forever"));
        Assert.Empty(registry.Descriptors);
    }

    [Fact]
    public void Every_plugin_gets_its_own_storage_box_and_a_logger_named_after_it()
    {
        var data = TempRoot();
        var plugin = new StubPlugin("pry.desktop", new StubCapability("noop"));
        CapabilityRegistry.Build([plugin], CapabilityConfig.Build(), data, new CapturingLoggerFactory());

        Assert.NotNull(plugin.LastContext);
        Assert.Equal(Path.GetFullPath(Path.Combine(data, "plugins", "pry.desktop")),
            plugin.LastContext!.Storage.RootDirectory);
        Assert.Equal("pry.desktop", plugin.LastContext.PluginId);
    }

    [Fact]
    public void The_empty_registry_has_no_open_door()
    {
        // 未接插件的宿主形状：名册为空，任何 id 都问不到——这正是分发器 capability_not_registered 的来源。
        Assert.Empty(CapabilityRegistry.EmptyRegistry.Descriptors);
        Assert.Null(CapabilityRegistry.EmptyRegistry.Find("open_url"));
        Assert.Null(CapabilityRegistry.EmptyRegistry.Find(""));
        Assert.Null(CapabilityRegistry.EmptyRegistry.Find(null!));
    }
}

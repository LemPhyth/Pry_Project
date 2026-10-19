using Pry.Core.Abstractions;

namespace Pry.Api.Capabilities;

/// <summary>一条已经生效的注册：描述符、实现，以及<b>本次进程真正采用</b>的权限档。</summary>
/// <param name="Permission">描述符里写的是能力自己的出厂档，这里是叠加宿主配置之后的结果。</param>
public sealed record CapabilityRegistration(CapabilityDescriptor Descriptor, ICapability Capability,
    CapabilityPermission Permission);

/// <summary>
/// 能力名册（RT-007）。它回答两个问题："有没有这一项"和"这一项今天的权限档是什么"，
/// 并且只在这两个问题上回答——<b>它不执行任何东西</b>，执行与取消预算归 <see cref="CapabilityDispatcher"/>。
/// </summary>
public sealed class CapabilityRegistry
{
    /// <summary>配置节：<c>Pry:Capabilities:Permissions:&lt;capabilityId&gt;</c> = allow / ask / deny。</summary>
    public const string PermissionsConfigKey = "Pry:Capabilities:Permissions";

    private readonly Dictionary<string, CapabilityRegistration> _byId;

    private CapabilityRegistry(Dictionary<string, CapabilityRegistration> byId) => _byId = byId;

    /// <summary>没有插件的宿主形状（测试与未配置的主机）：名册为空，任何请求都落到 <c>capability_not_registered</c>。</summary>
    public static CapabilityRegistry EmptyRegistry { get; } =
        new(new Dictionary<string, CapabilityRegistration>(StringComparer.Ordinal));

    public IReadOnlyList<CapabilityDescriptor> Descriptors =>
        _byId.Values.Select(x => x.Descriptor).OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();

    public IReadOnlyList<CapabilityRegistration> Registrations =>
        _byId.Values.OrderBy(x => x.Descriptor.Id, StringComparer.Ordinal).ToArray();

    public CapabilityRegistration? Find(string capabilityId) =>
        capabilityId is not null && _byId.TryGetValue(capabilityId, out var registration) ? registration : null;

    /// <summary>
    /// 把插件交上来的能力收进名册，同时叠加宿主配置。<b>一个 id 只有一条生效</b>：重复的按先来者留，
    /// 后来者当场写明并丢弃——为了一个重复 id 让整个后端起不来，代价是用户今天开不了这个程序，
    /// 而"谁抄了谁的名字"这件事日志里看得见，不必用启动失败来强调。
    /// </summary>
    public static CapabilityRegistry Build(IEnumerable<IPryPlugin> plugins, IConfiguration configuration,
        string dataDirectory, ILoggerFactory loggerFactory)
    {
        var byId = new Dictionary<string, CapabilityRegistration>(StringComparer.Ordinal);
        foreach (var plugin in plugins)
        {
            var context = new PluginContext(plugin.Id, plugin.DisplayName,
                new PluginStorage(Path.Combine(dataDirectory, "plugins", plugin.Id)),
                loggerFactory.CreateLogger($"plugin:{plugin.Id}"));
            var capabilities = plugin.CreateCapabilities(context);
            foreach (var capability in capabilities)
            {
                var descriptor = capability.Descriptor;
                if (!IsUsable(descriptor, context.Logger)) continue;
                var registration = new CapabilityRegistration(descriptor, capability,
                    EffectivePermission(descriptor, configuration, context.Logger));
                if (byId.TryAdd(descriptor.Id, registration)) continue;
                context.Logger.LogError("能力 {CapabilityId} 已由更早的注册占用，本次注册被丢弃", descriptor.Id);
            }
            context.Logger.LogInformation("插件 {Plugin} 提供 {Count} 项能力，存储格 {Storage}", plugin.Id,
                capabilities.Count, context.Storage.RootDirectory);
        }
        return new CapabilityRegistry(byId);
    }

    /// <summary>
    /// 描述符自己能保证的只有两件事：有名字、有正的超时。<c>TimeoutMs &lt;= 0</c> 不是"不设限"的写法，
    /// 而是一个会一直等的等待，所以这种注册直接不进名册。
    /// </summary>
    private static bool IsUsable(CapabilityDescriptor descriptor, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(descriptor.Id))
        {
            logger.LogError("能力描述符没有 id，已丢弃这条注册");
            return false;
        }
        if (descriptor.TimeoutMs <= 0)
        {
            logger.LogError("能力 {CapabilityId} 的超时不是正数（{TimeoutMs}），已丢弃这条注册", descriptor.Id,
                descriptor.TimeoutMs);
            return false;
        }
        return true;
    }

    /// <summary>
    /// 配置只能把权限往<b>严</b>的方向改吗？不是——Allow 是这一档存在的意义，宿主明令就是它生效的时候。
    /// 但认不出来的值一律落回能力自己的出厂档，并写明一声：把拼错的 <c>postive</c> 悄悄当成 allow
    /// 或 deny，都是在替用户做一个他没做过的决定。
    /// </summary>
    private static CapabilityPermission EffectivePermission(CapabilityDescriptor descriptor,
        IConfiguration configuration, ILogger logger)
    {
        var configured = configuration[$"{PermissionsConfigKey}:{descriptor.Id}"];
        if (string.IsNullOrWhiteSpace(configured)) return descriptor.Permission;
        switch (configured.Trim().ToLowerInvariant())
        {
            case "allow": return CapabilityPermission.Allow;
            case "ask": return CapabilityPermission.Ask;
            case "deny": return CapabilityPermission.Deny;
            default:
                logger.LogWarning("能力 {CapabilityId} 的权限配置读不出档位（值不作为判定），沿用出厂档 {Permission}",
                    descriptor.Id, descriptor.Permission);
                return descriptor.Permission;
        }
    }
}

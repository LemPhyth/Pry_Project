using Pry.Core.Abstractions;

namespace Pry.Api.Capabilities;

/// <summary>
/// 仓库里唯一的 <see cref="IPryPlugin"/>（RT-007 第一期）。它不是"插件系统的一个例子"，
/// 而是这两个能力今天的住处：<see cref="CapabilityRegistry"/> 因此有一条真实的注册路径，
/// 而不是为两个能力特判。<br/>
/// <b>没有加载器</b>：插件市场与任意程序集发现写在 <c>docs/full-scope-sprint.md</c> §3 第 4 层的
/// 不做清单里，这一版只把"插件能拿到什么"的形状定下来——一个 id、一格存储、一个日志器，
/// 没有数据库连接、没有 ServiceProvider、没有密钥。
/// </summary>
public sealed class DesktopPlugin(IConfiguration configuration, IProcessLauncher launcher) : IPryPlugin
{
    /// <summary>同时是存储目录名，所以用小写与点分隔，路径里没有空格。</summary>
    public const string PluginId = "pry.desktop";

    public string Id => PluginId;

    public string DisplayName => "本机桌面";

    public IReadOnlyList<ICapability> CreateCapabilities(PluginContext context) =>
    [
        new OpenUrlCapability(launcher),
        new OpenApplicationCapability(launcher, ReadAllowedApplications(configuration))
    ];

    /// <summary>
    /// 白名单出厂为空。空表不是"还没配好"的临时形状，而是这一版的判定结果：
    /// 没有任何一条用户可见路径会让她拿到一个可执行文件路径，所以没有任何一条路径需要被放行。
    /// </summary>
    public static IReadOnlyList<string> ReadAllowedApplications(IConfiguration configuration) =>
        configuration.GetSection(OpenApplicationCapability.AllowedApplicationsConfigKey).Get<string[]>() ?? [];
}

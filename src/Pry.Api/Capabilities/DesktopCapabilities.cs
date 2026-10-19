using Pry.Core.Abstractions;

namespace Pry.Api.Capabilities;

/// <summary>
/// 两个能力的参数名与错误码集中在这里，是为了让"分发器按描述符校验参数"和"能力自己校验语义"
/// 这两处引用同一个名字——参数名写错不会被编译器抓住，只会变成一条永远走不到的分支。
/// </summary>
public static class CapabilityIds
{
    public const string OpenUrl = "open_url";
    public const string OpenApplication = "open_application";

    public const string UrlArgument = "url";
    public const string PathArgument = "path";

    /// <summary>两个能力出厂同档：Ask。没被宿主明令 Allow 之前，一次外部启动都不许发生。</summary>
    public const CapabilityPermission DefaultPermission = CapabilityPermission.Ask;

    /// <summary>秒级：一次外部启动慢过它，就说明系统在用别的程序问话，那一轮不该替她等着。</summary>
    public const int TimeoutMs = 5_000;
}

/// <summary>
/// <c>open_url</c>：只放 http/https 过。
/// <c>file:</c> 会把本机目录端给浏览器，<c>javascript:</c> 把正文交给脚本引擎，裸路径等于
/// "她说要打开什么就能打开什么"。这一条拒绝发生在 <see cref="IProcessLauncher"/> 之前，
/// 所以坏形状的判据是"启动器一次都没被调用"，不是"启动了但被拦下"。
/// </summary>
public sealed class OpenUrlCapability(IProcessLauncher launcher) : ICapability
{
    private static readonly string[] AllowedSchemes = ["http", "https"];

    public CapabilityDescriptor Descriptor { get; } = new(CapabilityIds.OpenUrl, "打开链接",
        CapabilityIds.DefaultPermission,
        [new CapabilityArgument(CapabilityIds.UrlArgument, true, "要打开的网页地址")], CapabilityIds.TimeoutMs);

    public async Task<CapabilityRunResult> ExecuteAsync(IReadOnlyDictionary<string, string> arguments,
        CancellationToken cancellationToken)
    {
        if (!arguments.TryGetValue(CapabilityIds.UrlArgument, out var raw))
            return CapabilityRunResult.Refused("url_argument_missing");
        if (!Uri.TryCreate(raw.Trim(), UriKind.Absolute, out var uri) ||
            !AllowedSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase))
            return CapabilityRunResult.Refused("url_scheme_not_allowed");
        // 交出去的是规范化后的 URI，不是原文：前后空白与大写 scheme 在这里就定下来。
        await launcher.OpenUrlAsync(uri.AbsoluteUri, cancellationToken);
        // 成功不回显 URL。它会作为"能力结果"进提示词，而链接里可以带访问令牌。
        return CapabilityRunResult.Done();
    }
}

/// <summary>
/// <c>open_application</c>：只启动宿主白名单里的完整路径。白名单出厂为空，
/// 所以这一项在出厂配置下必然被拒——它是"能力可以存在但默认无权限"这一档的例子，
/// 不是"稍后就会自动生效"的开关。<br/>
/// 校验顺序是刻意的：先比白名单、后看文件在不在。反过来会把"这台机器上有没有某个程序"
/// 泄漏进一条错误码。
/// </summary>
public sealed class OpenApplicationCapability(IProcessLauncher launcher,
    IReadOnlyList<string> allowedApplications) : ICapability
{
    /// <summary>配置节：<c>Pry:Capabilities:AllowedApplications</c>，数组，每一项是完整可执行文件路径。</summary>
    public const string AllowedApplicationsConfigKey = "Pry:Capabilities:AllowedApplications";

    private readonly HashSet<string> _allowed = new(allowedApplications
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => Path.GetFullPath(x.Trim())), PathComparer);

    public CapabilityDescriptor Descriptor { get; } = new(CapabilityIds.OpenApplication, "打开应用",
        CapabilityIds.DefaultPermission,
        [new CapabilityArgument(CapabilityIds.PathArgument, true, "本机可执行文件的完整路径")],
        CapabilityIds.TimeoutMs);

    /// <summary>Windows 的路径大小写不敏感；其余系统按 Ordinal，否则同一个程序能用两种写法挤进同一格。</summary>
    private static IEqualityComparer<string> PathComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    public async Task<CapabilityRunResult> ExecuteAsync(IReadOnlyDictionary<string, string> arguments,
        CancellationToken cancellationToken)
    {
        if (!arguments.TryGetValue(CapabilityIds.PathArgument, out var raw) || string.IsNullOrWhiteSpace(raw))
            return CapabilityRunResult.Refused("application_argument_missing");
        if (!Path.IsPathRooted(raw))
            return CapabilityRunResult.Refused("application_path_not_absolute");
        var full = Path.GetFullPath(raw.Trim());
        if (!_allowed.Contains(full)) return CapabilityRunResult.Refused("application_not_allowed");
        if (!File.Exists(full)) return CapabilityRunResult.Refused("application_not_found");
        await launcher.StartApplicationAsync(full, cancellationToken);
        // 同样不回显路径。
        return CapabilityRunResult.Done();
    }
}

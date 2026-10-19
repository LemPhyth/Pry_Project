using Microsoft.Extensions.Logging;
using Pry.Core.Models;
using Pry.Core.Runtime;

namespace Pry.Core.Abstractions;

public interface IChatModel
{
    ModelProfile Profile { get; }
    IAsyncEnumerable<string> StreamAsync(string systemPrompt, IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<string>? imagePaths, ChatRequestOptions? options = null, CancellationToken cancellationToken = default);
}

public interface IEmbeddingProvider
{
    string ModelId { get; }
    int Dimensions { get; }
    Task<float[]> EncodeAsync(string text, CancellationToken cancellationToken = default);
}

public interface IMemoryRetriever
{
    Task<IReadOnlyList<MemoryRecord>> SearchMemoriesAsync(string characterId, string query, int count = 6,
        CancellationToken cancellationToken = default);
}

public interface IImageGenerationProvider
{
    string ModelId { get; }
    Task<string> GenerateAsync(string prompt, string outputDirectory,
        IProgress<double>? progress = null, CancellationToken cancellationToken = default);
}

public interface ISpeechRecognizer
{
    bool IsAvailable { get; }
    Task<string> RecognizeAsync(string audioPath, CancellationToken cancellationToken = default);
}

public interface IAvatarRenderer : IAsyncDisposable
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task SetExpressionAsync(string expression, double intensity, CancellationToken cancellationToken = default);
    Task SetMotionAsync(string motion, CancellationToken cancellationToken = default);
}

public interface IAgentToolRegistry
{
    IReadOnlyCollection<string> ToolNames { get; }
}

public sealed class NullAvatarRenderer : IAvatarRenderer
{
    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SetExpressionAsync(string expression, double intensity, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SetMotionAsync(string motion, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public sealed class EmptyAgentToolRegistry : IAgentToolRegistry
{
    public IReadOnlyCollection<string> ToolNames => Array.Empty<string>();
}

// ===== RT-007：Capability 与 Plugin 的形状（地基） =====
//
// 这两个信封原本住在 Pry.Core.Runtime。搬过来是 docs/phase2-runtime-contract.md §7.1 结论 3 与
// docs/phase1-platform-baseline.md §5.1 落笔的条件：能力是宿主与外部世界的接缝，不是会话状态机的一部分，
// Runtime 只消费请求与结果。同一个程序集里换命名空间，`TurnIdentity` 只能靠 using 引——这一条是刻意的
// 偏离，写在契约里而不是悄悄发生。
//
// 本段今天没有任何生产者：会话里没有人发起 CapabilityRequest（见 docs/character-runtime.md 与
// SituationSnapshot.AvailableCapabilityNames 的零生产引用），所以"她能不能真的打开一个东西"不由这些类型决定。

/// <summary>
/// 一次能力请求：<b>哪一轮</b>要做什么（<paramref name="RequestedBy"/> 由 Runtime 回填，见
/// ConversationRuntime 里 <c>capability_requested</c> 那段）、参数是什么。参数是扁平的字符串表，
/// 因为今天只有两个能力、每个一个参数；把它做成 JSON 树属于通用 Tool Loop，那是 §2.4 明令不做的。
/// </summary>
public sealed record CapabilityRequest(string CapabilityId, IReadOnlyDictionary<string, string> Arguments,
    TurnIdentity RequestedBy, string? ReplyWhenDone = null);

/// <summary>
/// 一次能力结果。必须同时带回 <c>CapabilityId</c> 与 <c>ForTurn</c>：Runtime 靠这两个坐标丢弃过期结果
/// （<c>capability_result_stale</c>），少一个坐标就会出现"上一轮的结果消掉这一轮的等待"。
/// </summary>
public sealed record CapabilityResult(string CapabilityId, TurnIdentity ForTurn, bool Succeeded, string? Output,
    string? ErrorCode);

/// <summary>
/// 能力权限。三档不是装饰：<b>Ask</b> 是出厂默认，它让"没被点头的能力"和"被明令禁止的能力"
/// 在账上分开——<c>capability_approval_required</c> 说的是"还没人批准过"，
/// <c>capability_denied</c> 说的是"宿主明确不许"。
/// </summary>
public enum CapabilityPermission
{
    Allow,
    Ask,
    Deny
}

/// <summary>能力声明的一个参数。<c>Required</c> 由分发器执行，语义校验留在能力自己那一侧。</summary>
public sealed record CapabilityArgument(string Name, bool Required, string? Description = null);

/// <summary>
/// 能力的自述。<c>TimeoutMs</c> 必须有界：一次外部调用慢的时候，用户等的是那句回复，
/// 不是一个卡死的窗口——超时后分发器补一个 <c>capability_timeout</c> 结果，这一轮继续走得下去的路。
/// </summary>
public sealed record CapabilityDescriptor(string Id, string DisplayName, CapabilityPermission Permission,
    IReadOnlyList<CapabilityArgument> Arguments, int TimeoutMs);

/// <summary>
/// 能力自己跑完的结论。注意它不带轮身份——把 <c>TurnIdentity</c> 盖上去是分发器的职责，
/// 让能力自己填就等于请它猜"我是被哪一轮叫起来的"。
/// </summary>
public sealed record CapabilityRunResult(bool Succeeded, string? Output, string? ErrorCode)
{
    public static CapabilityRunResult Done(string? output = null) => new(true, output, null);
    public static CapabilityRunResult Refused(string errorCode) => new(false, null, errorCode);
}

/// <summary>
/// 一项可被请求的能力。实现者的取消义务只有一条：调用方令牌被取消时抛
/// <see cref="OperationCanceledException"/>，不要把它翻译成一次失败结果——
/// PLAT-001 定的规矩是取消在任何一层都仍然是取消。
/// </summary>
public interface ICapability
{
    CapabilityDescriptor Descriptor { get; }

    Task<CapabilityRunResult> ExecuteAsync(IReadOnlyDictionary<string, string> arguments,
        CancellationToken cancellationToken);
}

/// <summary>
/// 插件向宿主交代自己是谁、提供哪些能力。<b>本接口今天没有对外加载器</b>：
/// 插件市场与任意程序集发现都在 §2.4 的不做清单里，仓库里唯一的实现是宿主自带的那一个。
/// </summary>
public interface IPryPlugin
{
    /// <summary>命名空间键：插件目录与日志分类都按它走，所以必须是小写、无分隔符的稳定 id。</summary>
    string Id { get; }

    string DisplayName { get; }

    IReadOnlyList<ICapability> CreateCapabilities(PluginContext context);
}

/// <summary>注册时交给插件的那一眼：我是谁、我能往哪儿写、日志往哪儿记。</summary>
public sealed record PluginContext(string PluginId, string DisplayName, PluginStorage Storage, ILogger Logger);

/// <summary>
/// 一格只属于某个插件的目录。<c>dataDirectory/plugins/&lt;pluginId&gt;</c> 由宿主算好，
/// 插件只拿到相对路径这一层——它因此碰不到数据库文件、别人的插件目录和配置里的密钥路径。
/// 越界的路径一律拒绝（见 <see cref="TryResolvePath"/>），这是本类存在的唯一理由。
/// </summary>
public sealed class PluginStorage
{
    private readonly string _root;

    public PluginStorage(string rootDirectory)
    {
        _root = Path.GetFullPath(rootDirectory);
    }

    public string RootDirectory => _root;

    /// <summary>
    /// 把插件给的相对路径折算成绝对路径。返回 false 的三种形状：空串、绝对路径（含盘符与 UNC）、
    /// 折算后落出自己那一格（<c>..</c> 词法拒绝，前缀比较再兜一次）。
    /// 前缀比较按 Ordinal：宁可错拒一次大小写不同的写法，也不要错放一次穿越。
    /// </summary>
    public bool TryResolvePath(string relativePath, out string absolutePath)
    {
        absolutePath = "";
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath)) return false;
        if (relativePath.Contains("..", StringComparison.Ordinal)) return false;
        var candidate = Path.GetFullPath(Path.Combine(_root, relativePath));
        var boundary = _root.EndsWith(Path.DirectorySeparatorChar) ? _root : _root + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(boundary, StringComparison.Ordinal)) return false;
        absolutePath = candidate;
        return true;
    }

    public async Task WriteTextAsync(string relativePath, string content, CancellationToken cancellationToken = default)
    {
        if (!TryResolvePath(relativePath, out var path))
            throw new ArgumentException($"插件存储路径必须在自己的目录内：{relativePath}", nameof(relativePath));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, content, cancellationToken);
    }

    /// <summary>读不到与越界都返回 null：读的一侧没有"写坏了要喊出来"的必要，越界本来就不该有内容。</summary>
    public async Task<string?> ReadTextAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (!TryResolvePath(relativePath, out var path) || !File.Exists(path)) return null;
        return await File.ReadAllTextAsync(path, cancellationToken);
    }
}

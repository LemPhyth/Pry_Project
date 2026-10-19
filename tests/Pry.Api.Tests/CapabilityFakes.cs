using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pry.Api.Capabilities;
using Pry.Core.Abstractions;

namespace Pry.Api.Tests;

/// <summary>
/// RT-007 判据的公共假人。它们只替掉两样东西：一次真启动（会在跑测试的机器上留下进程和窗口）
/// 和一段真延时（会把整条门禁拖成分钟级）。权限档、参数校验、取消与超时这些判定全部走真的
/// <see cref="CapabilityRegistry"/> 与 <see cref="CapabilityDispatcher"/>——替掉它们就等于把被判据守着的东西搬走。
/// </summary>
internal sealed class RecordingLauncher : IProcessLauncher
{
    public List<string> OpenedUrls { get; } = [];
    public List<string> StartedApplications { get; } = [];

    public Task OpenUrlAsync(string url, CancellationToken cancellationToken)
    {
        OpenedUrls.Add(url);
        return Task.CompletedTask;
    }

    public Task StartApplicationAsync(string executablePath, CancellationToken cancellationToken)
    {
        StartedApplications.Add(executablePath);
        return Task.CompletedTask;
    }

    public int TotalLaunches => OpenedUrls.Count + StartedApplications.Count;
}

/// <summary>按需给档、按需给超时、按需决定行为的桩能力。<see cref="Invocations"/> 用来钉"没批准就一次都不许跑"。</summary>
internal sealed class StubCapability(string id, CapabilityPermission permission = CapabilityPermission.Ask,
    int timeoutMs = 5_000, Func<IReadOnlyDictionary<string, string>, CancellationToken, Task<CapabilityRunResult>>? run = null,
    params string[] requiredArguments) : ICapability
{
    public int Invocations;
    public IReadOnlyDictionary<string, string>? LastArguments { get; private set; }

    public CapabilityDescriptor Descriptor { get; } = new(id, id, permission,
        requiredArguments.Select(x => new CapabilityArgument(x, true)).ToArray(), timeoutMs);

    public async Task<CapabilityRunResult> ExecuteAsync(IReadOnlyDictionary<string, string> arguments,
        CancellationToken cancellationToken)
    {
        Invocations++;
        LastArguments = arguments;
        return run is null ? CapabilityRunResult.Done() : await run(arguments, cancellationToken);
    }
}

/// <summary>注册时把宿主给的 <see cref="PluginContext"/> 留下，好让判据读得到存储格与日志器。</summary>
internal sealed class StubPlugin : IPryPlugin
{
    private readonly ICapability[] _capabilities;

    public StubPlugin(string id, params ICapability[] capabilities)
    {
        Id = id;
        DisplayName = id;
        _capabilities = capabilities;
    }

    public PluginContext? LastContext { get; private set; }
    public string Id { get; }
    public string DisplayName { get; }

    public IReadOnlyList<ICapability> CreateCapabilities(PluginContext context)
    {
        LastContext = context;
        return _capabilities;
    }
}

internal sealed class CapturingLogger : ILogger
{
    public List<string> Lines { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) => Lines.Add(formatter(state, exception));
}

/// <summary>注入到 <c>ILogger&lt;T&gt;</c> 形参上的同一只假人，判据靠它读审计行。</summary>
internal sealed class CapturingLogger<T> : ILogger<T>
{
    private readonly CapturingLogger _inner = new();
    public List<string> Lines => _inner.Lines;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _inner.BeginScope(state);
    public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) => _inner.Log(logLevel, eventId, state, exception, formatter);
}

internal sealed class CapturingLoggerFactory : ILoggerFactory
{
    public CapturingLogger Logger { get; } = new();
    public ILogger CreateLogger(string categoryName) => Logger;
    public void AddProvider(ILoggerProvider provider)
    {
    }

    public void Dispose()
    {
    }
}

internal static class CapabilityConfig
{
    public static IConfiguration Build(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values
            .ToDictionary(x => x.Key, x => (string?)x.Value, StringComparer.OrdinalIgnoreCase)).Build();
}

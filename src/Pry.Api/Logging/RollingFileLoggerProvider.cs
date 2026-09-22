using System.Text;
using Microsoft.Extensions.Logging;

namespace Pry.Api.Logging;

/// <summary>滚动文件日志的取值（PLAT-006）。三个键只走环境变量/测试注入，不进 appsettings.json。</summary>
public sealed record RollingFileLogOptions(string Directory, string FileName, long MaximumBytesPerFile,
    int RetainedFiles)
{
    public const string DefaultFileName = "pry-backend.log";
    public const long DefaultMaximumBytes = 5L * 1024 * 1024;
    public const long MinimumMaximumBytes = 64L * 1024;
    public const long MaximumMaximumBytes = 64L * 1024 * 1024;
    public const int DefaultRetainedFiles = 5;
    public const int MinimumRetainedFiles = 1;
    public const int MaximumRetainedFiles = 20;

    /// <summary>默认落在数据目录的 <c>logs/</c> 下：日志与数据库同生命周期，卸载时一起清掉，
    /// 不会散到数据目录之外。夹取而非报错：本地单机应用不该因为一个写错的数字起不来。</summary>
    public static RollingFileLogOptions Read(IConfiguration configuration, string dataDirectory) => new(
        configuration["Pry:LogDirectory"] ?? Path.Combine(dataDirectory, "logs"),
        DefaultFileName,
        Math.Clamp(configuration.GetValue("Pry:LogFileMaximumBytes", DefaultMaximumBytes),
            MinimumMaximumBytes, MaximumMaximumBytes),
        Math.Clamp(configuration.GetValue("Pry:LogFileCount", DefaultRetainedFiles),
            MinimumRetainedFiles, MaximumRetainedFiles));
}

/// <summary>
/// 后端本机滚动日志。原先只有 console sink——内嵌宿主双击启动时没有控制台，日志等于不存在，
/// 崩溃之后没有任何可读证据。这里只解决"留得下来、按大小滚动"，异步批量写不是 1.0 的需求。
/// </summary>
public sealed class RollingFileLoggerProvider : ILoggerProvider
{
    private readonly RollingFileLogOptions _options;
    private readonly LogRedactor _redactor;
    private readonly object _gate = new();
    private long _bytes = -1;
    private bool _failureReported;

    public RollingFileLoggerProvider(RollingFileLogOptions options, LogRedactor redactor)
    {
        _options = options;
        _redactor = redactor;
    }

    public ILogger CreateLogger(string categoryName) => new RollingFileLogger(categoryName, this);

    public void Dispose()
    {
    }

    internal void Append(string categoryName, LogLevel level, string message, string? exceptionText)
    {
        var builder = new StringBuilder();
        builder.Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"))
            .Append(' ').Append(Short(level)).Append(' ').Append(categoryName).Append(": ").Append(message);
        if (!string.IsNullOrEmpty(exceptionText))
            foreach (var detail in exceptionText.Split('\n'))
                builder.Append(Environment.NewLine).Append("  | ").Append(detail.TrimEnd('\r'));
        builder.Append(Environment.NewLine);
        var line = builder.ToString();
        var length = Encoding.UTF8.GetByteCount(line);

        lock (_gate)
        {
            // sink 自己出错不能把后端拖下去：写不进日志只是少一份证据，不是服务不可用。
            // 也不永久停用——临时目录被清理、文件被杀软占用都是一次性的，下一条就该写得进去。
            try
            {
                if (_bytes < 0) _bytes = CurrentLength();
                if (_bytes > 0 && _bytes + length > _options.MaximumBytesPerFile) Roll();
                // 每条一次 AppendAllText：写完即释放句柄。常驻句柄会让数据目录删不掉，
                // 而"关掉后端后清理/备份/卸载这个目录"正是 1.0 必须成立的动作。
                System.IO.Directory.CreateDirectory(_options.Directory);
                File.AppendAllText(CurrentPath(), line, Encoding.UTF8);
                _bytes += length;
            }
            catch (Exception ex)
            {
                _bytes = -1;
                if (_failureReported) return;
                // 原因只报一次：静默失效会让"日志没写出来"本身变成一个不可诊断的故障。
                _failureReported = true;
                Console.Error.WriteLine($"Pry 日志写入失败（后续同类错误不再重复报告）：{ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    private string CurrentPath() => Path.Combine(_options.Directory, _options.FileName);

    private long CurrentLength()
    {
        var existing = new FileInfo(CurrentPath());
        return existing.Exists ? existing.Length : 0;
    }

    /// <summary>按序号后移一档，最老的一份被挤掉。</summary>
    private void Roll()
    {
        var oldest = CurrentPath() + "." + _options.RetainedFiles;
        if (File.Exists(oldest)) File.Delete(oldest);
        for (var index = _options.RetainedFiles - 1; index >= 1; index--)
        {
            var from = CurrentPath() + "." + index;
            if (File.Exists(from)) File.Move(from, CurrentPath() + "." + (index + 1), overwrite: true);
        }
        if (File.Exists(CurrentPath())) File.Move(CurrentPath(), CurrentPath() + ".1", overwrite: true);
        _bytes = 0;
    }

    private static string Short(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRC",
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        LogLevel.Critical => "CRT",
        _ => "NONE"
    };

    private sealed class RollingFileLogger(string categoryName, RollingFileLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        /// <summary>遮蔽发生在渲染之后：结构化 <c>TState</c> 里的任何值都可能被格式化成秘密，
        /// 只有出口文本是唯一确定的面。</summary>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.None) return;
            provider.Append(categoryName, logLevel, provider.Render(state, exception, formatter, includeException: false),
                exception is null ? null : provider.Render(state, exception, formatter, includeException: true));
        }
    }

    /// <summary>消息与异常各遮一次：一次调用只能拿到"消息"或"异常原文"，两者的规则相同。</summary>
    private string Render<TState>(TState state, Exception? exception, Func<TState, Exception?, string> formatter,
        bool includeException)
    {
        try
        {
            if (includeException) return _redactor.Redact(exception?.ToString());
            var text = formatter(state, null);
            return _redactor.Redact(text);
        }
        catch (Exception)
        {
            return includeException ? "[日志渲染失败]" : "[日志渲染失败]";
        }
    }
}

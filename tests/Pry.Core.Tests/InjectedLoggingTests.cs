using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Pry.Core.Inference;
using Pry.Core.Memory;
using Pry.Core.Models;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// PLAT-006 的 Core 侧判据：Core 只负责把日志交出去，并把已知密钥在出口遮掉。
/// "日志落在哪个文件、留几份"由 Pry.Api 的 sink 负责（见 BackendLoggingTests），这里只验注入点。
/// </summary>
public sealed class InjectedLoggingTests
{
    [Fact]
    public async Task MemoryDatabase_reports_schema_movement_and_nothing_else()
    {
        using var fixture = new Fixture();

        await fixture.Database.InitializeAsync(TestContext.Current.CancellationToken);

        var logged = string.Join('\n', fixture.Logger.Messages);
        var ready = Assert.Single(fixture.Logger.Messages,
            message => message.Contains("持久层就绪", StringComparison.Ordinal));
        Assert.Contains("schema 0 → " + MemoryDatabase.SchemaVersion, ready, StringComparison.Ordinal);
        // 版本与耗时是结构事实；文件名、绝对路径、用户内容都不是。
        Assert.DoesNotContain(fixture.Directory, logged, StringComparison.Ordinal);
        Assert.DoesNotContain("memory.db", logged, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MemoryDatabase_logs_the_version_it_rolled_forward_from_not_only_its_target()
    {
        using var fixture = new Fixture();
        var ct = TestContext.Current.CancellationToken;
        await fixture.Database.InitializeAsync(ct);
        // 造出"上一个版本"的库现场，才能判定日志记的是升级前版本而不是常量 SchemaVersion 复读。
        await using (var connection = new SqliteConnection($"Data Source={fixture.Path}"))
        {
            await connection.OpenAsync(ct);
            await using var rewind = connection.CreateCommand();
            rewind.CommandText = "UPDATE schema_info SET version=$previous";
            rewind.Parameters.AddWithValue("$previous", MemoryDatabase.SchemaVersion - 1);
            await rewind.ExecuteNonQueryAsync(ct);
        }

        var logger = new CapturingLogger();
        await new MemoryDatabase(fixture.Path, logger).InitializeAsync(ct);

        var logged = string.Join('\n', logger.Messages);
        Assert.Contains(logger.Messages,
            message => message.Contains(
                $"schema {MemoryDatabase.SchemaVersion - 1} → {MemoryDatabase.SchemaVersion}",
                StringComparison.Ordinal));
        // 只报目标版本的实现会让"就地升级"和"全新建库"在日志里长得一样。
        Assert.DoesNotContain("schema 0 →", logged, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LlamaServerManager_logs_its_child_and_redacts_the_key_it_handed_over()
    {
        if (!OperatingSystem.IsWindows()) Assert.Skip("本用例用 cmd 批处理充当模型服务，只在 Windows 上成立。");
        using var fixture = new FakeServerFixture();
        var logger = new CapturingLogger();
        var secret = Guid.NewGuid().ToString("N");
        var profile = new ModelProfile
        {
            Id = "fake", DisplayName = "Fake", ModelPath = fixture.ModelPath, ApiKey = secret,
            BaseUrl = $"http://127.0.0.1:{fixture.Port}/v1", ComputeDevice = "cpu", ContextSize = 512
        };

        await using var manager = new LlamaServerManager(logger);
        var failure = await Assert.ThrowsAsync<ModelRuntimeException>(async () =>
            await manager.StartAsync(fixture.ExecutablePath, profile, TestContext.Current.CancellationToken));

        Assert.Equal("process_exited", failure.Code);
        var logged = string.Join('\n', logger.Messages);
        var startup = Assert.Single(logger.Messages,
            message => message.Contains("本地模型服务启动中", StringComparison.Ordinal));
        // 日志只认文件名：运行库和模型都在数据目录里，绝对路径会把它变成一份可外泄的清单。
        Assert.Contains("fake-llama-server.cmd", startup, StringComparison.Ordinal);
        Assert.Contains("fake-model.gguf", startup, StringComparison.Ordinal);
        Assert.DoesNotContain(fixture.Directory, startup, StringComparison.Ordinal);
        var exit = Assert.Single(logger.Messages,
            message => message.Contains("本地模型服务进程退出", StringComparison.Ordinal));
        // 子进程把宿主给的密钥原样吐回 stderr：它必须在这里就被遮掉，而不是指望 sink 兜底。
        Assert.Contains("退出码 7", exit, StringComparison.Ordinal);
        Assert.Contains(LlamaServerManager.RedactedPlaceholder, exit, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, logged, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, failure.DiagnosticDetails ?? string.Empty, StringComparison.Ordinal);
    }

    private sealed class Fixture : IDisposable
    {
        public string Directory { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            $"pry-core-logging-{Guid.NewGuid():N}");
        public string Path => System.IO.Path.Combine(Directory, "memory.db");
        public CapturingLogger Logger { get; } = new();
        public MemoryDatabase Database { get; }

        public Fixture() => Database = new MemoryDatabase(Path, Logger);

        public void Dispose()
        {
            SqliteTestPools.Clear(Path);
            if (System.IO.Directory.Exists(Directory)) System.IO.Directory.Delete(Directory, true);
        }
    }

    /// <summary>一个会退出的假 llama-server：把环境里的密钥写回 stderr，再以 7 退出。</summary>
    private sealed class FakeServerFixture : IDisposable
    {
        public string Directory { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            $"pry-fake-server-{Guid.NewGuid():N}");
        public string ExecutablePath => System.IO.Path.Combine(Directory, "fake-llama-server.cmd");
        public string ModelPath => System.IO.Path.Combine(Directory, "fake-model.gguf");
        public int Port { get; }

        public FakeServerFixture()
        {
            System.IO.Directory.CreateDirectory(Directory);
            using (var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0))
            {
                listener.Start();
                Port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            }
            File.WriteAllText(ModelPath, string.Empty);
            File.WriteAllText(ExecutablePath,
                "@echo off\r\necho fake-server: key=%LLAMA_API_KEY% 1>&2\r\nexit /b 7\r\n");
        }

        public void Dispose()
        {
            try { System.IO.Directory.Delete(Directory, true); }
            catch (IOException) { }
        }
    }

    private sealed class CapturingLogger : ILogger
    {
        private readonly List<string> _messages = [];

        public IReadOnlyList<string> Messages
        {
            get { lock (_messages) return _messages.ToArray(); }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (_messages) _messages.Add($"[{logLevel}] {formatter(state, exception)}");
        }
    }
}

using System.Diagnostics;
using System.Text.Json;
using Pry.Contracts;
using Xunit;

namespace Pry.Client.Tests;

/// <summary>
/// 发现文件的读取侧：独立进程只凭 <c>runtime.json</c> 决定"接谁"，所以**不确定就不能接**。
/// 猜一个入口比没有入口更坏——陈旧文件里的端口可能已被别的进程占用，令牌也随那个后端一起失效。
/// </summary>
public sealed class RuntimeDiscoveryTests : IDisposable
{
    private readonly List<string> _directories = [];

    [Fact]
    public void A_missing_descriptor_is_no_backend_at_all()
    {
        var directory = TemporaryDirectory(write: false);
        Assert.Null(RuntimeDiscovery.TryRead(directory));
        Assert.Equal(Path.Combine(directory, PryProtocol.RuntimeDescriptorFileName),
            RuntimeDiscovery.DescriptorPath(directory));
    }

    [Theory]
    [InlineData("半截 JSON", """{"pid":1""")]
    [InlineData("不是对象", "\"runtime\"")]
    [InlineData("空文件", "")]
    public void An_unparseable_descriptor_is_refused_rather_than_guessed(string because, string content)
    {
        var directory = TemporaryDirectory(content: content);
        Assert.True(File.Exists(RuntimeDiscovery.DescriptorPath(directory)), $"{because}：测试自己没写出文件");
        Assert.Null(RuntimeDiscovery.TryRead(directory));
    }

    [Theory]
    [InlineData("端口不在合法区间", 0)]
    [InlineData("端口超出范围", 70_000)]
    [InlineData("端口为负", -1)]
    public void A_descriptor_without_a_usable_port_is_refused(string because, int port)
    {
        var descriptor = RuntimeDiscovery.TryRead(TemporaryDirectory(port: port));
        Assert.True(descriptor is null, $"{because}：端口 {port} 的发现文件被当成了一个可用入口");
    }

    [Fact]
    public void A_descriptor_without_a_token_is_refused_because_nothing_could_pass_the_gate()
    {
        Assert.Null(RuntimeDiscovery.TryRead(TemporaryDirectory(token: "")));
    }

    [Fact]
    public async Task A_stale_pid_never_becomes_a_connection()
    {
        var dead = await ExitedProcessIdAsync();
        Assert.Null(RuntimeDiscovery.TryRead(TemporaryDirectory(pid: dead)));
    }

    [Fact]
    public void Our_own_descriptor_is_never_treated_as_an_external_backend()
    {
        // 内嵌时读同一服务对象才是事实源；绕磁盘读回自己刚写的文件会把 I/O 变成启动依赖，
        // 还可能在那次覆盖的窗口里读到上一实例的端口。
        Assert.Null(RuntimeDiscovery.TryRead(TemporaryDirectory(pid: Environment.ProcessId)));
    }

    [Fact]
    public async Task A_live_owner_is_still_not_connected_when_nothing_serves_that_port()
    {
        using var alive = IdleProcess.Start();
        var directory = TemporaryDirectory(pid: alive.Id);

        var descriptor = RuntimeDiscovery.TryRead(directory);
        Assert.NotNull(descriptor);
        // 端口与令牌都只来自文件，客户端不带任何备用端口。
        Assert.Equal(4_321, descriptor!.Port);
        Assert.Equal("token", descriptor.Token);

        using var http = await RuntimeDiscovery.TryConnectAsync(directory, TestContext.Current.CancellationToken);
        Assert.Null(http);
    }

    [Fact]
    public void Unknown_fields_from_a_newer_writer_stay_readable()
    {
        // 跨进程文件格式只增不改不删，读取方必须忽略未知字段；否则升级顺序被反向锁死。
        using var alive = IdleProcess.Start();
        var directory = TemporaryDirectory(write: false);
        File.WriteAllText(RuntimeDiscovery.DescriptorPath(directory), JsonSerializer.Serialize(new
        {
            pid = alive.Id,
            port = 4_321,
            token = "token",
            protocolVersion = PryProtocol.Version,
            schemaVersion = 9,
            startedAt = DateTimeOffset.UtcNow,
            someFieldFromANewerBuild = "ignored"
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        Assert.Equal(4_321, RuntimeDiscovery.TryRead(directory)!.Port);
    }

    [Fact]
    public void The_token_never_rides_along_in_a_string()
    {
        // RuntimeDescriptor 是 record，默认 ToString 会把每个字段打进去，而令牌是机密。
        using var alive = IdleProcess.Start();
        var descriptor = RuntimeDiscovery.TryRead(TemporaryDirectory(pid: alive.Id, token: "super-secret-token"))!;
        Assert.DoesNotContain("super-secret-token", descriptor.ToString(), StringComparison.Ordinal);
    }

    private string TemporaryDirectory(int pid = 4_200_000, int port = 4_321, string token = "token",
        string? content = null, bool write = true)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pry-discovery-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = RuntimeDiscovery.DescriptorPath(directory);
        if (content is not null) File.WriteAllText(path, content);
        else if (write) File.WriteAllText(path, JsonSerializer.Serialize(
            new RuntimeDescriptor(pid, port, token, PryProtocol.Version, 9, DateTimeOffset.UtcNow),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        _directories.Add(directory);
        return directory;
    }

    public void Dispose()
    {
        foreach (var directory in _directories)
        {
            try { Directory.Delete(directory, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static async Task<int> ExitedProcessIdAsync()
    {
        var process = StartIdle("dotnet", "--version");
        var pid = process.Id;
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        return pid;
    }

    /// <summary>一个只负责"活着"的旁观进程：读取侧要能区分"进程还在"与"那个端口真在服务"。</summary>
    private sealed class IdleProcess : IDisposable
    {
        private readonly Process _process;

        private IdleProcess(Process process) => _process = process;

        public static IdleProcess Start() => new(OperatingSystem.IsWindows()
            ? StartIdle("cmd", "/c ping -n 300 -w 1000 127.0.0.1")
            : StartIdle("/bin/sh", "-c \"sleep 300\""));

        public int Id => _process.Id;

        public void Dispose()
        {
            try { _process.Kill(entireProcessTree: true); }
            catch { /* 已经自己退出的旁观进程无需处理 */ }
            _process.Dispose();
        }
    }

    private static Process StartIdle(string fileName, string arguments) =>
        Start(new ProcessStartInfo(fileName, arguments) { UseShellExecute = false, CreateNoWindow = true });

    private static Process Start(ProcessStartInfo startInfo) =>
        Process.Start(startInfo) ?? throw new InvalidOperationException($"无法启动 {startInfo.FileName}。");
}

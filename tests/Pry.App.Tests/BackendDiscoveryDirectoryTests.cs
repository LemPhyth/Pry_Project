using System.Collections.Concurrent;
using System.Diagnostics;
using Pry.Api;
using Pry.Api.Services;
using Pry.App.Services;
using Pry.Client;
using Pry.Contracts;
using Xunit;

namespace Pry.App.Tests;

/// <summary>
/// F-1：描述符的<b>写侧</b>认 <c>Pry:DataDirectory</c>，<b>读侧</b>曾经是 <c>App.axaml.cs</c> 里的一处无参调用，
/// 于是只认默认目录。分叉的后果不是启动报错而是静默退化：界面看不见那个已在跑的后端，转而起第二个
/// 内嵌宿主去争同一份数据目录；走查 ③ 要的"断一次后端"也就无法在隔离目录里注入。
/// <para>
/// 这里判的性质只有一句：<b>只共享环境变量、不共享命令行的两个进程，必须落在同一个目录上</b>。
/// 读侧走界面用的那个入口（<see cref="BackendLocator"/>），写侧走真起的 <c>Pry.Api</c> 子进程，
/// 两边都不许拿临时目录字符串自证——那正是缺陷当时的形状。
/// </para>
/// <para>
/// 为什么在 Pry.App.Tests 而不是 Api.Tests：本汇编整串行为（<c>AssemblyInfo</c> 关掉并行），
/// 临时改宿主进程的环境变量不会漏进别的用例自建的配置根；而写侧的 argv 只抵达被启动的那个进程，
/// 所以子进程那份配置根必须靠环境变量传，不能靠 <c>--Pry:DataDirectory</c>。
/// </para>
/// </summary>
public sealed class BackendDiscoveryDirectoryTests
{
    [Fact]
    public async Task Configured_directory_lands_the_writer_and_the_reader_on_the_same_place()
    {
        var log = TestContext.Current.TestOutputHelper;
        var directory = Path.Combine(Path.GetTempPath(), $"pry-configured-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var apiDll = typeof(BackendApplication).Assembly.Location;
        Assert.True(File.Exists(apiDll), $"未找到后端程序集：{apiDll}");

        Process? child = null;
        var console = new Tail();
        var previousData = Environment.GetEnvironmentVariable("Pry__DataDirectory");
        var previousModels = Environment.GetEnvironmentVariable("Pry__ModelsDirectory");
        try
        {
            Environment.SetEnvironmentVariable("Pry__DataDirectory", directory);
            // 模型目录一起给：少了它子进程会去扫真实用户的资源目录，一次判据不该碰别人的机器。
            Environment.SetEnvironmentVariable("Pry__ModelsDirectory", Path.Combine(directory, "models"));

            // 读侧：界面用的就是这个入口，配置根也只由它自己建（界面不传目录、不传 argv）。
            var hostBuilder = BackendApplication.CreateHostBuilder();
            var resolved = BackendLocator.ResolveDataDirectory(hostBuilder);
            Assert.Equal(Path.GetFullPath(directory), resolved);
            // 同一个配置根下，写侧的解析点给出同一个值：两句话分别是修复前后各会失配的那一半。
            Assert.Equal(resolved, RuntimeDescriptorService.ResolveDataDirectory(hostBuilder.Configuration));

            child = StartBackend(apiDll);
            child.OutputDataReceived += (_, e) => console.Add(e.Data);
            child.ErrorDataReceived += (_, e) => console.Add(e.Data);
            child.BeginOutputReadLine();
            child.BeginErrorReadLine();

            RuntimeDescriptor? descriptor = null;
            var deadline = DateTime.UtcNow.AddSeconds(60);
            while (DateTime.UtcNow < deadline)
            {
                TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
                descriptor = RuntimeDiscovery.TryRead(resolved);
                if (descriptor is not null) break;
                if (child.HasExited)
                    Assert.Fail($"后端进程在写出发现文件前退出（{child.ExitCode}）。输出：\n{console}");
                await Task.Delay(250, TestContext.Current.CancellationToken);
            }

            Assert.NotNull(descriptor);
            Assert.Equal(child.Id, descriptor!.Pid);
            Assert.NotEqual(5078, descriptor.Port);
            Assert.True(File.Exists(Path.Combine(directory, PryProtocol.RuntimeDescriptorFileName)),
                "读侧解析到的目录里没有描述符，说明写侧落去了别处。");

            // 界面那条路径真的连上了：这一段不是"文件在不在"，是带着文件里的端口与令牌发过请求。
            using var discovered = await BackendLocator.TryConnectAsync(hostBuilder, TestContext.Current.CancellationToken);
            Assert.NotNull(discovered);
            Assert.Equal(descriptor.Port, discovered!.BaseAddress!.Port);
            var info = await new PryBackendClient(discovered).GetProtocolInfoAsync(TestContext.Current.CancellationToken);
            Assert.Equal(PryProtocol.Version, info.ProtocolVersion);

            log?.WriteLine($"配置目录 {directory} → 端口 {descriptor.Port}（令牌长度 {descriptor.Token.Length}，值不打印）");
        }
        finally
        {
            Environment.SetEnvironmentVariable("Pry__DataDirectory", previousData);
            Environment.SetEnvironmentVariable("Pry__ModelsDirectory", previousModels);
            TryKill(child);
            TryDelete(directory);
        }
    }

    [Fact]
    public void Without_configuration_both_sides_take_the_one_shared_default()
    {
        Environment.SetEnvironmentVariable("Pry__DataDirectory", null);
        var hostBuilder = BackendApplication.CreateHostBuilder();
        var readerSide = BackendLocator.ResolveDataDirectory(hostBuilder);
        var writerSide = RuntimeDescriptorService.ResolveDataDirectory(hostBuilder.Configuration);
        Assert.Equal(writerSide, readerSide);
        // 默认值也只有一处定义：读侧另写一份 %LOCALAPPDATA%\Pry... 时，这一句先红。
        Assert.Equal(PryProtocol.DefaultDataDirectory, readerSide);
    }

    /// <summary>子进程只带监听地址：数据目录与模型目录都靠环境变量越过进程边界，和界面看到的是同一份来源。</summary>
    private static Process StartBackend(string apiDll)
    {
        var startInfo = new ProcessStartInfo("dotnet", $"exec \"{apiDll}\" --Pry:Url=http://127.0.0.1:0")
        {
            // 工作目录停在后端自己的输出目录：宿主在那里找得到配置与依赖，测试的 bin 目录不是它的世界。
            WorkingDirectory = Path.GetDirectoryName(apiDll),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        return Process.Start(startInfo) ?? throw new InvalidOperationException("无法启动后端进程。");
    }

    private static void TryKill(Process? process)
    {
        if (process is null) return;
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            process.WaitForExit(5_000);
        }
        catch { /* 测试收尾不因为清理失败而误判产品行为 */ }
    }

    private static void TryDelete(string directory)
    {
        try { Directory.Delete(directory, recursive: true); }
        catch (IOException) { /* 后端进程退出前的文件句柄：留在临时目录里无害 */ }
        catch (UnauthorizedAccessException) { }
    }

    /// <summary>子进程输出必须有界保留：重定向的管道没人读会把后端堵死，全量留在内存里又会撑爆测试宿主。</summary>
    private sealed class Tail
    {
        private const int MaximumLines = 200;
        private readonly ConcurrentQueue<string> _lines = new();

        public void Add(string? line)
        {
            if (line is null) return;
            _lines.Enqueue(line);
            while (_lines.Count > MaximumLines && _lines.TryDequeue(out _)) { }
        }

        public override string ToString() => string.Join('\n', _lines.ToArray());
    }
}

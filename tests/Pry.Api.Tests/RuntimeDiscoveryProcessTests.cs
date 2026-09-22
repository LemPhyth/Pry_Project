using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Pry.Api;
using Pry.Client;
using Pry.Contracts;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// PLAT-005 M0-d 的客户端半片（场景 <c>PLT-003</c>）。服务端写文件那一半由 <see cref="SessionTokenTests"/> 判；
/// 这里判的是另一半：<b>另一个真进程只凭 <c>runtime.json</c> 就连上</b>——调用方在 <c>Pry.App</c> 之外，
/// 端口与令牌都从文件读来，代码里没有任何备用端口。
/// </summary>
public sealed class RuntimeDiscoveryProcessTests
{
    [Fact]
    public async Task Another_process_connects_with_only_the_discovery_file()
    {
        var log = TestContext.Current.TestOutputHelper;
        var directory = Path.Combine(Path.GetTempPath(), $"pry-discovered-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var apiDll = typeof(BackendApplication).Assembly.Location;
        Assert.True(File.Exists(apiDll), $"未找到后端程序集：{apiDll}");

        // 起的是真进程，不是同进程的第二个宿主：测试宿主自己不发这份令牌，也就没法在同进程里证明"只凭文件连上"。
        using var child = StartBackend(apiDll, directory);
        var console = new Tail();
        child.OutputDataReceived += (_, e) => console.Add(e.Data);
        child.ErrorDataReceived += (_, e) => console.Add(e.Data);
        child.BeginOutputReadLine();
        child.BeginErrorReadLine();

        try
        {
            RuntimeDescriptor? descriptor = null;
            var deadline = DateTime.UtcNow.AddSeconds(60);
            while (DateTime.UtcNow < deadline)
            {
                TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
                descriptor = RuntimeDiscovery.TryRead(directory);
                if (descriptor is not null) break;
                if (child.HasExited)
                    Assert.Fail($"后端进程在写出发现文件前退出（{child.ExitCode}）。输出：\n{console}");
                await Task.Delay(250, TestContext.Current.CancellationToken);
            }

            Assert.NotNull(descriptor);
            Assert.Equal(child.Id, descriptor!.Pid);
            // 端口由 Kestrel 自己选：文件里那个数字不可能是硬编码入口，否则这条判据等于自证。
            Assert.NotEqual(5078, descriptor.Port);

            using var discovered = await RuntimeDiscovery.TryConnectAsync(directory, TestContext.Current.CancellationToken);
            Assert.NotNull(discovered);
            Assert.Equal(descriptor.Port, discovered!.BaseAddress!.Port);
            // 与内嵌装配点同一条自查：这个客户端会被界面拿去做语音识别，兜底必须宽于逐请求预算的最宽一档。
            Assert.True(discovered.Timeout > TimeSpan.FromMilliseconds(PryProtocol.TranscriptionRequestTimeoutMs),
                $"发现客户端的兜底被收成 {discovered.Timeout.TotalSeconds:0.#} 秒，长请求会被它先判掉。");

            var client = new PryBackendClient(discovered);
            var info = await client.GetProtocolInfoAsync(TestContext.Current.CancellationToken);
            Assert.Equal(PryProtocol.Version, info.ProtocolVersion);
            Assert.Contains("runtime-discovery", info.Features);
            // 受令牌门保护的端点真的读到了数据——"连上"不是靠免鉴权豁免面凑出来的。
            Assert.NotNull(await client.GetConversationsAsync(token: TestContext.Current.CancellationToken));

            using var anonymous = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{descriptor.Port}/") };
            using var denied = await anonymous.GetAsync("api/v1/conversations", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);

            log?.WriteLine($"发现的入口：127.0.0.1:{descriptor.Port}（令牌长度 {descriptor.Token.Length}，值不打印）");
        }
        finally
        {
            TryKill(child);
            TryDelete(directory);
        }
    }

    private static Process StartBackend(string apiDll, string dataDirectory)
    {
        // 三个目录/地址参数全部显式给出：少了 ModelsDirectory，子进程就会去读真实用户的 LocalAppData。
        var startInfo = new ProcessStartInfo("dotnet",
            $"exec \"{apiDll}\" --Pry:Url=http://127.0.0.1:0 --Pry:DataDirectory=\"{dataDirectory}\" " +
            $"--Pry:ModelsDirectory=\"{Path.Combine(dataDirectory, "models")}\"")
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

    private static void TryKill(Process process)
    {
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

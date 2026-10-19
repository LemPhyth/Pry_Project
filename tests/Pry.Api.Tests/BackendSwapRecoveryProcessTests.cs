using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Pry.Api;
using Pry.Client;
using Pry.Contracts;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// <c>DEF-P1WALK-05</c> 判据③：走查 ③ 的"断一次后端"要能在真进程上判。
/// 这里判的<b>不是"读到了新文件"</b>，而是那个活下来的客户端实例自己接回来了——
/// 同端口换一个令牌重起，旧钥匙的那一声 401 之后，同一笔请求重发要拿到 200，
/// 而且界面真的能往后端递一句话并得到回执。中间的读文件、换入口、重发全都不由调用方插手
/// （<c>Pry.App</c> 里没有任何一行会解析 <c>runtime.json</c>，那是 F-1 三条源码门钉住的形状）。
/// <para>
/// 落在 <c>Pry.Api.Tests</c> 而不是 <c>Pry.Client.Tests</c>：客户端测试项目引用不到后端，
/// 起不了真进程；同一条发现链路的另一半（只凭文件连上）在 <see cref="RuntimeDiscoveryProcessTests"/>。
/// </para>
/// </summary>
public sealed class BackendSwapRecoveryProcessTests
{
    [Fact]
    public async Task The_same_client_answers_again_after_the_backend_is_replaced_on_its_port()
    {
        var log = TestContext.Current.TestOutputHelper;
        var token = TestContext.Current.CancellationToken;
        var directory = Path.Combine(Path.GetTempPath(), $"pry-swap-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var apiDll = typeof(BackendApplication).Assembly.Location;
        Assert.True(File.Exists(apiDll), $"未找到后端程序集：{apiDll}");

        Process? child = null;
        var console = new Tail();
        try
        {
            child = StartBackend(apiDll, directory, port: 0, console);
            var first = await ReadDescriptorAsync(directory, child, console, "第一次启动");
            Assert.NotEqual(5078, first.Port);

            using var http = await RuntimeDiscovery.TryConnectAsync(directory, token);
            Assert.NotNull(http);
            // 此后不再换任何对象：判据要的就是这同一个实例（连它身上的 PryBackendClient）自己接回来。
            var client = new PryBackendClient(http);
            var restorations = 0;
            client.SessionRestored = () => Interlocked.Increment(ref restorations);
            var room = await client.CreateConversationAsync(null, token);

            // 令牌只按摘要比对，值不进日志、也不进断言文本。
            var staleFingerprint = Fingerprint(first.Token);
            StopBackend(child);

            child = StartBackend(apiDll, directory, port: first.Port, console);
            var second = await ReadDescriptorAsync(directory, child, console, "同端口重起");
            Assert.Equal(first.Port, second.Port);
            // 正向探针：这个场景确实换了钥匙。两枚令牌同摘要时后面那三段断言全都等于自证。
            Assert.NotEqual(staleFingerprint, Fingerprint(second.Token));

            var rooms = await client.GetConversationsAsync(token: token);
            Assert.Contains(room.Id, rooms.Select(item => item.Id));
            Assert.Equal(1, restorations);

            var receipt = await client.SubmitTurnAsync(room.Id,
                new SubmitTurnRequest("换后端之后仍要说上话"), token);
            Assert.True(receipt.MessageId > 0, "换后端之后这一笔没有落到后端的消息表上。");
            Assert.Equal(1, restorations);

            log?.WriteLine($"换前/换后入口同为 127.0.0.1:{second.Port}（旧令牌 {staleFingerprint}，"
                + $"新令牌 {Fingerprint(second.Token)}，长度 {second.Token.Length}，值不打印）");
        }
        finally
        {
            StopBackend(child);
            TryDelete(directory);
        }
    }

    /// <summary>等到写侧真发布了一个可用入口：读侧只认还活着的 pid，所以这里只能等，不能猜。</summary>
    private static async Task<RuntimeDescriptor> ReadDescriptorAsync(string directory, Process child,
        Tail console, string because)
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            var descriptor = RuntimeDiscovery.TryRead(directory);
            if (descriptor is not null && descriptor.Pid == child.Id) return descriptor;
            if (child.HasExited)
                Assert.Fail($"后端进程在写出可用的发现文件前退出（{child.ExitCode}），阶段：{because}。输出：\n{console}");
            await Task.Delay(250, CancellationToken.None);
        }
        throw new InvalidOperationException($"等待{because}的描述符超时。输出：\n{console}");
    }

    private static Process StartBackend(string apiDll, string dataDirectory, int port, Tail console)
    {
        // 三个目录/地址参数全部显式给出：少了 ModelsDirectory，子进程就会去读真实用户的 LocalAppData。
        // 端口给 0 是让 Kestrel 自己选，重起时改成第一次那个号码——"同端口"是判据①的前半，不能含糊。
        var startInfo = new ProcessStartInfo("dotnet",
            $"exec \"{apiDll}\" --Pry:Url=http://127.0.0.1:{port} --Pry:DataDirectory=\"{dataDirectory}\" " +
            $"--Pry:ModelsDirectory=\"{Path.Combine(dataDirectory, "models")}\"")
        {
            // 工作目录停在后端自己的输出目录：宿主在那里找得到配置与依赖，测试的 bin 目录不是它的世界。
            WorkingDirectory = Path.GetDirectoryName(apiDll),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        var child = Process.Start(startInfo) ?? throw new InvalidOperationException("无法启动后端进程。");
        child.OutputDataReceived += (_, e) => console.Add(e.Data);
        child.ErrorDataReceived += (_, e) => console.Add(e.Data);
        child.BeginOutputReadLine();
        child.BeginErrorReadLine();
        return child;
    }

    /// <summary>不等优雅退出：走查要判的就是"协程被掐断之后"，而优雅退出会顺手把描述符删掉。</summary>
    private static void StopBackend(Process? process)
    {
        if (process is null) return;
        try
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            process.WaitForExit(10_000);
        }
        catch { /* 测试收尾不因为清理失败而误判产品行为 */ }
        process.Dispose();
    }

    /// <summary>令牌的指纹：长度加摘要前八位，够比对又不把它交出去。</summary>
    private static string Fingerprint(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return $"{value.Length}:{Convert.ToHexString(hash.AsSpan(0, 4)).ToLowerInvariant()}";
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

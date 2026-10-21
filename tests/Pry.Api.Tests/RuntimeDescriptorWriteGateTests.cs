using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Pry.Api;
using Pry.Api.Services;
using Pry.Contracts;
using Pry.Core.Memory;
using Pry.Core.Narrative;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// DEF-WRIGATE-01（契约 §10.2 第 2 条）：第二个宿主在数据目录的进程级独占写锁前必须**停下**，
/// 不是"记一条警告然后照样覆盖"。判据全部落在行为上——
/// ① 同目录两个宿主：第二个 <c>StartAsync</c> 失败，且第一个的 <c>runtime.json</c> 一字节未变（Pid/Port/Token 仍是前者的）；
/// ② 第二个被拒时描述文件可以根本不存在——拒绝路径同样不许凭空写出它；
/// ③ 锁随进程释放：第一个宿主优雅退出后，后继实例拿到写权、描述文件照写、优雅退出照删（单宿主正常路径行为不变）；
/// ④ 退出清理只删自己写的那份（既有 <c>Clear()</c> 语义不许被削弱）。
/// 一律用临时目录；真实用户目录（%LOCALAPPDATA%/PryCompanion）不是本文件的测试载体。
/// </summary>
public sealed class RuntimeDescriptorWriteGateTests
{
    [Fact]
    public async Task Second_host_on_the_same_directory_refuses_startup_without_touching_the_first_descriptor()
    {
        var directory = NewDirectory();
        var descriptorPath = Path.Combine(directory, PryProtocol.RuntimeDescriptorFileName);
        try
        {
            var first = await StartAsync(directory);
            await using (first)
            {
                var descriptor = await ReadDescriptorAsync(directory);
                var bytesBefore = await File.ReadAllBytesAsync(descriptorPath, TestContext.Current.CancellationToken);
                Assert.True(File.Exists(Path.Combine(directory, DataDirectoryWriteLease.LockFileName)),
                    "独占锁句柄应落在数据目录内");

                // 第二个宿主：同一份数据目录，另一个真 WebApplication 实例（自己的租约、自己的锁申请）。
                var second = await BackendApplication.BuildAsync(configure: builder => Configure(builder, directory),
                    token: TestContext.Current.CancellationToken);
                DataDirectoryWriteLockedException refused;
                try
                {
                    refused = await Assert.ThrowsAsync<DataDirectoryWriteLockedException>(
                        () => second.StartAsync(TestContext.Current.CancellationToken));
                }
                finally
                {
                    await second.DisposeAsync();
                }

                // 拒绝必须走可呈现链（PryErrorPresenter 的 IUserPresentableError 那一档），不是裸异常也不是只进日志。
                IUserPresentableError presentable = refused;
                Assert.Equal("这个数据目录正被另一个 Pry 同时写", presentable.Headline);
                Assert.False(presentable.RetryAllowed);
                Assert.Contains("另一个 Pry", presentable.Body, StringComparison.Ordinal);
                Assert.Contains("不会改变结果", presentable.Body, StringComparison.Ordinal);
                Assert.Equal(NarrativeContractReason.WriteLockHeldByOther, refused.Reason);

                // 行为判据本体：第二个宿主没有碰第一个的描述文件——字节级不变，Pid/Port/Token 仍是前者的。
                var bytesAfter = await File.ReadAllBytesAsync(descriptorPath, TestContext.Current.CancellationToken);
                Assert.Equal(bytesBefore, bytesAfter);
                var reread = JsonSerializer.Deserialize<RuntimeDescriptor>(
                    File.ReadAllText(descriptorPath), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
                Assert.Equal(descriptor.Pid, reread.Pid);
                Assert.Equal(descriptor.Port, reread.Port);
                Assert.Equal(descriptor.Token, reread.Token);
                // 拒绝路径没留下任何写入痕迹：没有临时文件、没有第二把锁。
                Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
            }
        }
        finally
        {
            Cleanup(directory);
        }
    }

    [Fact]
    public async Task Refusal_with_no_descriptor_yet_writes_none_and_a_successor_gets_the_lease_after_the_lock_is_released()
    {
        var directory = NewDirectory();
        var descriptorPath = Path.Combine(directory, PryProtocol.RuntimeDescriptorFileName);
        var lockPath = Path.Combine(directory, DataDirectoryWriteLease.LockFileName);
        try
        {
            // 模拟"别的活进程已持有独占句柄"：另一枚 FileShare.None 句柄（锁对文件句柄生效，与谁持有同源）。
            using var foreignHolder = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            Assert.False(File.Exists(descriptorPath));

            var blocked = await BackendApplication.BuildAsync(configure: builder => Configure(builder, directory),
                token: TestContext.Current.CancellationToken);
            try
            {
                await Assert.ThrowsAsync<DataDirectoryWriteLockedException>(
                    () => blocked.StartAsync(TestContext.Current.CancellationToken));
            }
            finally
            {
                await blocked.DisposeAsync();
            }

            // 拿不到写权 ⇒ 描述文件"不存在或仍是前者的"：这里它必须仍然不存在，也没有半截临时名。
            Assert.False(File.Exists(descriptorPath));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));

            // 锁随持有者释放：后继实例必须能正常取得写权——拒绝不许把入口永久焊死。
            foreignHolder.Dispose();
            await using var successor = await StartAsync(directory);
            var descriptor = await ReadDescriptorAsync(directory);
            Assert.Equal(Environment.ProcessId, descriptor.Pid);
            Assert.Equal(new Uri(successor.BoundUrl()).Port, descriptor.Port);
        }
        finally
        {
            Cleanup(directory);
        }
    }

    [Fact]
    public async Task Graceful_exit_clears_the_descriptor_and_the_next_host_can_write_its_own()
    {
        // 单宿主正常路径行为不变：描述文件照写、优雅退出照删；锁不跨进程存活，后继实例照常起步。
        var directory = NewDirectory();
        var descriptorPath = Path.Combine(directory, PryProtocol.RuntimeDescriptorFileName);
        try
        {
            var first = await StartAsync(directory);
            var firstPort = new Uri(first.BoundUrl()).Port;
            await first.StopAsync(TestContext.Current.CancellationToken);
            await first.DisposeAsync();
            Assert.False(File.Exists(descriptorPath));

            await using var second = await StartAsync(directory);
            var descriptor = await ReadDescriptorAsync(directory);
            Assert.Equal(new Uri(second.BoundUrl()).Port, descriptor.Port);
            Assert.NotEqual(firstPort, descriptor.Port);
        }
        finally
        {
            Cleanup(directory);
        }
    }

    [Fact]
    public async Task Exit_cleanup_never_deletes_a_descriptor_owned_by_a_successor()
    {
        // Clear() 的"只删自己写的那份"不许被削弱：文件已被后继覆盖（端口不同）时，退出清理必须跳过。
        var directory = NewDirectory();
        var descriptorPath = Path.Combine(directory, PryProtocol.RuntimeDescriptorFileName);
        try
        {
            await using var app = await StartAsync(directory);
            await ReadDescriptorAsync(directory);

            var successor = new RuntimeDescriptor(Environment.ProcessId, 54321, "successor-entry",
                PryProtocol.Version, MemoryDatabase.SchemaVersion, DateTimeOffset.UtcNow);
            File.WriteAllText(descriptorPath,
                JsonSerializer.Serialize(successor, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

            await app.StopAsync(TestContext.Current.CancellationToken);

            Assert.True(File.Exists(descriptorPath), "退出清理不得删掉后继实例写的描述文件");
            var current = JsonSerializer.Deserialize<RuntimeDescriptor>(
                File.ReadAllText(descriptorPath), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            Assert.Equal("successor-entry", current.Token);
        }
        finally
        {
            Cleanup(directory);
        }
    }

    private static string NewDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pry-write-gate-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void Configure(WebApplicationBuilder builder, string directory)
    {
        builder.Configuration["Pry:DataDirectory"] = directory;
        builder.Configuration["Pry:ModelsDirectory"] = Path.Combine(directory, "models");
        builder.Configuration["Pry:Url"] = "http://127.0.0.1:0";
        builder.Logging.ClearProviders();
    }

    private static async Task<WebApplication> StartAsync(string directory)
    {
        var app = await BackendApplication.BuildAsync(configure: builder => Configure(builder, directory),
            token: TestContext.Current.CancellationToken);
        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    private static async Task<RuntimeDescriptor> ReadDescriptorAsync(string directory)
    {
        var path = Path.Combine(directory, PryProtocol.RuntimeDescriptorFileName);
        for (var attempt = 0; attempt < 250 && !File.Exists(path); attempt++)
            await Task.Delay(20, TestContext.Current.CancellationToken);
        Assert.True(File.Exists(path), $"{PryProtocol.RuntimeDescriptorFileName} 未在后端启动后写出");
        return JsonSerializer.Deserialize<RuntimeDescriptor>(
            await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }

    private static void Cleanup(string directory)
    {
        SqliteTestPools.Clear(Path.Combine(directory, "memory.db"));
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
}

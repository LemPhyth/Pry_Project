using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Pry.Api;
using Pry.Api.Services;
using Pry.Contracts;
using Pry.Core.Memory;
using Pry.Core.Models;
using Pry.Core.Narrative;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// DEF-WRIGATE-01（契约 §10.2 第 2 条）：第二个宿主在数据目录的进程级独占写锁前必须**停下**，
/// 不是"记一条警告然后照样覆盖"。判据全部落在行为上——
/// ① 同目录两个宿主：第二个在 <c>BuildAsync</c> 就失败（DEF-WRILEASE-ORDER-01 把拒绝点提前到第一次写盘
///    之前，不再晚到 <c>StartAsync</c>），且第一个的 <c>runtime.json</c> 一字节未变（Pid/Port/Token 仍是前者的）；
/// ② 第二个被拒时描述文件可以根本不存在——拒绝路径同样不许凭空写出它；
/// ③ 锁随进程释放：第一个宿主优雅退出后，后继实例拿到写权、描述文件照写、优雅退出照删（单宿主正常路径行为不变）；
/// ④ 退出清理只删自己写的那份（既有 <c>Clear()</c> 语义不许被削弱）；
/// ⑤ 被拒的那一次连共享库都不许碰：第一个实例排好、还没发出去的那条消息（<c>reply_plans</c> 的
///    <c>open</c> 计划与指向它的 <c>planned_messages</c> 的 <c>pending</c> 行）状态一字不变——
///    这条判的是"拒绝发生在写库之前"本身，只判"抛出异常"的话清扫留在原位也一样绿。
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

                // 第二个宿主：同一份数据目录，另一个真 BuildAsync（自己的租约、自己的锁申请）。
                // DEF-WRILEASE-ORDER-01 把拒绝点提前到 BuildAsync——第一次写盘之前，不再是 StartAsync。
                // 失败路径上 BuildAsync 自己拆掉已经建好的容器，独占句柄不会留在本测试进程里。
                var refused = await Assert.ThrowsAsync<DataDirectoryWriteLockedException>(async () =>
                {
                    await using var second = await BackendApplication.BuildAsync(
                        configure: builder => Configure(builder, directory),
                        token: TestContext.Current.CancellationToken);
                });

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

            // DEF-WRILEASE-ORDER-01 之后拒绝点在 BuildAsync 里（第一次写盘之前），不再是 StartAsync。
            await Assert.ThrowsAsync<DataDirectoryWriteLockedException>(async () =>
            {
                await using var blocked = await BackendApplication.BuildAsync(
                    configure: builder => Configure(builder, directory),
                    token: TestContext.Current.CancellationToken);
            });

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
    public async Task A_refused_second_host_leaves_the_first_instances_open_plan_and_pending_message_untouched()
    {
        // DEF-WRILEASE-ORDER-01 的行为判据本体。缺陷形状：取锁排在第一次写盘之后，于是第二个进程
        // 在被拒之前已经跑过 InitializeAsync（PRAGMA journal_mode=WAL）与那次遗留计划清扫——
        // 用户看见的是"先开那个实例排好、还没发出去的一条消息被后开的取消掉"，而第一个进程毫无通知。
        // 两半都要判：①那两行的状态一字没变（修复前红在这里：两行都成了 cancelled）；
        // ②第二个进程确实被写锁拒掉。只判 ② 的话，把清扫留在原位也一样绿，那不算判据。
        var directory = NewDirectory();
        var token = TestContext.Current.CancellationToken;
        try
        {
            // 第一个实例：真起一次（走 StartAsync 辅助方法，它会写完自己的描述文件）。
            var first = await StartAsync(directory);
            await using (first)
            {
                // 遗留状态由 MemoryDatabase 直接写进第一个实例的库（不经 HTTP）：
                // reply_plans 一条 status='open'，planned_messages 一条 state='pending' 且指向它。
                var database = new MemoryDatabase(Path.Combine(directory, "memory.db"));
                var conversationId = $"pry-write-gate-{Guid.NewGuid():N}";
                var planId = await database.CreateReplyPlanAsync(conversationId,
                    [new PlannedReplyMessage
                    {
                        Type = ReplyMessageType.Text, Content = "排好了还没发出去的那一句", Sequence = 1
                    }], token);

                var planRowBefore = await ReadRuntimeRowsAsync(directory,
                    "SELECT id, status FROM reply_plans WHERE id=$id ORDER BY id", planId, token);
                var queuedRowBefore = await ReadRuntimeRowsAsync(directory,
                    """
                    SELECT reply_plan_id, sequence, type, content, state, COALESCE(delivered_at, '')
                    FROM planned_messages WHERE reply_plan_id=$id ORDER BY reply_plan_id, sequence
                    """, planId, token);
                // 起点必须真的是"排好、待发"：否则后面那句"没变"可以靠一行根本不存在的记录自证。
                Assert.Equal($"{planId}|open", planRowBefore);
                Assert.Equal($"{planId}|1|text|排好了还没发出去的那一句|pending|", queuedRowBefore);

                // 第二个进程：同一个数据目录，真 BuildAsync。修复前这一句成功返回并把上面两行改成
                // cancelled（本片要的红灯）；修复后异常就从这一句冒出来，BuildAsync 在失败路径上
                // 自己拆掉已经建好的容器，独占句柄不会留在同一个测试进程里。
                Exception? refusal = null;
                WebApplication? second = null;
                try
                {
                    second = await BackendApplication.BuildAsync(
                        configure: builder => Configure(builder, directory), token: token);
                }
                catch (Exception failure)
                {
                    refusal = failure;
                }

                try
                {
                    // 第一半：那两行的读数与落库时逐字相同。
                    Assert.Equal(planRowBefore, await ReadRuntimeRowsAsync(directory,
                        "SELECT id, status FROM reply_plans WHERE id=$id ORDER BY id", planId, token));
                    Assert.Equal(queuedRowBefore, await ReadRuntimeRowsAsync(directory,
                        """
                        SELECT reply_plan_id, sequence, type, content, state, COALESCE(delivered_at, '')
                        FROM planned_messages WHERE reply_plan_id=$id ORDER BY reply_plan_id, sequence
                        """, planId, token));

                    // 第二半：第二个进程被拒，而且拒在那把独占写锁上，不是别的错。
                    var refused = Assert.IsType<DataDirectoryWriteLockedException>(refusal);
                    Assert.Equal(NarrativeContractReason.WriteLockHeldByOther, refused.Reason);
                }
                finally
                {
                    // 缺陷回来时的形状（BuildAsync 不再抛）也不许把第二枚容器留在进程里。
                    if (second is not null) await second.DisposeAsync();
                }
            }
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

    /// <summary>把那两行投递状态的原文读成一份可比对的读数（列以 | 连接、行以 ; 连接）。
    /// 只旁观不写库：<c>Pooling=False</c> 不留池内句柄，与高版本拒写门那批读法同族。</summary>
    private static async Task<string> ReadRuntimeRowsAsync(string directory, string sql, long planId,
        CancellationToken token)
    {
        await using var connection = new SqliteConnection(
            $"Data Source={Path.Combine(directory, "memory.db")};Pooling=False");
        await connection.OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$id", planId);
        await using var reader = await command.ExecuteReaderAsync(token);
        var rows = new List<string>();
        while (await reader.ReadAsync(token))
            rows.Add(string.Join('|', Enumerable.Range(0, reader.FieldCount)
                .Select(index => reader.IsDBNull(index) ? "" : reader.GetValue(index)?.ToString() ?? "")));
        Assert.NotEmpty(rows);
        return string.Join(';', rows);
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

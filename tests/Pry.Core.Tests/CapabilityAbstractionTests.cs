using Pry.Core.Abstractions;
using Xunit;

namespace Pry.Core.Tests;

/// <summary>
/// RT-007 第一期的两处"住在哪儿"。它们看着像风格，其实是契约落笔的条件：
/// 能力的请求与结果归 <c>Pry.Core.Abstractions</c>（docs/phase2-runtime-contract.md §7.1 结论 3、
/// docs/phase1-platform-baseline.md §5.1），插件的存储格必须真的只有它自己那一格。
/// 第一条判据在移动之前无处可钉——类型搬错地方不会让任何一条行为断言变红。
/// </summary>
public sealed class CapabilityAbstractionTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"pry-capability-box-{Guid.NewGuid():N}");

    public ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        return ValueTask.CompletedTask;
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public void The_capability_envelopes_live_in_abstractions_not_in_the_state_machine()
    {
        // 搬回 Pry.Core.Runtime 就红（10-19 实测：把两条 record 挪回状态机并补上搬运者必须补的三处 using
        // 之后，这一条是唯一的红点）。裸挪不动：三处消费者会先编译失败，所以这条判据钉的是"搬完还能编得过"的那一步。
        Assert.Equal("Pry.Core.Abstractions", typeof(CapabilityRequest).Namespace);
        Assert.Equal("Pry.Core.Abstractions", typeof(CapabilityResult).Namespace);
    }

    [Fact]
    public async Task A_plugin_writes_and_reads_inside_its_own_box()
    {
        var storage = new PluginStorage(Path.Combine(_root, "pry.desktop"));
        await storage.WriteTextAsync("notes/first.txt", "她学会的第一句", Token);
        Assert.Equal("她学会的第一句", await storage.ReadTextAsync("notes/first.txt", Token));
        Assert.True(File.Exists(Path.Combine(_root, "pry.desktop", "notes", "first.txt")));
    }

    [Theory]
    [InlineData("../escaped.txt")]
    [InlineData("..\\escaped.txt")]
    [InlineData("notes/../../escaped.txt")]
    [InlineData("./../escaped.txt")]
    public async Task A_relative_path_that_walks_out_of_the_box_is_refused(string relativePath)
    {
        var storage = new PluginStorage(Path.Combine(_root, "pry.desktop"));
        Assert.False(storage.TryResolvePath(relativePath, out _));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => storage.WriteTextAsync(relativePath, "x", Token));
        // 拒绝必须发生在落盘之前，而不是写完再报错：否则越界文件已经在那儿了。
        Assert.False(File.Exists(Path.Combine(_root, "escaped.txt")));
    }

    [Fact]
    public async Task An_absolute_path_is_not_a_plugin_relative_path()
    {
        var storage = new PluginStorage(Path.Combine(_root, "pry.desktop"));
        var elsewhere = Path.Combine(_root, "somewhere-else.txt");
        Assert.False(storage.TryResolvePath(elsewhere, out _));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => storage.WriteTextAsync(elsewhere, "x", Token));
        Assert.False(File.Exists(elsewhere));
        // 空串同样不算"自己格子里的一个文件"。
        Assert.False(storage.TryResolvePath("  ", out _));
    }

    [Fact]
    public async Task One_plugin_cannot_read_the_box_of_another()
    {
        var first = new PluginStorage(Path.Combine(_root, "pry.desktop"));
        var second = new PluginStorage(Path.Combine(_root, "other.plugin"));
        await first.WriteTextAsync("note.txt", "只属于第一格", Token);
        Assert.Null(await second.ReadTextAsync("note.txt", Token));
        Assert.Equal("只属于第一格", await first.ReadTextAsync("note.txt", Token));
    }

    [Fact]
    public async Task A_read_of_a_file_that_is_not_there_is_null_not_a_failure()
    {
        var storage = new PluginStorage(Path.Combine(_root, "missing.box"));
        Assert.Null(await storage.ReadTextAsync("nope.txt", Token));
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Pry.Api;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// QA-P2-03（场景 PLT-001）：迁移文档 §8 第 1 条把"只绑回环"记成"现有行为回归"，但 `tests/` 里没有任何用例进入
/// 那条分支——守卫存在，判据为零。这里补的就是那一半：非回环配置必须在**绑定之前**失败，且失败时不留产物。
/// 正向那一半（回环起得来、真的在监听）由 <c>SessionTokenTests</c> 与 <c>EmbeddedBackendEndpointTests</c> 的真实
/// Kestrel 用例覆盖；这里只配对一条"回环不被同一句话拒掉"，用来证明守卫不是无脑拒绝一切。
/// </summary>
public sealed class LoopbackBindingTests
{
    [Theory]
    [InlineData("http://0.0.0.0:5078")]
    [InlineData("http://10.0.0.5:5078")]
    [InlineData("http://example.com:5078")]
    // 配成空串不得"退回默认值偷偷起来"：配置写坏了就该拒绝启动，而不是换一个监听面继续跑。
    [InlineData("")]
    public async Task A_bind_address_outside_loopback_refuses_to_start(string url)
    {
        var directory = NewDirectory();
        try
        {
            var failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                BuildAsync(url, directory, TestContext.Current.CancellationToken));

            Assert.Contains("回环", failure.Message);
            // 拒绝必须发生在写库、建日志目录之前，否则"不许对外暴露"会变成"暴露失败但已经留下半套数据"。
            Assert.False(Directory.Exists(directory), "守卫在建立数据目录之后才拒绝，等于启动失败仍留下产物。");
        }
        finally { Delete(directory); }
    }

    [Fact]
    public async Task A_loopback_bind_address_is_not_refused_by_that_guard()
    {
        var directory = NewDirectory();
        try
        {
            await using var app = await BuildAsync("http://127.0.0.1:0", directory, TestContext.Current.CancellationToken);
            Assert.NotNull(app);
        }
        finally { Delete(directory); }
    }

    private static async Task<WebApplication> BuildAsync(string url, string directory, CancellationToken token) =>
        await BackendApplication.BuildAsync(configure: builder =>
        {
            builder.Configuration["Pry:Url"] = url;
            builder.Configuration["Pry:DataDirectory"] = directory;
            builder.Configuration["Pry:ModelsDirectory"] = Path.Combine(directory, "models");
            builder.Logging.ClearProviders();
        }, token: token);

    private static string NewDirectory() => Path.Combine(Path.GetTempPath(), $"pry-loopback-{Guid.NewGuid():N}");

    /// <summary>
    /// 先放掉自己那条池再删目录。本刀把别人的进程级清池去掉之后，这个 fixture 才第一次暴露出"从不清池"：
    /// 之前那份 <c>memory.db</c> 是被别的 collection 的 <c>ClearAllPools</c> 顺手放开的（DEF-P1WALK-04 的副作用面）。
    /// 下面吞掉 <c>IOException</c> 仍然保留——杀软扫刚关闭的文件是真的会让删除慢一拍，但因此"漏删"在套件里是静默的，
    /// 所以这一族的实际读数只能用临时目录计数，不能用"全绿"。
    /// </summary>
    private static void Delete(string directory)
    {
        SqliteTestPools.Clear(Path.Combine(directory, "memory.db"));
        try { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

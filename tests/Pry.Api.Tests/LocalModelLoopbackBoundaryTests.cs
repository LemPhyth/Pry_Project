using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Pry.Api.Services;
using Pry.Contracts;
using Pry.Core.Intelligence;
using Pry.Core.Memory;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// S1（工单 <c>PLAT-003-BACKUP-RESTORE</c> §2）：<c>local-llama</c> 这一档以前只判"是不是不含凭据的 HTTP(S) 绝对地址"，
/// 于是 <c>http://192.168.1.20:8080/v1</c> 一路通过——而这一档由后端进程自己拉起 llama.cpp、只绑回环，
/// 存下远程地址等于把"本地"当成一个装饰性标签，聊天内容明文出境。现在它必须是回环。
/// <para>
/// 三条判据合起来才收得住这一刀：越界必须被挡下（而且挡在 <c>baseUrl</c> 这个字段上，不是碰巧报在别的检查里），
/// 回环三形（<c>127.0.0.1</c>／<c>localhost</c>／<c>::1</c>）必须仍然存得下并真的落到偏好文件里，
/// <c>openai-compatible</c> 那一档的既有规则一字不许跟着动——把远程 HTTPS 也一起拒掉就是超范围修复。
/// </para>
/// </summary>
public sealed class LocalModelLoopbackBoundaryTests : IAsyncLifetime
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"pry-model-loopback-{Guid.NewGuid():N}");
    private string _databasePath = null!;
    private MemoryDatabase _database = null!;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_root);
        _databasePath = Path.Combine(_root, "memory.db");
        _database = new MemoryDatabase(_databasePath);
        await _database.InitializeAsync(Token);
    }

    public ValueTask DisposeAsync()
    {
        SqliteTestPools.Clear(_databasePath);
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        return ValueTask.CompletedTask;
    }

    private sealed record Host(ConfigurationManager Configuration, ModelProcessRegistry Registry,
        BackendRuntime Runtime, MediaAssetStore Media, ConversationSessionService Sessions) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Sessions.DisposeAsync();
            await Runtime.StopAsync(CancellationToken.None);
            await Registry.DisposeAsync();
        }
    }

    /// <summary>真宿主服务链：校验发生在 <c>ConfigurationApplicationService</c>，不是控制器或客户端那一层。</summary>
    private ConfigurationApplicationService Service(Host host) => new(host.Configuration, host.Runtime, host.Sessions,
        host.Media, _database,
        new CharacterLearningCleanup(new SqliteCharacterStateRepository(_database),
            new SqliteBehaviorPatternRepository(_database), new SqliteBehaviorExperienceRepository(_database),
            new SqliteExpressionPatternRepository(_database)));

    private async Task<Host> StartHostAsync()
    {
        var configuration = new ConfigurationManager();
        configuration["Pry:DataDirectory"] = _root;
        var media = new MediaAssetStore(configuration);
        var registry = new ModelProcessRegistry(NullLogger<ModelProcessRegistry>.Instance);
        var runtime = new BackendRuntime(configuration, registry, NullLogger<BackendRuntime>.Instance);
        await runtime.StartAsync(Token);
        var sessions = new ConversationSessionService(_database, runtime, media,
            NullLogger<ConversationSessionService>.Instance);
        return new Host(configuration, registry, runtime, media, sessions);
    }

    /// <summary>写本地档必须带一个真存在的 GGUF（只验文件头四字节），所以先落一个临时文件。</summary>
    private string NewGguf(string name)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllBytes(path, [.. System.Text.Encoding.ASCII.GetBytes("GGUF"), .. new byte[64]]);
        return path;
    }

    private SaveCustomModelRequest Local(string url, string ggufName) => new("本地档模型", "local-llama",
        "local-model", url, NewGguf(ggufName), null, new ModelCapabilities(), 4096, 256, .5, 0, "cpu", false);

    [Fact]
    public async Task A_local_llama_profile_refuses_a_remote_endpoint()
    {
        await using var host = await StartHostAsync();
        var service = Service(host);

        // LAN 明文是这一刀要堵的那一条；HTTPS 远程一并拒——这一档由进程自己拉起服务，放行加密远端只会让人以为可以配远端。
        foreach (var url in new[] { "http://192.168.1.20:8080/v1", "http://model.example.com:8080/v1",
                                    "https://llama.example.com/v1" })
        {
            var error = await Assert.ThrowsAsync<ApiValidationException>(() =>
                service.CreateCustomModelAsync(Local(url, $"refused-{url.Length}.gguf"), Token));
            // 字段必须是 baseUrl：拦在别处（比如 localModelPath）说明 URL 那道门根本没执行。
            Assert.Equal("baseUrl", error.Field);
            Assert.Contains("回环", error.Message, StringComparison.Ordinal);
        }

        Assert.DoesNotContain(service.GetModels(), x => x.Custom && x.DisplayName == "本地档模型");
    }

    [Fact]
    public async Task Every_loopback_form_of_a_local_llama_profile_still_saves()
    {
        await using var host = await StartHostAsync();
        var service = Service(host);

        foreach (var url in new[] { "http://127.0.0.1:8080/v1", "http://localhost:8080/v1", "http://[::1]:8080/v1" })
        {
            var saved = await service.CreateCustomModelAsync(Local(url, $"loopback-{url.Length}.gguf"), Token);
            Assert.True(saved.Custom);
        }

        // "存得下"要看到落盘那一步：界面拿不到 baseUrl（响应里不返回地址），所以直接核对偏好文件。
        var preferences = await File.ReadAllTextAsync(Path.Combine(_root, "preferences.json"), Token);
        Assert.Contains("127.0.0.1", preferences, StringComparison.Ordinal);
        Assert.Contains("localhost", preferences, StringComparison.Ordinal);
        Assert.Contains("[::1]", preferences, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_network_lane_keeps_its_own_rules_untouched()
    {
        await using var host = await StartHostAsync();
        var service = Service(host);

        // 收紧只许落在本地档：远程 HTTPS 仍然放行，回环 HTTP 仍然放行，远程 HTTP 仍然按原句拒。
        var remote = await service.CreateNetworkModelAsync(new SaveNetworkModelRequest("远程接口", "remote-model",
            "https://models.example/v1", new ModelCapabilities(), 4096, 256, .5, 0, "remote", false, null), Token);
        Assert.Equal("network", remote.Source);
        var loopback = await service.CreateNetworkModelAsync(new SaveNetworkModelRequest("本机接口", "local-model",
            "http://127.0.0.1:19996/v1", new ModelCapabilities(), 4096, 256, .5, 0, "cpu", false, null), Token);
        Assert.True(loopback.Custom);
        await Assert.ThrowsAsync<ApiValidationException>(() => service.CreateNetworkModelAsync(
            new SaveNetworkModelRequest("不安全接口", "plain-model", "http://example.com/v1",
                new ModelCapabilities(), 4096, 256, .5, 0, "remote", false, null), Token));
    }
}

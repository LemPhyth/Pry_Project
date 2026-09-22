using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pry.Api;
using Pry.Api.Services;
using Pry.Contracts;
using Pry.Core.Memory;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// PLAT-005 M0-d：默认拒绝的会话令牌门，与 <c>runtime.json</c> 的进程发现。
/// 这一层的判据是"漏登记会被挡住"，所以豁免面按整张端点表反证，而不是逐个点名受保护端点。
/// </summary>
public sealed class SessionTokenTests
{
    /// <summary>白名单的独立声明。实现里若多豁免一条，端点遍历就会在这里失败。</summary>
    private static readonly string[] ExemptPaths = ["/health", "/api/v1/info"];

    [Fact]
    public async Task Every_registered_endpoint_outside_the_two_exempt_paths_requires_a_token()
    {
        var logs = new List<string>();
        var directory = NewDirectory();
        try
        {
            await using var app = await StartAsync(directory, logs);
            var inventory = Inventory(app);
            Assert.True(inventory.Count >= 25,
                $"端点遍历只取到 {inventory.Count} 条，判据已退化成自说自话：{string.Join(", ", inventory.Select(x => x.Method + " " + x.Path))}");
            // 遍历必须真的覆盖到豁免面，否则"其余全部 401"可以靠漏掉端点来凑数。
            foreach (var exempt in ExemptPaths)
                Assert.Contains(inventory, item => item.Path == exempt);
            using var anonymous = new HttpClient { BaseAddress = new Uri(app.BoundUrl() + "/") };

            var rejected = 0;
            foreach (var (method, path) in inventory)
            {
                if (IsExempt(method, path))
                {
                    using var allowed = await anonymous.GetAsync(path, TestContext.Current.CancellationToken);
                    Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
                    continue;
                }
                using var request = new HttpRequestMessage(new HttpMethod(method), path);
                using var response = await anonymous.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                    TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Contains("urn:pry:error:unauthorized",
                    await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
                rejected++;
            }
            Assert.True(rejected >= 20, $"只有 {rejected} 条端点被门禁覆盖，遍历面不完整");
        }
        finally
        {
            Cleanup(directory);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-token!!!")]
    [InlineData("c2hvcnQ=")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    public async Task A_wrong_token_answers_exactly_like_no_token(string presented)
    {
        var logs = new List<string>();
        var directory = NewDirectory();
        try
        {
            await using var app = await StartAsync(directory, logs);
            using var absent = new HttpClient { BaseAddress = new Uri(app.BoundUrl() + "/") };
            using var wrong = new HttpClient { BaseAddress = absent.BaseAddress };
            wrong.DefaultRequestHeaders.Add(PryProtocol.SessionTokenHeader, presented);
            var ct = TestContext.Current.CancellationToken;

            using var first = await absent.GetAsync("api/v1/conversations/room/runtime-state", ct);
            using var second = await wrong.GetAsync("api/v1/conversations/room/runtime-state", ct);

            Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
            Assert.Equal(first.StatusCode, second.StatusCode);
            // traceId 每条响应都不同，比较前必须抹掉；除此之外两种拒绝必须一字不差，
            // 否则响应本身就在告诉调用方"你带了一个像样的令牌但它是错的"。
            Assert.Equal(StripTrace(await first.Content.ReadAsStringAsync(ct)),
                StripTrace(await second.Content.ReadAsStringAsync(ct)));
        }
        finally
        {
            Cleanup(directory);
        }
    }

    [Fact]
    public async Task Every_registered_endpoint_lets_a_discovered_token_through_the_gate()
    {
        var logs = new List<string>();
        var directory = NewDirectory();
        try
        {
            await using var app = await StartAsync(directory, logs);
            // 只用发现文件里的令牌：这一轮同时证明写出去的东西真的能用。
            var descriptor = await ReadDescriptorAsync(directory);
            using var http = new HttpClient { BaseAddress = new Uri(app.BoundUrl() + "/") };
            http.DefaultRequestHeaders.Add(PryProtocol.SessionTokenHeader, descriptor.Token);

            foreach (var (method, path) in Inventory(app))
            {
                using var request = new HttpRequestMessage(new HttpMethod(method), path);
                // 每个请求单独限时：流式端点会一直不返回正文，那恰好说明它过了门；
                // 而被门禁挡下的响应是立刻回来的，所以超时不能算失败。
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
                deadline.CancelAfter(1_500);
                try
                {
                    using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                        deadline.Token);
                    Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
                }
                catch (OperationCanceledException) when (!TestContext.Current.CancellationToken.IsCancellationRequested)
                {
                }
            }
        }
        finally
        {
            Cleanup(directory);
        }
    }

    [Fact]
    public async Task Rejection_happens_before_the_write_reaches_the_persisted_journal()
    {
        var logs = new List<string>();
        var directory = NewDirectory();
        var ct = TestContext.Current.CancellationToken;
        try
        {
            await using var app = await StartAsync(directory, logs);
            var database = app.Services.GetRequiredService<MemoryDatabase>();
            await database.EnsureConversationAsync("room", "character", ct);
            var before = (await database.ReadProtocolEventsAsync("room", 0, 10, ct)).Count;
            using var anonymous = new HttpClient { BaseAddress = new Uri(app.BoundUrl() + "/") };

            using var rejected = await anonymous.PutAsJsonAsync("api/v1/conversations/room/read-state",
                new { lastReadMessageId = 42 }, ct);

            Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
            // 门在业务之前：被拒的写必须什么都没留下，而不是"报错了但已经改库"。
            Assert.Equal(before, (await database.ReadProtocolEventsAsync("room", 0, 10, ct)).Count);
        }
        finally
        {
            Cleanup(directory);
        }
    }

    [Fact]
    public async Task The_token_never_appears_in_a_response_or_a_log_line()
    {
        var logs = new List<string>();
        var directory = NewDirectory();
        var ct = TestContext.Current.CancellationToken;
        try
        {
            // 崩溃恢复记录写在 ApplicationStarted 上、随 StartAsync 同步完成，用它当"日志面确实被接上"的
            // 确定性证据。否则一次干净的启动可以完全不写日志，那三条 DoesNotContain 就成了空判。
            File.WriteAllText(Path.Combine(directory, PryProtocol.RuntimeDescriptorFileName),
                JsonSerializer.Serialize(new RuntimeDescriptor(DeadPid(), 1, "stale-marker", PryProtocol.Version,
                    MemoryDatabase.SchemaVersion, DateTimeOffset.UtcNow.AddDays(-1)),
                    new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            await using var app = await StartAsync(directory, logs);
            var token = app.Services.GetRequiredService<BackendSessionToken>().Value;
            Assert.Contains(logs, line => line.Contains("遗留", StringComparison.Ordinal));
            using var anonymous = new HttpClient { BaseAddress = new Uri(app.BoundUrl() + "/") };
            using var wrong = new HttpClient { BaseAddress = anonymous.BaseAddress };
            wrong.DefaultRequestHeaders.Add(PryProtocol.SessionTokenHeader, "ZmFrZS10b2tlbg==");

            var observed = new List<string>();
            foreach (var client in new[] { anonymous, wrong })
            {
                using var rejected = await client.GetAsync("api/v1/conversations/room/messages", ct);
                observed.Add($"{(int)rejected.StatusCode}{await rejected.Content.ReadAsStringAsync(ct)}");
                using var handshake = await client.GetAsync("api/v1/info", ct);
                observed.Add($"{(int)handshake.StatusCode}{await handshake.Content.ReadAsStringAsync(ct)}");
            }
            // 令牌确实存在过：发现文件里解析得出它，所以后面的"响应与日志都不含它"不是空判。
            // 只比较解析后的值：System.Text.Json 默认把 + 与 / 写成 \u002B / \u002F，
            // 对文件原文做子串检查会随随机令牌里有没有这两个字符而时好时坏。
            Assert.Equal(token, (await ReadDescriptorAsync(directory)).Token);

            foreach (var item in observed)
            {
                Assert.DoesNotContain(token, item, StringComparison.Ordinal);
                Assert.DoesNotContain("ZmFrZS10b2tlbg==", item, StringComparison.Ordinal);
            }
            Assert.DoesNotContain(logs, line => line.Contains(token, StringComparison.Ordinal));
            Assert.DoesNotContain(logs, line => line.Contains("ZmFrZS10b2tlbg==", StringComparison.Ordinal));
            // 被覆盖的旧令牌也不该出现在恢复记录里。
            Assert.DoesNotContain(logs, line => line.Contains("stale-marker", StringComparison.Ordinal));
        }
        finally
        {
            Cleanup(directory);
        }
    }

    [Fact]
    public async Task Runtime_descriptor_is_written_at_start_and_removed_on_graceful_stop()
    {
        var logs = new List<string>();
        var directory = NewDirectory();
        var path = Path.Combine(directory, PryProtocol.RuntimeDescriptorFileName);
        var startedBefore = DateTimeOffset.UtcNow.AddSeconds(-5);
        try
        {
            var app = await StartAsync(directory, logs);
            var descriptor = await ReadDescriptorAsync(directory);

            Assert.Equal(Environment.ProcessId, descriptor.Pid);
            Assert.Equal(new Uri(app.BoundUrl()).Port, descriptor.Port);
            Assert.Equal(app.Services.GetRequiredService<BackendSessionToken>().Value, descriptor.Token);
            Assert.Equal(PryProtocol.Version, descriptor.ProtocolVersion);
            // 版本只有一个写侧来源：这里读回同一个常量，而不是又一份字面量。
            Assert.Equal(MemoryDatabase.SchemaVersion, descriptor.SchemaVersion);
            Assert.InRange(descriptor.StartedAt, startedBefore, DateTimeOffset.UtcNow.AddSeconds(5));

            await app.StopAsync(TestContext.Current.CancellationToken);
            await app.DisposeAsync();
            Assert.False(File.Exists(path));
        }
        finally
        {
            Cleanup(directory);
        }
    }

    [Fact]
    public async Task A_descriptor_left_by_another_instance_is_replaced_and_recorded_once()
    {
        var logs = new List<string>();
        var directory = NewDirectory();
        var path = Path.Combine(directory, PryProtocol.RuntimeDescriptorFileName);
        try
        {
            // 崩溃恢复的输入：一个不属于本进程的 pid，和一个连不上的端口，加上一枚旧令牌。
            File.WriteAllText(path, JsonSerializer.Serialize(new RuntimeDescriptor(DeadPid(), 1, "stale-token",
                PryProtocol.Version, MemoryDatabase.SchemaVersion, DateTimeOffset.UtcNow.AddDays(-1)),
                new JsonSerializerOptions(JsonSerializerDefaults.Web)));

            await using var app = await StartAsync(directory, logs);

            var descriptor = await ReadDescriptorAsync(directory);
            Assert.Equal(Environment.ProcessId, descriptor.Pid);
            Assert.Equal(new Uri(app.BoundUrl()).Port, descriptor.Port);
            Assert.DoesNotContain("stale-token", descriptor.Token, StringComparison.Ordinal);
            Assert.Equal(1, logs.Count(line => line.Contains("遗留", StringComparison.Ordinal)));
            // 恢复记录只说 pid 与状态，绝不复述旧令牌。
            Assert.DoesNotContain(logs, line => line.Contains("stale-token", StringComparison.Ordinal));
        }
        finally
        {
            Cleanup(directory);
        }
    }

    [Fact]
    public async Task Discovery_publishes_the_port_the_server_actually_holds_and_two_instances_get_different_ones()
    {
        // 先占端口再让后端去绑会留一个可被抢走的窗口，所以端口只能从绑定结果里读回来。
        var logs = new List<string>();
        var first = NewDirectory();
        var second = NewDirectory();
        try
        {
            await using var a = await StartAsync(first, logs);
            await using var b = await StartAsync(second, logs);
            var firstPort = new Uri(a.BoundUrl()).Port;
            var secondPort = new Uri(b.BoundUrl()).Port;

            Assert.True(firstPort > 0 && secondPort > 0);
            Assert.NotEqual(firstPort, secondPort);
            using var http = a.CreateBackendHttpClient();
            // 装配点自查：HttpClient.Timeout 只是兜底，做主的是 PryBackendClient 逐请求声明的预算。
            // 兜底一旦收得比最宽的一档还小，那一档就永远轮不到做主——4 分钟语音预算曾被 32 秒顶掉就是这么发生的。
            Assert.True(http.Timeout > TimeSpan.FromMilliseconds(PryProtocol.TranscriptionRequestTimeoutMs),
                $"内嵌客户端的兜底被收成 {http.Timeout.TotalSeconds:0.#} 秒，长请求会被它先判掉。");
            using var reachable = await http.GetAsync("api/v1/conversations/room/messages",
                TestContext.Current.CancellationToken);
            Assert.NotEqual(HttpStatusCode.Unauthorized, reachable.StatusCode);
            Assert.Equal(firstPort, (await ReadDescriptorAsync(first)).Port);
            Assert.Equal(secondPort, (await ReadDescriptorAsync(second)).Port);
        }
        finally
        {
            Cleanup(first);
            Cleanup(second);
        }
    }

    private static string NewDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pry-session-token-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static async Task<WebApplication> StartAsync(string directory, List<string> logs)
    {
        var token = TestContext.Current.CancellationToken;
        var app = await BackendApplication.BuildAsync(configure: builder =>
        {
            builder.Configuration["Pry:DataDirectory"] = directory;
            builder.Configuration["Pry:ModelsDirectory"] = Path.Combine(directory, "models");
            builder.Configuration["Pry:Url"] = "http://127.0.0.1:0";
            // "令牌不进日志"要能判定，日志面就必须是可枚举的内存列表而不是真 sink。
            builder.Logging.ClearProviders();
            builder.Logging.AddProvider(new CapturingProvider(logs));
        }, token: token);
        await app.StartAsync(token);
        return app;
    }

    private static async Task<RuntimeDescriptor> ReadDescriptorAsync(string directory)
    {
        var path = Path.Combine(directory, PryProtocol.RuntimeDescriptorFileName);
        // 写出挂在 ApplicationStarted 上，与 StartAsync 返回的先后不在契约里，所以等它出现而不是假设顺序。
        // 这里判的是"后端到底写不写发现文件"，不是"两秒内写不写"：窗口要宽到不会被四程序集并行的磁盘 IO
        // 拖成假红（QA-P6-01 同一教训），真正永不写出的回归在 5 秒后照样红。
        for (var attempt = 0; attempt < 250 && !File.Exists(path); attempt++) await Task.Delay(20);
        Assert.True(File.Exists(path), $"{PryProtocol.RuntimeDescriptorFileName} 未在后端启动后写出");
        try
        {
            return JsonSerializer.Deserialize<RuntimeDescriptor>(
                await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken),
                new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"{path} 不是可解析的发现文件", ex);
        }
    }

    private static List<(string Method, string Path)> Inventory(WebApplication app)
    {
        var routes = app.Services.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints).OfType<RouteEndpoint>().ToList();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inventory = new List<(string, string)>();
        foreach (var route in routes)
        {
            var pattern = route.RoutePattern.RawText;
            if (string.IsNullOrEmpty(pattern)) continue;
            var path = pattern[0] == '/' ? pattern : "/" + pattern;
            // 路由模板要变成可请求的路径。填充值取 1：它同时满足 {id:int} 与 {id:long} 约束，
            // 字符串段收到 "1" 也无妨——这两轮只判定令牌门放没放行，不判定业务语义。
            path = Regex.Replace(path, "\\{[^}]*\\}", "1");
            var method = route.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.FirstOrDefault() ?? "GET";
            if (seen.Add($"{method} {path}")) inventory.Add((method, path));
        }
        return inventory.OrderBy(x => x.Item1, StringComparer.Ordinal)
            .ThenBy(x => x.Item2, StringComparer.Ordinal).ToList();
    }

    private static string StripTrace(string body) =>
        Regex.Replace(body, "\"traceId\":\"[^\"]*\"", string.Empty);

    private static bool IsExempt(string method, string path) =>
        HttpMethods.IsGet(method) && ExemptPaths.Any(exempt => path.Equals(exempt, StringComparison.OrdinalIgnoreCase));

    private static int DeadPid()
    {
        for (var pid = 2; pid < 4096; pid++)
        {
            try
            {
                if (System.Diagnostics.Process.GetProcessById(pid).HasExited) return pid;
            }
            catch (Exception)
            {
                return pid;
            }
        }
        return int.MaxValue;
    }

    private static void Cleanup(string directory)
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    private sealed class CapturingProvider(List<string> messages) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, messages);
        public void Dispose() { }
    }

    private sealed class CapturingLogger(string category, List<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            lock (messages) messages.Add($"{category}|{logLevel}|{formatter(state, exception)}");
        }
    }
}

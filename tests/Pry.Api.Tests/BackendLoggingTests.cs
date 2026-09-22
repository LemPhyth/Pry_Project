using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pry.Api;
using Pry.Api.Logging;
using Pry.Api.Services;
using Pry.Contracts;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// PLAT-006：后端本机滚动日志与敏感字段过滤器。判据不是"有没有写文件"，而是
/// "秘密以什么形式离开进程"——所以每条断言都同时要求 <c>[redacted]</c> 出现（遮蔽真的执行过），
/// 而不只是"秘密不在里面"（可能只是压根没写）。
/// </summary>
public sealed class BackendLoggingTests
{
    [Fact]
    public void File_sink_rolls_by_size_and_keeps_only_the_retained_generation()
    {
        var directory = NewDirectory();
        try
        {
            var options = new RollingFileLogOptions(directory, RollingFileLogOptions.DefaultFileName,
                RollingFileLogOptions.MinimumMaximumBytes, 2);
            using (var provider = new RollingFileLoggerProvider(options, new LogRedactor()))
            {
                var logger = provider.CreateLogger("Pry.Api.Tests");
                // 1200 条 ≈ 300 KB：足够滚过两档并越过保留代数，才能判定"最老的一份被挤掉"。
                for (var line = 0; line < 1200; line++)
                    logger.LogInformation("滚动填充 {Line} {Padding}", line, new string('x', 200));
            }

            var current = Path.Combine(directory, options.FileName);
            Assert.True(File.Exists(current));
            Assert.True(new FileInfo(current).Length <= options.MaximumBytesPerFile + 512,
                "当前文件超过单档上限说明滚动没有生效");
            Assert.True(File.Exists(current + ".1"), "超过上限后必须滚动");
            Assert.True(File.Exists(current + ".2"));
            Assert.False(File.Exists(current + ".3"), "超出保留代数的日志必须被挤掉");
        }
        finally { Cleanup(directory); }
    }

    [Fact]
    public void Log_sink_never_holds_the_data_directory_open()
    {
        var directory = NewDirectory();
        try
        {
            // 常驻句柄是这个 sink 最容易留下的坑：进程还活着时清理、备份、卸载都碰不得数据目录，
            // 而复用同一数据目录的重启测试会直接抛 IOException。所以这里刻意不 Dispose。
            var provider = new RollingFileLoggerProvider(new RollingFileLogOptions(directory,
                RollingFileLogOptions.DefaultFileName, RollingFileLogOptions.MinimumMaximumBytes, 2),
                new LogRedactor());
            var logger = provider.CreateLogger("Pry.Api.Tests.NoHandle");
            logger.LogInformation("第一条记录");
            Directory.Delete(directory, recursive: true);
            logger.LogInformation("第二条记录");

            Assert.Contains("第二条记录",
                File.ReadAllText(Path.Combine(directory, RollingFileLogOptions.DefaultFileName)),
                StringComparison.Ordinal);
            provider.Dispose();
        }
        finally { Cleanup(directory); }
    }

    [Fact]
    public void Registered_values_and_key_shapes_are_redacted_before_they_reach_the_file()
    {
        var directory = NewDirectory();
        const string sessionToken = "abcdefghijklmnopqrstuvwxyz0123456789TOKEN";
        const string remoteKey = "sk-abcdefghijklmnopqrstuvwxyz";
        const string inlineKey = "hunter2-super-secret-value";
        try
        {
            var redactor = new LogRedactor();
            redactor.RegisterSecret(sessionToken);
            redactor.RegisterSecret("abc");   // 过短：不登记，否则正常词会被遮成噪声
            redactor.RegisterSecret(null);
            using (var provider = new RollingFileLoggerProvider(
                       new RollingFileLogOptions(directory, RollingFileLogOptions.DefaultFileName,
                           RollingFileLogOptions.MinimumMaximumBytes, 2), redactor))
            {
                var logger = provider.CreateLogger("Pry.Api.Tests.Redaction");
                logger.LogWarning("请求头 {Header} 与 {Token}", "X-Pry-Token", sessionToken);
                logger.LogInformation("配置 {{\"api_key\": \"{Key}\"}} 已加载", remoteKey);
                logger.LogError(new InvalidOperationException(
                    $"Authorization: Bearer {remoteKey}; password={inlineKey}; " +
                    $"url=http://user:{inlineKey}@127.0.0.1:5078"), "调用远端模型失败");
                logger.LogInformation("password 为 {Value}", "abc");
            }

            var text = File.ReadAllText(Path.Combine(directory, RollingFileLogOptions.DefaultFileName));
            Assert.Contains("[redacted]", text);
            foreach (var secret in new[] { sessionToken, remoteKey, inlineKey })
                Assert.DoesNotContain(secret, text, StringComparison.Ordinal);
            // 键名要留着：遮住整行等于把"哪个字段泄了"这条线索也一起遮掉。
            Assert.Contains("api_key", text, StringComparison.Ordinal);
            Assert.Contains("password", text, StringComparison.Ordinal);
            Assert.Equal(1, redactor.RegisteredSecretCount);
            Assert.Contains("password 为 abc", text, StringComparison.Ordinal);
        }
        finally { Cleanup(directory); }
    }

    [Fact]
    public async Task Backend_startup_writes_a_readable_log_next_to_its_data_and_never_the_session_token()
    {
        var directory = NewDirectory();
        var logs = Path.Combine(directory, "logs");
        try
        {
            var app = await BackendApplication.BuildAsync(configure: builder =>
            {
                builder.Configuration["Pry:DataDirectory"] = directory;
                builder.Configuration["Pry:ModelsDirectory"] = Path.Combine(directory, "models");
                builder.Configuration["Pry:Url"] = "http://127.0.0.1:0";
                // 这里刻意不调用 ClearProviders()：判据是"默认装配就有可用日志"。
            }, token: TestContext.Current.CancellationToken);
            await app.StartAsync(TestContext.Current.CancellationToken);
            var token = app.Services.GetRequiredService<BackendSessionToken>().Value;
            using var http = app.CreateBackendHttpClient();
            using var response = await http.GetAsync("api/v1/media/policy", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await app.StopAsync(TestContext.Current.CancellationToken);
            await app.DisposeAsync();

            var current = Path.Combine(logs, RollingFileLogOptions.DefaultFileName);
            Assert.True(File.Exists(current), $"默认装配没有写出日志：{logs}");
            var text = File.ReadAllText(current);
            Assert.Contains("持久层就绪：schema", text, StringComparison.Ordinal);
            Assert.DoesNotContain(token, text, StringComparison.Ordinal);
        }
        finally { Cleanup(directory); }
    }

    [Fact]
    public void Log_options_clamp_instead_of_failing_startup()
    {
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationManager
        {
            ["Pry:LogFileMaximumBytes"] = "10",
            ["Pry:LogFileCount"] = "9999"
        };
        var options = RollingFileLogOptions.Read(configuration, @"C:\data");
        Assert.Equal(RollingFileLogOptions.MinimumMaximumBytes, options.MaximumBytesPerFile);
        Assert.Equal(RollingFileLogOptions.MaximumRetainedFiles, options.RetainedFiles);
        Assert.Equal(Path.Combine(@"C:\data", "logs"), options.Directory);

        var unset = RollingFileLogOptions.Read(new Microsoft.Extensions.Configuration.ConfigurationManager(), @"C:\data");
        Assert.Equal(RollingFileLogOptions.DefaultMaximumBytes, unset.MaximumBytesPerFile);
        Assert.Equal(RollingFileLogOptions.DefaultRetainedFiles, unset.RetainedFiles);
    }

    private static string NewDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pry-logging-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void Cleanup(string directory)
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
        catch (IOException) { }
    }
}

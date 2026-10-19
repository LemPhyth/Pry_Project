using Microsoft.Extensions.Logging.Abstractions;
using Pry.Api.Capabilities;
using Pry.Core.Abstractions;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// 两个出厂能力自己的守卫（RT-007 第一期）。这一组判据的形状是"启动器一次都没被调用"，
/// 不是"启动了但被拦下"：拒绝必须发生在系统调用之前，否则拦下的只是它的结果。
/// </summary>
public sealed class DesktopCapabilityGuardTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly IReadOnlyDictionary<string, string> Empty = new Dictionary<string, string>();

    [Theory]
    [InlineData("https://pry.test/notes", "https://pry.test/notes")]
    [InlineData("http://example.com/a/b?x=1", "http://example.com/a/b?x=1")]
    [InlineData("  HTTPS://pry.test/  ", "https://pry.test/")]
    public async Task A_web_address_is_opened_and_the_launcher_sees_the_normalized_uri(string given, string expected)
    {
        var launcher = new RecordingLauncher();
        var result = await new OpenUrlCapability(launcher)
            .ExecuteAsync(new Dictionary<string, string> { ["url"] = given }, Token);

        Assert.True(result.Succeeded);
        Assert.Equal([expected], launcher.OpenedUrls);
    }

    [Theory]
    [InlineData("file:///C:/Windows/win.ini")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/plain,hello")]
    [InlineData("mailto:someone@pry.test")]
    [InlineData("C:/Windows/System32/cmd.exe")]
    [InlineData("not-a-uri-at-all")]
    public async Task Anything_that_is_not_a_web_address_never_reaches_the_launcher(string given)
    {
        var launcher = new RecordingLauncher();
        var result = await new OpenUrlCapability(launcher)
            .ExecuteAsync(new Dictionary<string, string> { ["url"] = given }, Token);

        Assert.False(result.Succeeded);
        Assert.Equal("url_scheme_not_allowed", result.ErrorCode);
        Assert.Empty(launcher.OpenedUrls);
    }

    [Fact]
    public async Task A_url_argument_the_caller_forgot_is_named_as_a_missing_argument_not_a_bad_scheme()
    {
        var launcher = new RecordingLauncher();
        var result = await new OpenUrlCapability(launcher).ExecuteAsync(Empty, Token);

        Assert.Equal("url_argument_missing", result.ErrorCode);
        Assert.Empty(launcher.OpenedUrls);
    }

    [Fact]
    public async Task An_empty_allow_list_refuses_even_a_program_that_really_exists()
    {
        // 出厂就是这个形状。用真存在的可执行文件跑这一条，是为了钉住"拒绝的理由是名单，不是找不到文件"。
        var existing = Environment.GetCommandLineArgs()[0];
        var launcher = new RecordingLauncher();
        var result = await new OpenApplicationCapability(launcher, []).ExecuteAsync(
            new Dictionary<string, string> { ["path"] = existing }, Token);

        Assert.Equal("application_not_allowed", result.ErrorCode);
        Assert.Empty(launcher.StartedApplications);
    }

    [Fact]
    public async Task A_listed_program_is_started_by_the_path_the_host_wrote()
    {
        var temp = Path.GetTempFileName();
        try
        {
            var launcher = new RecordingLauncher();
            var capability = new OpenApplicationCapability(launcher, [temp]);

            var result = await capability.ExecuteAsync(
                new Dictionary<string, string> { ["path"] = temp }, Token);

            Assert.True(result.Succeeded);
            Assert.Equal([Path.GetFullPath(temp)], launcher.StartedApplications);
        }
        finally
        {
            File.Delete(temp);
        }
    }

    [Theory]
    [InlineData("notepad.exe")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_path_that_is_not_absolute_is_refused_without_a_filesystem_probe(string given)
    {
        var launcher = new RecordingLauncher();
        var capability = new OpenApplicationCapability(launcher, [given]);
        var result = await capability.ExecuteAsync(new Dictionary<string, string> { ["path"] = given }, Token);

        Assert.Equal(given.Length == 0 || given.Trim().Length == 0
            ? "application_argument_missing"
            : "application_path_not_absolute", result.ErrorCode);
        Assert.Empty(launcher.StartedApplications);
    }

    [Fact]
    public async Task A_listed_program_that_is_not_on_this_machine_says_so_after_the_allow_check()
    {
        // 顺序是刻意的：先比名单、后看存在性。反过来会把"这台机器上有没有某个程序"泄漏进错误码。
        var missing = Path.Combine(Path.GetTempPath(), $"pry-missing-{Guid.NewGuid():N}.exe");
        var launcher = new RecordingLauncher();
        var result = await new OpenApplicationCapability(launcher, [missing])
            .ExecuteAsync(new Dictionary<string, string> { ["path"] = missing }, Token);

        Assert.Equal("application_not_found", result.ErrorCode);
        Assert.Empty(launcher.StartedApplications);
    }

    [Fact]
    public async Task An_unlisted_program_is_not_confused_with_a_missing_one()
    {
        var listed = Path.GetTempFileName();
        var other = Path.Combine(Path.GetTempPath(), $"pry-other-{Guid.NewGuid():N}.exe");
        try
        {
            var launcher = new RecordingLauncher();
            var result = await new OpenApplicationCapability(launcher, [listed])
                .ExecuteAsync(new Dictionary<string, string> { ["path"] = other }, Token);

            Assert.Equal("application_not_allowed", result.ErrorCode);
        }
        finally
        {
            File.Delete(listed);
        }
    }

    [Fact]
    public async Task Neither_capability_echoes_what_it_opened_into_the_result()
    {
        // 输出会作为"能力结果"进提示词。回显链接就等于把她刚看过的那条带令牌的 URL 抄进模型上下文。
        var launcher = new RecordingLauncher();
        var url = await new OpenUrlCapability(launcher)
            .ExecuteAsync(new Dictionary<string, string> { ["url"] = "https://pry.test/cb?access_token=hunter2" }, Token);
        var file = Path.GetTempFileName();
        try
        {
            var app = await new OpenApplicationCapability(launcher, [file])
                .ExecuteAsync(new Dictionary<string, string> { ["path"] = file }, Token);

            Assert.Null(url.Output);
            Assert.Null(app.Output);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Both_desktop_capabilities_ship_at_the_ask_tier_with_a_bounded_budget()
    {
        var launcher = new RecordingLauncher();
        var descriptors = new DesktopPlugin(CapabilityConfig.Build(), launcher)
            .CreateCapabilities(new PluginContext("pry.desktop", "本机桌面",
                new PluginStorage(Path.GetTempPath()), Microsoft.Extensions.Logging.Abstractions
                    .NullLogger.Instance))
            .Select(x => x.Descriptor)
            .ToArray();

        Assert.All(descriptors, x => Assert.Equal(CapabilityPermission.Ask, x.Permission));
        Assert.All(descriptors, x => Assert.Equal(CapabilityIds.TimeoutMs, x.TimeoutMs));
        Assert.All(descriptors, x => Assert.True(x.TimeoutMs > 0));
    }

    [Fact]
    public async Task A_program_the_host_listed_in_configuration_is_the_one_that_starts()
    {
        // 这一条钉的是配置键本身：白名单只有一个读法（Pry:Capabilities:AllowedApplications），
        // 键名写错或读法改成永远返回空表，都不会让上面任何一条判据变红——只会让宿主"配了但没生效"。
        var listed = Path.GetTempFileName();
        var other = Path.GetTempFileName();
        try
        {
            var launcher = new RecordingLauncher();
            var capability = CreateFromConfig(
                CapabilityConfig.Build((OpenApplicationCapability.AllowedApplicationsConfigKey + ":0", listed)),
                launcher, CapabilityIds.OpenApplication);

            var allowed = await capability.ExecuteAsync(new Dictionary<string, string> { ["path"] = listed }, Token);
            var refused = await capability.ExecuteAsync(new Dictionary<string, string> { ["path"] = other }, Token);

            Assert.True(allowed.Succeeded);
            Assert.Equal([Path.GetFullPath(listed)], launcher.StartedApplications);
            Assert.Equal("application_not_allowed", refused.ErrorCode);
        }
        finally
        {
            File.Delete(listed);
            File.Delete(other);
        }
    }

    private static ICapability CreateFromConfig(Microsoft.Extensions.Configuration.IConfiguration configuration,
        IProcessLauncher launcher, string capabilityId) =>
        new DesktopPlugin(configuration, launcher)
            .CreateCapabilities(new PluginContext(DesktopPlugin.PluginId, "本机桌面",
                new PluginStorage(Path.GetTempPath()), NullLogger.Instance))
            .Single(x => x.Descriptor.Id == capabilityId);
}

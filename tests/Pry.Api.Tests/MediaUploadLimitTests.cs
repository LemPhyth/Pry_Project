using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pry.Api;
using Pry.Api.Controllers;
using Pry.Api.Services;
using Pry.Contracts;
using Xunit;

namespace Pry.Api.Tests;

/// <summary>
/// PLAT-006：上传硬上限。QA-P1-04 指出上限原先是**两处** <c>long.MaxValue</c> 而不是一处，
/// 所以这里既测"超了要被拒"，也测"每一个声明上限的地方都同值"——只测一处会留另一处漏网。
/// </summary>
public sealed class MediaUploadLimitTests
{
    private const long TestCap = MediaUploadLimits.MinimumMaximumUploadBytes; // 64 KiB：让回归不必写满磁盘
    private static readonly byte[] Png = [137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0, 0, 0, 0, 0];

    [Fact]
    public async Task File_over_the_cap_is_rejected_with_a_stable_code_before_anything_is_stored()
    {
        var directory = NewDirectory();
        try
        {
            await using var app = await StartAsync(directory, TestCap);
            using var http = app.CreateBackendHttpClient();
            using var response = await http.PostAsync("api/v1/media", Form(new byte[TestCap + 1]),
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
            var problem = await ReadProblemAsync(response);
            Assert.Equal("payload_too_large", problem.Extensions["code"]!.ToString());
            Assert.Equal("urn:pry:error:payload_too_large", problem.Type);
            Assert.Equal(TestCap, Convert.ToInt64(problem.Extensions["maximumBytes"]!.ToString()));
            // 上限之上的文件不能留下任何落盘痕迹：拒绝必须发生在写入之前。
            var media = Path.Combine(directory, "media");
            Assert.Empty(Directory.Exists(media) ? Directory.EnumerateFileSystemEntries(media) : []);
        }
        finally { Cleanup(directory); }
    }

    [Fact]
    public async Task File_under_the_cap_still_uploads_after_the_limit_was_tightened()
    {
        var directory = NewDirectory();
        try
        {
            await using var app = await StartAsync(directory, TestCap);
            using var http = app.CreateBackendHttpClient();
            using var response = await http.PostAsync("api/v1/media", Form(Png), TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var asset = JsonSerializer.Deserialize<MediaAssetResponse>(
                await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            Assert.NotNull(asset);
            Assert.True(File.Exists(Path.Combine(directory, "media", asset.Id + ".png")));
        }
        finally { Cleanup(directory); }
    }

    [Fact]
    public async Task Policy_endpoint_publishes_the_effective_cap()
    {
        var directory = NewDirectory();
        try
        {
            await using var app = await StartAsync(directory, TestCap);
            using var http = app.CreateBackendHttpClient();
            var policy = await http.GetFromJsonAsync<MediaUploadPolicyResponse>("api/v1/media/policy",
                new JsonSerializerOptions(JsonSerializerDefaults.Web), TestContext.Current.CancellationToken);

            Assert.NotNull(policy);
            Assert.Equal(TestCap, policy.MaximumBytes);
            Assert.Equal(MediaAssetStore.LargeFileWarningBytes, policy.WarningThresholdBytes);
            Assert.Equal(TestCap, app.Services.GetRequiredService<MediaAssetStore>().MaximumUploadBytes);
        }
        finally { Cleanup(directory); }
    }

    /// <summary>
    /// 上限的每一个声明点都必须等于同一个天花板。原先的缺陷形态正是"端点特性写了 long.MaxValue，
    /// 全局 FormOptions 又写了一个"，所以这一条按声明点逐个反证，而不是只测行为。
    /// </summary>
    [Fact]
    public async Task Every_upload_limit_site_carries_the_same_ceiling()
    {
        var directory = NewDirectory();
        try
        {
            await using var app = await StartAsync(directory, null);
            var upload = typeof(MediaController).GetMethod(nameof(MediaController.Upload))!;

            var formLimits = Assert.Single(upload.GetCustomAttributes(typeof(RequestFormLimitsAttribute), inherit: false)
                .Cast<RequestFormLimitsAttribute>());
            Assert.Equal(MediaUploadLimits.CeilingRequestBodyBytes, formLimits.MultipartBodyLengthLimit);

            var sizeLimit = Assert.Single(upload.GetCustomAttributes(typeof(RequestSizeLimitAttribute), inherit: false)
                .Cast<RequestSizeLimitAttribute>());
            // 取值只有一条显式接口实现可走，反射拿接口比假设它哪天变成公开属性稳。
            var declared = ((IRequestSizeLimitMetadata)sizeLimit).MaxRequestBodySize;
            Assert.Equal(MediaUploadLimits.CeilingRequestBodyBytes, declared);

            // 无限额的声明已经不存在了：出现即失败，别再靠"另一个地方有上限"自我安慰。
            Assert.Empty(upload.GetCustomAttributes(typeof(DisableRequestSizeLimitAttribute), inherit: false));

            Assert.Equal(MediaUploadLimits.CeilingRequestBodyBytes,
                app.Services.GetRequiredService<IOptions<FormOptions>>().Value.MultipartBodyLengthLimit);
            Assert.Equal(MediaUploadLimits.CeilingUploadBytes, app.Services.GetRequiredService<MediaAssetStore>().MaximumUploadBytes);
        }
        finally { Cleanup(directory); }
    }

    [Fact]
    public void Runtime_cap_can_only_be_tightened_below_the_ceiling()
    {
        Assert.Equal(MediaUploadLimits.CeilingUploadBytes, MediaUploadLimits.ResolveMaximumUploadBytes(New(null)));
        Assert.Equal(TestCap, MediaUploadLimits.ResolveMaximumUploadBytes(New(TestCap)));
        // 配得比天花板还大、或配成 0：夹取而不是照抄，也不是抛异常让后端起不来。
        Assert.Equal(MediaUploadLimits.CeilingUploadBytes,
            MediaUploadLimits.ResolveMaximumUploadBytes(New(4L * 1024 * 1024 * 1024)));
        Assert.Equal(MediaUploadLimits.MinimumMaximumUploadBytes, MediaUploadLimits.ResolveMaximumUploadBytes(New(0)));
        Assert.Equal(MediaUploadLimits.MinimumMaximumUploadBytes, MediaUploadLimits.ResolveMaximumUploadBytes(New(-1)));
        Assert.True(MediaUploadLimits.CeilingUploadBytes >= MediaAssetStore.LargeFileWarningBytes,
            "天花板低于警告阈值会让 large-file 提示永远不可能出现");
    }

    private static IConfiguration New(long? cap)
    {
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationManager();
        if (cap is not null) configuration["Pry:MaximumUploadBytes"] = cap.Value.ToString();
        return configuration;
    }

    private static MultipartFormDataContent Form(byte[] bytes, string name = "large.png")
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var form = new MultipartFormDataContent("pry-upload-test");
        form.Add(content, "file", name);
        return form;
    }

    private static async Task<ProblemDetails> ReadProblemAsync(HttpResponseMessage response) =>
        JsonSerializer.Deserialize<ProblemDetails>(
            await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

    private static string NewDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pry-upload-limit-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static async Task<WebApplication> StartAsync(string directory, long? cap)
    {
        var app = await BackendApplication.BuildAsync(configure: builder =>
        {
            builder.Configuration["Pry:DataDirectory"] = directory;
            builder.Configuration["Pry:ModelsDirectory"] = Path.Combine(directory, "models");
            builder.Configuration["Pry:Url"] = "http://127.0.0.1:0";
            if (cap is not null) builder.Configuration["Pry:MaximumUploadBytes"] = cap.Value.ToString();
            // 本文件判定上限，不判定日志；清掉 console sink 免得测试输出里混进无关行。
            builder.Logging.ClearProviders();
        }, token: TestContext.Current.CancellationToken);
        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    private static void Cleanup(string directory)
    {
        SqliteTestPools.Clear(Path.Combine(directory, "memory.db"));
        if (Directory.Exists(directory)) try { Directory.Delete(directory, recursive: true); }
        catch (IOException) { }
    }
}

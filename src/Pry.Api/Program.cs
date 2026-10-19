using System.Text.Json.Serialization;
using Pry.Api;

var app = await BackendApplication.BuildAsync(args);
await app.RunAsync();

namespace Pry.Api
{
    using Logging;
    using Middleware;
    using Services;
    using Capabilities;
    using Pry.Core.Abstractions;
    using Pry.Core.Memory;

    public static class BackendApplication
    {
        /// <summary>
        /// 内嵌后端的宿主配置根只有一个建法：界面在进程发现之前先用它解析数据目录，再把同一个 builder
        /// 交回 <c>BuildAsync(WebApplicationBuilder, ...)</c>。两边各建一次配置根就是两份事实源——
        /// 读侧少认一项时不会报错，只会静默起第二个宿主去争同一份数据目录。
        /// </summary>
        public static WebApplicationBuilder CreateHostBuilder() => WebApplication.CreateBuilder([]);

        public static async Task<WebApplication> BuildAsync(string[]? args = null,
            Action<WebApplicationBuilder>? configure = null, CancellationToken token = default) =>
            await BuildAsync(WebApplication.CreateBuilder(args ?? []), configure, token);

        public static async Task<WebApplication> BuildAsync(WebApplicationBuilder builder,
            Action<WebApplicationBuilder>? configure = null, CancellationToken token = default)
        {
            configure?.Invoke(builder);
            var listenUrl = builder.Configuration["Pry:Url"] ?? "http://127.0.0.1:5078";
            if (!Uri.TryCreate(listenUrl, UriKind.Absolute, out var listenUri) || !listenUri.IsLoopback)
                throw new InvalidOperationException("本地后端只允许监听回环地址；对外暴露需要 PLAT-005 之外的认证与传输层方案。");
            builder.WebHost.UseUrls(listenUrl);
            builder.Services.AddProblemDetails();
            builder.Services.AddControllers()
                .AddApplicationPart(typeof(BackendApplication).Assembly)
                .AddJsonOptions(options =>
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
                    {
                        Status = 400, Title = "请求参数不合法", Type = "urn:pry:error:validation_error",
                        Detail = "请求正文缺少必要字段或格式不正确", Instance = context.HttpContext.Request.Path
                    };
                    problem.Extensions["code"] = "validation_error";
                    problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                    return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(problem);
                };
            });
            var dataDirectory = RuntimeDescriptorService.ResolveDataDirectory(builder.Configuration);
            // 持久层的日志器由宿主注入（PLAT-006）：Core 只开口，去哪里由这里决定。
            builder.Services.AddSingleton(provider => new MemoryDatabase(Path.Combine(dataDirectory, "memory.db"),
                provider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()
                    .CreateLogger<MemoryDatabase>()));
            // 学习状态的落库实现（RT-003 缺口 ②）。四个适配器都只认自己的接口、内部委托到上面那一份
            // MemoryDatabase；这里把它们接进来，唯一的生产者是删角色卡时的那次清空——学习本身的写入
            // 仍然没有生产者，接线归 RT-002/RT-003 的双向接缝，不在这一刀。
            builder.Services.AddSingleton<Pry.Core.Abstractions.ICharacterStateRepository,
                Pry.Core.Memory.SqliteCharacterStateRepository>();
            builder.Services.AddSingleton<Pry.Core.Abstractions.IBehaviorPatternRepository,
                Pry.Core.Memory.SqliteBehaviorPatternRepository>();
            builder.Services.AddSingleton<Pry.Core.Abstractions.IBehaviorExperienceRepository,
                Pry.Core.Memory.SqliteBehaviorExperienceRepository>();
            builder.Services.AddSingleton<Pry.Core.Abstractions.IExpressionPatternRepository,
                Pry.Core.Memory.SqliteExpressionPatternRepository>();
            builder.Services.AddSingleton<Pry.Core.Intelligence.CharacterLearningCleanup>();
            // 角色智能的宿主接线处（说话前 DecideAsync / 投递后 CompleteTurnAsync）。一个进程一份：
            // 同一张卡的长期状态必须只有一个实例，见 CharacterIntelligenceGateway 的归属线注释。
            builder.Services.AddSingleton<Pry.Api.Runtime.CharacterIntelligenceGateway>();
            // 能力与插件（RT-007 第一期，地基）。名册在启动时算一次——权限叠配置、同 id 去重——之后只读。
            // 今天会话里没有任何一处会发起 CapabilityRequest（AvailableCapabilityNames 零生产引用），
            // 所以这三行接完，用户看得见行为一字未变；两个能力还都出厂在 Ask 档，
            // 而 Ask 在没有批准界面之前一律回"没人批准过"。
            builder.Services.AddSingleton<IProcessLauncher, DesktopProcessLauncher>();
            builder.Services.AddSingleton<IPryPlugin>(provider => new DesktopPlugin(
                provider.GetRequiredService<IConfiguration>(), provider.GetRequiredService<IProcessLauncher>()));
            builder.Services.AddSingleton(provider => CapabilityRegistry.Build(
                provider.GetServices<IPryPlugin>(), provider.GetRequiredService<IConfiguration>(), dataDirectory,
                provider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()));
            builder.Services.AddSingleton<CapabilityDispatcher>();
            builder.Services.AddSingleton<BackendSessionToken>();
            builder.Services.AddSingleton<ConversationApplicationService>();
            builder.Services.AddSingleton<ConversationReadService>();
            builder.Services.AddSingleton<MemoryApplicationService>();
            builder.Services.AddSingleton<ConversationFolderApplicationService>();
            builder.Services.AddSingleton<MediaAssetStore>();
            builder.Services.AddSingleton<ConfigurationApplicationService>();
            builder.Services.AddSingleton<StickerApplicationService>();
            builder.Services.AddSingleton<SpeechApplicationService>();
            builder.Services.AddSingleton<ModelProcessRegistry>();
            builder.Services.AddSingleton<EmbeddingProcessRegistry>();
            builder.Services.AddSingleton<ModelPerformancePolicy>();
            builder.Services.AddSingleton<LocalModelDiscoveryService>();
            builder.Services.AddSingleton<BackendRuntime>();
            builder.Services.AddSingleton<ConversationSessionService>();
            builder.Services.AddSingleton<CharacterInteractionProfileService>();
            // 显式表达反应的接收入口（DEF-P4LEARN-01）：认可第一次有了生产者。它只往学习队列里放，
            // 取走仍然只由上面那个排空通道做——把排空搬进请求里就等于让一次点击能拖慢下一次回复。
            builder.Services.AddSingleton<ExpressionFeedbackService>();
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSingleton<CompanionOperationCoordinator>();
            builder.Services.AddSingleton<ProactivePolicyService>();
            builder.Services.AddSingleton<CompanionActivityService>();
            builder.Services.AddSingleton<IProactiveMessageGenerator, ProactiveMessageGenerator>();
            builder.Services.AddSingleton<ProactiveScheduler>();
            builder.Services.AddSingleton<MemoryEmbeddingService>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<BackendRuntime>());
            builder.Services.AddHostedService(sp => sp.GetRequiredService<MemoryEmbeddingService>());
            builder.Services.AddHostedService(sp => sp.GetRequiredService<ProactiveScheduler>());
            builder.Services.AddHostedService<MediaCleanupService>();
            builder.Services.AddHostedService<ProtocolEventCleanupService>();
            builder.Services.AddHostedService<RuntimeDescriptorService>();
            builder.Services.AddHostedService<MemoryConsolidationService>();
            // 表达学习的排空通道（RT-004-DRAIN）：队列从建立起就一直只有放的人、没有取的人，
            // 少这一行就等于让 `QueueCapacity = 64` 满即丢成为发行行为，而丢的时候没有任何报错。
            builder.Services.AddHostedService<ExpressionDrainService>();
            builder.Services.AddHealthChecks();
            // 上传天花板（PLAT-006）：与 MediaController 端点特性同值。特性管这一个端点，
            // 全局 FormOptions 兜住其它表单解析路径，两侧都留出 multipart 余量。
            builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
                options.MultipartBodyLengthLimit = MediaUploadLimits.CeilingRequestBodyBytes);
            // 本机滚动日志与敏感字段过滤器（PLAT-006）。注册成 ILoggerProvider 而不是 AddProvider，
            // 测试里的 ClearProviders() 才照样能把它摘掉。
            var logRedactor = new LogRedactor();
            builder.Services.AddSingleton(logRedactor);
            builder.Services.AddSingleton<ILoggerProvider>(_ => new RollingFileLoggerProvider(
                RollingFileLogOptions.Read(builder.Configuration, dataDirectory), logRedactor));
            var app = builder.Build();
            // 会话令牌一旦出现在异常原文里就会被写出去，所以登记发生在第一个请求之前。
            logRedactor.RegisterSecret(app.Services.GetRequiredService<BackendSessionToken>().Value);
            // 令牌门排在协商之前：未授权的探测不该先学到服务端的协议版本与能力清单。
            app.UseMiddleware<SessionTokenMiddleware>();
            app.UseMiddleware<ProtocolNegotiationMiddleware>();
            app.UseMiddleware<ApiExceptionMiddleware>();
            app.MapHealthChecks("/health");
            app.MapControllers();
            await app.Services.GetRequiredService<MemoryDatabase>().InitializeAsync(token);
            // 上个进程遗留的未投递计划只清账、绝不重放：重启后不会重复发出旧消息。
            var swept = await Pry.Core.Runtime.ConversationRuntime.RecoverStalePlansAsync(
                new Pry.Api.Runtime.DatabaseRuntimeStore(app.Services.GetRequiredService<MemoryDatabase>()), token);
            if (swept.Count > 0) app.Services.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()
                .CreateLogger("Pry.Api.Runtime").LogInformation("已作废 {Count} 个上次进程遗留的投递计划", swept.Count);
            return app;
        }
    }
}

public partial class Program;

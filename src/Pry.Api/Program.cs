using System.Text.Json.Serialization;
using Pry.Api.Middleware;
using Pry.Api.Services;
using Pry.Core.Memory;

var builder = WebApplication.CreateBuilder(args);
var listenUrl = builder.Configuration["Pry:Url"] ?? "http://127.0.0.1:5078";
if (!Uri.TryCreate(listenUrl, UriKind.Absolute, out var listenUri) || !listenUri.IsLoopback)
    throw new InvalidOperationException("Pry.Api 在认证实现前只允许监听回环地址。");
builder.WebHost.UseUrls(listenUrl);
builder.Services.AddProblemDetails();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
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

var dataDirectory = builder.Configuration["Pry:DataDirectory"];
if (string.IsNullOrWhiteSpace(dataDirectory))
    dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PryCompanion");
var databasePath = Path.Combine(Path.GetFullPath(dataDirectory), "memory.db");
builder.Services.AddSingleton(new MemoryDatabase(databasePath));
builder.Services.AddSingleton<ConversationApplicationService>();
builder.Services.AddSingleton<MemoryApplicationService>();
builder.Services.AddSingleton<MediaAssetStore>();
builder.Services.AddSingleton<ConfigurationApplicationService>();
builder.Services.AddSingleton<StickerApplicationService>();
builder.Services.AddSingleton<SpeechApplicationService>();
builder.Services.AddSingleton<ModelProcessRegistry>();
builder.Services.AddSingleton<BackendRuntime>();
builder.Services.AddSingleton<ConversationSessionService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<BackendRuntime>());
builder.Services.AddHealthChecks();
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
    options.MultipartBodyLengthLimit = long.MaxValue);

var app = builder.Build();
app.UseMiddleware<ApiExceptionMiddleware>();
app.MapHealthChecks("/health");
app.MapControllers();

await app.Services.GetRequiredService<MemoryDatabase>().InitializeAsync();
await app.RunAsync();

public partial class Program;

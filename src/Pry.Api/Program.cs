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

var dataDirectory = builder.Configuration["Pry:DataDirectory"];
if (string.IsNullOrWhiteSpace(dataDirectory))
    dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PryCompanion");
var databasePath = Path.Combine(Path.GetFullPath(dataDirectory), "memory.db");
builder.Services.AddSingleton(new MemoryDatabase(databasePath));
builder.Services.AddSingleton<ConversationApplicationService>();
builder.Services.AddSingleton<MemoryApplicationService>();
builder.Services.AddSingleton<ModelProcessRegistry>();
builder.Services.AddSingleton<BackendRuntime>();
builder.Services.AddSingleton<ConversationSessionService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<BackendRuntime>());
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseMiddleware<ApiExceptionMiddleware>();
app.MapHealthChecks("/health");
app.MapControllers();

await app.Services.GetRequiredService<MemoryDatabase>().InitializeAsync();
await app.RunAsync();

public partial class Program;

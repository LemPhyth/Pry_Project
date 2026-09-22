using Pry.Core.Memory;

namespace Pry.Api.Services;

/// <summary>
/// 协议日志保留策略的读取与夹取（PLAT-005 M0-c）。落盘之前"每会话 200 条内存环"是一条意外的
/// 保留策略——进程一死就没了；落盘之后保留多久必须成为显式配置，否则对话明文会永久留在磁盘上。
/// 三个键只走环境变量/测试注入，不进 appsettings.json。下界夹取是硬要求：配成 0 会让每次重连
/// 都判定 buffer_expired，把补投机制退化成轮询。
/// </summary>
public static class ProtocolEventRetention
{
    public static (int PerConversation, int MaxAgeDays, int TotalRows) Read(IConfiguration configuration) =>
        (
            Math.Clamp(configuration.GetValue("Pry:ProtocolEventMaxPerConversation",
                ProtocolEventRetentionPolicy.DefaultPerConversation),
                ProtocolEventRetentionPolicy.MinPerConversation, ProtocolEventRetentionPolicy.MaxPerConversationLimit),
            Math.Clamp(configuration.GetValue("Pry:ProtocolEventMaxAgeDays",
                ProtocolEventRetentionPolicy.DefaultMaxAgeDays), 1, 365),
            Math.Clamp(configuration.GetValue("Pry:ProtocolEventMaxTotalRows",
                ProtocolEventRetentionPolicy.DefaultTotalRows),
                ProtocolEventRetentionPolicy.MinTotalRows, ProtocolEventRetentionPolicy.MaxTotalRowsLimit)
        );
}

/// <summary>
/// 周期性裁剪：条数上限由它兜住总量与年龄，每会话超额则在两次运行之间由写入量决定，
/// 本地单用户的写入速率下不会显著越过上限。
/// </summary>
public sealed class ProtocolEventCleanupService(MemoryDatabase database, IConfiguration configuration,
    ILogger<ProtocolEventCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await PruneAsync(stoppingToken);
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken)) await PruneAsync(stoppingToken);
    }

    private async Task PruneAsync(CancellationToken token)
    {
        try
        {
            var (perConversation, maxAgeDays, totalRows) = ProtocolEventRetention.Read(configuration);
            var removed = await database.PruneProtocolEventsAsync(perConversation, maxAgeDays, totalRows, token);
            if (removed > 0)
                logger.LogInformation("协议事件日志裁剪 {Count} 行（每会话 {Per} 条、{Age} 天、总计 {Total} 行）",
                    removed, perConversation, maxAgeDays, totalRows);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex) { logger.LogWarning(ex, "协议事件日志裁剪失败"); }
    }
}

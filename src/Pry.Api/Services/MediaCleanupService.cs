using Pry.Core.Memory;

namespace Pry.Api.Services;

public sealed class MediaCleanupService(MediaAssetStore media, BackendRuntime runtime, MemoryDatabase database,
    IConfiguration configuration, ILogger<MediaCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CleanupAsync(stoppingToken);
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken)) await CleanupAsync(stoppingToken);
    }

    private async Task CleanupAsync(CancellationToken token)
    {
        try
        {
            var references = new List<string>();
            Add(references, runtime.Preferences.Theme.BackgroundImagePath);
            Add(references, runtime.Preferences.Theme.UserAvatarPath);
            references.AddRange(runtime.Characters.Select(x => x.AvatarPath).Where(x => !string.IsNullOrWhiteSpace(x))!);
            references.AddRange(runtime.Stickers.All.Select(x => x.FilePath));
            references.AddRange(await database.GetReferencedImagePathsAsync(token));
            var retentionDays = ReadRetentionDays();
            var result = await media.CleanupOrphansAsync(references,
                DateTimeOffset.UtcNow.AddDays(-retentionDays), token);
            if (result.RemovedCount > 0)
                logger.LogInformation("Removed {Count} orphan media files using {RetentionDays}-day retention ({Bytes} bytes)",
                    result.RemovedCount, retentionDays, result.RemovedBytes);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex) { logger.LogWarning(ex, "Orphan media cleanup failed"); }
    }

    private double ReadRetentionDays()
    {
        var configured = configuration["Pry:Media:OrphanRetentionDays"];
        return double.TryParse(configured, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? Math.Clamp(value, 1, 365)
            : 7;
    }

    private static void Add(List<string> values, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) values.Add(value);
    }
}

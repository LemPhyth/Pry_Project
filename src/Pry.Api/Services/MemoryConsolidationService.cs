using Pry.Core.Memory;

namespace Pry.Api.Services;

public sealed class MemoryConsolidationService(MemoryDatabase database,
    ILogger<MemoryConsolidationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var worker = new MemoryWriteback(database.LongTerm, new ExplicitFactExtractor());
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // Bounded work per wake; extraction never runs on a turn request.
                for (var count = 0; count < 8 && await worker.ProcessOneAsync(stoppingToken); count++) { }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception)
            {
                // No private input or provider exception text in routine diagnostics.
                logger.LogWarning("Memory consolidation temporarily unavailable; durable jobs will be retried.");
            }
        }
    }
}

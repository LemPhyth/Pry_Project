using Pry.Core.Inference;
using Pry.Core.Models;

namespace Pry.Api.Services;

public sealed class ModelPerformancePolicy
{
    public const double MinimumUsableTokensPerSecond = 12;

    public IEnumerable<ModelProfile> CreateCandidates(ModelProfile requested, LlamaComputeDevice? device = null)
    {
        var tier = Classify(device);
        var firstContext = Math.Min(requested.ContextSize, tier.MaximumInitialContextSize);
        yield return WithContext(requested, firstContext);
        foreach (var contextSize in new[] { 131072, 65536, 32768, 16384, 8192, 4096 })
        {
            if (contextSize >= firstContext) continue;
            yield return WithContext(requested, contextSize);
        }
    }

    public GpuPerformanceTier Classify(LlamaComputeDevice? device)
    {
        if (device is null || device.IsIntegrated) return GpuPerformanceTier.Cpu;
        var memory = device.FreeMemoryMiB ?? device.TotalMemoryMiB;
        if (memory is not null)
        {
            // Driver-reported capacities are commonly a little below marketed GiB values.
            if (memory < 4800) return GpuPerformanceTier.Entry;
            if (memory < 7500) return GpuPerformanceTier.Basic;
            if (memory < 10500) return GpuPerformanceTier.Balanced;
            if (memory < 19500) return GpuPerformanceTier.High;
            return GpuPerformanceTier.Extreme;
        }
        return ClassifyByName(device.Name);
    }

    private static ModelProfile WithContext(ModelProfile source, int contextSize) => source with
    {
        ContextSize = contextSize,
        MaxOutputTokens = contextSize == source.ContextSize
            ? source.MaxOutputTokens
            : Math.Min(source.MaxOutputTokens, Math.Max(512, contextSize / 2))
    };

    private static GpuPerformanceTier ClassifyByName(string name)
    {
        var normalized = name.ToUpperInvariant();
        if (normalized.Contains("RTX 5090") || normalized.Contains("RTX 4090") ||
            normalized.Contains("RTX 3090") || normalized.Contains("RTX 6000")) return GpuPerformanceTier.Extreme;
        if (!normalized.Contains("LAPTOP") &&
            (normalized.Contains("RTX 5080") || normalized.Contains("RTX 5070") ||
             normalized.Contains("RTX 4080") || normalized.Contains("RTX 4070") ||
             normalized.Contains("RTX 3080 TI") || normalized.Contains("GTX 1080 TI")))
            return GpuPerformanceTier.High;
        if (normalized.Contains("RTX 50") || normalized.Contains("RTX 40") || normalized.Contains("RTX 3080") ||
            normalized.Contains("RTX 3070") || normalized.Contains("RTX 2080 TI") || normalized.Contains("RTX 20") ||
            normalized.Contains("GTX 1080") || normalized.Contains("GTX 1070"))
            return GpuPerformanceTier.Balanced;
        if (normalized.Contains("RTX 3060") || normalized.Contains("RTX 3050") || normalized.Contains("RTX 2050") ||
            normalized.Contains("GTX 1660") || normalized.Contains("GTX 1060")) return GpuPerformanceTier.Basic;
        return GpuPerformanceTier.Entry;
    }

    public bool IsTooSlow(ModelProfile profile, LlamaServerStartupMetrics metrics) =>
        profile.ContextSize > 4096 && metrics.TokensPerSecond is > 0 and < MinimumUsableTokensPerSecond;

    public bool MayTryLowerContext(ModelRuntimeException exception) => exception.Code is
        "cuda_out_of_memory" or "memory_allocation_failed" or "context_performance_low";
}

public sealed record GpuPerformanceTier(int Level, string Id, int MaximumInitialContextSize)
{
    public static readonly GpuPerformanceTier Cpu = new(1, "cpu-or-integrated", 4096);
    public static readonly GpuPerformanceTier Entry = new(1, "entry", 4096);
    public static readonly GpuPerformanceTier Basic = new(2, "basic", 8192);
    public static readonly GpuPerformanceTier Balanced = new(3, "balanced", 32768);
    public static readonly GpuPerformanceTier High = new(4, "high", 131072);
    public static readonly GpuPerformanceTier Extreme = new(5, "extreme", 262144);
}

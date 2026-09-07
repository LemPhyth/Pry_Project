namespace Pry.Core.Inference;

public sealed class ModelRuntimeException(
    string code,
    string safeMessage,
    bool retryable,
    string? diagnosticDetails = null,
    Exception? innerException = null) : Exception(safeMessage, innerException)
{
    public string Code { get; } = code;
    public string SafeMessage { get; } = safeMessage;
    public bool Retryable { get; } = retryable;
    public string? DiagnosticDetails { get; } = diagnosticDetails;

    public static ModelRuntimeException FromProcessExit(int exitCode, string diagnostics)
    {
        var normalized = diagnostics.ToLowerInvariant();
        if (normalized.Contains("cuda") &&
            (normalized.Contains("out of memory") || normalized.Contains("failed to allocate") ||
             normalized.Contains("cudamalloc failed")))
            return new("cuda_out_of_memory", "GPU 显存不足，无法加载本地模型。请释放显存或降低 GPU 层数后重试。",
                true, diagnostics);
        if (normalized.Contains("out of memory") || normalized.Contains("failed to allocate") ||
            normalized.Contains("cannot allocate memory") || normalized.Contains("bad_alloc"))
            return new("memory_allocation_failed", "系统内存不足，无法按当前配置加载本地模型。", true, diagnostics);
        if (normalized.Contains("invalid model") || normalized.Contains("invalid gguf") ||
            normalized.Contains("failed to load model"))
            return new("invalid_model", "本地模型无效或与当前运行库不兼容。", false, diagnostics);
        return new("process_exited", $"本地模型服务启动失败（退出码 {exitCode}）。", true, diagnostics);
    }
}

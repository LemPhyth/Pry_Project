using Pry.Core.Models;

namespace Pry.Core.Abstractions;

public interface IChatModel
{
    ModelProfile Profile { get; }
    IAsyncEnumerable<string> StreamAsync(string systemPrompt, IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<string>? imagePaths, ChatRequestOptions? options = null, CancellationToken cancellationToken = default);
}

public interface IEmbeddingProvider
{
    string ModelId { get; }
    int Dimensions { get; }
    Task<float[]> EncodeAsync(string text, CancellationToken cancellationToken = default);
}

public interface IImageGenerationProvider
{
    string ModelId { get; }
    Task<string> GenerateAsync(string prompt, string outputDirectory,
        IProgress<double>? progress = null, CancellationToken cancellationToken = default);
}

public interface ISpeechRecognizer
{
    bool IsAvailable { get; }
    Task<string> RecognizeAsync(string audioPath, CancellationToken cancellationToken = default);
}

public interface IAvatarRenderer : IAsyncDisposable
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task SetExpressionAsync(string expression, double intensity, CancellationToken cancellationToken = default);
    Task SetMotionAsync(string motion, CancellationToken cancellationToken = default);
}

public interface IAgentToolRegistry
{
    IReadOnlyCollection<string> ToolNames { get; }
}

public sealed class NullAvatarRenderer : IAvatarRenderer
{
    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SetExpressionAsync(string expression, double intensity, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SetMotionAsync(string motion, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public sealed class EmptyAgentToolRegistry : IAgentToolRegistry
{
    public IReadOnlyCollection<string> ToolNames => Array.Empty<string>();
}

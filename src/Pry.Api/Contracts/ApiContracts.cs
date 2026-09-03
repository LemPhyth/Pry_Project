using Pry.Core.Models;

namespace Pry.Api.Contracts;

public sealed record CreateConversationRequest(string? CharacterId);
public sealed record UpdateConversationRequest(string? Title, bool? IsPinned, string? FolderId, bool ClearFolder = false);
public sealed record CreateFolderRequest(string Name);
public sealed record RenameFolderRequest(string Name);
public sealed record CreateMessageRequest(ChatRole Role, string Content, string? ImagePath = null, string? StickerId = null);
public sealed record SubmitTurnRequest(string Content, string? StickerId = null, bool Immediate = false);
public sealed record SubmitTurnResponse(long MessageId);
public sealed record RuntimeStatusResponse(string State, string? TextModelId, string? VisionModelId, string? Error);
public sealed record ConversationEvent(long Sequence, string Type, DateTimeOffset OccurredAt, object Data);
public sealed record CreateMemoryRequest(string CharacterId, string Kind, string Summary, string Tags, double Importance = 0.5, long? SourceMessageId = null);
public sealed record UpdateMemoryRequest(string Kind, string Summary, string Tags, double Importance);
public sealed record ResourceId<T>(T Id);

public static class ContractValidation
{
    public static string Required(string? value, string field, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) throw new ApiValidationException(field, "不能为空");
        if (normalized.Length > maxLength) throw new ApiValidationException(field, $"长度不能超过 {maxLength}");
        return normalized;
    }

    public static string? OptionalId(string? value, string field) => string.IsNullOrWhiteSpace(value) ? null : Required(value, field, 128);

    public static void Importance(double value)
    {
        if (double.IsNaN(value) || value is < 0 or > 1) throw new ApiValidationException("importance", "必须在 0 到 1 之间");
    }
}

public sealed class ApiValidationException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}

public sealed class ResourceNotFoundException(string resource, object id) : Exception($"{resource} '{id}' 不存在");

using Pry.Api.Contracts;
using Pry.Core.Memory;
using Pry.Core.Models;

namespace Pry.Api.Services;

public sealed class ConversationApplicationService(MemoryDatabase database)
{
    public Task<IReadOnlyList<ConversationRoom>> ListAsync(int limit, CancellationToken token) =>
        database.ListConversationsAsync(Math.Clamp(limit, 1, 200), token);

    public async Task<ConversationRoom> GetAsync(string id, CancellationToken token) =>
        await database.GetConversationAsync(id, token) ?? throw new ResourceNotFoundException("conversation", id);

    public async Task<ConversationRoom> CreateAsync(CreateConversationRequest request, CancellationToken token)
    {
        var id = Guid.NewGuid().ToString("N");
        await database.EnsureConversationAsync(id, ContractValidation.OptionalId(request.CharacterId, "characterId"), token);
        return await GetAsync(id, token);
    }

    public async Task<ConversationRoom> UpdateAsync(string id, UpdateConversationRequest request, CancellationToken token)
    {
        _ = await GetAsync(id, token);
        if (request.Title is not null) await database.RenameConversationAsync(id, ContractValidation.Required(request.Title, "title", 200), token);
        if (request.IsPinned is not null) await database.SetConversationPinnedAsync(id, request.IsPinned.Value, token);
        if (request.ClearFolder || request.FolderId is not null)
        {
            var folderId = request.ClearFolder ? null : ContractValidation.Required(request.FolderId, "folderId", 128);
            if (folderId is not null && !await database.ConversationFolderExistsAsync(folderId, token)) throw new ResourceNotFoundException("folder", folderId);
            await database.MoveConversationToFolderAsync(id, folderId, token);
        }
        return await GetAsync(id, token);
    }

    public async Task DeleteAsync(string id, CancellationToken token)
    {
        _ = await GetAsync(id, token);
        await database.DeleteConversationAsync(id, token);
    }

    public async Task<IReadOnlyList<ChatMessage>> MessagesAsync(string id, int limit, CancellationToken token)
    {
        _ = await GetAsync(id, token);
        return await database.GetRecentMessagesAsync(id, Math.Clamp(limit, 1, 500), token);
    }

    public async Task<ChatMessage> AddMessageAsync(string id, CreateMessageRequest request, CancellationToken token)
    {
        _ = await GetAsync(id, token);
        if (request.Role != ChatRole.User) throw new ApiValidationException("role", "客户端只能写入 user 消息");
        if (!string.IsNullOrWhiteSpace(request.ImagePath)) throw new ApiValidationException("imagePath", "不接受客户端文件路径，请使用后续的受控媒体上传接口");
        var content = request.StickerId is null ? ContractValidation.Required(request.Content, "content", 20_000) : request.Content?.Trim() ?? "";
        var messageId = await database.AddMessageAsync(id, request.Role, content, request.ImagePath, token, request.StickerId);
        return await database.GetMessageAsync(id, messageId, token) ?? throw new InvalidOperationException("消息写入后无法读取");
    }
}

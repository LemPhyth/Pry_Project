using Pry.Core.Models;

namespace Pry.App.Services;

public sealed record ConversationListSnapshot(
    IReadOnlyList<ConversationRoom> Rooms,
    IReadOnlyList<ConversationFolder> Folders);

public sealed class ConversationListSynchronizer(
    Func<Task<IReadOnlyList<ConversationRoom>>> loadRooms,
    Func<Task<IReadOnlyList<ConversationFolder>>> loadFolders)
{
    private long _requestedVersion;

    public async Task<ConversationListSnapshot?> LoadLatestAsync()
    {
        var version = Interlocked.Increment(ref _requestedVersion);
        var roomsTask = loadRooms();
        var foldersTask = loadFolders();
        await Task.WhenAll(roomsTask, foldersTask);
        if (version != Volatile.Read(ref _requestedVersion)) return null;
        return new ConversationListSnapshot(await roomsTask, await foldersTask);
    }
}

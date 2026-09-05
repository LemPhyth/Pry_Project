using Pry.App.Services;
using Pry.Core.Models;
using Xunit;

namespace Pry.App.Tests;

public sealed class ConversationListSynchronizerTests
{
    [Fact]
    public async Task Loads_rooms_and_folders_concurrently()
    {
        var roomsStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var foldersStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var synchronizer = new ConversationListSynchronizer(async () =>
        {
            roomsStarted.SetResult();
            await foldersStarted.Task;
            return [Room("room")];
        }, async () =>
        {
            foldersStarted.SetResult();
            await roomsStarted.Task;
            return [Folder("folder")];
        });
        var result = await synchronizer.LoadLatestAsync();
        Assert.Equal("room", Assert.Single(result!.Rooms).Id);
        Assert.Equal("folder", Assert.Single(result.Folders).Id);
    }

    [Fact]
    public async Task Older_refresh_is_discarded_when_it_finishes_last()
    {
        var firstRooms = new TaskCompletionSource<IReadOnlyList<ConversationRoom>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondRooms = new TaskCompletionSource<IReadOnlyList<ConversationRoom>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstFolders = new TaskCompletionSource<IReadOnlyList<ConversationFolder>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondFolders = new TaskCompletionSource<IReadOnlyList<ConversationFolder>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var roomCall = 0;
        var folderCall = 0;
        var synchronizer = new ConversationListSynchronizer(
            () => Interlocked.Increment(ref roomCall) == 1 ? firstRooms.Task : secondRooms.Task,
            () => Interlocked.Increment(ref folderCall) == 1 ? firstFolders.Task : secondFolders.Task);
        var older = synchronizer.LoadLatestAsync();
        var newer = synchronizer.LoadLatestAsync();
        secondRooms.SetResult([Room("new")]);
        secondFolders.SetResult([Folder("new-folder")]);
        Assert.Equal("new", Assert.Single((await newer)!.Rooms).Id);
        firstRooms.SetResult([Room("old")]);
        firstFolders.SetResult([Folder("old-folder")]);
        Assert.Null(await older);
    }

    private static ConversationRoom Room(string id) =>
        new(id, id, null, DateTimeOffset.Now, DateTimeOffset.Now, 0);

    private static ConversationFolder Folder(string id) => new(id, id, DateTimeOffset.Now);
}

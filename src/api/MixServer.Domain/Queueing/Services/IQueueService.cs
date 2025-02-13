using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Enums;
using MixServer.Domain.Sessions.Entities;

namespace MixServer.Domain.Queueing.Services;

public interface IQueueService
{
    Task LoadQueueStateAsync();
    Task<QueueSnapshot> SetQueueFolderAsync(PlaybackSession nextSession);
    Task<QueueSnapshot> SetQueuePositionAsync(Guid queuePositionId);
    Task<QueueSnapshot> AddToQueueAsync(IFileExplorerFileNode file);
    Task<QueueSnapshot> RemoveUserQueueItemsAsync(List<Guid> ids);
    Task<QueueSnapshot> SetQueuePositionAsync(QueueSnapshotItem item);
    Task ResortQueueAsync();
    void ClearQueue();
    Task<QueueSnapshot> GenerateQueueSnapshotAsync();
    Task<(PlaylistIncrementResult Result, QueueSnapshotItem? Snapshot)> GetQueueSnapshotItemAsync(int offset);
    Task<IFileExplorerFileNode> GetCurrentPositionFileOrThrowAsync();
    string? GetCurrentQueueFolderAbsolutePath();
}
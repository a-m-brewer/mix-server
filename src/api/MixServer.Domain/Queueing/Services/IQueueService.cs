using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Enums;
using MixServer.Domain.Sessions.Entities;

namespace MixServer.Domain.Queueing.Services;

public interface IQueueService
{
    Task LoadQueueStateAsync();
    Task<QueueSnapshot> SetQueueFolderAsync(PlaybackSession nextSession);
    Task SetQueuePositionAsync(Guid queuePositionId);
    Task AddToQueueAsync(IFileExplorerFileNode file);
    Task RemoveUserQueueItemsAsync(List<Guid> ids);
    Task<(PlaylistIncrementResult Result, QueueSnapshot Snapshot)> IncrementQueuePositionAsync(int offset);
    Task ResortQueueAsync();
    void ClearQueue();
    Task<QueueSnapshot> GenerateQueueSnapshotAsync();
    Task<IFileExplorerFileNode> GetCurrentPositionFileOrThrowAsync();
    string? GetCurrentQueueFolderAbsolutePath();
}
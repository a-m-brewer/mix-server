using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Enums;
using MixServer.Domain.Sessions.Entities;

namespace MixServer.Domain.Queueing.Services;

public interface IQueueService
{
    Task<QueueSnapshot> SetQueueFolderAsync(PlaybackSession nextSession, CancellationToken cancellationToken);
    Task<QueueSnapshot> SetQueuePositionAsync(Guid queuePositionId, CancellationToken cancellationToken);
    Task<QueueSnapshot> AddToQueueAsync(IFileExplorerFileNode file, CancellationToken cancellationToken);
    Task<QueueSnapshot> RemoveUserQueueItemsAsync(List<Guid> ids, CancellationToken cancellationToken);
    Task<(PlaylistIncrementResult Result, QueueSnapshot Snapshot)> IncrementQueuePositionAsync(int offset, CancellationToken cancellationToken);
    Task ResortQueueAsync(CancellationToken cancellationToken);
    void ClearQueue();
    Task<QueueSnapshot> GenerateQueueSnapshotAsync(CancellationToken cancellationToken);
    Task<IFileExplorerFileNode> GetCurrentPositionFileOrThrowAsync(CancellationToken cancellationToken);
    Task<NodePath?> GetCurrentQueueFolderPathAsync(CancellationToken cancellationToken);
}
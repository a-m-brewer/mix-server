using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Models;
using MixServer.Domain.Users.Models;

namespace MixServer.Domain.Queueing.Repositories;

public interface IQueueRepository : ITransientRepository
{
    Task SetFolderAsync(string userId, CancellationToken cancellationToken);
    Task SetFolderAsync(string userId, Guid nodeId, CancellationToken cancellationToken);
    Task SkipAsync(string userId, IDeviceState? deviceState = null, CancellationToken cancellationToken = default);
    Task PreviousAsync(string userId, IDeviceState? deviceState = null, CancellationToken cancellationToken = default);
    Task<QueueItemEntity> SetQueuePositionAsync(string userId, Guid queueItemId, CancellationToken cancellationToken);
    Task AddFileAsync(string userId, NodePath nodePath, CancellationToken cancellationToken);
    void RemoveQueueItems(string userId, List<Guid> ids);
    Task ClearQueueAsync(string userId, CancellationToken cancellationToken);
    
    Task<QueuePosition> GetQueuePositionAsync(
        string userId,
        IDeviceState? deviceState = null,
        CancellationToken cancellationToken = default);

    Task<List<QueueItemEntity>> GetQueuePageAsync(
        string userId,
        Page page,
        CancellationToken cancellationToken = default);

    Task<IFileExplorerFolderEntity?> GetQueueCurrentFolderAsync(string currentUserId,
        CancellationToken cancellationToken);
}
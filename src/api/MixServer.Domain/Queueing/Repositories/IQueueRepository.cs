using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Users.Models;

namespace MixServer.Domain.Queueing.Repositories;

public interface IQueueRepository : ITransientRepository
{
    Task SetFolderAsync(string userId, Guid nodeId, CancellationToken cancellationToken);
    Task SkipAsync(string userId, IDeviceState? deviceState = null, CancellationToken cancellationToken = default);
    Task PreviousAsync(string userId, IDeviceState? deviceState = null, CancellationToken cancellationToken = default);
    Task SetQueuePositionAsync(string userId, Guid fileId, CancellationToken cancellationToken);
    void RemoveQueueItems(string userId, List<Guid> ids);

    Task<QueueItemEntity?> GetCurrentPositionAsync(
        string userId,
        IDeviceState? deviceState = null,
        CancellationToken cancellationToken = default);
    
    Task<QueueItemEntity?> GetNextPositionAsync(
        string userId,
        IDeviceState? deviceState = null,
        CancellationToken cancellationToken = default);

    Task<QueueItemEntity?> GetPreviousPositionAsync(
        string userId,
        IDeviceState? deviceState = null,
        CancellationToken cancellationToken = default);
    
    Task<List<QueueItemEntity>> GetQueuePageAsync(
        string userId,
        Page page,
        CancellationToken cancellationToken = default);
}
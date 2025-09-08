using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Models;
using MixServer.Domain.Queueing.Repositories;
using MixServer.Domain.Sessions.Accessors;
using MixServer.Domain.Users.Repositories;
using MixServer.Domain.Users.Services;

namespace MixServer.Domain.Queueing.Services;

public interface IUserQueueService
{
    Task SetFolderAsync(CancellationToken cancellationToken);
    Task SetFolderAsync(Guid nodeId, CancellationToken cancellationToken);
    Task<QueueItemEntity> SetQueuePositionAsync(Guid queueItemId, CancellationToken cancellationToken);
    Task AddFileAsync(NodePath nodePath, CancellationToken cancellationToken);
    void RemoveQueueItems(List<Guid> ids);
    Task ClearQueueAsync(CancellationToken cancellationToken);
    Task<QueuePosition> GetQueuePositionAsync(CancellationToken cancellationToken = default);
    Task<List<QueueItemEntity>> GetQueuePageAsync(Page page, CancellationToken cancellationToken = default);
    Task<IFileExplorerFolderEntity?> GetQueueCurrentFolderAsync(CancellationToken cancellationToken);
}

public class UserQueueService(
    ICurrentUserRepository currentUserRepository,
    IDeviceTrackingService deviceTrackingService,
    IPlaybackTrackingAccessor playbackTrackingAccessor,
    IQueueRepository queueRepository) : IUserQueueService
{
    public Task SetFolderAsync(CancellationToken cancellationToken)
    {
        return queueRepository.SetFolderAsync(currentUserRepository.CurrentUserId, cancellationToken);
    }

    public Task SetFolderAsync(Guid nodeId, CancellationToken cancellationToken)
    {
        return queueRepository.SetFolderAsync(currentUserRepository.CurrentUserId, nodeId, cancellationToken);
    }

    public Task<QueueItemEntity> SetQueuePositionAsync(Guid queueItemId, CancellationToken cancellationToken)
    {
        return queueRepository.SetQueuePositionAsync(currentUserRepository.CurrentUserId, queueItemId, cancellationToken);
    }

    public Task AddFileAsync(NodePath nodePath, CancellationToken cancellationToken)
    {
        return queueRepository.AddFileAsync(currentUserRepository.CurrentUserId, nodePath, cancellationToken);
    }

    public void RemoveQueueItems(List<Guid> ids)
    {
        queueRepository.RemoveQueueItems(currentUserRepository.CurrentUserId, ids);
    }
    
    public Task ClearQueueAsync(CancellationToken cancellationToken)
    {
        return queueRepository.ClearQueueAsync(currentUserRepository.CurrentUserId, cancellationToken);
    }

    public async Task<QueuePosition> GetQueuePositionAsync(CancellationToken cancellationToken = default)
    {
        var playbackState = await playbackTrackingAccessor.GetPlaybackStateOrDefaultAsync(cancellationToken);
        var deviceState = (playbackState?.HasDevice ?? false) && deviceTrackingService.HasDeviceState(playbackState.DeviceId.Value)
            ? deviceTrackingService.GetDeviceStateOrThrow(playbackState.DeviceId.Value)
            : null;
        
        var position = await queueRepository.GetQueuePositionAsync(
            currentUserRepository.CurrentUserId,
            deviceState,
            cancellationToken);
        
        return position;
    }

    public Task<List<QueueItemEntity>> GetQueuePageAsync(Page page, CancellationToken cancellationToken = default)
    {
        return queueRepository.GetQueuePageAsync(currentUserRepository.CurrentUserId, page, cancellationToken);
    }

    public Task<IFileExplorerFolderEntity?> GetQueueCurrentFolderAsync(CancellationToken cancellationToken)
    {
        return queueRepository.GetQueueCurrentFolderAsync(currentUserRepository.CurrentUserId, cancellationToken);
    }
}
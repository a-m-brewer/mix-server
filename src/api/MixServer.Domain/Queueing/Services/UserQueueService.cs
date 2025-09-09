using MixServer.Domain.Callbacks;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Models;
using MixServer.Domain.Queueing.Repositories;
using MixServer.Domain.Sessions.Accessors;
using MixServer.Domain.Users.Models;
using MixServer.Domain.Users.Repositories;
using MixServer.Domain.Users.Services;
using Range = MixServer.Domain.FileExplorer.Models.Range;

namespace MixServer.Domain.Queueing.Services;

public interface IUserQueueService
{
    Task RefreshQueueAsync(CancellationToken cancellationToken);
    Task SetQueuePositionAndFolderAsync(FileExplorerFileNodeEntity nodeId, CancellationToken cancellationToken);
    Task<QueueItemEntity> SetQueuePositionAsync(Guid queueItemId, CancellationToken cancellationToken);
    Task AddFileAsync(NodePath nodePath, CancellationToken cancellationToken);
    void RemoveQueueItems(List<Guid> ids);
    Task ClearQueueAsync(CancellationToken cancellationToken);
    Task<QueueItemEntity?> GetCurrentPositionAsync(CancellationToken cancellationToken);
    Task<QueuePosition> GetQueuePositionAsync(CancellationToken cancellationToken = default);
    Task<List<QueueItemEntity>> GetQueuePageAsync(Page page, CancellationToken cancellationToken = default);
    Task<List<QueueItemEntity>> GetQueueRangeAsync(Range range, CancellationToken cancellationToken = default);
    Task<IFileExplorerFolderEntity?> GetQueueCurrentFolderAsync(CancellationToken cancellationToken);
}

public class UserQueueService(
    ICurrentDeviceRepository currentDeviceRepository,
    ICurrentUserRepository currentUserRepository,
    IDeviceTrackingService deviceTrackingService,
    IPlaybackTrackingAccessor playbackTrackingAccessor,
    IQueueRepository queueRepository,
    IUnitOfWork unitOfWork) : IUserQueueService
{
    public async Task RefreshQueueAsync(CancellationToken cancellationToken)
    {
        await queueRepository.SetFolderAsync(currentUserRepository.CurrentUserId, cancellationToken);
        NotifyQueueFolderChanged();
    }

    public async Task SetQueuePositionAndFolderAsync(FileExplorerFileNodeEntity file,
        CancellationToken cancellationToken)
    {
        var parentId = file.ParentId ?? file.RootChildId;
        await queueRepository.SetFolderAsync(currentUserRepository.CurrentUserId, parentId, cancellationToken);
        await queueRepository.SetQueuePositionByFileIdAsync(currentUserRepository.CurrentUserId, file.Id,
            cancellationToken);
        NotifyQueueFolderChanged();
    }

    public async Task<QueueItemEntity> SetQueuePositionAsync(Guid queueItemId, CancellationToken cancellationToken)
    {
        var position =
            await queueRepository.SetQueuePositionAsync(currentUserRepository.CurrentUserId, queueItemId,
                cancellationToken);
        NotifyQueuePositionChanged();
        return position;
    }

    public async Task AddFileAsync(NodePath nodePath, CancellationToken cancellationToken)
    {
        var queueItem = await queueRepository.AddFileAsync(currentUserRepository.CurrentUserId, nodePath, cancellationToken);
        NotifyQueueItemsAdded([queueItem]);
    }

    public void RemoveQueueItems(List<Guid> ids)
    {
        queueRepository.RemoveQueueItems(currentUserRepository.CurrentUserId, ids);
        NotifyQueueItemsRemoved(ids);
    }

    public async Task ClearQueueAsync(CancellationToken cancellationToken)
    {
        await queueRepository.ClearQueueAsync(currentUserRepository.CurrentUserId, cancellationToken);
        NotifyQueueFolderChanged();
    }

    public async Task<QueueItemEntity?> GetCurrentPositionAsync(CancellationToken cancellationToken)
    {
        var deviceState = await CurrentDeviceState(cancellationToken);
        return await queueRepository.GetCurrentPositionAsync(currentUserRepository.CurrentUserId, deviceState,
            cancellationToken);
    }

    public async Task<QueuePosition> GetQueuePositionAsync(CancellationToken cancellationToken = default)
    {
        var deviceState = await CurrentDeviceState(cancellationToken);

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

    public Task<List<QueueItemEntity>> GetQueueRangeAsync(Range range, CancellationToken cancellationToken = default)
    {
        return queueRepository.GetQueueRangeAsync(currentUserRepository.CurrentUserId, range, cancellationToken);
    }

    public Task<IFileExplorerFolderEntity?> GetQueueCurrentFolderAsync(CancellationToken cancellationToken)
    {
        return queueRepository.GetQueueCurrentFolderAsync(currentUserRepository.CurrentUserId, cancellationToken);
    }

    private async Task<IDeviceState?> CurrentDeviceState(CancellationToken cancellationToken)
    {
        var playbackState = await playbackTrackingAccessor.GetPlaybackStateOrDefaultAsync(cancellationToken);
        var deviceState = (playbackState?.HasDevice ?? false) &&
                          deviceTrackingService.HasDeviceState(playbackState.DeviceId.Value)
            ? deviceTrackingService.GetDeviceStateOrThrow(playbackState.DeviceId.Value)
            : null;

        return deviceState;
    }
    
    private void NotifyQueueItemsAdded(IEnumerable<QueueItemEntity> queueItem)
    {
        NotifyWithPosition((position, cb) =>
            cb.QueueItemsAdded(currentUserRepository.CurrentUserId, position, queueItem));
    }
    
    private void NotifyQueueItemsRemoved(List<Guid> ids)
    {
        NotifyWithPosition((position, cb) =>
            cb.QueueItemsRemoved(currentUserRepository.CurrentUserId, position, ids));
    }

    private void NotifyQueuePositionChanged(bool notifyCallingDevice = false)
    {
        NotifyWithPosition((position, cb) =>
            cb.QueuePositionChanged(currentUserRepository.CurrentUserId, currentDeviceRepository.DeviceId, position, notifyCallingDevice));
    }

    private void NotifyQueueFolderChanged(bool notifyCallingDevice = false)
    {
        NotifyWithPosition((position, cb) =>
            cb.QueueFolderChanged(currentUserRepository.CurrentUserId, currentDeviceRepository.DeviceId, position, notifyCallingDevice));
    }

    private void NotifyWithPosition(Func<QueuePosition, ICallbackService, Task> notificationAction)
    {
        unitOfWork.InvokeCallbackOnSaved(NotifyAsync);

        return;

        async Task NotifyAsync(ICallbackService callbackService)
        {
            var position = await GetQueuePositionAsync();
            await notificationAction(position, callbackService);
        }
    }
}
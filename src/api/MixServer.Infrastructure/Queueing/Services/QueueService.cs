using Microsoft.Extensions.Logging;
using MixServer.Domain.Callbacks;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Enums;
using MixServer.Domain.Queueing.Services;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Validators;
using MixServer.Domain.Users.Repositories;
using MixServer.Infrastructure.Queueing.Models;
using MixServer.Infrastructure.Queueing.Repositories;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Infrastructure.Queueing.Services;

public class QueueService(
    ICanPlayOnDeviceValidator canPlayOnDeviceValidator,
    ICallbackService callbackService,
    ICurrentDeviceRepository currentDeviceRepository,
    ICurrentUserRepository currentUserRepository,
    IFileService fileService,
    ILogger<QueueService> logger,
    IRootFileExplorerFolder rootFileExplorerFolder,
    IQueueRepository queueRepository,
    IUnitOfWork unitOfWork)
    : IQueueService
{
    public async Task LoadQueueStateAsync()
    {
        if (queueRepository.HasQueue(currentUserRepository.CurrentUserId))
        {
            logger.LogDebug("Skipping initializing queue for user as queue is already initialized");
            return;
        }

        await currentUserRepository.LoadCurrentPlaybackSessionAsync();

        if (currentUserRepository.CurrentUser.CurrentPlaybackSession == null)
        {
            logger.LogDebug("Skipping initializing queue as user has no playback session");
            return;
        }

        await SetQueueFolderAsync(currentUserRepository.CurrentUser.CurrentPlaybackSession);
    }

    public async Task<QueueSnapshot> SetQueueFolderAsync(PlaybackSession nextSession)
    {
        var queue = queueRepository.GetOrAddQueue(currentUserRepository.CurrentUserId);

        queue.CurrentFolderPath = rootFileExplorerFolder.GetNodePath(nextSession.GetParentFolderPathOrThrow());
        queue.ClearUserQueue();
        
        var files = await GetPlayableFilesInFolderAsync(queue.CurrentFolderPath);
        var nextSessionPath = rootFileExplorerFolder.GetNodePath(nextSession.AbsolutePath);
        
        if (files.All(a => !a.Path.IsEqualTo(nextSessionPath)))
        {
            files.Add(fileService.GetFile(nextSessionPath));
        }

        queue.RegenerateFolderQueueSortItems(files);
        queue.SetQueuePositionFromFolderItemOrThrow(nextSessionPath);

        var queueSnapshot = GenerateQueueSnapshot(queue, files);

        unitOfWork.InvokeCallbackOnSaved(c =>
            c.CurrentQueueUpdated(currentUserRepository.CurrentUserId, currentDeviceRepository.DeviceId, queueSnapshot));

        return queueSnapshot;
    }

    public async Task<QueueSnapshot> SetQueuePositionAsync(Guid queuePositionId)
    {
        var queue = queueRepository.GetOrAddQueue(currentUserRepository.CurrentUserId);
        
        if (!queue.QueueItemExists(queuePositionId))
        {
            throw new NotFoundException(nameof(QueueSnapshot), queuePositionId);
        }

        queue.SetQueuePosition(queuePositionId);
        
        var queueSnapshot = await GenerateQueueSnapshotAsync(queue);
        unitOfWork.InvokeCallbackOnSaved(c =>
            c.CurrentQueueUpdated(currentUserRepository.CurrentUserId, currentDeviceRepository.DeviceId, queueSnapshot));

        return queueSnapshot;
    }

    public async Task<QueueSnapshot> AddToQueueAsync(IFileExplorerFileNode file)
    {
        if (!file.PlaybackSupported)
        {
            throw new InvalidRequestException(nameof(file.PlaybackSupported), "Playback not supported for this file");
        }

        var queue = queueRepository.GetOrAddQueue(currentUserRepository.CurrentUserId);
        queue.AddToQueue(file);
        
        var queueSnapshot = await GenerateQueueSnapshotAsync(queue);
        await callbackService.CurrentQueueUpdated(currentUserRepository.CurrentUserId, currentDeviceRepository.DeviceId, queueSnapshot);

        return queueSnapshot;
    }

    public async Task<QueueSnapshot> RemoveUserQueueItemsAsync(List<Guid> ids)
    {
        var queue = queueRepository.GetOrAddQueue(currentUserRepository.CurrentUserId);

        if (queue.CurrentQueuePositionId.HasValue && ids.Contains(queue.CurrentQueuePositionId.Value))
        {
            throw new InvalidRequestException("Can not remove the current item in the queue");
        }

        if (!queue.AllIdsPresentInUserQueue(ids))
        {
            throw new InvalidRequestException("User Queue Item does not exist");
        }

        queue.RemoveUserQueueItems(ids);
        
        var queueSnapshot = await GenerateQueueSnapshotAsync(queue);
        await callbackService.CurrentQueueUpdated(currentUserRepository.CurrentUserId, currentDeviceRepository.DeviceId, queueSnapshot);

        return queueSnapshot;
    }

    public async Task<(PlaylistIncrementResult Result, QueueSnapshot Snapshot)> IncrementQueuePositionAsync(int offset)
    {
        var queue = queueRepository.GetOrAddQueue(currentUserRepository.CurrentUserId);
        var queueOrder = queue.QueueOrder;
        
        var currentQueueItemIndex = queueOrder.FindIndex(id => id == queue.CurrentQueuePositionId);

        var nextIndex = currentQueueItemIndex + offset;

        if (nextIndex < 0)
        {
            return (PlaylistIncrementResult.PreviousOutOfBounds, QueueSnapshot.Empty);
        }

        if (queueOrder.Count <= nextIndex)
        {
            return (PlaylistIncrementResult.NextOutOfBounds, QueueSnapshot.Empty);
        }

        var nextQueuePositionId = queueOrder[nextIndex];

        queue.SetQueuePosition(nextQueuePositionId);
        
        var queueSnapshot = await GenerateQueueSnapshotAsync(queue);
        unitOfWork.InvokeCallbackOnSaved(c =>
            c.CurrentQueueUpdated(currentUserRepository.CurrentUserId, currentDeviceRepository.DeviceId, queueSnapshot));
        
        return (PlaylistIncrementResult.Success, queueSnapshot);
    }

    public async Task ResortQueueAsync()
    {
        var queue = queueRepository.GetOrAddQueue(currentUserRepository.CurrentUserId);
     
        var files = await GetPlayableFilesInFolderAsync(queue.CurrentFolderPath);

        queue.Sort(files);

        var queueSnapshot = GenerateQueueSnapshot(queue, files);
        unitOfWork.InvokeCallbackOnSaved(c => c.CurrentQueueUpdated(currentUserRepository.CurrentUserId, queueSnapshot));
    }

    public void ClearQueue()
    {
        queueRepository.Remove(currentUserRepository.CurrentUserId);
        unitOfWork.InvokeCallbackOnSaved(c =>
            c.CurrentQueueUpdated(currentUserRepository.CurrentUserId, currentDeviceRepository.DeviceId, QueueSnapshot.Empty));
    }

    public async Task<IFileExplorerFileNode> GetCurrentPositionFileOrThrowAsync()
    {
        var queue = queueRepository.GetOrAddQueue(currentUserRepository.CurrentUserId);
        var queueSnapshot = await GenerateQueueSnapshotAsync(queue);

        return queueSnapshot.Items.FirstOrDefault(f => f.Id == queue.CurrentQueuePositionId)?.File
               ?? throw new NotFoundException(nameof(QueueSnapshot),
                   queue.CurrentQueuePositionId.ToString() ?? "unknown");
    }

    public NodePath? GetCurrentQueueFolderPath()
    {
        return queueRepository.GetOrAddQueue(currentUserRepository.CurrentUserId).CurrentFolderPath;
    }

    public async Task<QueueSnapshot> GenerateQueueSnapshotAsync()
    {
        var queue = queueRepository.GetOrAddQueue(currentUserRepository.CurrentUserId);

        return await GenerateQueueSnapshotAsync(queue);
    }
    
    private async Task<QueueSnapshot> GenerateQueueSnapshotAsync(UserQueue userQueue)
    {
        return GenerateQueueSnapshot(userQueue, await GetPlayableFilesInFolderAsync(userQueue.CurrentFolderPath));
    }
    
    private QueueSnapshot GenerateQueueSnapshot(UserQueue userQueue, IEnumerable<IFileExplorerFileNode> folderFiles)
    {
        var userQueueFiles = fileService.GetFiles(userQueue.UserQueueItemsAbsoluteFilePaths);

        var allFiles = new List<IFileExplorerFileNode>(userQueueFiles);
        allFiles.AddRange(folderFiles);

        var distinctFiles = allFiles
            .DistinctBy(d => d.Path.AbsolutePath)
            .ToDictionary(k => k.Path, v => v);

        var items = userQueue.GenerateQueueSnapshotItems(distinctFiles);
        
        var previousValidOffset = userQueue.CurrentQueuePositionId.HasValue
            ? GetNextValidQueuePosition(userQueue.CurrentQueuePositionId.Value, false, items)
            : null;
        var nextValidOffset = userQueue.CurrentQueuePositionId.HasValue
            ? GetNextValidQueuePosition(userQueue.CurrentQueuePositionId.Value, true, items)
            : null;
        
        return new QueueSnapshot(userQueue.CurrentQueuePositionId, previousValidOffset, nextValidOffset, items);
    }

    private async Task<List<IFileExplorerFileNode>> GetPlayableFilesInFolderAsync(NodePath? nodePath)
    {
        if (nodePath is null)
        {
            return [];
        }

        var folder = await fileService.GetFolderAsync(nodePath);

        return folder.GenerateSortedChildren<IFileExplorerFileNode>()
            .Where(w => w.PlaybackSupported)
            .ToList();
    }
    
    private Guid? GetNextValidQueuePosition(
        Guid currentQueuePosition,
        bool skip,
        List<QueueSnapshotItem> queueItems)
    {
        var requestedOffset = skip ? 1 : -1;
        
        var currentIndex = queueItems.FindIndex(f => f.Id == currentQueuePosition);
        var offsetIndex = currentIndex + requestedOffset;

        while (offsetIndex >= 0 && offsetIndex < queueItems.Count)
        {
            var item = queueItems[offsetIndex];
            if (canPlayOnDeviceValidator.CanPlay(item.File))
            {
                return item.Id;
            }

            var increment = requestedOffset < 0 ? -1 : 1;
            offsetIndex += increment;
        }

        return null;
    }
}
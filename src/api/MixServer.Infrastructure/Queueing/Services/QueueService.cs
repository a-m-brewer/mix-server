using Microsoft.Extensions.Logging;
using MixServer.Domain.Callbacks;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Persistence;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Enums;
using MixServer.Domain.Queueing.Services;
using MixServer.Domain.Sessions.Accessors;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Validators;
using MixServer.Domain.Users.Repositories;
using MixServer.Infrastructure.Queueing.Models;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Infrastructure.Queueing.Services;

// public class QueueService(
//     ICanPlayOnDeviceValidator canPlayOnDeviceValidator,
//     ICurrentDeviceRepository currentDeviceRepository,
//     ICurrentUserRepository currentUserRepository,
//     IRequestedPlaybackDeviceAccessor requestedPlaybackDeviceAccessor,
//     IFileService fileService,
//     ILogger<QueueService> logger,
//     IQueueRepository queueRepository,
//     IUnitOfWork unitOfWork)
//     : IQueueService
// {
//     public async Task<QueueSnapshot> SetQueueFolderAsync(PlaybackSession nextSession, CancellationToken cancellationToken)
//     {
//         var queue = await GetOrAddQueueAsync(cancellationToken);
//
//         return await SetQueueFolderAsync(queue, nextSession, cancellationToken);
//     }
//
//     public async Task<QueueSnapshot> SetQueuePositionAsync(Guid queuePositionId, CancellationToken cancellationToken)
//     {
//         var queue = await GetOrAddQueueAsync(cancellationToken);
//         
//         if (!queue.QueueItemExists(queuePositionId))
//         {
//             throw new NotFoundException(nameof(QueueSnapshot), queuePositionId);
//         }
//
//         queue.SetQueuePosition(queuePositionId);
//         
//         var queueSnapshot = await GenerateQueueSnapshotAsync(queue, cancellationToken);
//         unitOfWork.InvokeCallbackOnSaved(c =>
//             c.CurrentQueueUpdated(currentUserRepository.CurrentUserId, currentDeviceRepository.DeviceId, queueSnapshot));
//
//         return queueSnapshot;
//     }
//
//     public async Task<QueueSnapshot> AddToQueueAsync(IFileExplorerFileNode file, CancellationToken cancellationToken)
//     {
//         if (!file.PlaybackSupported)
//         {
//             throw new InvalidRequestException(nameof(file.PlaybackSupported), "Playback not supported for this file");
//         }
//
//         var queue = await GetOrAddQueueAsync(cancellationToken);
//         queue.AddToQueue(file);
//         
//         var queueSnapshot = await GenerateQueueSnapshotAsync(queue, cancellationToken);
//         unitOfWork.InvokeCallbackOnSaved(cb => cb.CurrentQueueUpdated(currentUserRepository.CurrentUserId, currentDeviceRepository.DeviceId, queueSnapshot));
//
//         return queueSnapshot;
//     }
//
//     public async Task<QueueSnapshot> RemoveUserQueueItemsAsync(List<Guid> ids, CancellationToken cancellationToken)
//     {
//         var queue = await GetOrAddQueueAsync(cancellationToken);
//
//         if (queue.CurrentQueuePositionId.HasValue && ids.Contains(queue.CurrentQueuePositionId.Value))
//         {
//             throw new InvalidRequestException("Can not remove the current item in the queue");
//         }
//
//         if (!queue.AllIdsPresentInUserQueue(ids))
//         {
//             throw new InvalidRequestException("User Queue Item does not exist");
//         }
//
//         queue.RemoveUserQueueItems(ids);
//         
//         var queueSnapshot = await GenerateQueueSnapshotAsync(queue, cancellationToken);
//         unitOfWork.InvokeCallbackOnSaved(cb => cb.CurrentQueueUpdated(currentUserRepository.CurrentUserId, currentDeviceRepository.DeviceId, queueSnapshot));
//
//         return queueSnapshot;
//     }
//
//     public async Task<(PlaylistIncrementResult Result, QueueSnapshot Snapshot)> IncrementQueuePositionAsync(int offset, CancellationToken cancellationToken)
//     {
//         var queue = await GetOrAddQueueAsync(cancellationToken);
//         var queueOrder = queue.QueueOrder;
//         
//         var currentQueueItemIndex = queueOrder.FindIndex(id => id == queue.CurrentQueuePositionId);
//
//         var nextIndex = currentQueueItemIndex + offset;
//
//         if (nextIndex < 0)
//         {
//             return (PlaylistIncrementResult.PreviousOutOfBounds, QueueSnapshot.Empty);
//         }
//
//         if (queueOrder.Count <= nextIndex)
//         {
//             return (PlaylistIncrementResult.NextOutOfBounds, QueueSnapshot.Empty);
//         }
//
//         var nextQueuePositionId = queueOrder[nextIndex];
//
//         queue.SetQueuePosition(nextQueuePositionId);
//         
//         var queueSnapshot = await GenerateQueueSnapshotAsync(queue, cancellationToken);
//         unitOfWork.InvokeCallbackOnSaved(c =>
//             c.CurrentQueueUpdated(currentUserRepository.CurrentUserId, currentDeviceRepository.DeviceId, queueSnapshot));
//         
//         return (PlaylistIncrementResult.Success, queueSnapshot);
//     }
//
//     public async Task ResortQueueAsync(CancellationToken cancellationToken)
//     {
//         var queue = await GetOrAddQueueAsync(cancellationToken);
//      
//         var files = await GetPlayableFilesInFolderAsync(queue.CurrentFolderPath, cancellationToken);
//
//         queue.Sort(files);
//
//         var queueSnapshot = await GenerateQueueSnapshotAsync(queue, files);
//         unitOfWork.InvokeCallbackOnSaved(c => c.CurrentQueueUpdated(currentUserRepository.CurrentUserId, queueSnapshot));
//     }
//
//     public void ClearQueue()
//     {
//         queueRepository.Remove(currentUserRepository.CurrentUserId);
//         unitOfWork.InvokeCallbackOnSaved(c =>
//             c.CurrentQueueUpdated(currentUserRepository.CurrentUserId, currentDeviceRepository.DeviceId, QueueSnapshot.Empty));
//     }
//
//     public async Task<IFileExplorerFileNode> GetCurrentPositionFileOrThrowAsync(CancellationToken cancellationToken)
//     {
//         var queue = await GetOrAddQueueAsync(cancellationToken);
//         var queueSnapshot = await GenerateQueueSnapshotAsync(queue, cancellationToken);
//
//         return queueSnapshot.Items.FirstOrDefault(f => f.Id == queue.CurrentQueuePositionId)?.File
//                ?? throw new NotFoundException(nameof(QueueSnapshot),
//                    queue.CurrentQueuePositionId.ToString() ?? "unknown");
//     }
//
//     public async Task<NodePath?> GetCurrentQueueFolderPathAsync(CancellationToken cancellationToken)
//     {
//         return (await GetOrAddQueueAsync(cancellationToken)).CurrentFolderPath;
//     }
//
//     public async Task<QueueSnapshot> GenerateQueueSnapshotAsync(CancellationToken cancellationToken)
//     {
//         var queue = await GetOrAddQueueAsync(cancellationToken);
//
//         return await GenerateQueueSnapshotAsync(queue, cancellationToken);
//     }
//     
//     private async Task<QueueEntity> GetOrAddQueueAsync(CancellationToken cancellationToken)
//     {
//         await currentUserRepository.LoadQueueAsync(cancellationToken);
//         var currentUser = await currentUserRepository.GetCurrentUserAsync();
//         
//         if (currentUser.Queue is not null)
//         {
//             logger.LogDebug("Skipping initializing queue for user as queue is already initialized");
//             return currentUser.Queue;
//         }
//
//         await currentUserRepository.LoadCurrentPlaybackSessionAsync(cancellationToken);
//
//         var queue = queueRepository.CreateAsync(currentUser.Id);
//         currentUser.Queue = queue;
//
//         await SetQueueFolderAsync(queue, currentUser.CurrentPlaybackSession, cancellationToken);
//         
//         return queue;
//     }
//     
//     private async Task<QueueSnapshot> SetQueueFolderAsync(
//         QueueEntity queue,
//         PlaybackSession? nextSession,
//         CancellationToken cancellationToken)
//     {
//         queue.SetCurrentFolderAndPosition(nextSession);
//         await ClearUserQueueAsync(queue, cancellationToken);
//
//         var queueSnapshot = await GenerateQueueSnapshotAsync(queue);
//
//         // TODO: work out the best way to do updates for paged queues
//         // unitOfWork.InvokeCallbackOnSaved(c =>
//         //     c.CurrentQueueUpdated(currentUserRepository.CurrentUserId, currentDeviceRepository.DeviceId, queueSnapshot));
//
//         return queueSnapshot;
//     }
//
//     private async Task ClearUserQueueAsync(QueueEntity queue, CancellationToken cancellationToken)
//     {
//         await queueRepository.MarkUserQueueItemsAsDeletedAsync(queue, cancellationToken);
//         queue.UserQueueItems.Clear();
//     }
//     
//     private async Task<QueueSnapshot> GenerateQueueSnapshotAsync(QueueEntity userQueue, CancellationToken cancellationToken)
//     {
//         var userQueueFiles = userQueue.UserQueueItems;
//
//         var allFiles = new List<IFileExplorerFileNode>(userQueueFiles);
//         allFiles.AddRange(folderFiles);
//
//         var distinctFiles = allFiles
//             .DistinctBy(d => d.Path.AbsolutePath)
//             .ToDictionary(k => k.Path, v => v);
//
//         var items = userQueue.GenerateQueueSnapshotItems(distinctFiles);
//         
//         var previousValidOffset = userQueue.CurrentQueuePositionId.HasValue
//             ? await GetNextValidQueuePositionAsync(userQueue.CurrentQueuePositionId.Value, false, items)
//             : null;
//         var nextValidOffset = userQueue.CurrentQueuePositionId.HasValue
//             ? await GetNextValidQueuePositionAsync(userQueue.CurrentQueuePositionId.Value, true, items)
//             : null;
//         
//         return new QueueSnapshot(userQueue.CurrentQueuePositionId, previousValidOffset, nextValidOffset, items);
//     }
//
//     private async Task<List<IFileExplorerFileNode>> GetPlayableFilesInFolderAsync(NodePath? nodePath, CancellationToken cancellationToken)
//     {
//         if (nodePath is null)
//         {
//             return [];
//         }
//
//         // TODO: paging
//         var folder = await fileService.GetFolderAsync(nodePath, null, cancellationToken);
//
//         return folder.GenerateSortedChildren<IFileExplorerFileNode>()
//             .Where(w => w.PlaybackSupported)
//             .ToList();
//     }
//     
//     private async Task<Guid?> GetNextValidQueuePositionAsync(Guid currentQueuePosition,
//         bool skip,
//         List<QueueSnapshotItem> queueItems)
//     {
//         if (!await requestedPlaybackDeviceAccessor.HasPlaybackDeviceAsync())
//         {
//             return null;
//         }
//         
//         var requestedOffset = skip ? 1 : -1;
//         
//         var currentIndex = queueItems.FindIndex(f => f.Id == currentQueuePosition);
//         var offsetIndex = currentIndex + requestedOffset;
//
//         while (offsetIndex >= 0 && offsetIndex < queueItems.Count)
//         {
//             var item = queueItems[offsetIndex];
//             if (await canPlayOnDeviceValidator.CanPlayAsync(item.File))
//             {
//                 return item.Id;
//             }
//
//             var increment = requestedOffset < 0 ? -1 : 1;
//             offsetIndex += increment;
//         }
//
//         return null;
//     }
// }
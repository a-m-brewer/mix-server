using Microsoft.Extensions.Logging;
using MixServer.Domain.Callbacks;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Enums;
using MixServer.Domain.Queueing.Services;
using MixServer.Domain.Sessions.Entities;
using MixServer.Infrastructure.Queueing.Models;
using MixServer.Infrastructure.Queueing.Repositories;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Infrastructure.Queueing.Services;

public class QueueService : IQueueService
{
    private readonly ICallbackService _callbackService;
    private readonly ICurrentUserRepository _currentUserRepository;
    private readonly IFileService _fileService;
    private readonly ILogger<QueueService> _logger;
    private readonly IQueueRepository _queueRepository;

    public QueueService(
        ICallbackService callbackService,
        ICurrentUserRepository currentUserRepository,
        IFileService fileService,
        ILogger<QueueService> logger,
        IQueueRepository queueRepository)
    {
        _callbackService = callbackService;
        _currentUserRepository = currentUserRepository;
        _fileService = fileService;
        _logger = logger;
        _queueRepository = queueRepository;
    }

    public async Task LoadQueueStateAsync()
    {
        if (_queueRepository.HasQueue(_currentUserRepository.CurrentUserId))
        {
            _logger.LogDebug("Skipping initializing queue for user as queue is already initialized");
            return;
        }

        await _currentUserRepository.LoadCurrentPlaybackSessionAsync();

        if (_currentUserRepository.CurrentUser.CurrentPlaybackSession == null)
        {
            _logger.LogDebug("Skipping initializing queue as user has no playback session");
            return;
        }

        await SetQueueFolderAsync(_currentUserRepository.CurrentUser.CurrentPlaybackSession);
    }

    public async Task SetQueueFolderAsync(PlaybackSession nextSession)
    {
        var queue = _queueRepository.GetOrAddQueue(_currentUserRepository.CurrentUserId);

        queue.CurrentFolderAbsolutePath = nextSession.GetParentFolderPathOrThrow();
        queue.ClearUserQueue();

        var files = await GetPlayableFilesInFolderAsync(queue.CurrentFolderAbsolutePath);

        queue.RegenerateFolderQueueSortItems(files);
        queue.SetQueuePositionFromFolderItemOrThrow(nextSession.AbsolutePath);

        var queueSnapshot = GenerateQueueSnapshot(queue, files);
        _callbackService.InvokeCallbackOnSaved(c => c.CurrentQueueUpdated(_currentUserRepository.CurrentUserId, queueSnapshot));
    }

    public async Task SetQueuePositionAsync(Guid queuePositionId)
    {
        var queue = _queueRepository.GetOrAddQueue(_currentUserRepository.CurrentUserId);
        
        if (!queue.QueueItemExists(queuePositionId))
        {
            throw new NotFoundException(nameof(QueueSnapshot), queuePositionId);
        }

        queue.SetQueuePosition(queuePositionId);
        
        var queueSnapshot = await GenerateQueueSnapshotAsync(queue);
        _callbackService.InvokeCallbackOnSaved(c => c.CurrentQueueUpdated(_currentUserRepository.CurrentUserId, queueSnapshot));
    }

    public async Task AddToQueueAsync(IFileExplorerFileNode file)
    {
        if (!file.PlaybackSupported)
        {
            throw new InvalidRequestException(nameof(file.PlaybackSupported), "Playback not supported for this file");
        }

        var queue = _queueRepository.GetOrAddQueue(_currentUserRepository.CurrentUserId);
        queue.AddToQueue(file);
        
        var queueSnapshot = await GenerateQueueSnapshotAsync(queue);
        await _callbackService.CurrentQueueUpdated(_currentUserRepository.CurrentUserId, queueSnapshot);
    }

    public async Task RemoveUserQueueItemsAsync(List<Guid> ids)
    {
        var queue = _queueRepository.GetOrAddQueue(_currentUserRepository.CurrentUserId);

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
        await _callbackService.CurrentQueueUpdated(_currentUserRepository.CurrentUserId, queueSnapshot);
    }

    public async Task<(PlaylistIncrementResult Result, QueueSnapshot Snapshot)> IncrementQueuePositionAsync(int offset)
    {
        var queue = _queueRepository.GetOrAddQueue(_currentUserRepository.CurrentUserId);
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
        _callbackService.InvokeCallbackOnSaved(c => c.CurrentQueueUpdated(_currentUserRepository.CurrentUserId, queueSnapshot));
        
        return (PlaylistIncrementResult.Success, queueSnapshot);
    }

    public async Task ResortQueueAsync()
    {
        var queue = _queueRepository.GetOrAddQueue(_currentUserRepository.CurrentUserId);
     
        var files = await GetPlayableFilesInFolderAsync(queue.CurrentFolderAbsolutePath);

        queue.Sort(files);

        var queueSnapshot = GenerateQueueSnapshot(queue, files);
        _callbackService.InvokeCallbackOnSaved(c => c.CurrentQueueUpdated(_currentUserRepository.CurrentUserId, queueSnapshot));
    }

    public void ClearQueue()
    {
        _queueRepository.Remove(_currentUserRepository.CurrentUserId);
        _callbackService.InvokeCallbackOnSaved(c => c.CurrentQueueUpdated(_currentUserRepository.CurrentUserId, QueueSnapshot.Empty));
    }

    public async Task<IFileExplorerFileNode> GetCurrentPositionFileOrThrowAsync()
    {
        var queue = _queueRepository.GetOrAddQueue(_currentUserRepository.CurrentUserId);
        var queueSnapshot = await GenerateQueueSnapshotAsync(queue);

        return queueSnapshot.Items.FirstOrDefault(f => f.Id == queue.CurrentQueuePositionId)?.File
               ?? throw new NotFoundException(nameof(QueueSnapshot),
                   queue.CurrentQueuePositionId.ToString() ?? "unknown");
    }

    public string? GetCurrentQueueFolderAbsolutePath()
    {
        return _queueRepository.GetOrAddQueue(_currentUserRepository.CurrentUserId).CurrentFolderAbsolutePath;
    }

    public async Task<QueueSnapshot> GenerateQueueSnapshotAsync()
    {
        var queue = _queueRepository.GetOrAddQueue(_currentUserRepository.CurrentUserId);

        return await GenerateQueueSnapshotAsync(queue);
    }
    
    private async Task<QueueSnapshot> GenerateQueueSnapshotAsync(UserQueue userQueue)
    {
        return GenerateQueueSnapshot(userQueue, await GetPlayableFilesInFolderAsync(userQueue.CurrentFolderAbsolutePath));
    }
    
    private QueueSnapshot GenerateQueueSnapshot(UserQueue userQueue, IEnumerable<IFileExplorerFileNode> folderFiles)
    {
        var userQueueFiles = _fileService.GetFiles(userQueue.UserQueueItemsAbsoluteFilePaths);

        var allFiles = new List<IFileExplorerFileNode>(userQueueFiles);
        allFiles.AddRange(folderFiles);

        var distinctFiles = allFiles
            .Where(w => !string.IsNullOrWhiteSpace(w.AbsolutePath))
            .DistinctBy(d => d.AbsolutePath!)
            .ToDictionary(k => k.AbsolutePath!, v => v);

        return userQueue.GenerateQueueSnapshot(distinctFiles);
    }

    private async Task<List<IFileExplorerFileNode>> GetPlayableFilesInFolderAsync(string? absoluteFolderPath)
    {
        if (string.IsNullOrWhiteSpace(absoluteFolderPath))
        {
            return [];
        }

        var folder = await _fileService.GetFilesInFolderAsync(absoluteFolderPath);

        return folder.ChildPlayableFiles;
    }
}
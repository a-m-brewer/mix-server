using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Enums;
using MixServer.Domain.Utilities;

namespace MixServer.Infrastructure.Queueing.Models;

public class UserQueue
{
    private readonly IReadWriteLock _readWriteLock;
    private string? _currentFolderAbsolutePath;

    public UserQueue(string userId, IReadWriteLock readWriteLock)
    {
        _readWriteLock = readWriteLock;
        UserId = userId;
    }

    public string UserId { get; }

    public string? CurrentFolderAbsolutePath
    {
        get => _readWriteLock.ForRead(() => _currentFolderAbsolutePath);
        set => _readWriteLock.ForWrite(() => _currentFolderAbsolutePath = value);
    }

    private Dictionary<string, FolderQueueSortItem> CurrentFolderSortItems { get; } = new();

    private List<UserQueueSortItem> UserQueueSortItems { get; } = [];

    private List<FolderQueueSortItem> OrderedCurrentFolderQueueSortItems => CurrentFolderSortItems
        .Values
        .OrderBy(o => o.Position)
        .ToList();

    public IReadOnlyList<string> UserQueueItemsAbsoluteFilePaths => _readWriteLock.ForRead(() => UserQueueSortItems
        .Select(s => s.AbsoluteFilePath)
        .Distinct()
        .ToList());

    /// <summary>
    /// The last queue item played that was from the folder of <see cref="CurrentFolderAbsolutePath"/>
    /// As User queue items should only be related to folder items
    /// </summary>
    private Guid? CurrentFolderQueueItemPositionId { get; set; }

    /// <summary>
    /// The actual current point in the queue
    /// </summary>
    public Guid? CurrentQueuePositionId { get; private set; }

    public void Sort(IEnumerable<IFileExplorerFileNode> files)
    {
        var filteredFiles = files
            .Where(w => !string.IsNullOrWhiteSpace(w.AbsolutePath))
            .ToList();

        _readWriteLock.ForWrite(() =>
        {
            foreach (var (_, item) in CurrentFolderSortItems)
            {
                var index = filteredFiles.FindIndex(f => f.AbsolutePath == item.AbsoluteFilePath);
                if (index == -1)
                {
                    continue;
                }

                item.Position = index;
            }
        });
    }

    public void SetQueuePosition(Guid id)
    {
        _readWriteLock.ForWrite(() =>
        {
            var folderItem = CurrentFolderSortItems.Values.FirstOrDefault(f => f.Id == id);
            if (folderItem != null)
            {
                CurrentFolderQueueItemPositionId = id;
            }

            CurrentQueuePositionId = id;
        });
    }

    public void ClearUserQueue() => _readWriteLock.ForWrite(() => UserQueueSortItems.Clear());
    
    public void RegenerateFolderQueueSortItems(IEnumerable<IFileExplorerFileNode> files)
    {
        _readWriteLock.ForWrite(() =>
        {
            CurrentFolderSortItems.Clear();

            var filteredFiles = files
                .Where(w => !string.IsNullOrWhiteSpace(w.AbsolutePath))
                .ToList();

            for (var i = 0; i < filteredFiles.Count; i++)
            {
                var file = filteredFiles[i];
                CurrentFolderSortItems[file.AbsolutePath!] = new FolderQueueSortItem(Guid.NewGuid(), file.AbsolutePath!, i);
            }
        });
    }
    
    public void SetQueuePositionFromFolderItemOrThrow(string absoluteFilePath)
    {
        _readWriteLock.ForUpgradeableRead(() =>
        {
            if (!CurrentFolderSortItems.TryGetValue(absoluteFilePath, out var queueItemId))
            {
                throw new NotFoundException(nameof(CurrentFolderSortItems), absoluteFilePath);
            }

            _readWriteLock.ForWrite(() =>
            {
                CurrentFolderQueueItemPositionId = queueItemId.Id;
                CurrentQueuePositionId = queueItemId.Id;
            });
        });
    }

    public void AddToQueue(IFileExplorerFileNode file)
    {
        _readWriteLock.ForWrite(() =>
            UserQueueSortItems.Add(new UserQueueSortItem(Guid.NewGuid(), file.AbsolutePath!, CurrentFolderQueueItemPositionId)));
    }
    
    public void RemoveUserQueueItems(List<Guid> ids)
    {
        _readWriteLock.ForWrite(() => UserQueueSortItems.RemoveAll(r => ids.Contains(r.Id)));
    }

    public bool QueueItemExists(Guid queuePositionId)
    {
        return _readWriteLock.ForRead(() => CurrentFolderSortItems.Values.Any(a => a.Id == queuePositionId) ||
                                            UserQueueSortItems.Any(a => a.Id == queuePositionId));
    }

    public bool AllIdsPresentInUserQueue(List<Guid> ids)
    {
        return _readWriteLock.ForRead(() => ids.All(id => UserQueueSortItems.Any(a => a.Id == id)));
    }

    public QueueSnapshot GenerateQueueSnapshot(Dictionary<string, IFileExplorerFileNode> files) =>
        _readWriteLock.ForRead(() =>
        {
            var finalQueue = GenerateQueueOrder()
                .Select(i => GenerateSnapshotQueueItem(i, files))
                .ToList();

            return new QueueSnapshot(CurrentQueuePositionId, finalQueue);
        });

    public List<Guid> QueueOrder => _readWriteLock.ForRead(() => GenerateQueueOrder().Select(s => s.Id).ToList());

    private IEnumerable<QueueSortItem> GenerateQueueOrder()
    {
        var finalQueue = UserQueueSortItems
            .Where(w => !w.PreviousFolderItemId.HasValue)
            .Select(s => s)
            .Cast<QueueSortItem>()
            .ToList();

        foreach (var item in OrderedCurrentFolderQueueSortItems)
        {
            finalQueue.Add(item);
            finalQueue
                .AddRange(UserQueueSortItems
                    .OrderBy(o => o.Added)
                    .Where(w => w.PreviousFolderItemId == item.Id));
        }

        return finalQueue;
    }

    private static QueueSnapshotItem GenerateSnapshotQueueItem(QueueSortItem item, Dictionary<string, IFileExplorerFileNode> files)
    {
        return new QueueSnapshotItem(
            item.Id,
            item switch
            {
                FolderQueueSortItem => QueueSnapshotItemType.Folder,
                UserQueueSortItem => QueueSnapshotItemType.User,
                _ => throw new ArgumentOutOfRangeException(nameof(item))
            },
            files.TryGetValue(item.AbsoluteFilePath, out var file)
                ? file
                : throw new NotFoundException(nameof(files), item.AbsoluteFilePath));
    }
}
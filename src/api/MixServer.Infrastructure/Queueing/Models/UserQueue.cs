using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Enums;
using MixServer.Domain.Utilities;

namespace MixServer.Infrastructure.Queueing.Models;

public class UserQueue(string userId, IReadWriteLock readWriteLock)
{
    private string? _currentFolderAbsolutePath;

    public string UserId { get; } = userId;

    public string? CurrentFolderAbsolutePath
    {
        get => readWriteLock.ForRead(() => _currentFolderAbsolutePath);
        set => readWriteLock.ForWrite(() => _currentFolderAbsolutePath = value);
    }

    private Dictionary<string, FolderQueueSortItem> CurrentFolderSortItems { get; } = new();

    private List<UserQueueSortItem> UserQueueSortItems { get; } = [];

    private List<FolderQueueSortItem> OrderedCurrentFolderQueueSortItems => CurrentFolderSortItems
        .Values
        .OrderBy(o => o.Position)
        .ToList();

    public IReadOnlyList<string> UserQueueItemsAbsoluteFilePaths => readWriteLock.ForRead(() => UserQueueSortItems
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

        readWriteLock.ForWrite(() =>
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
        readWriteLock.ForWrite(() =>
        {
            var folderItem = CurrentFolderSortItems.Values.FirstOrDefault(f => f.Id == id);
            if (folderItem != null)
            {
                CurrentFolderQueueItemPositionId = id;
            }

            CurrentQueuePositionId = id;
        });
    }

    public void ClearUserQueue() => readWriteLock.ForWrite(() => UserQueueSortItems.Clear());
    
    public void RegenerateFolderQueueSortItems(IEnumerable<IFileExplorerFileNode> files)
    {
        readWriteLock.ForWrite(() =>
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
        readWriteLock.ForUpgradeableRead(() =>
        {
            if (!CurrentFolderSortItems.TryGetValue(absoluteFilePath, out var queueItemId))
            {
                throw new NotFoundException(nameof(CurrentFolderSortItems), absoluteFilePath);
            }

            readWriteLock.ForWrite(() =>
            {
                CurrentFolderQueueItemPositionId = queueItemId.Id;
                CurrentQueuePositionId = queueItemId.Id;
            });
        });
    }

    public void AddToQueue(IFileExplorerFileNode file)
    {
        readWriteLock.ForWrite(() =>
            UserQueueSortItems.Add(new UserQueueSortItem(Guid.NewGuid(), file.AbsolutePath!, CurrentFolderQueueItemPositionId)));
    }
    
    public void RemoveUserQueueItems(List<Guid> ids)
    {
        readWriteLock.ForWrite(() => UserQueueSortItems.RemoveAll(r => ids.Contains(r.Id)));
    }

    public bool QueueItemExists(Guid queuePositionId)
    {
        return readWriteLock.ForRead(() => CurrentFolderSortItems.Values.Any(a => a.Id == queuePositionId) ||
                                            UserQueueSortItems.Any(a => a.Id == queuePositionId));
    }

    public bool AllIdsPresentInUserQueue(List<Guid> ids)
    {
        return readWriteLock.ForRead(() => ids.All(id => UserQueueSortItems.Any(a => a.Id == id)));
    }

    public List<QueueSnapshotItem> GenerateQueueSnapshotItems(Dictionary<string, IFileExplorerFileNode> files) =>
        readWriteLock.ForRead(() =>
        {
            return GenerateQueueOrder()
                .Select(i => GenerateSnapshotQueueItem(i, files))
                .ToList();
        });

    public List<Guid> QueueOrder => readWriteLock.ForRead(() => GenerateQueueOrder().Select(s => s.Id).ToList());

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
using LexoAlgorithm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Queueing.Enums;
using MixServer.Domain.Queueing.Models;
using MixServer.Domain.Queueing.Repositories;
using MixServer.Domain.Streams.Enums;
using MixServer.Domain.Users.Models;
using MixServer.Infrastructure.EF.Entities;
using MixServer.Infrastructure.EF.Extensions;
using Range = MixServer.Domain.FileExplorer.Models.Range;

namespace MixServer.Infrastructure.EF.Repositories;

public class EfQueueRepository(
    MixServerDbContext context,
    ILogger<EfQueueRepository> logger) : IQueueRepository
{
    private const int BatchSize = 1000;

    public async Task SetFolderAsync(string userId, CancellationToken cancellationToken)
    {
        var currentFolder = await GetQueueCurrentFolderAsync(userId, cancellationToken);

        if (currentFolder == null)
        {
            throw new NotFoundException(nameof(QueueEntity), $"No current folder set in queue for user {userId}");
        }
        
        await SetFolderAsync(userId, currentFolder.Id, cancellationToken);
    }

    public async Task SetFolderAsync(string userId, Guid nodeId, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var (user, queue) = await GetOrCreateQueueAsync(userId, nodeId, cancellationToken);
            var userSort = user.FolderSorts.SingleOrDefault();
            var folderSort = userSort ?? (IFolderSort)FolderSortModel.Default;
            
            await ClearQueueAsync(queue, nodeId, cancellationToken);

            // Get total count for logging and progress tracking
            var totalFileCount = await GetFileCountAsync(nodeId, cancellationToken);
            
            if (totalFileCount == 0)
            {
                logger.LogDebug("No files found for user {UserId} in node {NodeId}", userId, nodeId);
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return;
            }
            
            logger.LogDebug("Re-sorting orphaned user queue items for user {UserId} in node {NodeId}", userId, nodeId);
            LexoRank? lastRank = null;
            var orphanedItems = await context.QueueItems
                .Where(w => w.QueueId == queue.Id && w.Type == QueueItemType.User && w.ParentId == null)
                .OrderBy(o => o.Rank)
                .ToListAsync(cancellationToken);
            foreach (var item in orphanedItems)
            {
                lastRank = lastRank is null ? LexoRank.Middle() : lastRank.GenNext();
                item.Rank = lastRank.ToString();
            }

            logger.LogDebug("Processing {FileCount} files for user {UserId} in node {NodeId}", 
                totalFileCount, userId, nodeId);

            await ProcessFilesInBatchesAsync(queue, nodeId, folderSort, totalFileCount, cancellationToken: cancellationToken);
            
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            logger.LogInformation("Successfully processed {FileCount} files for user {UserId} in node {NodeId}", 
                totalFileCount, userId, nodeId);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error setting folder for user {UserId} to node {NodeId}", userId, nodeId);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<QueueItemEntity> AddFileAsync(string userId, NodePath nodePath, CancellationToken cancellationToken)
    {
        var fileEntity = await context.Nodes
            .OfType<FileExplorerFileNodeEntity>()
            .IncludeParents()
            .Include(i => i.Metadata)
            .FirstOrDefaultAsync(f => f.RootChild.RelativePath == nodePath.RootPath && f.RelativePath == nodePath.RelativePath,
                cancellationToken: cancellationToken);

        if (fileEntity is null)
        {
            throw new NotFoundException(nameof(context.Nodes), nodePath.AbsolutePath);
        }

        var (_, queue) = await GetOrCreateQueueAsync(userId, fileEntity.Id, cancellationToken);
        
        var nextItem = await GetNextItemOfTypeAsync(queue.Id, QueueItemType.Folder, queue.CurrentPosition, cancellationToken: cancellationToken);
        var previousItem = await GetPreviousItemAsync(queue.Id, nextItem, cancellationToken: cancellationToken);
        var previousFolderItem = queue.CurrentPosition ?? await GetPreviousItemOfTypeAsync(queue.Id, QueueItemType.Folder, nextItem, cancellationToken: cancellationToken);

        var rank = LexoRank.Middle();
        if (previousItem != null && nextItem != null)
        {
            rank = LexoRank.Parse(previousItem.Rank).Between(LexoRank.Parse(nextItem.Rank));
        }
        else if (previousItem != null)
        {
            rank = LexoRank.Parse(previousItem.Rank).GenNext();
        }
        else if (nextItem != null)
        {
            rank = LexoRank.Parse(nextItem.Rank).GenPrev();
        }
        
        var queueItem = new QueueItemEntity
        {
            Id = Guid.NewGuid(),
            Rank = rank.ToString(),
            Queue = queue,
            FileId = fileEntity.Id,
            Type = QueueItemType.User,
            Parent = previousFolderItem,
            ParentId = previousFolderItem?.Id,
            AddedAt = DateTime.UtcNow
        };

        await context.QueueItems.AddAsync(queueItem, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        
        return queueItem;
    }

    public async Task SkipAsync(string userId, IDeviceState? deviceState = null, CancellationToken cancellationToken = default)
    {
        var nextItem = await GetNextPositionAsync(userId, deviceState, cancellationToken);
        
        if (nextItem == null)
        {
            throw new NotFoundException(nameof(QueueItemEntity), $"No next item found in queue for user {userId}");
        }
        
        await SetQueuePositionAsync(nextItem, cancellationToken);
    }
    
    public async Task PreviousAsync(string userId, IDeviceState? deviceState = null, CancellationToken cancellationToken = default)
    {
        var previousItem = await GetPreviousPositionAsync(userId, deviceState, cancellationToken);
        
        if (previousItem == null)
        {
            throw new NotFoundException(nameof(QueueItemEntity), $"No previous item found in queue for user {userId}");
        }
        
        await SetQueuePositionAsync(previousItem, cancellationToken);
    }

    public async Task<QueueItemEntity> SetQueuePositionAsync(string userId, Guid queueItemId, CancellationToken cancellationToken)
    {
        var queueItem = await QueueItemEntityQuery
            .FirstOrDefaultAsync(f => f.Queue.UserId == userId && f.Id == queueItemId, cancellationToken: cancellationToken);

        if (queueItem == null)
        {
            throw new NotFoundException(nameof(QueueItemEntity), queueItemId);
        }
        
        await SetQueuePositionAsync(queueItem, cancellationToken);
        
        return queueItem;
    }

    public async Task SetQueuePositionByFileIdAsync(string userId, Guid fileId, CancellationToken cancellationToken)
    {
        var queueItem = await context.QueueItems
            .Include(i => i.Queue)
            .FirstOrDefaultAsync(f => f.Queue.UserId == userId && f.FileId == fileId && f.Type == QueueItemType.Folder, cancellationToken: cancellationToken);

        if (queueItem == null)
        {
            throw new NotFoundException(nameof(QueueItemEntity), fileId);
        }
        
        await SetQueuePositionAsync(queueItem, cancellationToken);
    }

    public void RemoveQueueItems(string userId, List<Guid> ids)
    { 
        context.QueueItems.RemoveRange(context.QueueItems.Include(i => i.Queue)
            .Where(i => i.Queue.UserId == userId && ids.Contains(i.Id)));
    }

    public Task ClearQueueAsync(string userId, CancellationToken cancellationToken)
    {
        return context.QueueItems
            .Include(i => i.Queue)
            .Where(w => w.Queue.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<QueuePosition> GetQueuePositionAsync(string userId, IDeviceState? deviceState = null,
        CancellationToken cancellationToken = default)
    {
        var currentTask = GetCurrentPositionAsync(userId, deviceState, cancellationToken);
        var nextTask = GetNextPositionAsync(userId, deviceState, cancellationToken);
        var previousTask = GetPreviousPositionAsync(userId, deviceState, cancellationToken);
        
        await Task.WhenAll(currentTask, nextTask, previousTask);
        
        return new QueuePosition(currentTask.Result, nextTask.Result, previousTask.Result);
    }

    public async Task<QueueItemEntity?> GetCurrentPositionAsync(
        string userId,
        IDeviceState? deviceState = null,
        CancellationToken cancellationToken = default)
    {
        return await GetPositionAsync(userId, position: QueuePositionDirection.Current, deviceState, cancellationToken);
    }
    
    public async Task<QueueItemEntity?> GetNextPositionAsync(
        string userId,
        IDeviceState? deviceState = null,
        CancellationToken cancellationToken = default)
    {
        return await GetPositionAsync(userId, position: QueuePositionDirection.Next, deviceState, cancellationToken);
    }
    
    public async Task<QueueItemEntity?> GetPreviousPositionAsync(
        string userId,
        IDeviceState? deviceState = null,
        CancellationToken cancellationToken = default)
    {
        return await GetPositionAsync(userId, position: QueuePositionDirection.Previous, deviceState, cancellationToken);
    }

    public async Task<List<QueueItemEntity>> GetQueuePageAsync(string userId, Page page, CancellationToken cancellationToken = default)
    {
        return await QueueItemEntityQuery
            .Where(w => w.Queue.UserId == userId)
            .OrderBy(o => o.Rank)
            .Skip(page.PageIndex * page.PageSize)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public Task<List<QueueItemEntity>> GetQueueRangeAsync(string userId, Range range, CancellationToken cancellationToken)
    {
        return QueueItemEntityQuery
            .Where(w => w.Queue.UserId == userId)
            .OrderBy(o => o.Rank)
            .Skip(range.Start)
            .Take(range.End)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<IFileExplorerFolderEntity?> GetQueueCurrentFolderAsync(string currentUserId,
        CancellationToken cancellationToken)
    {
        var queue = await context.Queues
            .Where(w => w.UserId == currentUserId)
            .Include(i => i.CurrentFolder)
            .ThenInclude(t => t!.RootChild)
            .Include(i => i.CurrentRootChild)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        return queue?.CurrentFolderEntity;
    }

    private async Task<QueueItemEntity?> GetPositionAsync(
        string userId,
        QueuePositionDirection position,
        IDeviceState? deviceState = null,
        CancellationToken cancellationToken = default)
    {
        var queue = await context.Queues
            .Include(i => i.CurrentPosition)
            .ThenInclude(t => t!.Queue)
            .FirstOrDefaultAsync(f => f.UserId == userId, cancellationToken: cancellationToken);

        if (queue is null)
        {
            throw new NotFoundException(nameof(QueueEntity), userId);
        }

        // Handle the case when there's no current position
        if (queue.CurrentPosition is null)
        {
            // For GetNextPositionAsync, return first item when no current position
            // For GetPreviousPositionAsync, return null when no current position
            return position == QueuePositionDirection.Next
                ? await GetFirstQueueItemAsync(queue.Id, deviceState, cancellationToken)
                : null;
        }
        
        if (position == QueuePositionDirection.Current)
        {
            await LoadCurrentPositionAsync(queue, cancellationToken);
            return queue.CurrentPosition;
        }
        
        var isNext = position == QueuePositionDirection.Next;

        var query = BuildQueueItemQuery(queue.Id, deviceState);
        
        // Apply position filtering based on direction
        query = isNext
            ? query.Where(w => string.Compare(w.Rank, queue.CurrentPosition.Rank) > 0)
            : query.Where(w => string.Compare(w.Rank, queue.CurrentPosition.Rank) < 0);
        
        // Apply ordering based on direction
        var orderedQuery = isNext 
            ? query.OrderBy(o => o.Rank)
            : query.OrderByDescending(o => o.Rank);
        
        return await orderedQuery.FirstOrDefaultAsync(cancellationToken);
    }
    
    private IQueryable<QueueItemEntity> BuildQueueItemQuery(Guid queueId, IDeviceState? deviceState)
    {
        var query = QueueItemEntityQuery
            .Where(w => w.QueueId == queueId &&
                        w.File != null &&
                        w.File.Exists &&
                        w.File.Metadata != null &&
                        w.File.Metadata.IsMedia)
            .AsQueryable();

        if (deviceState is not null)
        {
            var supportedMimeTypes = deviceState.Capabilities
                .Where(w => w.Value)
                .Select(s => s.Key)
                .ToHashSet();
            query = query
                .Where(w => w.File != null &&
                            w.File.Metadata != null &&
                            (supportedMimeTypes.Contains(w.File.Metadata.MimeType) ||
                             (w.File.Transcode != null && w.File.Transcode.State == TranscodeState.Completed)));
        }
        
        return query;
    }
    
    private async Task<QueueItemEntity?> GetFirstQueueItemAsync(
        Guid queueId,
        IDeviceState? deviceState,
        CancellationToken cancellationToken)
    {
        var query = BuildQueueItemQuery(queueId, deviceState);
        return await query.OrderBy(o => o.Rank).FirstOrDefaultAsync(cancellationToken);
    }
    
    private async Task<int> GetFileCountAsync(Guid nodeId, CancellationToken cancellationToken)
    {
        return await context.Nodes
            .OfType<FileExplorerFileNodeEntity>()
            .Where(w => (w.ParentId == nodeId || w.RootChildId == nodeId) && 
                        w.Exists &&
                        w.Metadata != null &&
                        w.Metadata.IsMedia)
            .CountAsync(cancellationToken);
    }

    private async Task ProcessFilesInBatchesAsync(
        QueueEntity queue, 
        Guid nodeId, 
        IFolderSort folderSort, 
        int totalCount,
        LexoRank? lastRank = null,
        CancellationToken cancellationToken = default)
    {
        var processedCount = 0;

        // Process all files in batches - works efficiently for both small and large datasets
        for (var skip = 0; skip < totalCount; skip += BatchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batchFileIds = await context.Nodes
                .OfType<FileExplorerFileNodeEntity>()
                .Include(i => i.Metadata)
                .Where(w => (w.ParentId == nodeId || w.RootChildId == nodeId) && 
                            w.Exists &&
                            w.Metadata != null &&
                            w.Metadata.IsMedia)
                .ApplySort(folderSort, null)
                .Skip(skip)
                .Take(BatchSize)
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            if (batchFileIds.Count == 0)
            {
                break;
            }
            
            var existingQueueItems = await context.QueueItems
                .Where(w => w.QueueId == queue.Id && w.FileId.HasValue && batchFileIds.Contains(w.FileId.Value))
                .Include(i => i.Children.OrderBy(o => o.AddedAt))
                .ToListAsync(cancellationToken);

            var queueItems = new List<QueueItemEntity>(batchFileIds.Count);
            
            foreach (var fileId in batchFileIds)
            {
                lastRank = lastRank is null ? LexoRank.Middle() : lastRank.GenNext();
                var existingItem = existingQueueItems.SingleOrDefault(s => s.FileId == fileId);
                if (existingItem != null)
                {
                    existingItem.Rank = lastRank.ToString();
                    foreach (var child in existingItem.Children)
                    {
                        lastRank = lastRank.GenNext();
                        child.Rank = lastRank.ToString();
                    }
                }
                else
                {
                    existingItem = new QueueItemEntity
                    {
                        Id = Guid.NewGuid(),
                        Rank = lastRank.ToString(),
                        Queue = queue,
                        FileId = fileId,
                        Type = QueueItemType.Folder,
                        AddedAt = DateTime.UtcNow
                    };
                    queueItems.Add(existingItem);
                }
            }
            
            await context.QueueItems.AddRangeAsync(queueItems, cancellationToken);
            
            // For small datasets, this saves once at the end
            // For large datasets, this saves incrementally to manage memory
            if (skip + BatchSize < totalCount) // Not the last batch
            {
                await context.SaveChangesAsync(cancellationToken);
                context.ChangeTracker.Clear(); // Free memory for large datasets
            }
            
            processedCount += batchFileIds.Count;
            
            // Only log progress for larger datasets to avoid spam
            if (totalCount > BatchSize)
            {
                logger.LogDebug("Processed {ProcessedCount}/{TotalCount} files", 
                    processedCount, totalCount);
            }
        }
    }
    
    private async Task<(DbUser User, QueueEntity Queue)> GetOrCreateQueueAsync(
        string userId, 
        Guid nodeId, 
        CancellationToken cancellationToken)
    {
        var user = await context.Users
            .Include(u => u.Queue)
            .ThenInclude(t => t!.CurrentPosition)
            .Include(i => i.FolderSorts.Where(w => w.NodeId == nodeId))
            .SingleAsync(u => u.Id == userId, cancellationToken);
        
        if (user.Queue is not null)
        {
            return (user, user.Queue);
        }

        if (!await context.Nodes.AnyAsync(a => a.Id == nodeId, cancellationToken))
        {
            throw new NotFoundException(nameof(context.Nodes), nodeId);
        }
        
        var queue = new QueueEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CurrentFolderId = nodeId
        };
        await context.Queues.AddAsync(queue, cancellationToken);
        
        user.Queue = queue;
        
        return (user, queue);
    }
    
    private async Task ClearQueueAsync(QueueEntity queue, Guid newNodeId, CancellationToken cancellationToken)
    {
        var deletedCount =
            await (from qi in context.QueueItems
                    join f in context.Nodes.OfType<FileExplorerFileNodeEntity>() on qi.FileId equals f.Id
                    where qi.QueueId == queue.Id && f.ParentId != newNodeId && qi.Type == QueueItemType.Folder
                    select qi)
                .ExecuteDeleteAsync(cancellationToken);

            
        logger.LogDebug("Cleared {DeletedCount} existing queue items for queue {QueueId}", 
            deletedCount, queue.Id);
    }
    
    private async Task SetQueuePositionAsync(QueueItemEntity queueItem, CancellationToken cancellationToken)
    {
        queueItem.Queue.CurrentPosition = queueItem;
        queueItem.Queue.CurrentPositionId = queueItem.Id;
        
        await context.SaveChangesAsync(cancellationToken);
    }
    
    private async Task<QueueItemEntity?> GetNextItemOfTypeAsync(
        Guid queueId, 
        QueueItemType itemType, 
        QueueItemEntity? afterItem = null, 
        IDeviceState? deviceState = null, 
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueueItemQuery(queueId, deviceState)
            .Where(w => w.Type == itemType);
        
        if (afterItem != null)
        {
            // Get items after the specified item
            query = query.Where(w => string.Compare(w.Rank, afterItem.Rank) > 0);
        }
        
        return await query.OrderBy(o => o.Rank).FirstOrDefaultAsync(cancellationToken);
    }
    
    private async Task<QueueItemEntity?> GetPreviousItemOfTypeAsync(
        Guid queueId, 
        QueueItemType itemType, 
        QueueItemEntity? beforeItem = null, 
        IDeviceState? deviceState = null, 
        CancellationToken cancellationToken = default)
    {
        var query = BuildQueueItemQuery(queueId, deviceState)
            .Where(w => w.Type == itemType);
        
        if (beforeItem != null)
        {
            // Get items before the specified item
            query = query.Where(w => string.Compare(w.Rank, beforeItem.Rank) < 0);
        }
        
        return await query.OrderByDescending(o => o.Rank).FirstOrDefaultAsync(cancellationToken);
    }
    
    private async Task<QueueItemEntity?> GetPreviousItemAsync(
        Guid queueId,
        QueueItemEntity? beforeItem,
        IDeviceState? deviceState = null,
        CancellationToken cancellationToken = default)
    {
        if (beforeItem == null)
        {
            return null;
        }
        
        var query = BuildQueueItemQuery(queueId, deviceState)
            .Where(w => string.Compare(w.Rank, beforeItem.Rank) < 0);
        
        return await query.OrderByDescending(o => o.Rank).FirstOrDefaultAsync(cancellationToken);
    }

    private async Task LoadCurrentPositionAsync(QueueEntity queueEntity, CancellationToken cancellationToken)
    {
        if (queueEntity.CurrentPosition is null)
        {
            return;
        }

        await QueueItemIncludes(context.Entry(queueEntity)
                .Reference(r => r.CurrentPosition)
                .Query())
            .LoadAsync(cancellationToken: cancellationToken);

    }

    private IQueryable<QueueItemEntity> QueueItemEntityQuery => QueueItemIncludes(context.QueueItems);


    private static IQueryable<QueueItemEntity> QueueItemIncludes(IQueryable<QueueItemEntity> queryable)
    {
        return queryable.Include(i => i.File)
            .ThenInclude(t => t!.Metadata)
            .Include(i => i.File)
            .ThenInclude(t => t!.Parent)
            .Include(i => i.File)
            .ThenInclude(t => t!.RootChild)
            .Include(i => i.File)
            .ThenInclude(t => t!.Transcode)
            .Include(i => i.Queue);
    }
    
    private enum QueuePositionDirection
    {
        Current,
        Next,
        Previous
    }
}
using LexoAlgorithm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Streams.Enums;
using MixServer.Domain.Users.Models;
using MixServer.Infrastructure.EF.Entities;
using MixServer.Infrastructure.EF.Extensions;

namespace MixServer.Infrastructure.EF.Repositories;

public class EfQueueRepository(
    MixServerDbContext context,
    ILogger<EfQueueRepository> logger)
{
    private const int BatchSize = 1000;

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

            logger.LogDebug("Processing {FileCount} files for user {UserId} in node {NodeId}", 
                totalFileCount, userId, nodeId);

            await ProcessFilesInBatchesAsync(queue, nodeId, folderSort, totalFileCount, cancellationToken);
            
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

    public async Task SetQueuePositionAsync(string userId, Guid fileId, CancellationToken cancellationToken)
    {
        var queueItem = await context.QueueItems
            .Include(i => i.Queue)
            .FirstOrDefaultAsync(f => f.Queue.UserId == userId && f.FileId == fileId, cancellationToken: cancellationToken);

        if (queueItem == null)
        {
            throw new NotFoundException(nameof(QueueItemEntity), fileId);
        }
        
        queueItem.Queue.CurrentPosition = queueItem;
        queueItem.Queue.CurrentPositionId = queueItem.Id;
        
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<QueueItemEntity?> GetNextPositionAsync(
        string userId,
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

        var query = context.QueueItems
            .Include(i => i.File)
            .ThenInclude(t => t!.Metadata)
            .Where(w => w.QueueId == queue.Id &&
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
                .Include(i => i.File)
                .ThenInclude(t => t!.Transcode)
                .Where(w => w.File != null &&
                            w.File.Metadata != null &&
                            (supportedMimeTypes.Contains(w.File.Metadata.MimeType) ||
                             (w.File.Transcode != null && w.File.Transcode.State == TranscodeState.Completed)));
        }
        
        if (queue.CurrentPosition is not null)
        {
            query = query.Where(w => string.Compare(w.Rank, queue.CurrentPosition.Rank) > 0);
        }
        
        var nextItem = await query.OrderBy(o => o.Rank)
            .FirstOrDefaultAsync(cancellationToken);

        return nextItem;
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
        CancellationToken cancellationToken)
    {
        var processedCount = 0;
        LexoRank? lastRank = null;

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
                .ToListAsync(cancellationToken);

            var queueItems = new List<QueueItemEntity>(batchFileIds.Count);
            
            foreach (var fileId in batchFileIds)
            {
                lastRank = lastRank is null ? LexoRank.Middle() : lastRank.GenNext();
                var existingItem = existingQueueItems.SingleOrDefault(s => s.FileId == fileId);
                if (existingItem != null)
                {
                    existingItem.Rank = lastRank.ToString();
                }
                else
                {
                    existingItem = new QueueItemEntity
                    {
                        Id = Guid.NewGuid(),
                        Rank = lastRank.ToString(),
                        Queue = queue,
                        FileId = fileId
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
            .Include(i => i.FolderSorts.Where(w => w.NodeId == nodeId))
            .SingleAsync(u => u.Id == userId, cancellationToken);
        
        if (user.Queue is not null)
        {
            return (user, user.Queue);
        }
        
        var queue = new QueueEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id
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
                    where qi.QueueId == queue.Id && f.ParentId != newNodeId
                    select qi)
                .ExecuteDeleteAsync(cancellationToken);

            
        logger.LogDebug("Cleared {DeletedCount} existing queue items for queue {QueueId}", 
            deletedCount, queue.Id);
    }
}
using LexoAlgorithm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Queueing.Entities;
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
            
            await ClearQueueAsync(queue, cancellationToken);

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
    
    private async Task ClearQueueAsync(QueueEntity queue, CancellationToken cancellationToken)
    {
        var deletedCount = await context.QueueItems
            .Where(w => w.QueueId == queue.Id)
            .ExecuteDeleteAsync(cancellationToken);
            
        logger.LogDebug("Cleared {DeletedCount} existing queue items for queue {QueueId}", 
            deletedCount, queue.Id);
    }
}
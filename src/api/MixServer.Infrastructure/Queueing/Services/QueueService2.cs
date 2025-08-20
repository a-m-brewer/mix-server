using Microsoft.Extensions.Logging;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Sessions.Entities;
using MixServer.Infrastructure.Queueing.Repositories;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Infrastructure.Queueing.Services;

public interface IQueueService2
{
    Task<QueuePosition> SetQueueFolderAsync(PlaybackSession nextSession, CancellationToken cancellationToken);
}

public class QueueService2(
    ICurrentUserRepository currentUserRepository,
    ILogger<QueueService2> logger,
    IQueueRepository queueRepository) : IQueueService2
{
    public async Task<QueuePosition> SetQueueFolderAsync(PlaybackSession nextSession, CancellationToken cancellationToken)
    {
        var queue = await GetOrAddQueueAsync(cancellationToken);

        await ClearUserQueueAsync(queue, cancellationToken);
        queue.SetCurrentFolderAndPosition(nextSession);

        throw new NotImplementedException();
    }
    
    private async Task<QueueEntity> GetOrAddQueueAsync(CancellationToken cancellationToken)
    {
        await currentUserRepository.LoadQueueAsync(cancellationToken);
        var currentUser = await currentUserRepository.GetCurrentUserAsync();
        
        if (currentUser.Queue is not null)
        {
            logger.LogDebug("Skipping initializing queue for user as queue is already initialized");
            return currentUser.Queue;
        }

        await currentUserRepository.LoadCurrentPlaybackSessionAsync(cancellationToken);

        var queue = queueRepository.CreateAsync(currentUser.Id);
        currentUser.Queue = queue;
        
        return queue;
    }
    
    private async Task ClearUserQueueAsync(QueueEntity queue, CancellationToken cancellationToken)
    {
        await queueRepository.MarkUserQueueItemsAsDeletedAsync(queue, cancellationToken);
        queue.UserQueueItems.Clear();
    }
}
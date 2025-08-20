using MixServer.Domain.Persistence;
using MixServer.Domain.Sessions.Entities;

namespace MixServer.Infrastructure.Queueing.Repositories;

public interface IQueueRepository : IScopedRepository
{
    QueueEntity CreateAsync(string userId);
    bool HasQueue(string userId);
    void Remove(string userId);
    Task MarkUserQueueItemsAsDeletedAsync(QueueEntity queue, CancellationToken cancellationToken);
}

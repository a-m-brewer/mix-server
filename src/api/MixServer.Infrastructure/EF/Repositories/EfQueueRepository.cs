
using MixServer.Domain.Sessions.Entities;
using MixServer.Infrastructure.Queueing.Repositories;

namespace MixServer.Infrastructure.EF.Repositories;

public class EfQueueRepository(MixServerDbContext context) : IQueueRepository
{
    public QueueEntity CreateAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public bool HasQueue(string userId)
    {
        throw new NotImplementedException();
    }

    public void Remove(string userId)
    {
        throw new NotImplementedException();
    }

    public Task MarkUserQueueItemsAsDeletedAsync(QueueEntity queue, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
using Microsoft.Extensions.DependencyInjection;
using MixServer.Domain.Utilities;
using MixServer.Infrastructure.Queueing.Models;

namespace MixServer.Infrastructure.Queueing.Repositories;

public interface IQueueRepository
{
    UserQueue GetOrAddQueue(string userId);
    bool HasQueue(string userId);
    void Remove(string userId);
}

public class QueueRepository(IReadWriteLock readWriteLock, IServiceProvider serviceProvider)
    : IQueueRepository
{
    private readonly Dictionary<string, UserQueue> _queues = new();

    public UserQueue GetOrAddQueue(string userId)
    {
        return readWriteLock.ForUpgradeableRead(() =>
        {
            if (_queues.TryGetValue(userId, out var queue))
            {
                return queue;
            }

            return readWriteLock.ForWrite(() =>
            {
                var newQueue = new UserQueue(userId, serviceProvider.GetRequiredService<IReadWriteLock>());
                _queues[userId] = newQueue;

                return newQueue;
            });
        });
    }

    public bool HasQueue(string userId)
    {
        return readWriteLock.ForRead(() => _queues.ContainsKey(userId));
    }

    public void Remove(string userId)
    {
        readWriteLock.ForWrite(() => _queues.Remove(userId));
    }
}
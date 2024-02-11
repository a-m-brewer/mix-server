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

public class QueueRepository : IQueueRepository
{
    private readonly IReadWriteLock _readWriteLock;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, UserQueue> _queues = new();
    
    public QueueRepository(IReadWriteLock readWriteLock, IServiceProvider serviceProvider)
    {
        _readWriteLock = readWriteLock;
        _serviceProvider = serviceProvider;
    }
    
    public UserQueue GetOrAddQueue(string userId)
    {
        return _readWriteLock.ForUpgradeableRead(() =>
        {
            if (_queues.TryGetValue(userId, out var queue))
            {
                return queue;
            }

            return _readWriteLock.ForWrite(() =>
            {
                var newQueue = new UserQueue(userId, _serviceProvider.GetRequiredService<IReadWriteLock>());
                _queues[userId] = newQueue;

                return newQueue;
            });
        });
    }

    public bool HasQueue(string userId)
    {
        return _readWriteLock.ForRead(() => _queues.ContainsKey(userId));
    }

    public void Remove(string userId)
    {
        _readWriteLock.ForWrite(() => _queues.Remove(userId));
    }
}
using System.Collections.Concurrent;
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

public class QueueRepository(IServiceProvider serviceProvider)
    : IQueueRepository
{
    private readonly ConcurrentDictionary<string, UserQueue> _queues = new();

    public UserQueue GetOrAddQueue(string userId)
    {
        if (_queues.TryGetValue(userId, out var queue))
        {
            return queue;
        }
        
        var newQueue = new UserQueue(userId, serviceProvider.GetRequiredService<IReadWriteLock>());
        _queues[userId] = newQueue;

        return newQueue;
    }

    public bool HasQueue(string userId)
    {
        return _queues.ContainsKey(userId);
    }

    public void Remove(string userId)
    {
        _queues.TryRemove(userId, out _);
    }
}
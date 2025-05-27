using System.Collections.Concurrent;
using System.Diagnostics;
using MixServer.Domain.Utilities;

namespace MixServer.Domain.FileExplorer.Services.Caching;

public class SegmentedLruCache<TKey, TValue>(int capacity, Action<TKey, TValue>? evictionCallback = null)
    where TValue : notnull
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, CacheItem> _hot = new();
    private readonly ConcurrentDictionary<TKey, CacheItem> _cold = new();
    private readonly ReadWriteLock _lock = new();

    private class CacheItem(Func<Task<TValue>> factory, long lastAccessed)
    {
        public Lazy<Task<TValue>> Lazy { get; } = new(factory);
        public long LastAccessed { get; set; } = lastAccessed;
    }

    public Task<TValue> GetOrAddAsync(TKey key, Func<TKey, Task<TValue>> factory)
    {
        if (_hot.TryGetValue(key, out var hotItem))
        {
            hotItem.LastAccessed = Stopwatch.GetTimestamp();
            return hotItem.Lazy.Value;
        }

        if (_cold.TryRemove(key, out var coldItem))
        {
            coldItem.LastAccessed = Stopwatch.GetTimestamp();
            _hot[key] = coldItem;
            return coldItem.Lazy.Value;
        }

        var newItem = new CacheItem(() => factory(key), Stopwatch.GetTimestamp());
        _cold[key] = newItem;

        TryEvictIfNeeded();

        return newItem.Lazy.Value;
    }

    public void Remove(TKey key)
    {
        if (_hot.TryRemove(key, out var hotItem))
        {
            TrySendEvictionCallback(key, hotItem);
        }
        else if (_cold.TryRemove(key, out var coldItem))
        {
            TrySendEvictionCallback(key, coldItem);
        }
    }

    private void TryEvictIfNeeded()
    {
        var total = _hot.Count + _cold.Count;
        if (total <= capacity)
            return;

        _lock.ForWrite(() =>
        {
            total = _hot.Count + _cold.Count;
            if (total <= capacity)
                return;

            KeyValuePair<TKey, CacheItem>? victim = null;

            if (!_cold.IsEmpty)
                victim = _cold.OrderBy(kv => kv.Value.LastAccessed).FirstOrDefault();
            else if (!_hot.IsEmpty)
                victim = _hot.OrderBy(kv => kv.Value.LastAccessed).FirstOrDefault();

            if (!victim.HasValue)
                return;

            var (key, item) = victim.Value;
            _cold.TryRemove(key, out _);
            _hot.TryRemove(key, out _);

            TrySendEvictionCallback(key, item);
        });
    }

    private void TrySendEvictionCallback(TKey key, CacheItem item)
    {
        if (evictionCallback is null)
            return;

        // Avoid blocking; we can't await Lazy.Value, so fire-and-forget
        _ = Task.Run(async () =>
        {
            try
            {
                var value = await item.Lazy.Value.ConfigureAwait(false);
                evictionCallback(key, value);
            }
            catch
            {
                // Swallow exceptions from failed tasks
            }
        });
    }
}

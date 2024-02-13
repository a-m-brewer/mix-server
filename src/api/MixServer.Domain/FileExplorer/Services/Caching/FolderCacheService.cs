using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Models.Caching;
using MixServer.Domain.Utilities;

namespace MixServer.Domain.FileExplorer.Services.Caching;

public interface IFolderCacheService
{
    event EventHandler<FolderCacheServiceItemAddedEventArgs> ItemAdded;

    event EventHandler<FolderCacheServiceItemUpdatedEventArgs> ItemUpdated;

    event EventHandler<FolderCacheServiceItemRemovedEventArgs> ItemRemoved;

    Task<IFolderCacheItem> GetOrAddAsync(string absolutePath);
}

public class FolderCacheService(
    IReadWriteLock readWriteLock,
    ILogger<FolderCacheService> logger,
    ILoggerFactory loggerFactory,
    IOptions<FolderCacheSettings> options,
    IMimeTypeService mimeTypeService) : IFolderCacheService
{
    private ConcurrentDictionary<object, SemaphoreSlim> _cacheKeySemaphores = new();

    private readonly MemoryCache _cache = new(new MemoryCacheOptions
    {
        SizeLimit = options.Value.MaxCachedDirectories
    });

    public event EventHandler<FolderCacheServiceItemAddedEventArgs>? ItemAdded;

    public event EventHandler<FolderCacheServiceItemUpdatedEventArgs>? ItemUpdated;

    public event EventHandler<FolderCacheServiceItemRemovedEventArgs>? ItemRemoved;

    public async Task<IFolderCacheItem> GetOrAddAsync(string absolutePath)
    {
        return await readWriteLock.ForUpgradeableRead(async () =>
        {
            var maxDirectories = options.Value.MaxCachedDirectories;
            if (maxDirectories > 0 && _cache.Count >= maxDirectories)
            {
                readWriteLock.ForWrite(() =>
                {
                    // Try and only remove one item, but memory cache only allows to specify in percentage
                    var percentage = 1d / maxDirectories;
                    logger.LogInformation("Cache is full, compacting by {Percentage}", percentage);
                    _cache.Compact(percentage);
                });
            }
            
            return await _cache.GetOrCreateAsync<IFolderCacheItem>(absolutePath, async entry =>
            {
                var semaphore = GetOrAddCacheKeySemaphore(absolutePath);

                await semaphore.WaitAsync();

                try
                {
                    logger.LogInformation("Cache miss for {AbsolutePath} creating new cache entry", absolutePath);
                    entry.Size = 1;
                    entry.RegisterPostEvictionCallback(PostEvictionCallback);

                    IFolderCacheItem item =
                        new FolderCacheItem(absolutePath, loggerFactory.CreateLogger<FolderCacheItem>(), mimeTypeService);
                    item.ItemAdded += OnItemAdded;
                    item.ItemUpdated += OnItemUpdated;
                    item.ItemRemoved += OnItemRemoved;

                    return item;
                }
                finally
                {
                    semaphore.Release();
                }
            }) ?? throw new NotFoundException(nameof(FolderCacheService), absolutePath);
        });
    }

    private void PostEvictionCallback(object key, object? value, EvictionReason reason, object? state)
    {
        logger.LogInformation("Cache entry for {Key} has been evicted. Reason: {Reason}", key, reason);
        if (!IsFolderCacheItem(value, out var item)) return;
        
        item.ItemAdded -= OnItemAdded;
        item.ItemUpdated -= OnItemUpdated;
        item.ItemRemoved -= OnItemRemoved;
        
        item.Dispose();
    }

    private void OnItemAdded(object? sender, ICacheFileSystemInfo e)
    {
        if (!IsFolderCacheItem(sender, out var parent)) return;
        ItemAdded?.Invoke(this, new FolderCacheServiceItemAddedEventArgs(parent.DirectoryInfo, e));
    }

    private void OnItemUpdated(object? sender, FolderItemUpdatedEventArgs e)
    {
        if (!IsFolderCacheItem(sender, out var parent)) return;
        ItemUpdated?.Invoke(this, new FolderCacheServiceItemUpdatedEventArgs(parent.DirectoryInfo, e.Item, e.OldFullPath));
    }

    private void OnItemRemoved(object? sender, string e)
    {
        if (!IsFolderCacheItem(sender, out var parent)) return;
        ItemRemoved?.Invoke(this, new FolderCacheServiceItemRemovedEventArgs(parent.DirectoryInfo, e));
    }

    private bool IsFolderCacheItem(object? sender, [MaybeNullWhen(false)] out IFolderCacheItem parent)
    {
        if (sender is not IFolderCacheItem folderCacheItem)
        {
            logger.LogWarning("event raised by non-folder cache item Sender: {Sender}",
                sender?.GetType().Name ?? "null");
            parent = null;
            return false;
        }

        parent = folderCacheItem;
        return true;
    }
    
    private SemaphoreSlim GetOrAddCacheKeySemaphore(object key) =>
        _cacheKeySemaphores.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
}
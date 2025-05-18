using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Caching;

namespace MixServer.Domain.FileExplorer.Services.Caching;

public interface IFolderCacheService
{
    event EventHandler<IFileExplorerNode> ItemAdded;

    event EventHandler<FolderCacheServiceItemUpdatedEventArgs> ItemUpdated;

    event EventHandler<FolderCacheServiceItemRemovedEventArgs> ItemRemoved;

    event EventHandler<IFileExplorerFolder> FolderAdded;
    
    event EventHandler<IFileExplorerFolder> FolderRemoved;

    Task<ICacheFolder> GetOrAddAsync(string absolutePath);
    Task<IFileExplorerFileNode> GetFileAsync(string fileAbsolutePath);
    Task<(IFileExplorerFolder Parent, IFileExplorerFileNode File)> GetFileAndFolderAsync(string absoluteFilePath);
    void InvalidateFolder(string absolutePath);
    Task<IFileExplorerFolder> GetRootFolderAsync();
}

public class FolderCacheService(
    ILogger<FolderCacheService> logger,
    ILoggerFactory loggerFactory,
    IOptions<FolderCacheSettings> options,
    IFileExplorerConverter fileExplorerConverter,
    IRootFileExplorerFolder rootFolder) : IFolderCacheService
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions
    {
        SizeLimit = options.Value.MaxCachedDirectories
    });

    public event EventHandler<IFileExplorerNode>? ItemAdded;

    public event EventHandler<FolderCacheServiceItemUpdatedEventArgs>? ItemUpdated;

    public event EventHandler<FolderCacheServiceItemRemovedEventArgs>? ItemRemoved;
    public event EventHandler<IFileExplorerFolder>? FolderAdded;
    public event EventHandler<IFileExplorerFolder>? FolderRemoved;

    public Task<ICacheFolder> GetOrAddAsync(string absolutePath)
    {
        var maxDirectories = options.Value.MaxCachedDirectories;
        if (maxDirectories > 0 && _cache.Count >= maxDirectories)
        {
            // Try and only remove one item, but memory cache only allows to specify in percentage
            var percentage = 1d / maxDirectories;
            logger.LogInformation("Cache is full, compacting by {Percentage}", percentage);
            _cache.Compact(percentage);
        }

        var cacheMiss = false;
        var cacheItem = _cache.GetOrCreate<IFolderCacheItem>(absolutePath, entry =>
        {
            return new Lazy<IFolderCacheItem>(() =>
            {
                cacheMiss = true;
                entry.Size = 1;
                entry.RegisterPostEvictionCallback(PostEvictionCallback);

                IFolderCacheItem item =
                    new FolderCacheItem(absolutePath,
                        loggerFactory.CreateLogger<FolderCacheItem>(),
                        fileExplorerConverter);
                item.ItemAdded += OnItemAdded;
                item.ItemUpdated += OnItemUpdated;
                item.ItemRemoved += OnItemRemoved;

                return item;
            }, LazyThreadSafetyMode.ExecutionAndPublication).Value;
        }) ?? throw new NotFoundException(nameof(FolderCacheService), absolutePath);

        if (cacheItem.Folder.Node.Exists)
        {
            if (cacheMiss)
            {
                Task.Run(() => FolderAdded?.Invoke(this, cacheItem.Folder));
            }
        }
        else
        {
            // We don't want to keep missing folders in the cache.
            _cache.Remove(absolutePath);
        }

        return Task.FromResult<ICacheFolder>(cacheItem);
    }

    public Task<IFileExplorerFileNode> GetFileAsync(string fileAbsolutePath)
    {
        var directoryAbsolutePath = Path.GetDirectoryName(fileAbsolutePath);

        if (string.IsNullOrWhiteSpace(directoryAbsolutePath))
        {
            throw new InvalidRequestException(nameof(directoryAbsolutePath), "Directory Absolute Path is Null");
        }

        var dir = _cache.TryGetValue<IFolderCacheItem>(directoryAbsolutePath, out var cacheItem) ? cacheItem : null;

        if (dir is null)
        {
            logger.LogWarning("Directory {DirectoryAbsolutePath} not found in cache when retrieving file",
                directoryAbsolutePath);

            return Task.FromResult(fileExplorerConverter.Convert(fileAbsolutePath));
        }

        var file = dir.Folder.Children.OfType<IFileExplorerFileNode>()
            .FirstOrDefault(f => f.AbsolutePath == fileAbsolutePath);

        if (file is null)
        {
            logger.LogWarning("File {FileAbsolutePath} not found in cache", fileAbsolutePath);

            if (Debugger.IsAttached && File.Exists(fileAbsolutePath))
            {
                Debugger.Break();
            }

            return Task.FromResult(fileExplorerConverter.Convert(new FileInfo(fileAbsolutePath), dir.Folder.Node));
        }

        return Task.FromResult(file);
    }

    public async Task<(IFileExplorerFolder Parent, IFileExplorerFileNode File)> GetFileAndFolderAsync(string absoluteFilePath)
    {
        var directoryAbsolutePath = Path.GetDirectoryName(absoluteFilePath);

        if (string.IsNullOrWhiteSpace(directoryAbsolutePath))
        {
            throw new InvalidRequestException(nameof(directoryAbsolutePath), "Directory Absolute Path is Null");
        }

        var folder = await GetOrAddAsync(directoryAbsolutePath);

        var file = folder.Folder.Children
                       .OfType<IFileExplorerFileNode>()
                       .SingleOrDefault(f => f.AbsolutePath == absoluteFilePath) ??
                   throw new NotFoundException(directoryAbsolutePath, Path.GetFileName(absoluteFilePath));

        return (folder.Folder, file);
    }

    public void InvalidateFolder(string absolutePath)
    {
        _cache.Remove(absolutePath);
    }

    public Task<IFileExplorerFolder> GetRootFolderAsync()
    {
        return Task.FromResult<IFileExplorerFolder>(rootFolder);
    }

    private void PostEvictionCallback(object key, object? value, EvictionReason reason, object? state)
    {
        logger.LogInformation("Cache entry for {Key} has been evicted. Reason: {Reason}", key, reason);
        if (!IsFolderCacheItem(value, out var item)) return;

        item.ItemAdded -= OnItemAdded;
        item.ItemUpdated -= OnItemUpdated;
        item.ItemRemoved -= OnItemRemoved;

        item.Dispose();
        
        FolderRemoved?.Invoke(this, item.Folder);
    }

    private void OnItemAdded(object? sender, IFileExplorerNode e)
    {
        if (!IsFolderCacheItem(sender, out var parent)) return;
        ItemAdded?.Invoke(parent.Folder, e);
    }

    private void OnItemUpdated(object? sender, FolderItemUpdatedEventArgs e)
    {
        if (!IsFolderCacheItem(sender, out var parent)) return;
        ItemUpdated?.Invoke(parent.Folder, new FolderCacheServiceItemUpdatedEventArgs(e.Item, e.OldFullPath));
    }

    private void OnItemRemoved(object? sender, string e)
    {
        if (!IsFolderCacheItem(sender, out var parent)) return;
        ItemRemoved?.Invoke(parent.Folder, new FolderCacheServiceItemRemovedEventArgs(parent.Folder.Node, e));
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
}
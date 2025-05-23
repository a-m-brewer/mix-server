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

    ICacheFolder GetOrAdd(NodePath nodePath);
    IFileExplorerFileNode GetFile(NodePath nodePath);
    (IFileExplorerFolder Parent, IFileExplorerFileNode File) GetFileAndFolder(NodePath nodePath);
    void InvalidateFolder(NodePath nodePath);
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

    public ICacheFolder GetOrAdd(NodePath nodePath)
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
        var cacheItem = _cache.GetOrCreate<IFolderCacheItem>(nodePath, entry =>
        {
            return new Lazy<IFolderCacheItem>(() =>
            {
                cacheMiss = true;
                entry.Size = 1;
                entry.RegisterPostEvictionCallback(PostEvictionCallback);

                IFolderCacheItem item =
                    new FolderCacheItem(nodePath,
                        loggerFactory.CreateLogger<FolderCacheItem>(),
                        fileExplorerConverter,
                        rootFolder);
                item.ItemAdded += OnItemAdded;
                item.ItemUpdated += OnItemUpdated;
                item.ItemRemoved += OnItemRemoved;

                return item;
            }, LazyThreadSafetyMode.ExecutionAndPublication).Value;
        }) ?? throw new NotFoundException(nameof(FolderCacheService), nodePath.ToString());

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
            _cache.Remove(nodePath);
        }

        return cacheItem;
    }

    public IFileExplorerFileNode GetFile(NodePath nodePath)
    {
        var dir = _cache.TryGetValue<IFolderCacheItem>(nodePath.Parent, out var cacheItem) ? cacheItem : null;

        if (dir is null)
        {
            logger.LogWarning("Directory {DirectoryAbsolutePath} not found in cache when retrieving file",
                nodePath.Parent.AbsolutePath);

            return fileExplorerConverter.Convert(nodePath);
        }

        var file = dir.Folder.Children.OfType<IFileExplorerFileNode>()
            .FirstOrDefault(f => f.Path.RootPath == nodePath.RootPath && f.Path.RelativePath == nodePath.RelativePath);

        if (file is null)
        {
            logger.LogWarning("File {FileAbsolutePath} not found in cache", nodePath.AbsolutePath);

            if (Debugger.IsAttached && File.Exists(nodePath.AbsolutePath))
            {
                Debugger.Break();
            }

            return fileExplorerConverter.Convert(new FileInfo(nodePath.AbsolutePath), dir.Folder.Node);
        }

        return file;
    }

    public (IFileExplorerFolder Parent, IFileExplorerFileNode File) GetFileAndFolder(NodePath nodePath)
    {
        var folder = GetOrAdd(nodePath.Parent);

        var file = folder
                       .Folder
                       .Children
                       .OfType<IFileExplorerFileNode>()
                       .SingleOrDefault(f => f.Path.RootPath == nodePath.RootPath && f.Path.RelativePath == nodePath.RelativePath) ??
                   throw new NotFoundException(nodePath.Parent.AbsolutePath, nodePath.FileName);

        return (folder.Folder, file);
    }

    public void InvalidateFolder(NodePath nodePath)
    {
        _cache.Remove(nodePath);
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
        ItemUpdated?.Invoke(parent.Folder, new FolderCacheServiceItemUpdatedEventArgs
        {
            Item = e.Item,
            OldPath = e.OldPath
        });
    }

    private void OnItemRemoved(object? sender, NodePath nodePath)
    {
        if (!IsFolderCacheItem(sender, out var parent)) return;
        ItemRemoved?.Invoke(parent.Folder, new FolderCacheServiceItemRemovedEventArgs
        {
            Path = nodePath,
            Parent = parent.Folder.Node
        });
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
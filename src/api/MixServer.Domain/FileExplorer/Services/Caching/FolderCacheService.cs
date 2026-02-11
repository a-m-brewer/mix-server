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

    Task<ICacheFolder> GetOrAddAsync(NodePath nodePath);
    Task<IFileExplorerFileNode> GetFileAsync(NodePath nodePath);
    Task<(IFileExplorerFolder Parent, IFileExplorerFileNode File)> GetFileAndFolderAsync(NodePath nodePath);
    void InvalidateFolder(NodePath nodePath);
}

public class FolderCacheService : IFolderCacheService
{
    private readonly SegmentedLruCache<string, IFolderCacheItem> _cache;

    private readonly ILogger<FolderCacheService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IFileExplorerConverter _fileExplorerConverter;
    private readonly IRootFileExplorerFolder _rootFolder;

    public FolderCacheService(ILogger<FolderCacheService> logger,
        ILoggerFactory loggerFactory,
        IOptions<FolderCacheSettings> options,
        IFileExplorerConverter fileExplorerConverter,
        IRootFileExplorerFolder rootFolder)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _fileExplorerConverter = fileExplorerConverter;
        _rootFolder = rootFolder;
        _cache = new SegmentedLruCache<string, IFolderCacheItem>(options.Value.MaxCachedDirectories, PostEvictionCallback);
    }

    public event EventHandler<IFileExplorerNode>? ItemAdded;

    public event EventHandler<FolderCacheServiceItemUpdatedEventArgs>? ItemUpdated;

    public event EventHandler<FolderCacheServiceItemRemovedEventArgs>? ItemRemoved;
    public event EventHandler<IFileExplorerFolder>? FolderAdded;
    public event EventHandler<IFileExplorerFolder>? FolderRemoved;

    public async Task<ICacheFolder> GetOrAddAsync(NodePath nodePath)
    {
        var cacheMiss = false;
        var cacheItem = await _cache.GetOrAddAsync(nodePath.AbsolutePath, _ =>
        {
            cacheMiss = true;

            IFolderCacheItem item =
                new FolderCacheItem(nodePath,
                    _loggerFactory.CreateLogger<FolderCacheItem>(),
                    _fileExplorerConverter,
                    _rootFolder);
            item.ItemAdded += OnItemAdded;
            item.ItemUpdated += OnItemUpdated;
            item.ItemRemoved += OnItemRemoved;

            _logger.LogInformation("Cache miss for {NodePath}, adding to cache", nodePath.AbsolutePath);
            return Task.FromResult(item);
        }) ?? throw new NotFoundException(nameof(FolderCacheService), nodePath.ToString());

        if (cacheItem.Folder.Node.Exists)
        {
            if (cacheMiss)
            {
                _ = Task.Run(() => FolderAdded?.Invoke(this, cacheItem.Folder));
            }
        }
        else
        {
            // We don't want to keep missing folders in the cache.
            _cache.Remove(nodePath.AbsolutePath);
        }

        return cacheItem;
    }

    public async Task<IFileExplorerFileNode> GetFileAsync(NodePath nodePath)
    {
        var dir = await GetOrAddAsync(nodePath.Parent);

        var file = dir.Folder.Children.OfType<IFileExplorerFileNode>()
            .FirstOrDefault(f => f.Path.RootPath == nodePath.RootPath && f.Path.RelativePath == nodePath.RelativePath);

        if (file is not null)
        {
            return file;
        }

        _logger.LogDebug("File {FileAbsolutePath} not found in cache, loading from disk", nodePath.AbsolutePath);

        var fileFromDisk = _fileExplorerConverter.Convert(new FileInfo(nodePath.AbsolutePath), dir.Folder.Node);
        
        // Add the file to the cache so subsequent requests find it
        dir.AddChildIfNotExists(fileFromDisk);
        
        return fileFromDisk;
    }

    public async Task<(IFileExplorerFolder Parent, IFileExplorerFileNode File)> GetFileAndFolderAsync(NodePath nodePath)
    {
        var folder = await GetOrAddAsync(nodePath.Parent);

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
        _cache.Remove(nodePath.AbsolutePath);
    }

    private void PostEvictionCallback(string key, IFolderCacheItem value)
    {
        _logger.LogInformation("Cache entry for {Key} has been evicted", key);

        value.ItemAdded -= OnItemAdded;
        value.ItemUpdated -= OnItemUpdated;
        value.ItemRemoved -= OnItemRemoved;

        value.Dispose();
        
        FolderRemoved?.Invoke(this, value.Folder);
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

    private void OnItemRemoved(object? sender, IFileExplorerNode node)
    {
        if (!IsFolderCacheItem(sender, out var parent)) return;
        ItemRemoved?.Invoke(parent.Folder, new FolderCacheServiceItemRemovedEventArgs
        {
            Node = node,
            Parent = parent.Folder.Node
        });
    }

    private bool IsFolderCacheItem(object? sender, [MaybeNullWhen(false)] out IFolderCacheItem parent)
    {
        if (sender is not IFolderCacheItem folderCacheItem)
        {
            _logger.LogWarning("event raised by non-folder cache item Sender: {Sender}",
                sender?.GetType().Name ?? "null");
            parent = null;
            return false;
        }

        parent = folderCacheItem;
        return true;
    }
}
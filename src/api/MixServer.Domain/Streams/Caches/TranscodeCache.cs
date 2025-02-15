using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Caching;
using MixServer.Domain.Settings;


namespace MixServer.Domain.Streams.Caches;

public interface ITranscodeCache : IDisposable
{
    void Initialize();
}

public class TranscodeCache(
    IOptions<DataFolderSettings> dataFolderSettings,
    IFileExplorerConverter fileExplorerConverter,
    ILogger<TranscodeCache> logger,
    ILoggerFactory loggerFactory) : ITranscodeCache
{
    private FolderCacheItem? _cacheFolder;
    private ConcurrentDictionary<string, FolderCacheItem> _transcodeFolders = new();

    [MemberNotNullWhen(true, nameof(_cacheFolder))]
    public void Initialize()
    {
        var transcodeFolder = dataFolderSettings.Value.TranscodesFolder;
        if (!Directory.Exists(transcodeFolder))
        {
            Directory.CreateDirectory(transcodeFolder);
        }

        _cacheFolder = new FolderCacheItem(transcodeFolder, loggerFactory.CreateLogger<FolderCacheItem>(), fileExplorerConverter);

        foreach (var child in _cacheFolder.Folder.Children)
        {
            TranscodeFolderOnItemAdded(this, child);
        }
        
        _cacheFolder.ItemAdded += CacheFolderOnItemAdded;
        _cacheFolder.ItemUpdated += CacheFolderOnItemUpdated;
        _cacheFolder.ItemRemoved += CacheFolderOnItemRemoved;
    }

    private void CacheFolderOnItemAdded(object? sender, IFileExplorerNode e)
    {
        if (e is not IFileExplorerFolderNode)
        {
            return;
        }

        var hash = Path.GetDirectoryName(e.AbsolutePath);

        if (string.IsNullOrWhiteSpace(hash))
        {
            logger.LogWarning("Missing directory name for {Path}", e.AbsolutePath);
            return;
        }
        
        if (_transcodeFolders.ContainsKey(hash))
        {
            logger.LogWarning("Duplicate directory name for {Hash}", hash);
            return;
        }
        
        var folder = new FolderCacheItem(e.AbsolutePath, loggerFactory.CreateLogger<FolderCacheItem>(), fileExplorerConverter);
        folder.ItemAdded += TranscodeFolderOnItemAdded;
        folder.ItemUpdated += TranscodeFolderOnItemUpdated;
        folder.ItemRemoved += TranscodeFolderOnItemRemoved;
        _transcodeFolders[hash] = folder;
        logger.LogInformation("Added transcode folder {Hash}", hash);
    }

    private void CacheFolderOnItemRemoved(object? sender, string absolutePath)
    {
        var hash = Path.GetDirectoryName(absolutePath);

        if (string.IsNullOrWhiteSpace(hash) || !_transcodeFolders.TryRemove(hash, out var folder))
        {
            return;
        }
        
        folder.ItemAdded -= TranscodeFolderOnItemAdded;
        folder.ItemUpdated -= TranscodeFolderOnItemUpdated;
        folder.ItemRemoved -= TranscodeFolderOnItemRemoved;
        folder.Dispose();
        logger.LogInformation("Removed transcode folder {Hash}", absolutePath);
    }
    
    private void CacheFolderOnItemUpdated(object? sender, FolderItemUpdatedEventArgs e)
    {
        if (e.Item is not IFileExplorerFolderNode)
        {
            return;
        }
        
        CacheFolderOnItemRemoved(sender, e.OldFullPath);
        CacheFolderOnItemAdded(sender, e.Item);
    }
    
    private void TranscodeFolderOnItemRemoved(object? sender, string e)
    {
        logger.LogInformation("Transcode folder item removed {Path}", e);
    }

    private void TranscodeFolderOnItemUpdated(object? sender, FolderItemUpdatedEventArgs e)
    {
        logger.LogInformation("Transcode folder item updated {Path}", e.Item.AbsolutePath);
    }

    private void TranscodeFolderOnItemAdded(object? sender, IFileExplorerNode e)
    {
        logger.LogInformation("Transcode folder item added {Path}", e.AbsolutePath);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        foreach (var transcodeFolder in _transcodeFolders.Values)
        {
            TranscodeFolderOnItemRemoved(this, transcodeFolder.Folder.Node.AbsolutePath);
        }
        _transcodeFolders.Clear();

        if (_cacheFolder is null)
        {
            return;
        }
        
        _cacheFolder.ItemAdded -= CacheFolderOnItemAdded;
        _cacheFolder.ItemUpdated -= CacheFolderOnItemUpdated;
        _cacheFolder.ItemRemoved -= CacheFolderOnItemRemoved;
        _cacheFolder.Dispose();
        _cacheFolder = null;
    }
}
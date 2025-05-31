using System.Collections.Concurrent;
using DebounceThrottle;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models.Indexing;
using MixServer.Domain.FileExplorer.Repositories;

namespace MixServer.Domain.FileExplorer.Services;

public class RootChildDirectoryWatcher : IDisposable
{
    private readonly string _absolutePath;
    private readonly ILogger<RootChildDirectoryWatcher> _logger;
    private readonly IRootChildDirectoryChangeChannel _channel;
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    private readonly FileSystemWatcher _watcher;
    
    public RootChildDirectoryWatcher(
        string absolutePath,
        ILogger<RootChildDirectoryWatcher> logger,
        IRootChildDirectoryChangeChannel channel)
    {
        _absolutePath = absolutePath;
        _logger = logger;
        _channel = channel;
        _watcher = new FileSystemWatcher(absolutePath)
        {
            NotifyFilter = NotifyFilters.LastWrite
                           | NotifyFilters.DirectoryName
                           | NotifyFilters.FileName
        };
        _watcher.IncludeSubdirectories = false;
        _watcher.InternalBufferSize = 64 * 1024; // 64 KB buffer size (maximum recommended size)

        _watcher.Created += OnCreated;
        _watcher.Deleted += OnDeleted;
        _watcher.Changed += OnChanged;
        _watcher.Renamed += OnRenamed;
        _watcher.Error += WatcherOnError;
        
        _watcher.EnableRaisingEvents = true;
    }

    private void OnCreated(object sender, FileSystemEventArgs e) =>
        SubmitCacheUpdate(e.FullPath, RootFolderChangeType.Changed, e.ChangeType);

    private void OnDeleted(object sender, FileSystemEventArgs e) =>
        SubmitCacheUpdate(e.FullPath, RootFolderChangeType.Deleted, e.ChangeType);

    private void OnChanged(object sender, FileSystemEventArgs e) =>
        SubmitCacheUpdate(e.FullPath, RootFolderChangeType.Changed, e.ChangeType);

    private void OnRenamed(object sender, RenamedEventArgs e) =>
        SubmitCacheUpdate(e.FullPath, RootFolderChangeType.Renamed, e.ChangeType, e.OldFullPath);

    private void WatcherOnError(object sender, ErrorEventArgs e)
    {
        var ex = e.GetException();

        if (ex is IOException)
        {
            // TODO: trigger full re-scan
            _logger.LogWarning(ex, "IO Exception occurred in FileSystemWatcher for {AbsolutePath}. Triggering rescan...", _absolutePath);
        }
        else
        {
            _logger.LogError(ex, "Unknown Error occurred in FileSystemWatcher for {AbsolutePath}", _absolutePath);
        }
    }
    
    private void SubmitCacheUpdate(string fullName, RootFolderChangeType rootFolderChangeType, WatcherChangeTypes watcherChangeType, string oldFullName = "")
    {
        var key = $"{fullName}-{rootFolderChangeType}";
        var cacheItem = _cache.GetOrCreate<DebounceDispatcher>(key, entry =>
        {
            var maxDelay = TimeSpan.FromSeconds(5);
            var debounceDispatcher = new DebounceDispatcher(TimeSpan.FromSeconds(1), maxDelay);
            entry.SlidingExpiration = maxDelay * 1.25;
        
            return debounceDispatcher;
        });
        
        if (cacheItem is null)
        {
            _logger.LogError("Failed to create or retrieve debounce dispatcher for {FullName}", fullName);
            return;
        }
        
        cacheItem.Debounce(() =>
        {
            _ = _channel.WriteAsync(new RootChildChangeEvent
            {
                FullName = fullName,
                RootFolderChangeType = rootFolderChangeType,
                WatcherChangeType = watcherChangeType,
                OldFullName = oldFullName
            }); 
        });
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _watcher.Created -= OnCreated;
        _watcher.Deleted -= OnDeleted;
        _watcher.Changed -= OnChanged;
        _watcher.Renamed -= OnRenamed;
        _watcher.Error -= WatcherOnError;
        _watcher.Dispose();
    }
}
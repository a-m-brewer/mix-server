using Microsoft.Extensions.Logging;
using MixServer.Domain.FileExplorer.Services;

namespace MixServer.Domain.FileExplorer.Models.Caching;

public interface IFolder
{
    ICacheDirectoryInfo DirectoryInfo { get; }

    IReadOnlyCollection<ICacheDirectoryInfo> Directories { get; }

    IReadOnlyCollection<ICacheFileInfo> Files { get; }
}

public interface IFolderCacheItem : IFolder, IDisposable
{
    event EventHandler<ICacheFileSystemInfo> ItemAdded;

    event EventHandler<FolderItemUpdatedEventArgs> ItemUpdated;

    event EventHandler<string> ItemRemoved;
}

public class FolderCacheItem : IFolderCacheItem
{
    private SemaphoreSlim _semaphore = new(1, 1);
    
    private readonly string _absolutePath;
    private readonly ILogger<FolderCacheItem> _logger;
    private readonly IMimeTypeService _mimeTypeService;

    private readonly FileSystemWatcher _watcher;

    public FolderCacheItem(
        string absolutePath,
        ILogger<FolderCacheItem> logger,
        IMimeTypeService mimeTypeService)
    {
        _absolutePath = absolutePath;
        _logger = logger;
        _mimeTypeService = mimeTypeService;

        var directoryInfo = new DirectoryInfo(absolutePath);
        DirectoryInfo = new CacheDirectoryInfo(directoryInfo);

        foreach (var directory in directoryInfo.GetDirectories())
        {
            FileSystemItems.Add(new CacheDirectoryInfo(directory));
        }

        foreach (var file in directoryInfo.GetFiles())
        {
            FileSystemItems.Add(new CacheFileInfo(file, _mimeTypeService.GetMimeType(file.FullName)));
        }

        _watcher = new FileSystemWatcher(absolutePath)
        {
            NotifyFilter = NotifyFilters.Attributes
                           | NotifyFilters.CreationTime
                           | NotifyFilters.DirectoryName
                           | NotifyFilters.FileName
                           | NotifyFilters.LastAccess
                           | NotifyFilters.LastWrite
                           | NotifyFilters.Security
                           | NotifyFilters.Size
        };

        _watcher.Created += OnCreated;
        _watcher.Deleted += OnDeleted;
        _watcher.Changed += OnChanged;
        _watcher.Renamed += OnRenamed;
        _watcher.Error += WatcherOnError;
        
        _watcher.EnableRaisingEvents = true;
    }

    public event EventHandler<ICacheFileSystemInfo>? ItemAdded;
    public event EventHandler<FolderItemUpdatedEventArgs>? ItemUpdated;
    public event EventHandler<string>? ItemRemoved;
    
    public ICacheDirectoryInfo DirectoryInfo { get; }
    
    private ICollection<ICacheFileSystemInfo> FileSystemItems { get; } = new List<ICacheFileSystemInfo>();

    public IReadOnlyCollection<ICacheDirectoryInfo> Directories =>
        FileSystemItems.OfType<ICacheDirectoryInfo>().ToList();

    public IReadOnlyCollection<ICacheFileInfo> Files => FileSystemItems.OfType<ICacheFileInfo>().ToList();

    private void OnCreated(object sender, FileSystemEventArgs e) =>
        UpdateCache(e.FullPath, ChangeType.Created);

    private void OnDeleted(object sender, FileSystemEventArgs e) =>
        UpdateCache(e.FullPath, ChangeType.Deleted);

    private void OnChanged(object sender, FileSystemEventArgs e) =>
        UpdateCache(e.FullPath, ChangeType.Changed);

    private void OnRenamed(object sender, RenamedEventArgs e) =>
        UpdateCache(e.FullPath, ChangeType.Renamed, e.OldFullPath);

    private void WatcherOnError(object sender, ErrorEventArgs e)
    {
        _logger.LogError(e.GetException(), "Error occurred in FileSystemWatcher for {AbsolutePath}", _absolutePath);
    }
    
    private async void UpdateCache(string fullName, ChangeType changeType, string oldFullName = "")
    {
        await _semaphore.WaitAsync();

        try
        {
            _logger.LogInformation("[{FolderAbsolutePath}]: File: {FileAbsolutePath} Change: {ChangeType}",
                _absolutePath,
                fullName,
                changeType);
        
            var directoryExists = Directory.Exists(fullName);
            var fileExists = File.Exists(fullName);
            var isFile = fileExists && !directoryExists;
        
            switch (changeType)
            {
                case ChangeType.Created:
                    if (FileSystemItems.Any(a => a.FullName == fullName))
                    {
                        _logger.LogTrace("Item already exists in cache, skipping: {FullName}", fullName);
                        return;
                    }
                    var newItem = Create(isFile, fullName);
                    ItemAdded?.Invoke(this, newItem);
                    break;
                case ChangeType.Deleted:
                    Delete(fullName);
                    ItemRemoved?.Invoke(this, fullName);
                    break;
                case ChangeType.Changed:
                    var changedItem = Replace(isFile, fullName);
                    ItemUpdated?.Invoke(this, new FolderItemUpdatedEventArgs(changedItem, fullName));
                    break;
                case ChangeType.Renamed:
                    var renamedItem = Replace(isFile, fullName, oldFullName);
                    ItemUpdated?.Invoke(this, new FolderItemUpdatedEventArgs(renamedItem, oldFullName));
                    break;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }


    private ICacheFileSystemInfo Replace(bool isFile, string fullName) => Replace(isFile, fullName, fullName);

    private ICacheFileSystemInfo Replace(bool isFile, string fullName, string oldFullName)
    {
        Delete(oldFullName);
        
        return Create(isFile, fullName);
    }

    private ICacheFileSystemInfo Create(bool isFile, string fullName)
    {
        ICacheFileSystemInfo info = isFile
            ? new CacheFileInfo(new FileInfo(fullName), _mimeTypeService.GetMimeType(fullName))
            : new CacheDirectoryInfo(new DirectoryInfo(fullName));
        FileSystemItems.Add(info);
        _logger.LogDebug("Added: {AbsolutePath} to {CurrentFolder}", fullName, _absolutePath);

        return info;
    }

    private void Delete(string fullName)
    {
        var item = FileSystemItems.FirstOrDefault(x => x.FullName == fullName);
        if (item is not null)
        {
            FileSystemItems.Remove(item);
            _logger.LogDebug("Removed: {AbsolutePath} from {CurrentFolder}", fullName, _absolutePath);
        }
        else
        {
            _logger.LogTrace("Could not remove: {AbsolutePath} as it was not found in {CurrentFolder}", fullName, _absolutePath);
        }
    }

    private enum ChangeType
    {
        Created,
        Deleted,
        Changed,
        Renamed
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
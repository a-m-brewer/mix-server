using Microsoft.Extensions.Logging;
using MixServer.Domain.FileExplorer.Converters;
using DirectoryInfo = System.IO.DirectoryInfo;

namespace MixServer.Domain.FileExplorer.Models.Caching;

public interface ICacheFolder
{
    IFileExplorerFolder Folder { get; }
}

public interface IFolderCacheItem : ICacheFolder, IDisposable
{
    event EventHandler<IFileExplorerNode> ItemAdded;

    event EventHandler<FolderItemUpdatedEventArgs> ItemUpdated;

    event EventHandler<string> ItemRemoved;
}

public class FolderCacheItem : IFolderCacheItem
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    private readonly string _absolutePath;
    private readonly ILogger<FolderCacheItem> _logger;
    private readonly IFileExplorerConverter _fileExplorerConverter;

    private readonly FileSystemWatcher _watcher;
    private readonly FileExplorerFolder _folder;

    public FolderCacheItem(
        string absolutePath,
        ILogger<FolderCacheItem> logger,
        IFileExplorerConverter fileExplorerConverter)
    {
        _absolutePath = absolutePath;
        _logger = logger;
        _fileExplorerConverter = fileExplorerConverter;

        var directoryInfo = new DirectoryInfo(absolutePath);

        _folder = fileExplorerConverter.ConvertToFolder(directoryInfo);

        foreach (var directory in directoryInfo.GetDirectories())
        {
            _folder.AddChild(fileExplorerConverter.ConvertToFolderNode(directory));
        }

        foreach (var file in directoryInfo.GetFiles())
        {
            _folder.AddChild(fileExplorerConverter.ConvertToFileNode(file, _folder.Node));
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

    public event EventHandler<IFileExplorerNode>? ItemAdded;
    public event EventHandler<FolderItemUpdatedEventArgs>? ItemUpdated;
    public event EventHandler<string>? ItemRemoved;
    
    public IFileExplorerFolder Folder => _folder;

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
                    if (Folder.Children.Any(a => a.AbsolutePath == fullName))
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


    private IFileExplorerNode Replace(bool isFile, string fullName) => Replace(isFile, fullName, fullName);

    private IFileExplorerNode Replace(bool isFile, string fullName, string oldFullName)
    {
        Delete(oldFullName);
        
        return Create(isFile, fullName);
    }

    private IFileExplorerNode Create(bool isFile, string fullName)
    {
        IFileExplorerNode info = isFile
            ? _fileExplorerConverter.ConvertToFileNode(new FileInfo(fullName), _folder.Node)
            : _fileExplorerConverter.ConvertToFolderNode(new DirectoryInfo(fullName));
        _folder.AddChild(info);
        _logger.LogDebug("Added: {AbsolutePath} to {CurrentFolder}", fullName, _absolutePath);

        return info;
    }

    private void Delete(string fullName)
    {
        var item = Folder.Children.FirstOrDefault(x => x.AbsolutePath == fullName);
        if (item is not null)
        {
            _folder.RemoveChild(item);
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
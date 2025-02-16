using System.Collections.Concurrent;
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
    private record FolderChangeEvent(
        string fullName,
        ChangeType changeType,
        WatcherChangeTypes watcherChangeType,
        string oldFullName = "");
    
    private readonly CancellationTokenSource _cts = new();
    private readonly BlockingCollection<FolderChangeEvent> _events = new();
    
    private readonly string _absolutePath;
    private readonly ILogger<FolderCacheItem> _logger;
    private readonly IFileExplorerConverter _fileExplorerConverter;
    private readonly LogLevel _logLevel;

    private readonly FileSystemWatcher? _watcher;
    private readonly FileExplorerFolder _folder;

    public FolderCacheItem(
        string absolutePath,
        ILogger<FolderCacheItem> logger,
        IFileExplorerConverter fileExplorerConverter,
        LogLevel logLevel = LogLevel.Information)
    {
        _absolutePath = absolutePath;
        _logger = logger;
        _fileExplorerConverter = fileExplorerConverter;
        _logLevel = logLevel;

        var directoryInfo = new DirectoryInfo(absolutePath);
        
        _folder = new FileExplorerFolder(_fileExplorerConverter.Convert(directoryInfo));

        if (!_folder.Node.Exists)
        {
            return;
        }
        
        foreach (var directory in directoryInfo.GetDirectories())
        {
            _folder.AddChild(fileExplorerConverter.Convert(directory));
        }

        foreach (var file in directoryInfo.GetFiles())
        {
            _folder.AddChild(fileExplorerConverter.Convert(file, _folder.Node));
        }

        _watcher = new FileSystemWatcher(absolutePath)
        {
            NotifyFilter = NotifyFilters.Attributes
                           | NotifyFilters.CreationTime
                           | NotifyFilters.DirectoryName
                           | NotifyFilters.FileName
        };

        _watcher.Created += OnCreated;
        _watcher.Deleted += OnDeleted;
        _watcher.Changed += OnChanged;
        _watcher.Renamed += OnRenamed;
        _watcher.Error += WatcherOnError;
        
        _watcher.EnableRaisingEvents = true;
        
        Task.Run(ProcessEvents);
    }

    public event EventHandler<IFileExplorerNode>? ItemAdded;
    public event EventHandler<FolderItemUpdatedEventArgs>? ItemUpdated;
    public event EventHandler<string>? ItemRemoved;
    
    public IFileExplorerFolder Folder => _folder;

    private void OnCreated(object sender, FileSystemEventArgs e) =>
        SubmitCacheUpdate(e.FullPath, ChangeType.Created, e.ChangeType);

    private void OnDeleted(object sender, FileSystemEventArgs e) =>
        SubmitCacheUpdate(e.FullPath, ChangeType.Deleted, e.ChangeType);

    private void OnChanged(object sender, FileSystemEventArgs e) =>
        SubmitCacheUpdate(e.FullPath, ChangeType.Changed, e.ChangeType);

    private void OnRenamed(object sender, RenamedEventArgs e) =>
        SubmitCacheUpdate(e.FullPath, ChangeType.Renamed, e.ChangeType, e.OldFullPath);

    private void WatcherOnError(object sender, ErrorEventArgs e)
    {
        _logger.LogError(e.GetException(), "Error occurred in FileSystemWatcher for {AbsolutePath}", _absolutePath);
    }
    
    private void SubmitCacheUpdate(string fullName, ChangeType changeType, WatcherChangeTypes watcherChangeType, string oldFullName = "")
    {
        _events.Add(new FolderChangeEvent(fullName, changeType, watcherChangeType, oldFullName));
    }
    
    private void ProcessEvents()
    {
        try
        {
            foreach (var e in _events.GetConsumingEnumerable(_cts.Token))
            {
                try
                {
                    UpdateCache(e);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Error occurred processing event: {Event}", e);
                }
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Stopping processing events for {AbsolutePath}", _absolutePath);
        }
    }

    private void UpdateCache(FolderChangeEvent e)
    {
        var directoryExists = Directory.Exists(e.fullName);
        var fileExists = File.Exists(e.fullName);
        var isFile = fileExists && !directoryExists;
        
        switch (e.changeType)
        {
            case ChangeType.Created:
                if (Folder.Children.Any(a => a.AbsolutePath == e.fullName))
                {
                    return;
                }
                var newItem = Create(isFile, e.fullName);
                ItemAdded?.Invoke(this, newItem);
                break;
            case ChangeType.Deleted:
                Delete(e.fullName);
                ItemRemoved?.Invoke(this, e.fullName);
                break;
            case ChangeType.Changed:
                var changedItem = Replace(isFile, e.fullName);
                ItemUpdated?.Invoke(this, new FolderItemUpdatedEventArgs(changedItem, e.fullName));
                break;
            case ChangeType.Renamed:
                var renamedItem = Replace(isFile, e.fullName, e.oldFullName);
                ItemUpdated?.Invoke(this, new FolderItemUpdatedEventArgs(renamedItem, e.oldFullName));
                break;
        }
    }

    private IFileExplorerNode Replace(bool isFile, string fullName) => Replace(isFile, fullName, fullName);

    private IFileExplorerNode Replace(bool isFile, string fullName, string oldFullName)
    {
        Delete(fullName);
        if (oldFullName != fullName)
        {
            Delete(oldFullName);
        }
        
        return Create(isFile, fullName);
    }

    private IFileExplorerNode Create(bool isFile, string fullName)
    {
        IFileExplorerNode info = isFile
            ? _fileExplorerConverter.Convert(new FileInfo(fullName), _folder.Node)
            : _fileExplorerConverter.Convert(new DirectoryInfo(fullName));
        _folder.AddChild(info);
        _logger.Log(_logLevel, "Added: {AbsolutePath} to {CurrentFolder}", fullName, _absolutePath);

        return info;
    }

    private void Delete(string fullName)
    {
        var name = Path.GetFileName(fullName);
        _folder.RemoveChild(name);
        _logger.Log(_logLevel, "Removed: {AbsolutePath} from {CurrentFolder}", fullName, _absolutePath);
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
        
        _cts.Cancel();

        if (_watcher is null)
        {
            return;
        }

        _watcher.Created -= OnCreated;
        _watcher.Deleted -= OnDeleted;
        _watcher.Changed -= OnChanged;
        _watcher.Renamed -= OnRenamed;
        _watcher.Error -= WatcherOnError;
        _watcher.Dispose();
    }
}
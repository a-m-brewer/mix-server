using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MixServer.Domain.Callbacks;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Caching;
using MixServer.Domain.Settings;
using MixServer.Domain.Streams.Enums;
using MixServer.Domain.Streams.Models;


namespace MixServer.Domain.Streams.Caches;

public interface ITranscodeCache : IDisposable
{
    void Initialize();
    TranscodeState GetTranscodeStatus(string hash);
    void CalculateHasCompletePlaylist(string hash);
}

public class TranscodeCache(
    IOptions<DataFolderSettings> dataFolderSettings,
    IFileExplorerConverter fileExplorerConverter,
    ILogger<TranscodeCache> logger,
    ILoggerFactory loggerFactory,
    IServiceProvider serviceProvider) : ITranscodeCache
{
    private FolderCacheItem? _cacheFolder;
    private ConcurrentDictionary<string, Transcode> _transcodeFolders = new();

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
            CacheFolderOnItemAdded(this, child);
        }
        
        _cacheFolder.ItemAdded += CacheFolderOnItemAdded;
        _cacheFolder.ItemUpdated += CacheFolderOnItemUpdated;
        _cacheFolder.ItemRemoved += CacheFolderOnItemRemoved;
    }

    public TranscodeState GetTranscodeStatus(string hash)
    {
        if (!_transcodeFolders.TryGetValue(hash, out var transcode))
        {
            return TranscodeState.None;
        }
        
        return transcode.HasCompletePlaylist
            ? TranscodeState.Completed 
            : TranscodeState.InProgress;
    }

    public void CalculateHasCompletePlaylist(string hash)
    {
        if (!_transcodeFolders.TryGetValue(hash, out var transcode))
        {
            return;
        }
        
        transcode.CalculateHasCompletePlaylist();
    }

    private async void CacheFolderOnItemAdded(object? sender, IFileExplorerNode e)
    {
        if (e is not IFileExplorerFolderNode)
        {
            return;
        }

        var hash = e.Name;

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
        
        var folder = new FolderCacheItem(e.AbsolutePath, loggerFactory.CreateLogger<FolderCacheItem>(), fileExplorerConverter, LogLevel.None);
        folder.ItemAdded += TranscodeFolderOnItemAdded;
        folder.ItemUpdated += TranscodeFolderOnItemUpdated;
        folder.ItemRemoved += TranscodeFolderOnItemRemoved;
        var transcode = new Transcode(loggerFactory.CreateLogger<Transcode>())
        {
            FileHash = hash,
            TranscodeFolder = folder
        };

        try
        {
            await transcode.InitializeAsync();
            
            await SendTranscodeStatusUpdatedAsync(transcode.FileHash, 
                transcode.HasCompletePlaylist
                    ? TranscodeState.Completed
                    : TranscodeState.InProgress);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to initialize transcode folder {Hash}", hash);
            return;
        }
        
        transcode.HasCompletePlaylistChanged += TranscodeOnHasCompletePlaylistChanged;
        
        _transcodeFolders[hash] = transcode;
        logger.LogInformation("Added transcode folder {Hash}", hash);
    }

    private void CacheFolderOnItemRemoved(object? sender, string absolutePath)
    {
        var hash = Path.GetFileName(absolutePath);

        if (string.IsNullOrWhiteSpace(hash) || !_transcodeFolders.TryRemove(hash, out var transcode))
        {
            return;
        }
        
        transcode.HasCompletePlaylistChanged -= TranscodeOnHasCompletePlaylistChanged;

        var folder = transcode.TranscodeFolder;
        
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
    
    private void TranscodeFolderOnItemRemoved(object? sender, string absolutePath)
    {
        CalculateHasCompletePlaylistFromPath(absolutePath);
    }

    private void TranscodeFolderOnItemUpdated(object? sender, FolderItemUpdatedEventArgs e)
    {
        CalculateHasCompletePlaylistFromPath(e.Item.AbsolutePath);
    }

    private void TranscodeFolderOnItemAdded(object? sender, IFileExplorerNode e)
    {
        CalculateHasCompletePlaylistFromPath(e.AbsolutePath);
    }
    
    private void TranscodeOnHasCompletePlaylistChanged(object? sender, EventArgs e)
    {
        if (sender is not Transcode transcode)
        {
            return;
        }

        _ = SendTranscodeStatusUpdatedAsync(
            transcode.FileHash, 
            transcode.HasCompletePlaylist
                ? TranscodeState.Completed
                : TranscodeState.InProgress);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        foreach (var transcodeFolder in _transcodeFolders.Values)
        {
            TranscodeFolderOnItemRemoved(this, transcodeFolder.TranscodeFolder.Folder.Node.AbsolutePath);
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
    
    private void CalculateHasCompletePlaylistFromPath(string absolutePath)
    {
        var hash = Path.GetDirectoryName(absolutePath);
        var fileName = Path.GetFileName(absolutePath);
        
        if (string.IsNullOrWhiteSpace(hash) || 
            !fileName.EndsWith(".m3u8"))
        {
            return;
        }
        
        CalculateHasCompletePlaylist(hash);
    }
    
    private async Task SendTranscodeStatusUpdatedAsync(string hash, TranscodeState state)
    {
        using var scope = serviceProvider.CreateScope();
        var callbackService = scope.ServiceProvider.GetRequiredService<ICallbackService>();

        await callbackService.TranscodeStatusUpdated(hash, state);
    }
}
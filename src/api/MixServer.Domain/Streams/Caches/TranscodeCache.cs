using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Caching;
using MixServer.Domain.Settings;
using MixServer.Domain.Streams.Enums;
using MixServer.Domain.Streams.Events;
using MixServer.Domain.Streams.Models;
using Newtonsoft.Json;


namespace MixServer.Domain.Streams.Caches;

public interface ITranscodeCache : IDisposable
{
    event EventHandler<TranscodeStatusUpdatedEventArgs>? TranscodeStatusUpdated;
    
    Task InitializeAsync();
    TranscodeState GetTranscodeStatus(string hash);
    void CalculateHasCompletePlaylist(string hash);
    Task AddTranscodeMappingAsync(string hash, string absoluteFilePath);
}

public class TranscodeCache(
    IOptions<DataFolderSettings> dataFolderSettings,
    IFileExplorerConverter fileExplorerConverter,
    ILogger<TranscodeCache> logger,
    ILoggerFactory loggerFactory) : ITranscodeCache
{
    private FolderCacheItem? _cacheFolder;
    private readonly ConcurrentDictionary<string, string> _transcodeHashToFileMappings = new();
    private readonly ConcurrentDictionary<string, Transcode> _transcodeFolders = new();

    public event EventHandler<TranscodeStatusUpdatedEventArgs>? TranscodeStatusUpdated;

    [MemberNotNullWhen(true, nameof(_cacheFolder))]
    public async Task InitializeAsync()
    {
        var transcodeFolder = dataFolderSettings.Value.TranscodesFolder;
        if (!Directory.Exists(transcodeFolder))
        {
            Directory.CreateDirectory(transcodeFolder);
        }
        
        await LoadTranscodeMappingsAsync();

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

    public async Task AddTranscodeMappingAsync(string hash, string absoluteFilePath)
    {
        _transcodeHashToFileMappings[hash] = absoluteFilePath;
        await SaveTranscodeMappingsAsync();
    }

    private async Task SaveTranscodeMappingsAsync()
    {
        await using var streamWriter = File.CreateText(Path.Join(dataFolderSettings.Value.TranscodesFolder, "transcode-mappings.json"));
        JsonSerializer serializer = new();
        serializer.Serialize(streamWriter, _transcodeHashToFileMappings);
    }

    private async Task LoadTranscodeMappingsAsync()
    {
        if (!File.Exists(Path.Join(dataFolderSettings.Value.TranscodesFolder, "transcode-mappings.json")))
        {
            _transcodeHashToFileMappings.Clear();
            return;
        }
        
        using var streamReader = File.OpenText(Path.Join(dataFolderSettings.Value.TranscodesFolder, "transcode-mappings.json"));
        await using var jsonReader = new JsonTextReader(streamReader);
        JsonSerializer serializer = new();
        var mappings = serializer.Deserialize<Dictionary<string, string>>(jsonReader);
        
        _transcodeHashToFileMappings.Clear();
        
        if (mappings is null)
        {
            return;
        }
        
        foreach (var (key, value) in mappings)
        {
            _transcodeHashToFileMappings[key] = value;
        }
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
        
        if (!_transcodeHashToFileMappings.TryGetValue(hash, out var absoluteFilePath))
        {
            logger.LogWarning("Missing file path for {Hash}", hash);
            return;
        }
        
        var folder = new FolderCacheItem(e.AbsolutePath, loggerFactory.CreateLogger<FolderCacheItem>(), fileExplorerConverter, LogLevel.None);
        folder.ItemAdded += TranscodeFolderOnItemAdded;
        folder.ItemUpdated += TranscodeFolderOnItemUpdated;
        folder.ItemRemoved += TranscodeFolderOnItemRemoved;
        var transcode = new Transcode(loggerFactory.CreateLogger<Transcode>())
        {
            FileHash = hash,
            TranscodeFolder = folder,
            AbsoluteFilePath = absoluteFilePath
        };

        try
        {
            await transcode.InitializeAsync();
            
            SendTranscodeStatusUpdated(transcode.AbsoluteFilePath);
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

        SendTranscodeStatusUpdated(transcode.AbsoluteFilePath);
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
    
    private void SendTranscodeStatusUpdated(string absoluteFilePath)
    {
        _ = Task.Run(() =>
        {
            try
            {
                TranscodeStatusUpdated?.Invoke(this, new TranscodeStatusUpdatedEventArgs
                {
                    AbsoluteFilePath = absoluteFilePath
                });
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to send transcode status updated event");
            }
        });
    }
}
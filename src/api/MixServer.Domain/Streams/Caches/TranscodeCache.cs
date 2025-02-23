using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Caching;
using MixServer.Domain.Settings;
using MixServer.Domain.Streams.Entities;
using MixServer.Domain.Streams.Enums;
using MixServer.Domain.Streams.Events;
using MixServer.Domain.Streams.Models;
using MixServer.Domain.Streams.Repositories;


namespace MixServer.Domain.Streams.Caches;

public interface ITranscodeCache : IDisposable
{
    event EventHandler<TranscodeStatusUpdatedEventArgs>? TranscodeStatusUpdated;

    Task InitializeAsync();
    TranscodeState GetTranscodeStatus(Guid transcodeId);
    TranscodeState GetTranscodeStatus(string absoluteFilePath);
    void CalculateHasCompletePlaylist(Guid transcodeId);
    HlsPlaylistStreamFile GetPlaylistOrThrowAsync(Guid transcodeId);
    HlsSegmentStreamFile GetSegmentOrThrow(string segment);
}

public class TranscodeCache(
    IOptions<CacheFolderSettings> cacheFolderSettings,
    IFileExplorerConverter fileExplorerConverter,
    ILogger<TranscodeCache> logger,
    ILoggerFactory loggerFactory,
    IServiceProvider serviceProvider) : ITranscodeCache
{
    private FolderCacheItem? _cacheFolder;
    private readonly ConcurrentDictionary<Guid, TranscodeCacheItem> _transcodeFolders = new();

    public event EventHandler<TranscodeStatusUpdatedEventArgs>? TranscodeStatusUpdated;

    [MemberNotNullWhen(true, nameof(_cacheFolder))]
    public Task InitializeAsync()
    {
        var transcodeFolder = cacheFolderSettings.Value.TranscodesFolder;
        if (!Directory.Exists(transcodeFolder))
        {
            Directory.CreateDirectory(transcodeFolder);
        }

        _cacheFolder = new FolderCacheItem(transcodeFolder, loggerFactory.CreateLogger<FolderCacheItem>(),
            fileExplorerConverter);

        foreach (var child in _cacheFolder.Folder.Children)
        {
            CacheFolderOnItemAdded(this, child);
        }

        _cacheFolder.ItemAdded += CacheFolderOnItemAdded;
        _cacheFolder.ItemUpdated += CacheFolderOnItemUpdated;
        _cacheFolder.ItemRemoved += CacheFolderOnItemRemoved;

        return Task.CompletedTask;
    }

    public TranscodeState GetTranscodeStatus(Guid transcodeId)
    {
        if (!_transcodeFolders.TryGetValue(transcodeId, out var transcode))
        {
            return TranscodeState.None;
        }

        return transcode.HasCompletePlaylist
            ? TranscodeState.Completed
            : TranscodeState.InProgress;
    }

    public TranscodeState GetTranscodeStatus(string absoluteFilePath)
    {
        var transcode = _transcodeFolders.Values.SingleOrDefault(s => s.AbsoluteFilePath == absoluteFilePath);

        if (transcode is null)
        {
            return TranscodeState.None;
        }

        return transcode.HasCompletePlaylist
            ? TranscodeState.Completed
            : TranscodeState.InProgress;
    }

    public void CalculateHasCompletePlaylist(Guid transcodeId)
    {
        if (!_transcodeFolders.TryGetValue(transcodeId, out var transcode))
        {
            return;
        }

        transcode.CalculateHasCompletePlaylist();
    }

    public HlsPlaylistStreamFile GetPlaylistOrThrowAsync(Guid transcodeId)
    {
        var transcode = _transcodeFolders.TryGetValue(transcodeId, out var value)
            ? value
            : throw new NotFoundException(nameof(Transcode), transcodeId);
        
        return transcode.GetPlaylistOrThrow();
    }

    public HlsSegmentStreamFile GetSegmentOrThrow(string segment)
    {
        var split = segment.Split("_");

        if (!Guid.TryParse(split[0], out var transcodeId) ||
            !_transcodeFolders.TryGetValue(transcodeId, out var transcode))
        {
            throw new NotFoundException(nameof(Transcode), split[0]);
        }
        
        return transcode.GetSegmentOrThrow(segment);
    }

    private async void CacheFolderOnItemAdded(object? sender, IFileExplorerNode e)
    {
        if (e is not IFileExplorerFolderNode)
        {
            return;
        }

        var transcodeIdString = e.Name;

        if (string.IsNullOrWhiteSpace(transcodeIdString))
        {
            logger.LogWarning("Missing directory name for {Path}", e.AbsolutePath);
            return;
        }
        
        if (!Guid.TryParse(transcodeIdString, out var transcodeId))
        {
            logger.LogWarning("Invalid directory name for {Hash}", transcodeIdString);
            return;
        }

        if (_transcodeFolders.ContainsKey(transcodeId))
        {
            logger.LogWarning("Duplicate directory name for {Hash}", transcodeIdString);
            return;
        }

        try
        {
            using var scope = serviceProvider.CreateScope();
            var transcodeRepository = scope.ServiceProvider.GetRequiredService<ITranscodeRepository>();
            var transcodeEntity = await transcodeRepository.GetAsync(transcodeId);

            var folder = new FolderCacheItem(e.AbsolutePath, loggerFactory.CreateLogger<FolderCacheItem>(),
                fileExplorerConverter, LogLevel.None);
            folder.ItemAdded += TranscodeFolderOnItemAdded;
            folder.ItemUpdated += TranscodeFolderOnItemUpdated;
            folder.ItemRemoved += TranscodeFolderOnItemRemoved;
            var transcode = new TranscodeCacheItem(loggerFactory.CreateLogger<TranscodeCacheItem>())
            {
                TranscodeId = transcodeId,
                TranscodeFolder = folder,
                AbsoluteFilePath = transcodeEntity.AbsolutePath
            };

            await transcode.InitializeAsync();

            SendTranscodeStatusUpdated(transcode.AbsoluteFilePath);

            transcode.HasCompletePlaylistChanged += TranscodeOnHasCompletePlaylistChanged;
            
            _transcodeFolders[transcodeId] = transcode;
            logger.LogInformation("Added transcode folder {Hash}", transcodeIdString);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to initialize transcode folder {Hash}", transcodeIdString);
        }
    }

    private void CacheFolderOnItemRemoved(object? sender, string absolutePath)
    {
        var hash = Path.GetFileName(absolutePath);

        if (string.IsNullOrWhiteSpace(hash) ||
            !Guid.TryParse(hash, out var transcodeId) ||
            !_transcodeFolders.TryRemove(transcodeId, out var transcode))
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
        if (sender is not TranscodeCacheItem transcode)
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
            !fileName.EndsWith(".m3u8") ||
            !Guid.TryParse(hash, out var transcodeId))
        {
            return;
        }

        CalculateHasCompletePlaylist(transcodeId);
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
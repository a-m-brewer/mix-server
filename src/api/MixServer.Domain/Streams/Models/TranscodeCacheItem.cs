using System.Diagnostics.CodeAnalysis;
using DebounceThrottle;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Caching;

namespace MixServer.Domain.Streams.Models;

public class TranscodeCacheItem(ILogger<TranscodeCacheItem> logger)
{
    private readonly DebounceDispatcher _calculateHasCompletePlaylistDebounceThrottle = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
    private bool _hasCompletePlaylist;
    
    public event EventHandler? HasCompletePlaylistChanged;

    public required Guid TranscodeId { get; init; }
    
    public required string AbsoluteFilePath { get; init; }

    public bool HasCompletePlaylist
    {
        get => _hasCompletePlaylist;
        private set
        {
            if (_hasCompletePlaylist == value)
            {
                return;
            }

            logger.LogInformation("{TranscodeId} HasCompletePlaylist changed from {PreviousHasCompletePlaylist} to {HasCompletePlaylist}", TranscodeId, _hasCompletePlaylist, value);
            
            _hasCompletePlaylist = value;
            HasCompletePlaylistChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public required FolderCacheItem TranscodeFolder { get; init; }

    public async Task InitializeAsync()
    {
        _hasCompletePlaylist = await HasCompletePlaylistAsync();
    }
    
    public void CalculateHasCompletePlaylist()
    {
        _calculateHasCompletePlaylistDebounceThrottle.Debounce(async void () =>
        {
            try
            {
                HasCompletePlaylist = await HasCompletePlaylistAsync();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to calculate HasCompletePlaylist");
            }
        });
    }

    private async Task<bool> HasCompletePlaylistAsync()
    {
        if (!TryGetPlaylistFile(out var playlistFile))
        {
            return false;
        }

        var lines = await File.ReadAllLinesAsync(playlistFile.AbsolutePath);
        
        return lines.LastOrDefault()?.StartsWith("#EXT-X-ENDLIST") ?? false;
    }

    public HlsPlaylistStreamFile GetPlaylistOrThrow()
    {
        if (!TryGetPlaylistFile(out var playlistFile))
        {
            throw new NotFoundException(TranscodeId.ToString(), "m3u8");
        }

        return new HlsPlaylistStreamFile
        {
            FilePath = playlistFile.AbsolutePath
        };
    }
    
    private bool TryGetPlaylistFile([MaybeNullWhen(false)] out IFileExplorerNode playlistFile)
    {
        playlistFile = TranscodeFolder.Folder.Children.SingleOrDefault(s => s.Name.EndsWith(".m3u8"));
        return playlistFile is not null && playlistFile.Exists;
    }

    public HlsSegmentStreamFile GetSegmentOrThrow(string segment)
    {
        var segmentFile = TranscodeFolder.Folder.Children.SingleOrDefault(s => s.Name == segment);
        
        if (segmentFile is null || !segmentFile.Exists)
        {
            throw new NotFoundException(TranscodeId.ToString(), segment);
        }
        
        return new HlsSegmentStreamFile
        {
            FilePath = segmentFile.AbsolutePath
        };
    }
}
using DebounceThrottle;
using Microsoft.Extensions.Logging;
using MixServer.Domain.FileExplorer.Models.Caching;

namespace MixServer.Domain.Streams.Models;

public class Transcode(ILogger<Transcode> logger)
{
    private readonly DebounceDispatcher _calculateHasCompletePlaylistDebounceThrottle = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
    private bool _hasCompletePlaylist;
    
    public event EventHandler? HasCompletePlaylistChanged;

    public required string FileHash { get; init; }
    
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

            logger.LogInformation("{FileHash} HasCompletePlaylist changed from {PreviousHasCompletePlaylist} to {HasCompletePlaylist}", FileHash, _hasCompletePlaylist, value);
            
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
        var playlistFile = TranscodeFolder.Folder.Children.SingleOrDefault(s => s.Name.EndsWith(".m3u8"));
        
        if (playlistFile is null || !playlistFile.Exists)
        {
            return false;
        }

        var lines = await File.ReadAllLinesAsync(playlistFile.AbsolutePath);
        
        return lines.LastOrDefault()?.StartsWith("#EXT-X-ENDLIST") ?? false;
    }
}
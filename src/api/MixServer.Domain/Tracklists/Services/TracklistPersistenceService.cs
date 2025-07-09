using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.Persistence;
using MixServer.Domain.Tracklists.Converters;
using MixServer.Domain.Tracklists.Dtos.Import;
using MixServer.Domain.Tracklists.Entities;
using MixServer.Domain.Tracklists.Repositories;

namespace MixServer.Domain.Tracklists.Services;

public interface ITracklistPersistenceService
{
    Task AddOrUpdateTracklistAsync(FileExplorerFileNodeEntity file, ImportTracklistDto updatedTracklist, CancellationToken cancellationToken = default);
}

public class TracklistPersistenceService(ITracklistConverter tracklistConverter,
    ITracklistRepository tracklistRepository,
    IUnitOfWork unitOfWork) : ITracklistPersistenceService
{
    public async Task AddOrUpdateTracklistAsync(FileExplorerFileNodeEntity file, ImportTracklistDto updatedTracklist, CancellationToken cancellationToken = default)
    {
        // Create new tracklist
        if (file.Tracklist is null || file.Tracklist.Cues.Count == 0)
        {
            var tracklist = tracklistConverter.Convert(updatedTracklist, file);
            await tracklistRepository.AddAsync(tracklist, cancellationToken);
            file.Tracklist = tracklist;
        }
        // Update existing tracklist
        else
        {
            var existingTracklist = file.Tracklist;
            await AddOrUpdateCuesAsync(existingTracklist, updatedTracklist, cancellationToken);
        }
        
        unitOfWork.InvokeCallbackOnSaved(cb => cb.TracklistUpdated(file));
    }
    
    private async Task AddOrUpdateCuesAsync(TracklistEntity existingTracklist, ImportTracklistDto updatedTracklist, CancellationToken cancellationToken)
    {
        foreach (var cue in updatedTracklist.Cues)
        {
            var existingCue = existingTracklist.Cues.FirstOrDefault(c => c.Cue == cue.Cue);
            if (existingCue is not null)
            {
                await UpdateTracksForCueAsync(existingCue, cue, cancellationToken);
            }
            else
            {
                await AddCueAsync(existingTracklist, cue, cancellationToken);
            }
        }
        
        // Remove cues that are not in the updated tracklist
        var cuesToRemove = existingTracklist.Cues
            .Where(c => updatedTracklist.Cues.All(u => u.Cue != c.Cue))
            .ToList();
        tracklistRepository.RemoveRange(cuesToRemove);
    }

    private async Task AddCueAsync(TracklistEntity existingTracklist, ImportCueDto cue, CancellationToken cancellationToken)
    {
        var convertedCue = tracklistConverter.Convert(cue, existingTracklist);
        await tracklistRepository.AddAsync(convertedCue, cancellationToken);
    }

    private async Task UpdateTracksForCueAsync(CueEntity existingCue, ImportCueDto cue, CancellationToken cancellationToken)
    {
        foreach (var track in cue.Tracks)
        {
            var existingTrack = existingCue.Tracks.FirstOrDefault(f => f.Name == track.Name && f.Artist == track.Artist);
            if (existingTrack is not null)
            {
                // Update existing track
                await UpdatePlayersForTrackAsync(existingTrack, track, cancellationToken);
            }
            else
            {
                // Add new track
                await AddTrackToCueAsync(existingCue, track, cancellationToken);
            }
        }
        
        // Remove tracks that are not in the updated cue
        var tracksToRemove = existingCue.Tracks
            .Where(t => !cue.Tracks.Any(u => u.Name == t.Name && u.Artist == t.Artist))
            .ToList();
        tracklistRepository.RemoveRange(tracksToRemove);
    }

    private async Task AddTrackToCueAsync(CueEntity existingCue, ImportTrackDto track, CancellationToken cancellationToken)
    {
        var convertedTrack = tracklistConverter.Convert(track, existingCue);
        await tracklistRepository.AddAsync(convertedTrack, cancellationToken);

        existingCue.Tracks.Add(convertedTrack);
    }

    private async Task UpdatePlayersForTrackAsync(TrackEntity existingTrack, ImportTrackDto track, CancellationToken cancellationToken)
    {
        foreach (var player in track.Players)
        {
            foreach (var url in player.Urls)
            {
                var existingPlayer = existingTrack.Players.FirstOrDefault(f => f.Type == player.Type && f.Url == url);
                if (existingPlayer is not null)
                {
                    // There is nothing to update for existing players
                    continue;
                }

                var convertedPlayer = tracklistConverter.Convert(player, existingTrack, url);
                await tracklistRepository.AddAsync(convertedPlayer, cancellationToken);
                    
                existingTrack.Players.Add(convertedPlayer);
            }
        }
        
        // Remove players that are not in the updated track
        var playersToRemove = existingTrack.Players
            .Where(p => !track.Players.Any(u => u.Type == p.Type && u.Urls.Contains(p.Url)))
            .ToList();
        tracklistRepository.RemoveRange(playersToRemove);
    }
}
using MixServer.Domain.Tracklists.Entities;
using MixServer.Domain.Tracklists.Repositories;

namespace MixServer.Infrastructure.EF.Repositories;

public class EfTracklistRepository(MixServerDbContext context) : ITracklistRepository
{
    public async Task AddRangeAsync(List<TracklistEntity> tracklists, CancellationToken cancellationToken)
    {
        await context.TracklistPlayers
            .AddRangeAsync(
                tracklists
                    .SelectMany(s => s.Cues)
                    .SelectMany(s => s.Tracks)
                    .SelectMany(s => s.Players), cancellationToken);
        await context.Tracks
            .AddRangeAsync(
                tracklists
                    .SelectMany(s => s.Cues)
                    .SelectMany(s => s.Tracks), cancellationToken);
        await context.Cues
            .AddRangeAsync(tracklists.SelectMany(s => s.Cues), cancellationToken);
        
        await context.Tracklists.AddRangeAsync(tracklists, cancellationToken);
    }

    public Task AddAsync(TracklistEntity tracklist, CancellationToken cancellationToken) =>
        AddRangeAsync([tracklist], cancellationToken);

    public async Task AddAsync(CueEntity cue, CancellationToken cancellationToken)
    {
        await context.TracklistPlayers.AddRangeAsync(cue.Tracks.SelectMany(t => t.Players), cancellationToken);
        await context.Tracks.AddRangeAsync(cue.Tracks, cancellationToken);
        
        await context.Cues.AddAsync(cue, cancellationToken);
    }

    public void RemoveRange(List<CueEntity> cues)
    {
        context.Cues.RemoveRange(cues);
    }

    public async Task AddAsync(TrackEntity track, CancellationToken cancellationToken)
    {
        await context.TracklistPlayers.AddRangeAsync(track.Players, cancellationToken);
        await context.Tracks.AddAsync(track, cancellationToken);
    }

    public void RemoveRange(List<TrackEntity> tracks)
    {
        context.Tracks.RemoveRange(tracks);
    }

    public async Task AddAsync(TracklistPlayersEntity player, CancellationToken cancellationToken)
    {
        await context.TracklistPlayers.AddAsync(player, cancellationToken);
    }

    public void RemoveRange(List<TracklistPlayersEntity> playersToRemove)
    {
        context.TracklistPlayers.RemoveRange(playersToRemove);
    }
}
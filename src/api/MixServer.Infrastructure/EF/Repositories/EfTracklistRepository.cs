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
}
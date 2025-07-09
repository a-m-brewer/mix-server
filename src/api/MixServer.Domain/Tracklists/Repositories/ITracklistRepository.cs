using MixServer.Domain.Persistence;
using MixServer.Domain.Tracklists.Entities;

namespace MixServer.Domain.Tracklists.Repositories;

public interface ITracklistRepository : ITransientRepository
{
    Task AddRangeAsync(List<TracklistEntity> tracklists, CancellationToken cancellationToken);
    Task AddAsync(TracklistEntity tracklist, CancellationToken cancellationToken);
    Task AddAsync(CueEntity cue, CancellationToken cancellationToken);
    void RemoveRange(List<CueEntity> cues);
    Task AddAsync(TrackEntity track, CancellationToken cancellationToken);
    void RemoveRange(List<TrackEntity> tracks);
    Task AddAsync(TracklistPlayersEntity player, CancellationToken cancellationToken);
    void RemoveRange(List<TracklistPlayersEntity> playersToRemove);
}
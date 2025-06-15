using MixServer.Domain.Persistence;
using MixServer.Domain.Tracklists.Entities;

namespace MixServer.Domain.Tracklists.Repositories;

public interface ITracklistRepository : ITransientRepository
{
    Task AddRangeAsync(List<TracklistEntity> tracklists, CancellationToken cancellationToken);
}
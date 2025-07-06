using MixServer.Domain.Tracklists.Dtos.Import;

namespace MixServer.Domain.Tracklists.Services;

public interface ITracklistPersistenceService
{
    Task AddOrUpdateTracklistAsync(ImportTracklistDto tracklist, CancellationToken cancellationToken = default);
}

public class TracklistPersistenceService : ITracklistPersistenceService
{
    public Task AddOrUpdateTracklistAsync(ImportTracklistDto tracklist, CancellationToken cancellationToken = default)
    {
        // TODO: Implement the logic to add or update the tracklist in the database or storage.
        return Task.CompletedTask;
    }
}
using MixServer.Domain.FileExplorer.Entities;
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
    ITracklistRepository tracklistRepository) : ITracklistPersistenceService
{
    public async Task AddOrUpdateTracklistAsync(FileExplorerFileNodeEntity file, ImportTracklistDto updatedTracklist, CancellationToken cancellationToken = default)
    {
        // Create new tracklist
        if (file.Tracklist is null || file.Tracklist.Cues.Count == 0)
        {
            var tracklist = tracklistConverter.Convert(updatedTracklist, file);
            await tracklistRepository.AddAsync(tracklist, cancellationToken);
            file.Tracklist = tracklist;
            return;
        }

        throw new NotImplementedException();
    }
}
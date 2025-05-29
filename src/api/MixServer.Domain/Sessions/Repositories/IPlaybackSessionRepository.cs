using MixServer.Domain.Persistence;
using MixServer.Domain.Sessions.Entities;

namespace MixServer.Domain.Sessions.Repositories;

public interface IPlaybackSessionRepository : ITransientRepository
{
    Task<PlaybackSession> GetAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(PlaybackSession session, CancellationToken cancellationToken);
}
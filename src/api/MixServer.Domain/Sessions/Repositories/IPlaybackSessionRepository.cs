using MixServer.Domain.Persistence;
using MixServer.Domain.Sessions.Entities;

namespace MixServer.Domain.Sessions.Repositories;

public interface IPlaybackSessionRepository : ITransientRepository
{
    Task<PlaybackSession> GetAsync(Guid id);
    Task AddAsync(PlaybackSession session);
}
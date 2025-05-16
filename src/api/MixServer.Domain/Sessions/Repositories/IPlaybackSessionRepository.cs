using MixServer.Domain.Persistence;
using MixServer.Domain.Sessions.Entities;
using MixServer.Shared.Interfaces;

namespace MixServer.Domain.Sessions.Repositories;

public interface IPlaybackSessionRepository : ITransientRepository
{
    Task<PlaybackSession> GetAsync(Guid id);
    Task AddAsync(PlaybackSession session);
}
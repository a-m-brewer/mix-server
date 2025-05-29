using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Requests;

namespace MixServer.Domain.Sessions.Services;

public interface ISessionService
{
    Task<PlaybackSession> AddOrUpdateSessionAsync(IAddOrUpdateSessionRequest request);
    Task ClearUsersCurrentSessionAsync();
    Task<PlaybackSession> GetPlaybackSessionByIdAsync(Guid id);
    Task<PlaybackSession> GetCurrentPlaybackSessionWithFileAsync();
    Task<PlaybackSession> GetCurrentPlaybackSessionAsync();
    Task<PlaybackSession?> GetCurrentPlaybackSessionOrDefaultAsync();
    Task<List<PlaybackSession>> GetUsersPlaybackSessionHistoryAsync(int startIndex, int pageSize);
}
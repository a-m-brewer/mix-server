using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Requests;

namespace MixServer.Domain.Sessions.Services;

public interface ISessionService
{
    Task LoadPlaybackStateAsync();
    Task<PlaybackSession> AddOrUpdateSessionAsync(IAddOrUpdateSessionRequest request);
    void ClearUsersCurrentSession();
    Task<PlaybackSession> GetPlaybackSessionByIdAsync(Guid id, string username);
    Task<PlaybackSession> GetCurrentPlaybackSessionWithFileAsync();
    Task<PlaybackSession> GetCurrentPlaybackSessionAsync();
    Task<PlaybackSession?> GetCurrentPlaybackSessionOrDefaultAsync();
    Task<List<PlaybackSession>> GetUsersPlaybackSessionHistoryAsync(int startIndex, int pageSize);
}
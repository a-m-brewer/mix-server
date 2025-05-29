using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Requests;

namespace MixServer.Domain.Sessions.Services;

public interface ISessionService
{
    Task<PlaybackSession> AddOrUpdateSessionAsync(IAddOrUpdateSessionRequest request,
        CancellationToken cancellationToken);
    Task ClearUsersCurrentSessionAsync();
    Task<PlaybackSession> GetPlaybackSessionByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PlaybackSession> GetCurrentPlaybackSessionWithFileAsync(CancellationToken cancellationToken);
    Task<PlaybackSession> GetCurrentPlaybackSessionAsync(CancellationToken cancellationToken);
    Task<PlaybackSession?> GetCurrentPlaybackSessionOrDefaultAsync(CancellationToken cancellationToken);
    Task<List<PlaybackSession>> GetUsersPlaybackSessionHistoryAsync(int startIndex, int pageSize,
        CancellationToken cancellationToken);
}
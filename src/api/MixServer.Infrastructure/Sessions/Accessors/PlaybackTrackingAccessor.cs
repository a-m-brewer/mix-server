using Microsoft.Extensions.Logging;
using MixServer.Domain.Exceptions;
using MixServer.Domain.Sessions.Accessors;
using MixServer.Domain.Sessions.Models;
using MixServer.Domain.Sessions.Services;
using MixServer.Infrastructure.Sessions.Services;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Infrastructure.Sessions.Accessors;

public class PlaybackTrackingAccessor(
    ICurrentUserRepository currentUserRepository,
    ILogger<PlaybackTrackingAccessor> logger,
    PlaybackTrackingService playbackTrackingService,
    ISessionService sessionService) : IPlaybackTrackingAccessor
{
    public async Task<PlaybackState> GetPlaybackStateAsync(CancellationToken cancellationToken)
    {
        return await GetPlaybackStateOrDefaultAsync(cancellationToken) ??
               throw new NotFoundException(nameof(PlaybackState), currentUserRepository.CurrentUserId);
    }

    public Task<PlaybackState?> GetPlaybackStateOrDefaultAsync(CancellationToken cancellationToken)
    {
        return GetPlaybackStateAsync(currentUserRepository.CurrentUserId, cancellationToken);
    }
    
    private async Task<PlaybackState?> GetPlaybackStateAsync(string userId, CancellationToken cancellationToken)
    {
        if (playbackTrackingService.IsTracking(userId))
        {
            logger.LogDebug("Skipping loading playback state as it is already being tracked");
            return playbackTrackingService.GetOrThrow(userId);
        }

        var session = await sessionService.GetCurrentPlaybackSessionOrDefaultAsync(cancellationToken);

        if (session == null)
        {
            logger.LogDebug("Skipping loading playback state as there is currently no playback session to track");
            return null;
        }

        playbackTrackingService.UpdateSessionState(session);

        return playbackTrackingService.GetOrThrow(userId);
    }
}
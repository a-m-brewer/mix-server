using MixServer.Domain.Sessions.Models;

namespace MixServer.Domain.Sessions.Accessors;

public interface IPlaybackTrackingAccessor
{
    Task<PlaybackState> GetPlaybackStateAsync(CancellationToken cancellationToken);
    Task<PlaybackState?> GetPlaybackStateOrDefaultAsync(CancellationToken cancellationToken);
}
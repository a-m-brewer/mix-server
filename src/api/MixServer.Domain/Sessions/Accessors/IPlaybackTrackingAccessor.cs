using MixServer.Domain.Sessions.Models;

namespace MixServer.Domain.Sessions.Accessors;

public interface IPlaybackTrackingAccessor
{
    Task<PlaybackState> GetPlaybackStateAsync();
    Task<PlaybackState?> GetPlaybackStateOrDefaultAsync();
}
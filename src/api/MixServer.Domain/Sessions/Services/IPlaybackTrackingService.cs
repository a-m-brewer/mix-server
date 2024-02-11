using System.Diagnostics.CodeAnalysis;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Sessions.Models;

namespace MixServer.Domain.Sessions.Services;

public interface IPlaybackTrackingService
{
    PlaybackState GetOrThrow(string userId);
    bool TryGet(string userId, [MaybeNullWhen(false)] out PlaybackState state);
    bool IsTracking(string userId);
    void UpdateSessionState(IPlaybackSession session);
    void UpdateSessionStateIncludingPlaying(IPlaybackSession session);
    void ClearSession(string userId);
    void UpdatePlaybackState(
        string userId,
        Guid requestingDeviceId,
        TimeSpan currentTime);
    void UpdatePlaybackDevice(string userId, Guid deviceId);
    void SetWaitingForPause(string userId);
    void WaitForPause(string userId);
    void SetPlaying(string userId, bool playing, TimeSpan currentTime);
    void Seek(string userId, TimeSpan time);
    void HandleDeviceDisconnected(string userId, Guid deviceId);
    void Populate(IPlaybackSession session);
}
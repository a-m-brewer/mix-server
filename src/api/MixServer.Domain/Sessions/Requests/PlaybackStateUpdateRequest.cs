using MixServer.Domain.Sessions.Models;

namespace MixServer.Domain.Sessions.Requests;

public class PlaybackStateUpdateRequest : IPlaybackState
{
    public PlaybackStateUpdateRequest(string userId, Guid sessionId, Guid deviceId, bool playing, TimeSpan currentTime)
    {
        SessionId = sessionId;
        DeviceId = deviceId;
        CurrentTime = currentTime;
        UserId = userId;
        Playing = playing;
    }

    public string UserId { get; }
    public Guid? SessionId { get; }
    public Guid? DeviceId { get; }
    public bool Playing { get; }
    public TimeSpan CurrentTime { get; }
}
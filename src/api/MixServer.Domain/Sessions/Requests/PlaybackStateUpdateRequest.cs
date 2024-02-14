using MixServer.Domain.Sessions.Models;

namespace MixServer.Domain.Sessions.Requests;

public class PlaybackStateUpdateRequest(
    string userId,
    Guid sessionId,
    Guid deviceId,
    bool playing,
    TimeSpan currentTime)
    : IPlaybackState
{
    public string UserId { get; } = userId;
    public Guid? SessionId { get; } = sessionId;
    public Guid? DeviceId { get; } = deviceId;
    public bool Playing { get; } = playing;
    public TimeSpan CurrentTime { get; } = currentTime;
}
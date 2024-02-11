using MixServer.Domain.Sessions.Enums;
using MixServer.Domain.Sessions.Models;

namespace MixServer.Application.Sessions.Dtos;

public class PlaybackStateDto
{
    public PlaybackStateDto(IPlaybackState state, AudioPlayerStateUpdateType updateType)
    {
        UpdateType = updateType;
        DeviceId = state.DeviceId;
        Playing = state.Playing;
        CurrentTime = state.CurrentTime.TotalSeconds;
    }

    public Guid? DeviceId { get; set; }
    public bool Playing { get; set; }
    public double CurrentTime { get; set; }

    public AudioPlayerStateUpdateType UpdateType { get; set; }
}
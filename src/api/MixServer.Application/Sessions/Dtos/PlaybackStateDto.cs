using MixServer.Domain.Sessions.Enums;
using MixServer.Domain.Sessions.Models;

namespace MixServer.Application.Sessions.Dtos;

public class PlaybackStateDto(IPlaybackState state, AudioPlayerStateUpdateType updateType)
{
    public Guid? DeviceId { get; set; } = state.DeviceId;
    public bool Playing { get; set; } = state.Playing;
    public double CurrentTime { get; set; } = state.CurrentTime.TotalSeconds;

    public AudioPlayerStateUpdateType UpdateType { get; set; } = updateType;
}
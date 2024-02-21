using MixServer.Domain.Sessions.Enums;
using MixServer.Domain.Sessions.Models;

namespace MixServer.Application.Sessions.Dtos;

public class PlaybackGrantedDto(IPlaybackState state, AudioPlayerStateUpdateType updateType, bool useDeviceCurrentTime)
    : PlaybackStateDto(state, updateType)
{
    public bool UseDeviceCurrentTime { get; set; } = useDeviceCurrentTime;
}
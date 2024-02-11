using MixServer.Domain.Sessions.Enums;
using MixServer.Domain.Sessions.Models;

namespace MixServer.Application.Sessions.Dtos;

public class PlaybackGrantedDto : PlaybackStateDto
{
    public PlaybackGrantedDto(IPlaybackState state, AudioPlayerStateUpdateType updateType, bool useDeviceCurrentTime) : base(state, updateType)
    {
        UseDeviceCurrentTime = useDeviceCurrentTime;
    }
    
    public bool UseDeviceCurrentTime { get; set; }
}
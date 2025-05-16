using MixServer.Application.Sessions.Dtos;
using MixServer.Domain.Sessions.Enums;
using MixServer.Domain.Sessions.Models;
using MixServer.Shared.Interfaces;

namespace MixServer.Application.Sessions.Converters;

public interface IPlaybackStateConverter : 
    IConverter<IPlaybackState, AudioPlayerStateUpdateType, PlaybackStateDto>,
    IConverter<IPlaybackState, bool, PlaybackGrantedDto>
{
}

public class PlaybackStateConverter : IPlaybackStateConverter
{
    public PlaybackStateDto Convert(IPlaybackState value, AudioPlayerStateUpdateType value2)
    {
        return new PlaybackStateDto(value, value2);
    }

    public PlaybackGrantedDto Convert(IPlaybackState state, bool value2)
    {
        return new PlaybackGrantedDto(state, AudioPlayerStateUpdateType.PlaybackGranted, value2);
    }
}
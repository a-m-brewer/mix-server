using MixServer.Application.FileExplorer.Queries.GetNode;
using MixServer.Application.Sessions.Responses;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Sessions.Entities;

namespace MixServer.Application.Sessions.Converters;

public class PlaybackSessionDtoConverter(IConverter<IFileExplorerFileNode, FileNodeResponse> fileNodeConverter)
    :
        IConverter<IPlaybackSession, bool, PlaybackSessionDto>,
        IConverter<IPlaybackSession, PlaybackSessionDto>
{
    public PlaybackSessionDto Convert(IPlaybackSession value, bool value2)
    {
        return new PlaybackSessionDto
        {
            Id = value.Id,
            File = fileNodeConverter.Convert(value.File ?? throw new InvalidOperationException()),
            LastPlayed = value.LastPlayed,
            AutoPlay = value2,
            Playing = value.Playing,
            CurrentTime = value.CurrentTime.TotalSeconds,
            DeviceId = value.DeviceId
        };
    }

    public PlaybackSessionDto Convert(IPlaybackSession value)
    {
        return Convert(value, false);
    }
}
using MixServer.Application.FileExplorer.Queries.GetNode;
using MixServer.Application.Queueing.Responses;
using MixServer.Application.Sessions.Dtos;
using MixServer.Application.Sessions.Responses;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Sessions.Entities;

namespace MixServer.Application.Sessions.Converters;


public interface IPlaybackSessionDtoConverter
    : IConverter<IPlaybackSession?, QueueSnapshot, bool, CurrentSessionUpdatedDto>,
        IConverter<IPlaybackSession, bool, PlaybackSessionDto>,
        IConverter<IPlaybackSession, PlaybackSessionDto>
{
}

public class PlaybackSessionDtoConverter(
    IConverter<IFileExplorerFileNode, FileExplorerFileNodeResponse> fileNodeConverter,
    IConverter<QueueSnapshot, QueueSnapshotDto> queueSnapshotDtoConverter)
    : IPlaybackSessionDtoConverter
        
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

    public CurrentSessionUpdatedDto Convert(IPlaybackSession? session, QueueSnapshot queue, bool autoPlay)
    {
        var sessionDto = session == null ? null : Convert(session, autoPlay);
        var queueDto = queueSnapshotDtoConverter.Convert(queue);

        return new CurrentSessionUpdatedDto(sessionDto, queueDto);
    }
}
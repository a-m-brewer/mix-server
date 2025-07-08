using MixServer.Application.FileExplorer.Queries.GetNode;
using MixServer.Application.Queueing.Responses;
using MixServer.Application.Sessions.Dtos;
using MixServer.Application.Sessions.Responses;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Queueing.Entities;
using MixServer.Domain.Sessions.Entities;
using MixServer.Domain.Tracklists.Converters;

namespace MixServer.Application.Sessions.Converters;


public interface IPlaybackSessionDtoConverter
    : IConverter<IPlaybackSession?, QueueSnapshot, bool, CurrentSessionUpdatedDto>,
        IConverter<IPlaybackSession, bool, PlaybackSessionDto>,
        IConverter<IPlaybackSession, PlaybackSessionDto>
{
}

public class PlaybackSessionDtoConverter(
    IFileExplorerEntityToResponseConverter fileNodeConverter,
    IConverter<QueueSnapshot, QueueSnapshotDto> queueSnapshotDtoConverter,
    ITracklistDtoConverter tracklistDtoConverter)
    : IPlaybackSessionDtoConverter
        
{
    public PlaybackSessionDto Convert(IPlaybackSession value, bool value2)
    {
        var tracklist = tracklistDtoConverter.Convert(value.NodeEntity.Tracklist);
        
        return new PlaybackSessionDto
        {
            Id = value.Id,
            File = fileNodeConverter.Convert(value.NodeEntity),
            StreamKey = new StreamKeyDto
            {
                Key = value.StreamKey.Key,
                Expires = value.StreamKey.Expires
            },
            LastPlayed = value.LastPlayed,
            AutoPlay = value2,
            Playing = value.Playing,
            CurrentTime = value.CurrentTime.TotalSeconds,
            DeviceId = value.DeviceId,
            Tracklist = tracklist
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
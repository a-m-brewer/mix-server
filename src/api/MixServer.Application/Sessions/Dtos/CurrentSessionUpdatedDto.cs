using MixServer.Application.Queueing.Responses;
using MixServer.Application.Sessions.Responses;

namespace MixServer.Application.Sessions.Dtos;

public class CurrentSessionUpdatedDto(PlaybackSessionDto? session, QueueSnapshotDto queue)
{
    public PlaybackSessionDto? Session { get; } = session;

    public QueueSnapshotDto Queue { get; } = queue;
}
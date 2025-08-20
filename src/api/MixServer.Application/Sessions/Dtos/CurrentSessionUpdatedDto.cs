using MixServer.Application.Queueing.Responses;
using MixServer.Application.Sessions.Responses;

namespace MixServer.Application.Sessions.Dtos;

public class CurrentSessionUpdatedDto
{
    public required PlaybackSessionDto? Session { get; init; }

    public required QueuePositionDto QueuePosition { get; init; }
}
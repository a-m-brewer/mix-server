using MixServer.Application.Sessions.Responses;

namespace MixServer.SignalR.Events;

public class CurrentSessionUpdatedEventDto
{
    public PlaybackSessionDto? CurrentPlaybackSession { get; set; }
}
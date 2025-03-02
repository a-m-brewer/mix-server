using MixServer.Application.Sessions.Responses;

namespace MixServer.Application.Sessions.Commands.SyncPlaybackSession;

public class SyncPlaybackSessionResponse
{
    public bool UseClientState { get; set; }

    public PlaybackSessionDto? Session { get; set; }
}
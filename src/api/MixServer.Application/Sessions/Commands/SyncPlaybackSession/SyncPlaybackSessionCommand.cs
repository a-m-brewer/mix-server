namespace MixServer.Application.Sessions.Commands.SyncPlaybackSession;

public class SyncPlaybackSessionCommand
{
    public Guid? PlaybackSessionId { get; set; }
    
    public bool Playing { get; set; }
    
    public double CurrentTime { get; set; }
}
using MixServer.Domain.Streams.Enums;

namespace MixServer.SignalR.Events;

public class TranscodeStatusUpdatedDto
{
    public required string FileHash { get; init; }
    
    public required TranscodeState TranscodeState { get; init; }
}
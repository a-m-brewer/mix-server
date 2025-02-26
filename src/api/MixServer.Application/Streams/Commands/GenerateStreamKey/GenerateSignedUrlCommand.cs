namespace MixServer.Application.Streams.Commands.GenerateStreamKey;

public class GenerateStreamKeyCommand
{
    public required Guid PlaybackSessionId { get; init; }
}
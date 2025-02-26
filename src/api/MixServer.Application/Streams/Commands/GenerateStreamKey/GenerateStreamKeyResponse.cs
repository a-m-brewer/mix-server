namespace MixServer.Application.Streams.Commands.GenerateStreamKey;

public class GenerateStreamKeyResponse
{
    public required string Key { get; init; }
    
    public required double Expires { get; init; }
}
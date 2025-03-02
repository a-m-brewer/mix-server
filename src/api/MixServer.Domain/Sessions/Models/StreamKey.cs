namespace MixServer.Domain.Sessions.Models;

public class StreamKey
{
    public required string Key { get; init; }
    
    public required double Expires { get; init; }
}
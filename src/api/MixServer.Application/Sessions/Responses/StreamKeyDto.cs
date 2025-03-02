namespace MixServer.Application.Sessions.Responses;

public class StreamKeyDto
{
    public required string Key { get; init; }
    
    public required double Expires { get; init; }
}
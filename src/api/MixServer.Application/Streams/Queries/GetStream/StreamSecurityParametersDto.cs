namespace MixServer.Application.Streams.Queries.GetStream;

public class StreamSecurityParametersDto
{
    public required string Key { get; init; }
    
    public required double Expires { get; init; }
    
    public required Guid DeviceId { get; init; }
}
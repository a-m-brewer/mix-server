namespace MixServer.Application.Streams.Queries.GetStream;

public class GetStreamQuery
{
    public required string Id { get; init; }
    
    public required StreamSecurityParametersDto SecurityParameters { get; init; }
}
namespace MixServer.Application.Streams.Queries.GetStream;

public class GetStreamQuery
{
    public required string Id { get; init; }

    public required string AccessToken { get; init; }
}
namespace MixServer.Application.Streams.Queries.GetStream;

public class GetStreamQuery
{
    public required Guid PlaybackSessionId { get; init; }

    public required string AccessToken { get; init; }
    
    public required bool Transcode { get; set; }
}
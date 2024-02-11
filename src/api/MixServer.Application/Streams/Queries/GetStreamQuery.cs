namespace MixServer.Application.Streams.Queries;

public class GetStreamQuery
{
    public Guid PlaybackSessionId { get; set; }

    public string AccessToken { get; set; } = string.Empty;
}
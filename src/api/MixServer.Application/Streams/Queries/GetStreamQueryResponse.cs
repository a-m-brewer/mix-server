namespace MixServer.Application.Streams.Queries;

public class GetStreamQueryResponse
{
    public string AbsoluteFilePath { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;
}
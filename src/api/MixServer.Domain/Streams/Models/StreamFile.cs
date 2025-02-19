namespace MixServer.Domain.Streams.Models;

public abstract class StreamFile
{
    public abstract string ContentType { get; }
    
    public required string FilePath { get; init; }
}

public class DirectStreamFile(string contentType) : StreamFile
{
    public override string ContentType { get; } = contentType;
}

public class HlsPlaylistStreamFile : StreamFile
{
    public override string ContentType => "application/vnd.apple.mpegurl";
}

public class HlsSegmentStreamFile : StreamFile
{
    public override string ContentType => "video/mp2t";
}
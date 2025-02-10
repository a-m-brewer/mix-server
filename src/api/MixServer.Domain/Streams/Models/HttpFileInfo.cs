namespace MixServer.Domain.Streams.Models;

public class PlaylistFileInfo : HttpFileInfo
{
    public override string MimeType => "application/vnd.apple.mpegurl";
}

public class SegmentFileInfo : HttpFileInfo
{
    public override string MimeType => "video/mp2t";
}

public class DirectFileInfo(string mimeType) : HttpFileInfo
{
    public override string MimeType { get; } = mimeType;
}

public abstract class HttpFileInfo
{
    public required string Path { get; init; }

    public abstract string MimeType { get; }
}
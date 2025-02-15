using MixServer.Domain.Tracklists.Dtos.Import;

namespace MixServer.Domain.FileExplorer.Models.Metadata;

public class MediaMetadata(string mimeType, TimeSpan duration, int bitrate, string fileHash, ImportTracklistDto tracklist) : FileMetadata(mimeType), IMediaMetadata
{
    public TimeSpan Duration { get; } = duration;
    public int Bitrate { get; } = bitrate;

    public string FileHash { get; } = fileHash;
    public ImportTracklistDto Tracklist { get; } = tracklist;
}
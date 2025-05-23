using MixServer.Domain.Tracklists.Dtos.Import;

namespace MixServer.Domain.FileExplorer.Models.Metadata;

public class MediaInfo
{
    public required NodePath Path { get; init; }
    
    public required int Bitrate { get; init; }
    
    public required TimeSpan Duration { get; init; }
    
    public required ImportTracklistDto Tracklist { get; init; }
}
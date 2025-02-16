using MixServer.Domain.Streams.Models;
using MixServer.Domain.Tracklists.Dtos.Import;

namespace MixServer.Domain.FileExplorer.Models.Metadata;

public interface IMediaMetadata : IFileMetadata
{
    TimeSpan Duration { get; }
    
    int Bitrate { get; }
    
    string FileHash { get; }
    
    ImportTracklistDto Tracklist { get; }
}
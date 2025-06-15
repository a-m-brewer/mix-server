using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models.Metadata;

namespace MixServer.Domain.FileExplorer.Entities;

public class FileMetadataEntity : IFileMetadata
{
    public required Guid Id { get; init; }
    public required string MimeType { get; set; }
    public required bool IsMedia { get; init; }
    
    public Guid NodeId { get; set; }
    public required FileExplorerFileNodeEntity Node { get; set; }
    
    public FileMetadataType Type { get; set; }
}

public class MediaMetadataEntity : FileMetadataEntity
{
    public required int Bitrate { get; set; }
    
    public required TimeSpan Duration { get; set; }
}


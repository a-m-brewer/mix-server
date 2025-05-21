namespace MixServer.FolderIndexer.Domain.Entities;

public class MediaMetadataEntity : FileMetadataEntity
{
    public required int Bitrate { get; set; }
    
    public required TimeSpan Duration { get; set; }
}
namespace MixServer.FolderIndexer.Domain.Entities;

public class FileMetadataEntity
{
    public required Guid Id { get; init; }
    public required string MimeType { get; set; }
    
    public FileInfoEntity File { get; set; } = null!;
    public Guid FileId { get; set; }
    
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public string Type { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
}
namespace MixServer.Domain.FileExplorer.Models.Metadata;

public class FileMetadata : IFileMetadata
{
    public required string MimeType { get; init; }
    public required bool IsMedia { get; init; }
}
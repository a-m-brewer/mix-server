namespace MixServer.Domain.FileExplorer.Models.Metadata;

public class FileMetadata(string mimeType) : IFileMetadata
{
    public string MimeType { get; } = mimeType;
}
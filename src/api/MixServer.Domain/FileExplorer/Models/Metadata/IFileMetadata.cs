namespace MixServer.Domain.FileExplorer.Models.Metadata;

public interface IFileMetadata
{
    string MimeType { get; }
    
    bool IsMedia { get; }
}
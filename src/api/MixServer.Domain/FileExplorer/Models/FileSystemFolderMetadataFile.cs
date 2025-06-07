namespace MixServer.Domain.FileExplorer.Models;

public class FileSystemFolderMetadataFileDto
{
    public required NodePath Path { get; set; }
    
    public Guid FolderId { get; set; }
}

internal class FileSystemFolderMetadataFileJson
{
    public Guid FolderId { get; set; }
}
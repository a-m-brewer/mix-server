namespace MixServer.FolderIndexer.Domain.Entities;

public class FileSystemRootEntity
{
    public required Guid Id { get; set; }
    
    public required string AbsolutePath { get; set; }
    
    public List<DirectoryInfoEntity> Directories { get; set; } = new();
}
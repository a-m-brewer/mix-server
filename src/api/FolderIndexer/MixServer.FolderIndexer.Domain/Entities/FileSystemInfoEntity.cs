namespace MixServer.FolderIndexer.Domain.Entities;

public abstract class FileSystemInfoEntity
{
    public required Guid Id { get; set; }
    
    public required string Name { get; set; }

    public required string AbsolutePath { get; set; }

    public required bool Exists { get; set; }

    public required DateTime CreationTimeUtc { get; set; }
    
    public Guid? ParentId { get; set; }

    public DirectoryInfoEntity? Parent { get; set; }
    public string Type { get; set; } = string.Empty;
}
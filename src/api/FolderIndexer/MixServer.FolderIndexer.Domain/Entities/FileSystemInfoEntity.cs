namespace MixServer.FolderIndexer.Domain.Entities;

public class FileSystemInfoEntity
{
    public required Guid Id { get; set; }
    
    public required string Name { get; set; }

    public required string AbsolutePath { get; set; }

    public required bool Exists { get; set; }

    public required DateTime CreationTimeUtc { get; set; }
    
    public Guid? ParentId { get; set; }

    public DirectoryInfoEntity? Parent { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public string Type { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
}
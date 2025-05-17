namespace MixServer.FolderIndexer.Domain.Entities;

public class DirectoryInfoEntity : FileSystemInfoEntity
{
    public List<FileSystemInfoEntity> Children { get; set; } = [];
}
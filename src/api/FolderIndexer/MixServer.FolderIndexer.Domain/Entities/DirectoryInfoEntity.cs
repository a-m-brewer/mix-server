namespace MixServer.FolderIndexer.Domain.Entities;

public class DirectoryInfoEntity : FileSystemInfoEntity
{
    public bool IsRoot { get; set; }
}
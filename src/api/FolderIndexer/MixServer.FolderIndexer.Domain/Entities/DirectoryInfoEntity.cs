namespace MixServer.FolderIndexer.Domain.Entities;

public class DirectoryInfoEntity : FileSystemInfoEntity
{
    public FileSystemRootEntity? FileSystemRoot { get; set; }
    
    public Guid? FileSystemRootId { get; set; }
}
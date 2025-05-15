namespace MixServer.FolderIndexer.Domain.Entities;

public class FileInfoEntity : FileSystemInfoEntity
{
    public string Extension { get; set; } = string.Empty;
}
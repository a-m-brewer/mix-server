namespace MixServer.FolderIndexer.Interface.Models;

public interface IFileSystemInfo
{
    Guid Id { get; }
    string Name { get; }
    string RelativePath { get; }
    bool Exists { get; }
    DateTime CreationTimeUtc { get; }
    
    // Computed
    string FullName { get; }
}
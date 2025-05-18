namespace MixServer.FolderIndexer.Interface.Models;

public interface IDirectoryInfo : IFileSystemInfo
{
    IReadOnlyCollection<IFileSystemInfo> ChildItems { get; }
    bool IsRoot { get; }
    
    IDirectoryInfo? ParentDirectory { get; }
    IRootDirectoryInfo? RootDirectory { get; }
}
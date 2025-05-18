namespace MixServer.FolderIndexer.Interface.Models;

public interface IFileInfo : IFileSystemInfo
{
    string Extension { get; }
    
    IDirectoryInfo ParentDirectory { get; }
    IRootDirectoryInfo RootDirectory { get; }
}
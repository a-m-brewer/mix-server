using MixServer.FolderIndexer.Interface.Models;

namespace MixServer.FolderIndexer.Domain.Entities;

public class DirectoryInfoEntity : FileSystemInfoEntity, IDirectoryInfo
{
    public List<FileSystemInfoEntity> Children { get; set; } = [];
    
    public IReadOnlyCollection<IFileSystemInfo> ChildItems => Children.Cast<IFileSystemInfo>().ToList();

    public virtual bool IsRoot => false;
    public IDirectoryInfo? ParentDirectory => Parent;
    public IRootDirectoryInfo? RootDirectory => Root;
}
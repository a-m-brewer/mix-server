namespace MixServer.Domain.FileExplorer.Models;

public interface IFileExplorerRootChildFolderNode : IFileExplorerFolderNode
{
}

public class FileExplorerRootChildFolderNode(DirectoryInfo directoryInfo)
    : FileExplorerFolderNode(new FolderInfo
    {
        Name = directoryInfo.Name,
        AbsolutePath = directoryInfo.FullName,
        ParentAbsolutePath = null,
        BelongsToRoot = true,
        BelongsToRootChild = false,
        Exists = directoryInfo.Exists
    }), IFileExplorerRootChildFolderNode
{
    
}
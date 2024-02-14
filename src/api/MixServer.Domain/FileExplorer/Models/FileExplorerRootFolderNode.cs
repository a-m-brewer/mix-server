namespace MixServer.Domain.FileExplorer.Models;

public interface IFileExplorerRootFolderNode : IFileExplorerFolderNode
{
}

public class FileExplorerRootFolderNode()
    : FileExplorerFolderNode(new FolderInfo
    {
        Name = string.Empty,
        AbsolutePath = string.Empty,
        ParentAbsolutePath = null,
        CanRead = true,
        Exists = true
    }), IFileExplorerRootFolderNode;
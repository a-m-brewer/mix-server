namespace MixServer.Domain.FileExplorer.Models;

public interface IFileExplorerRootFolderNode : IFileExplorerFolderNode
{
}

public class FileExplorerRootFolderNode()
    : FileExplorerFolderNode(string.Empty, null, true), IFileExplorerRootFolderNode;
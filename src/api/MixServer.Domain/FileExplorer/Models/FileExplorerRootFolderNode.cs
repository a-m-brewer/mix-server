namespace MixServer.Domain.FileExplorer.Models;

public interface IFileExplorerRootFolderNode : IFileExplorerFolderNode
{
}

public class FileExplorerRootFolderNode()
    : FileExplorerFolderNode(string.Empty, string.Empty, null, true, true), IFileExplorerRootFolderNode;
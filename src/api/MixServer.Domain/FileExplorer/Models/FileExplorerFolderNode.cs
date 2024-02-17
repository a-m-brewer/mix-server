using MixServer.Domain.FileExplorer.Enums;

namespace MixServer.Domain.FileExplorer.Models;

public class FileExplorerFolderNode(
    string name,
    string absolutePath,
    FileExplorerNodeType type,
    bool exists,
    DateTime creationTimeUtc,
    bool belongsToRoot,
    bool belongsToRootChild,
    IFileExplorerFolderNodeInfo? parent)
    : FileExplorerFolderNodeInfo(name, absolutePath, type, exists, creationTimeUtc, belongsToRoot, belongsToRootChild),
        IFileExplorerFolderNode
{
    public IFileExplorerFolderNodeInfo? Parent { get; } = parent;
}
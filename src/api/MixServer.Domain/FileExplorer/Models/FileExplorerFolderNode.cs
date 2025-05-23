using MixServer.Domain.FileExplorer.Enums;

namespace MixServer.Domain.FileExplorer.Models;

public class FileExplorerFolderNode(
    NodePath path,
    FileExplorerNodeType type,
    bool exists,
    DateTime creationTimeUtc,
    bool belongsToRoot,
    bool belongsToRootChild,
    IFileExplorerFolderNode? parent)
    : IFileExplorerFolderNode
{
    public NodePath Path { get; } = path;
    public FileExplorerNodeType Type { get; } = type;
    public bool Exists { get; } = exists;
    public DateTime CreationTimeUtc { get; } = creationTimeUtc;
    public bool BelongsToRoot { get; } = belongsToRoot;
    public bool BelongsToRootChild { get; } = belongsToRootChild;
    public IFileExplorerFolderNode? Parent { get; } = parent;
}
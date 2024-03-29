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
    IFileExplorerFolderNode? parent)
    : IFileExplorerFolderNode
{
    public string Name { get; } = name;
    public string AbsolutePath { get; } = absolutePath;
    public FileExplorerNodeType Type { get; } = type;
    public bool Exists { get; } = exists;
    public DateTime CreationTimeUtc { get; } = creationTimeUtc;
    public bool BelongsToRoot { get; } = belongsToRoot;
    public bool BelongsToRootChild { get; } = belongsToRootChild;
    public IFileExplorerFolderNode? Parent { get; } = parent;
}
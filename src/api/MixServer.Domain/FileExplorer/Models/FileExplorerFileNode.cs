using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models.Metadata;

namespace MixServer.Domain.FileExplorer.Models;

public class FileExplorerFileNode(
    NodePath path,
    FileExplorerNodeType type,
    bool exists,
    DateTime creationTimeUtc,
    IFileMetadata fileMetadata,
    IFileExplorerFolderNode parent)
    : IFileExplorerFileNode
{
    public NodePath Path { get; } = path;
    public FileExplorerNodeType Type { get; } = type;
    public bool Exists { get; } = exists;
    public DateTime CreationTimeUtc { get; } = creationTimeUtc;

    public IFileMetadata Metadata { get; } = fileMetadata;

    public bool PlaybackSupported => Parent.BelongsToRootChild &&
                                     Exists && 
                                     Metadata.IsMedia;
    public IFileExplorerFolderNode Parent { get; } = parent;
}
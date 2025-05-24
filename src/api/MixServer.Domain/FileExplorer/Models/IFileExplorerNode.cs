using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models.Metadata;

namespace MixServer.Domain.FileExplorer.Models;

public interface IFileExplorerNode
{
    NodePath Path { get; }

    FileExplorerNodeType Type { get; }

    bool Exists { get; }

    DateTime CreationTimeUtc { get; }
}

public interface IFileExplorerFileNode : IFileExplorerNode
{
    IFileMetadata Metadata { get; }

    bool PlaybackSupported { get; }
    
    IFileExplorerFolderNode Parent { get; }
}

public interface IFileExplorerFolderNode : IFileExplorerNode
{
    bool BelongsToRoot { get; }

    bool BelongsToRootChild { get; }
    
    IFileExplorerFolderNode? Parent { get; }
}

public interface IFileExplorerFolder
{
    IFileExplorerFolderNode Node { get; }

    IReadOnlyCollection<IFileExplorerNode> Children { get; }

    IFolderSort Sort { get; set; }

    IReadOnlyCollection<T> GenerateSortedChildren<T>() where T : IFileExplorerNode;

    IReadOnlyCollection<T> GenerateSortedChildren<T>(IFolderSort sort) where T : IFileExplorerNode;
}

public interface IRootFileExplorerFolder : IFileExplorerFolder
{
    NodePath GetNodePath(string absolutePath);
    IFileExplorerFolderNode GetRootChildOrThrow(NodePath nodePath);
    void RefreshChildren();
    bool DescendantOfRoot(NodePath nodePath);
}
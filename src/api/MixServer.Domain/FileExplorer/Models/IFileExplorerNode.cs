using MixServer.Domain.FileExplorer.Enums;

namespace MixServer.Domain.FileExplorer.Models;

public interface IFileExplorerNode
{
    string Name { get; }

    string AbsolutePath { get; }

    FileExplorerNodeType Type { get; }

    bool Exists { get; }

    DateTime CreationTimeUtc { get; }
}

public interface IFileExplorerFileNode : IFileExplorerNode
{
    string MimeType { get; }

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
}

public interface IRootFileExplorerFolder : IFileExplorerFolder
{
    bool BelongsToRoot(string directoryInfoFullName);

    bool BelongsToRootChild(string absolutePath);
}
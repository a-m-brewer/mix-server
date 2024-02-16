using MixServer.Domain.FileExplorer.Enums;

namespace MixServer.Domain.FileExplorer.Models;

public interface IFolderInfo : IFileExplorerNode
{
    string? ParentAbsolutePath { get; }

    bool BelongsToRoot { get; }

    bool BelongsToRootChild { get; }
}

public class FolderInfo : IFolderInfo
{
    public string Name { get; init; } = string.Empty;

    public string NameIdentifier { get; init; } = string.Empty;

    public FileExplorerNodeType Type => FileExplorerNodeType.Folder;

    public string? AbsolutePath { get; init; }

    public string? ParentAbsolutePath { get; init; }

    public bool Exists { get; init; }
    
    public bool BelongsToRoot { get; init; }

    public bool BelongsToRootChild { get; init; }

    public DateTime CreationTimeUtc { get; init; }
}
using MixServer.Domain.FileExplorer.Enums;

namespace MixServer.Domain.FileExplorer.Models;

public interface IFileExplorerFolderNode : IFileExplorerNode
{
    string? ParentAbsolutePath { get; }
    bool CanRead { get; }
    List<IFileExplorerNode> Children { get; set; }
    List<IFileExplorerFolderNode> ChildFolders { get; }
    List<IFileExplorerFileNode> ChildFiles { get; }
    List<IFileExplorerFileNode> ChildPlayableFiles { get; }
    IFolderSort Sort { get; set; }
}

public class FileExplorerFolderNode(string? name, string? absolutePath, string? parentDirectory, bool exists, bool canRead)
    : FileExplorerNode(FileExplorerNodeType.Folder), IFileExplorerFolderNode
{
    public override bool Exists => exists;
    public override string? AbsolutePath { get; } = absolutePath;

    public override string Name { get; } = name ?? string.Empty;

    public string? ParentAbsolutePath { get; } = parentDirectory;

    public bool CanRead { get; } = canRead;
    public List<IFileExplorerNode> Children { get; set; } = [];
    public List<IFileExplorerFolderNode> ChildFolders => Children.OfType<IFileExplorerFolderNode>().ToList();
    public List<IFileExplorerFileNode> ChildFiles => Children.OfType<IFileExplorerFileNode>().ToList();
    public List<IFileExplorerFileNode> ChildPlayableFiles => ChildFiles
        .Where(w => w.PlaybackSupported)
        .ToList();

    public IFolderSort Sort { get; set; } = FolderSortModel.Default;
}
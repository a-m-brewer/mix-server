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

public class FileExplorerFolderNode : FileExplorerNode, IFileExplorerFolderNode
{
    public FileExplorerFolderNode(string? absolutePath, string? parentDirectory, bool canRead) : base(FileExplorerNodeType.Folder)
    {
        AbsolutePath = absolutePath;
        ParentAbsolutePath = parentDirectory;
        CanRead = canRead;
    }

    public override bool Exists => Directory.Exists(AbsolutePath);
    public override string? AbsolutePath { get; }
    
    public override string Name => Directory.Exists(AbsolutePath)
        ? new DirectoryInfo(AbsolutePath).Name
        : string.Empty;

    public string? ParentAbsolutePath { get; }

    public bool CanRead { get; }
    public List<IFileExplorerNode> Children { get; set; } = [];
    public List<IFileExplorerFolderNode> ChildFolders => Children.OfType<IFileExplorerFolderNode>().ToList();
    public List<IFileExplorerFileNode> ChildFiles => Children.OfType<IFileExplorerFileNode>().ToList();
    public List<IFileExplorerFileNode> ChildPlayableFiles => ChildFiles
        .Where(w => w.PlaybackSupported)
        .ToList();

    public IFolderSort Sort { get; set; } = FolderSortModel.Default;
}
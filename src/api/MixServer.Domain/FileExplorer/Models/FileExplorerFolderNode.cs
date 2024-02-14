using MixServer.Domain.FileExplorer.Enums;

namespace MixServer.Domain.FileExplorer.Models;

public interface IFileExplorerFolderNode : IFileExplorerNode
{
    string? ParentAbsolutePath { get; }
    public IFolderInfo Info { get; }
    List<IFileExplorerNode> Children { get; }
    IReadOnlyCollection<IFileExplorerNode> SortedChildren { get; }
    IFolderSort Sort { get; set; }
    
    public List<T> GenerateSortedChildren<T>()
        where T : IFileExplorerNode;
}

public class FileExplorerFolderNode(IFolderInfo info)
    : FileExplorerNode(FileExplorerNodeType.Folder), IFileExplorerFolderNode
{
    public override bool Exists => Info.Exists;

    public override string? AbsolutePath => Info.AbsolutePath;
    public override DateTime CreationTimeUtc => Info.CreationTimeUtc;

    public IFolderInfo Info => info;
    
    public override string Name => Info.Name;

    public string? ParentAbsolutePath => Info.ParentAbsolutePath;

    public List<IFileExplorerNode> Children { get; set; } = [];
    public IReadOnlyCollection<IFileExplorerNode> SortedChildren =>
        GenerateSortedChildren<IFileExplorerNode>().AsReadOnly();

    public IFolderSort Sort { get; set; } = FolderSortModel.Default;

    public List<T> GenerateSortedChildren<T>()
        where T : IFileExplorerNode
    {
        return OrderNodes<T>().ToList();
    }
    
    private IEnumerable<T> OrderNodes<T>()
        where T : IFileExplorerNode
    {
        Func<IFileExplorerNode, object> func = Sort.SortMode switch
        {
            FolderSortMode.Name => i => i.Name,
            FolderSortMode.Created => i => i.CreationTimeUtc,
            _ => i => i.Name
        };

        var values = Sort.SortMode switch
        {
            FolderSortMode.Name => Children.OrderByDescending(o => o.Type),
            _ => Children.OrderBy(o => o.Type)
        };

        values = Sort.Descending
            ? values.ThenByDescending(func)
            : values.ThenBy(func);
        
        return values.OfType<T>();
    }
    
}
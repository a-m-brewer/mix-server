using MixServer.Domain.FileExplorer.Enums;

namespace MixServer.Domain.FileExplorer.Models;

public class FileExplorerFolder(IFileExplorerFolderNode node) : IFileExplorerFolder
{
    private readonly List<IFileExplorerNode> _children = new();

    public IFileExplorerFolderNode Node { get; } = node;

    public IReadOnlyCollection<IFileExplorerNode> Children => _children;

    public IFolderSort Sort { get; set; } = FolderSortModel.Default;

    public IReadOnlyCollection<T> GenerateSortedChildren<T>() where T : IFileExplorerNode
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
        
        return values.OfType<T>().ToList();
    }

    public void AddChild(IFileExplorerNode node)
    {
        _children.Add(node);
    }

    public void RemoveChild(IFileExplorerNode node)
    {
        _children.Remove(node);
    }
}
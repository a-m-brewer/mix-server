using System.Collections.Concurrent;
using System.Diagnostics;
using MixServer.Domain.FileExplorer.Enums;

namespace MixServer.Domain.FileExplorer.Models;

public class FileExplorerFolder(IFileExplorerFolderNode node, ConcurrentDictionary<string, IFileExplorerNode>? childNodes = null) : IFileExplorerFolder
{
    protected readonly ConcurrentDictionary<string, IFileExplorerNode> ChildNodes = childNodes ?? new ConcurrentDictionary<string, IFileExplorerNode>();

    public IFileExplorerFolderNode Node { get; } = node;

    public IReadOnlyCollection<IFileExplorerNode> Children => GenerateSortedChildren<IFileExplorerNode>();

    public IFolderSort Sort { get; set; } = FolderSortModel.Default;

    public IReadOnlyCollection<T> GenerateSortedChildren<T>() where T : IFileExplorerNode => GenerateSortedChildren<T>(Sort);
    
    public IReadOnlyCollection<T> GenerateSortedChildren<T>(IFolderSort sort) where T : IFileExplorerNode
    {
        Func<IFileExplorerNode, object> func = sort.SortMode switch
        {
            FolderSortMode.Name => i => i.Name,
            FolderSortMode.Created => i => i.CreationTimeUtc,
            _ => i => i.Name
        };

        var values = sort.SortMode switch
        {
            FolderSortMode.Name => ChildNodes.Values.OrderByDescending(o => o.Type),
            _ => ChildNodes.Values.OrderBy(o => o.Type)
        };

        values = sort.Descending
            ? values.ThenByDescending(func)
            : values.ThenBy(func);
        
        return values.OfType<T>().ToList();
    }

    public void AddChild(IFileExplorerNode node)
    {
        if (!ChildNodes.TryAdd(node.Name, node))
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }
    }

    public void RemoveChild(string name)
    {
        ChildNodes.TryRemove(name, out _);
    }
}
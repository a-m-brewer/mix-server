using MixServer.Application.FileExplorer.Dtos;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Queries.GetNode;

public interface INodeResponseConverter
    : IConverter<IFileExplorerNode, NodeResponse>,
        IConverter<IFileExplorerFileNode, FileNodeResponse>,
        IConverter<IFileExplorerFolderNode, FolderNodeResponse>,
        IConverter<IFileExplorerRootChildFolderNode, RootFolderChildNodeResponse>,
        IConverter<IFileExplorerRootFolderNode, RootFolderNodeResponse>,
        IConverter<IFolderInfo, FolderInfoResponse>
{
}

public class NodeResponseConverter : INodeResponseConverter
{
    public FileNodeResponse Convert(IFileExplorerFileNode value)
    {
        return new FileNodeResponse(
            value.Name,
            value.NameIdentifier,
            value.AbsolutePath,
            value.Type,
            value.Exists,
            value.MimeType,
            value.PlaybackSupported,
            Convert(value.Parent));
    }

    public FolderNodeResponse Convert(IFileExplorerFolderNode value)
    {
        return value switch
        {
            IFileExplorerRootFolderNode fileExplorerRootFolderNode => Convert(fileExplorerRootFolderNode),
            _ => new FolderNodeResponse(
                value.NameIdentifier,
                Convert(value.Info),
                new FolderSortDto(value.Sort))
            {
                Children = value
                    .SortedChildren
                    .Select(Convert)
                    .ToList()
            }
        };
    }

    public RootFolderNodeResponse Convert(IFileExplorerRootFolderNode value)
    {
        return new RootFolderNodeResponse(value.NameIdentifier, Convert(value.Info))
        {
            Children = value.SortedChildren
                .Select(Convert)
                .ToList()
        };
    }

    public RootFolderChildNodeResponse Convert(IFileExplorerRootChildFolderNode value)
    {
        return new RootFolderChildNodeResponse(value.NameIdentifier, Convert(value.Info), new FolderSortDto(value.Sort))
        {
            Children = value.SortedChildren
                .Select(Convert)
                .ToList()
        };
    }
    
    public NodeResponse Convert(IFileExplorerNode value)
    {
        return value switch
        {
            IFileExplorerFileNode fileExplorerFileNode => Convert(fileExplorerFileNode),
            IFileExplorerRootFolderNode fileExplorerRootFolderNode => Convert(fileExplorerRootFolderNode),
            IFileExplorerRootChildFolderNode fileExplorerRootChildFolderNode => Convert(fileExplorerRootChildFolderNode),
            IFileExplorerFolderNode fileExplorerFolderNode => Convert(fileExplorerFolderNode),
            _ => throw new ArgumentOutOfRangeException(nameof(value))
        };
    }

    public FolderInfoResponse Convert(IFolderInfo value)
    {
        return new FolderInfoResponse(value.Name, value.AbsolutePath, value.ParentAbsolutePath, value.Exists, value.BelongsToRoot, value.BelongsToRootChild);
    }
}
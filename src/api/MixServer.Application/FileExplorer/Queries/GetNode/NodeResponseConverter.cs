using MixServer.Application.FileExplorer.Dtos;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Queries.GetNode;

public class NodeResponseConverter :
    IConverter<IFileExplorerNode, NodeResponse>,
    IConverter<IFileExplorerFileNode, FileNodeResponse>,
    IConverter<IFileExplorerFolderNode, FolderNodeResponse>,
    IConverter<IFileExplorerRootFolderNode, RootFolderNodeResponse>
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
                value.Name,
                value.NameIdentifier,
                value.AbsolutePath,
                value.Type,
                value.Exists,
                value.ParentAbsolutePath,
                new FolderSortDto(value.Sort))
            {
                Children = value.Children.Select(Convert).ToList()
            }
        };
    }

    public RootFolderNodeResponse Convert(IFileExplorerRootFolderNode value)
    {
        return new RootFolderNodeResponse(value.Name, value.NameIdentifier, value.AbsolutePath, value.Type, value.Exists)
        {
            Children = value.Children.Select(Convert).ToList()
        };
    }
    
    public NodeResponse Convert(IFileExplorerNode value)
    {
        return value switch
        {
            IFileExplorerFileNode fileExplorerFileNode => Convert(fileExplorerFileNode),
            IFileExplorerRootFolderNode fileExplorerRootFolderNode => Convert(fileExplorerRootFolderNode),
            IFileExplorerFolderNode fileExplorerFolderNode => Convert(fileExplorerFolderNode),
            _ => throw new ArgumentOutOfRangeException(nameof(value))
        };
    }
}
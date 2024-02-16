using MixServer.Application.FileExplorer.Dtos;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Queries.GetNode;

public interface IFileExplorerResponseConverter
    : IConverter<IFileExplorerNode, FileExplorerNodeResponse>,
        IConverter<IFileExplorerFileNode, FileExplorerFileNodeResponse>,
        IConverter<IFileExplorerFolderNode, FileExplorerFolderNodeResponse>,
        IConverter<IFileExplorerFolder, FileExplorerFolderResponse>,
        IConverter<IRootFileExplorerFolder, RootFileExplorerFolderResponse>
{
}

public class FileExplorerResponseConverter : IFileExplorerResponseConverter
{
    public FileExplorerNodeResponse Convert(IFileExplorerNode value)
    {
        return value switch
        {
            IFileExplorerFileNode fileNode => Convert(fileNode),
            IFileExplorerFolderNode folderNode => Convert(folderNode),
            _ => throw new ArgumentOutOfRangeException(nameof(value))
        };
    }

    public FileExplorerFileNodeResponse Convert(IFileExplorerFileNode value)
    {
        return new FileExplorerFileNodeResponse
        {
            AbsolutePath = value.AbsolutePath,
            Exists = value.Exists,
            Name = value.Name,
            Type = value.Type,
            MimeType = value.MimeType,
            PlaybackSupported = value.PlaybackSupported,
            Parent = Convert(value.Parent)
        };
    }

    public FileExplorerFolderNodeResponse Convert(IFileExplorerFolderNode value)
    {
        return new FileExplorerFolderNodeResponse
        {
            AbsolutePath = value.AbsolutePath,
            Exists = value.Exists,
            Name = value.Name,
            Type = value.Type,
            BelongsToRoot = value.BelongsToRoot,
            BelongsToRootChild = value.BelongsToRootChild
        };
    }

    public FileExplorerFolderResponse Convert(IFileExplorerFolder value)
    {
        return value switch
        {
            IRootFileExplorerFolder rootFolder => Convert(rootFolder),
            _ => new FileExplorerFolderResponse
            {
                Node = Convert(value.Node),
                Children = value.Children.Select(Convert).ToList(),
                Sort = new FolderSortDto(value.Sort)
            }
        };
    }

    public RootFileExplorerFolderResponse Convert(IRootFileExplorerFolder value)
    {
        return new RootFileExplorerFolderResponse
        {
            Node = Convert(value.Node),
            Children = value.Children.Select(Convert).ToList(),
            Sort = new FolderSortDto(value.Sort)
        };
    }
}
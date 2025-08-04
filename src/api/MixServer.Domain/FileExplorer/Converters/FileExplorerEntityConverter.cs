using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;

namespace MixServer.Domain.FileExplorer.Converters;

public interface IFileExplorerEntityConverter
    : IConverter<IFileExplorerFolderEntity, IFileExplorerFolder>,
        IConverter<FileExplorerFileNodeEntity, IFileExplorerFileNode>
{
    IFileExplorerFolderPage ConvertPage(IFileExplorerFolderEntity value, Page page, IFolderSort sort);
}

public class FileExplorerEntityConverter(IRootFileExplorerFolder rootFolder) : IFileExplorerEntityConverter
{
    public IFileExplorerFolder Convert(IFileExplorerFolderEntity value)
    {
        var node = ConvertEntityToNode(value);
        var folder = new FileExplorerFolder(node);

        foreach (var childFolder in value.Children.OfType<FileExplorerFolderNodeEntity>())
        {
            folder.AddChild(ConvertFolderEntityToNode(childFolder, node));
        }

        foreach (var childFile in value.Children.OfType<FileExplorerFileNodeEntity>())
        {
            folder.AddChild(ConvertFileEntityToNode(childFile, node));
        }

        return folder;
    }
    
    public IFileExplorerFileNode Convert(FileExplorerFileNodeEntity value)
    {
        var parent = value.Parent is null
            ? ConvertRootChildEntityToNode(value.RootChild)
            : ConvertFolderEntityToNode(value.Parent, false);
        
        return ConvertFileEntityToNode(value, parent);
    }

    public IFileExplorerFolderPage ConvertPage(IFileExplorerFolderEntity value, Page page, IFolderSort sort)
    {
        var node = ConvertEntityToNode(value);

        var convertedChildren = new List<IFileExplorerNode>();
        foreach (var child in value.Children)
        {
            switch (child)
            {
                case FileExplorerFolderNodeEntity childFolder:
                    convertedChildren.Add(ConvertFolderEntityToNode(childFolder, node));
                    break;
                case FileExplorerFileNodeEntity childFile:
                    convertedChildren.Add(ConvertFileEntityToNode(childFile, node));
                    break;
            }
        }

        return new FileExplorerFolderPage
        {
            Node = node,
            Page = new FileExplorerFolderChildPage
            {
                PageIndex = page.PageIndex,
                Children = convertedChildren
            },
            Sort = sort
        };
    }

    private IFileExplorerFolderNode ConvertEntityToNode(IFileExplorerFolderEntity entity)
    {
        if (entity is FileExplorerRootChildNodeEntity rootChild)
        {
            return ConvertRootChildEntityToNode(rootChild);
        }

        if (entity is FileExplorerFolderNodeEntity folderEntity)
        {
            return ConvertFolderEntityToNode(folderEntity, true);
        }
        
        throw new NotSupportedException($"Unsupported entity type: {entity.GetType().Name}");
    }

    private IFileExplorerFolderNode ConvertRootChildEntityToNode(FileExplorerRootChildNodeEntity rootChild)
    {
        return new FileExplorerFolderNode(rootChild.Path,
            FileExplorerNodeType.Folder,
            rootChild.Exists,
            rootChild.CreationTimeUtc,
            true,
            true,
            rootFolder.Node);
    }
    
    private IFileExplorerFolderNode ConvertFolderEntityToNode(FileExplorerFolderNodeEntity folderEntity, bool includeParent)
    {
        IFileExplorerFolderNode? parent = null;
        if (includeParent)
        {
            parent = folderEntity.Parent is null
                ? ConvertRootChildEntityToNode(folderEntity.RootChild)
                : ConvertFolderEntityToNode(folderEntity.Parent, false);
        }
        
        return ConvertFolderEntityToNode(folderEntity, parent);
    }
    
    private IFileExplorerFolderNode ConvertFolderEntityToNode(FileExplorerFolderNodeEntity folderEntity, IFileExplorerFolderNode? parent)
    {
        return new FileExplorerFolderNode(folderEntity.Path,
            FileExplorerNodeType.Folder,
            folderEntity.Exists,
            folderEntity.CreationTimeUtc,
            true,
            true,
            parent);
    }
    
    private IFileExplorerFileNode ConvertFileEntityToNode(FileExplorerFileNodeEntity fileEntity, IFileExplorerFolderNode parent)
    {
        return new FileExplorerFileNode(fileEntity.Path,
            FileExplorerNodeType.File,
            fileEntity.Exists,
            fileEntity.CreationTimeUtc,
            fileEntity.MetadataEntity,
            parent);
    }
}
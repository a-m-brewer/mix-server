using MixServer.Domain.Extensions;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;

namespace MixServer.Domain.FileExplorer.Converters;

public interface IFileExplorerConverter
    : IConverter<NodePath, IFileExplorerFileNode>,
    IConverter<DirectoryInfo, IFileExplorerFolderNode>,
    IConverter<FileInfo, IFileExplorerFolderNode, IFileExplorerFileNode>
{
}

public class FileExplorerConverter(
    IFileMetadataConverter metadataConverter,
    IRootFileExplorerFolder rootFolder) : IFileExplorerConverter
{
    public IFileExplorerFileNode Convert(NodePath fileNodePath)
    {
        var parentAbsolutePath = fileNodePath.Parent.AbsolutePath;
        var parent = Convert(new DirectoryInfo(parentAbsolutePath), false);
        
        return Convert(new FileInfo(fileNodePath.AbsolutePath), parent);
    }

    public IFileExplorerFolderNode Convert(DirectoryInfo value)
    {
        return Convert(value, true);
    }
    
    private FileExplorerFolderNode Convert(DirectoryInfo directoryInfo, bool includeParent)
    {
        var currentFolderBelongsToRoot = rootFolder.BelongsToRoot(directoryInfo.FullName);

        IFileExplorerFolderNode? parent = null;
        if (includeParent && directoryInfo.Parent is not null)
        {
            parent = currentFolderBelongsToRoot
                ? rootFolder.Node
                : Convert(directoryInfo.Parent, false);
        }
        
        var path = rootFolder.GetNodePath(directoryInfo.FullName);
        
        return new FileExplorerFolderNode(
            path,
            FileExplorerNodeType.Folder,
            directoryInfo.Exists,
            directoryInfo.CreationTimeUtc,
            currentFolderBelongsToRoot,
            rootFolder.BelongsToRootChild(directoryInfo.FullName),
            parent);
    }

    public IFileExplorerFileNode Convert(FileInfo fileInfo, IFileExplorerFolderNode parent)
    {
        var path = rootFolder.GetNodePath(fileInfo.FullName);
        
        return new FileExplorerFileNode(
            path,
            FileExplorerNodeType.File,
            fileInfo.Exists,
            fileInfo.CreationTimeUtc,
            metadataConverter.Convert(fileInfo),
            parent);
    }
}
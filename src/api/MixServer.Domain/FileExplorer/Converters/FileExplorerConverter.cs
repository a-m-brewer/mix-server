using MixServer.Domain.Extensions;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;

namespace MixServer.Domain.FileExplorer.Converters;

public interface IFileExplorerConverter
    : IConverter<string, IFileExplorerFileNode>,
    IConverter<DirectoryInfo, IFileExplorerFolderNode>,
    IConverter<FileInfo, IFileExplorerFolderNode, IFileExplorerFileNode>
{
}

public class FileExplorerConverter(
    IRootFileExplorerFolder rootFolder,
    IMimeTypeService mimeTypeService) : IFileExplorerConverter
{
    public IFileExplorerFileNode Convert(string fileAbsolutePath)
    {
        var parentAbsolutePath = fileAbsolutePath.GetParentFolderPathOrThrow();
        var parent = Convert(new DirectoryInfo(parentAbsolutePath), false);
        
        return Convert(new FileInfo(fileAbsolutePath), parent);
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
        
        return new FileExplorerFolderNode(
            directoryInfo.Name,
            directoryInfo.FullName,
            FileExplorerNodeType.Folder,
            directoryInfo.Exists,
            directoryInfo.CreationTimeUtc,
            currentFolderBelongsToRoot,
            rootFolder.BelongsToRootChild(directoryInfo.FullName),
            parent);
    }

    public IFileExplorerFileNode Convert(FileInfo fileInfo, IFileExplorerFolderNode parent)
    {
        return new FileExplorerFileNode(
            fileInfo.Name,
            fileInfo.FullName,
            fileInfo.Extension,
            FileExplorerNodeType.File,
            fileInfo.Exists,
            fileInfo.CreationTimeUtc,
            mimeTypeService.GetMimeType(fileInfo.FullName, fileInfo.Extension),
            parent);
    }
}
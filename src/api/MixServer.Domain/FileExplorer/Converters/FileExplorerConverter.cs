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
        var parent = Convert(new DirectoryInfo(parentAbsolutePath));
        
        return Convert(new FileInfo(fileAbsolutePath), parent);
    }

    public IFileExplorerFolderNode Convert(DirectoryInfo directoryInfo)
    {
        return new FileExplorerFolderNode(
            directoryInfo.Name,
            directoryInfo.FullName,
            FileExplorerNodeType.Folder,
            directoryInfo.Exists,
            directoryInfo.CreationTimeUtc,
            rootFolder.BelongsToRoot(directoryInfo.FullName),
            rootFolder.BelongsToRootChild(directoryInfo.FullName));
    }

    public IFileExplorerFileNode Convert(FileInfo fileInfo, IFileExplorerFolderNode parent)
    {
        return new FileExplorerFileNode(
            fileInfo.Name,
            fileInfo.FullName,
            FileExplorerNodeType.File,
            fileInfo.Exists,
            fileInfo.CreationTimeUtc,
            mimeTypeService.GetMimeType(fileInfo.FullName),
            parent);

    }
}
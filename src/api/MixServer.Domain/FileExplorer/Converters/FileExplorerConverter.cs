using MixServer.Domain.Extensions;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;

namespace MixServer.Domain.FileExplorer.Converters;

public interface IFileExplorerConverter
{
    IFileExplorerFileNode ConvertToFileNode(string fileAbsolutePath);
    IFileExplorerFolderNode ConvertToFolderNode(DirectoryInfo directoryInfo);
    IFileExplorerFileNode ConvertToFileNode(FileInfo fileInfo, IFileExplorerFolderNode parent);
    FileExplorerFolder ConvertToFolder(DirectoryInfo directoryInfo);
}

public class FileExplorerConverter(
    IRootFileExplorerFolder rootFolder,
    IMimeTypeService mimeTypeService) : IFileExplorerConverter
{
    public IFileExplorerFileNode ConvertToFileNode(string fileAbsolutePath)
    {
        var parentAbsolutePath = fileAbsolutePath.GetParentFolderPathOrThrow();
        var parent = ConvertToFolderNode(new DirectoryInfo(parentAbsolutePath));
        
        return ConvertToFileNode(new FileInfo(fileAbsolutePath), parent);
    }

    public IFileExplorerFolderNode ConvertToFolderNode(DirectoryInfo directoryInfo)
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

    public IFileExplorerFileNode ConvertToFileNode(FileInfo fileInfo, IFileExplorerFolderNode parent)
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

    public FileExplorerFolder ConvertToFolder(DirectoryInfo directoryInfo)
    {
        return new FileExplorerFolder(ConvertToFolderNode(directoryInfo));
    }
}
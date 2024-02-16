using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;

namespace MixServer.Domain.FileExplorer.Converters;

public interface IFileSystemInfoConverter
{
    IFileExplorerFolderNode ConvertToFolderNode(string absolutePath);

    IFileExplorerFolderNode ConvertToFolderNode(DirectoryInfo directoryInfo);
    IFileExplorerFileNode ConvertToFileNode(string fileAbsolutePath, IFolderInfo parentInfo);
    IFileExplorerFileNode ConvertToFileNode(FileInfo file, IFolderInfo nodeInfo);
}

public class FileSystemInfoConverter(
    IFileExplorerRootFolderNode rootFolder,
    IMimeTypeService mimeTypeService) : IFileSystemInfoConverter
{
    public IFileExplorerFolderNode ConvertToFolderNode(string absolutePath)
    {
        return ConvertToFolderNode(new DirectoryInfo(absolutePath));
    }

    public IFileExplorerFolderNode ConvertToFolderNode(DirectoryInfo directoryInfo)
    {
        return new FileExplorerFolderNode(ConvertToFolderInfo(directoryInfo));
    }

    public IFileExplorerFileNode ConvertToFileNode(string fileAbsolutePath, IFolderInfo parentInfo)
    {
        return ConvertToFileNode(new FileInfo(fileAbsolutePath), parentInfo);
    }

    public IFileExplorerFileNode ConvertToFileNode(FileInfo file, IFolderInfo nodeInfo)
    {
        return new FileExplorerFileNode(file.Name, mimeTypeService.GetMimeType(file.FullName), file.Exists, file.CreationTimeUtc, nodeInfo);
    }
    
    private FolderInfo ConvertToFolderInfo(DirectoryInfo directoryInfo)
    {
        return new FolderInfo
        {
            Name = directoryInfo.Name,
            AbsolutePath = directoryInfo.FullName,
            ParentAbsolutePath = directoryInfo.Parent?.FullName,
            BelongsToRoot = rootFolder.BelongsToRoot(directoryInfo.FullName),
            BelongsToRootChild = rootFolder.BelongsToRootChild(directoryInfo.FullName),
            Exists = directoryInfo.Exists
        };
    }
}
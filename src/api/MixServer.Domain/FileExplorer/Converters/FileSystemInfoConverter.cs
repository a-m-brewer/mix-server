using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;

namespace MixServer.Domain.FileExplorer.Converters;

public interface IFileSystemInfoConverter
{
    IFileExplorerFolderNode ConvertToFolderNode(string absolutePath, bool canRead);

    IFileExplorerFolderNode ConvertToFolderNode(DirectoryInfo directoryInfo, bool canRead);
    IFileExplorerFileNode ConvertToFileNode(string fileAbsolutePath, IFolderInfo parentInfo);
    IFileExplorerFileNode ConvertToFileNode(FileInfo file, IFolderInfo nodeInfo);
}

public class FileSystemInfoConverter(IMimeTypeService mimeTypeService) : IFileSystemInfoConverter
{
    public IFileExplorerFolderNode ConvertToFolderNode(string absolutePath, bool canRead)
    {
        return ConvertToFolderNode(new DirectoryInfo(absolutePath), canRead);
    }

    public IFileExplorerFolderNode ConvertToFolderNode(DirectoryInfo directoryInfo, bool canRead)
    {
        return new FileExplorerFolderNode(ConvertToFolderInfo(directoryInfo, canRead));
    }

    public IFileExplorerFileNode ConvertToFileNode(string fileAbsolutePath, IFolderInfo parentInfo)
    {
        return ConvertToFileNode(new FileInfo(fileAbsolutePath), parentInfo);
    }

    public IFileExplorerFileNode ConvertToFileNode(FileInfo file, IFolderInfo nodeInfo)
    {
        return new FileExplorerFileNode(file.Name, mimeTypeService.GetMimeType(file.FullName), file.Exists, file.CreationTimeUtc, nodeInfo);
    }
    
    private static IFolderInfo ConvertToFolderInfo(DirectoryInfo directoryInfo, bool canRead)
    {
        return new FolderInfo
        {
            Name = directoryInfo.Name,
            AbsolutePath = directoryInfo.FullName,
            ParentAbsolutePath = directoryInfo.Parent?.FullName,
            CanRead = canRead,
            Exists = directoryInfo.Exists
        };
    }
}
using MixServer.Domain.Extensions;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Caching;

namespace MixServer.Domain.FileExplorer.Converters;

public interface IFileSystemInfoConverter
{
    IFileExplorerFolderNode ConvertToFolderNode(string absolutePath, string? parentDirectory, bool canRead);
    IFileExplorerFolderNode ConvertToFolderNode(ICacheDirectoryInfo directoryInfo, bool canRead);
    IFileExplorerFileNode ConvertToFileNode(ICacheFileInfo fileInfo, IFileExplorerFolderNode parent);
}

public class FileSystemInfoConverter : IFileSystemInfoConverter
{
    public IFileExplorerFolderNode ConvertToFolderNode(string absolutePath, string? parentDirectory, bool canRead)
    {
        var exists = Directory.Exists(absolutePath);
        var name = exists ? new DirectoryInfo(absolutePath).Name : null;
        return new FileExplorerFolderNode(name, absolutePath, parentDirectory, exists, canRead);
    }

    public IFileExplorerFolderNode ConvertToFolderNode(ICacheDirectoryInfo directoryInfo, bool canRead)
    {
        return new FileExplorerFolderNode(directoryInfo.Name, directoryInfo.FullName, directoryInfo.ParentDirectory,
            directoryInfo.Exists, canRead);
    }

    public IFileExplorerFileNode ConvertToFileNode(ICacheFileInfo fileInfo, IFileExplorerFolderNode parent)
    {
        return new FileExplorerFileNode(fileInfo.Name, fileInfo.MimeType, fileInfo.Exists, parent);
    }
}
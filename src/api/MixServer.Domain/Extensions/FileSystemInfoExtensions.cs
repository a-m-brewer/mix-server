using MixServer.Domain.Constants;
using MixServer.Domain.FileExplorer.Settings;

namespace MixServer.Domain.Extensions;

public static class FileSystemInfoExtensions
{
    public static IEnumerable<FileSystemInfo> MsEnumerateFileSystemInfos(this DirectoryInfo directoryInfo)
    {
        return directoryInfo.EnumerateFileSystemInfos("*", FileSystemEnumeration.Options)
            .Where(w => w is DirectoryInfo || w.Name != FolderMetadataConstants.MetadataFileName);
    }
    
    public static IEnumerable<FileInfo> MsEnumerateFiles(this DirectoryInfo directoryInfo)
    {
        return directoryInfo.EnumerateFiles("*", FileSystemEnumeration.Options)
            .Where(w => w.Name != FolderMetadataConstants.MetadataFileName);
    }
    
    public static IEnumerable<DirectoryInfo> MsEnumerateDirectories(this DirectoryInfo directoryInfo)
    {
        return directoryInfo.EnumerateDirectories("*", FileSystemEnumeration.Options);
    }
}
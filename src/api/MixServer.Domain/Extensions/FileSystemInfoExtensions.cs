using MixServer.Domain.Constants;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Settings;

namespace MixServer.Domain.Extensions;

public static class FileSystemInfoExtensions
{
    public static IEnumerable<FileSystemInfo> MsEnumerateFileSystemInfos(this DirectoryInfo directoryInfo)
    {
        return directoryInfo.EnumerateFileSystemInfos("*", FileSystemEnumeration.Options)
            .Where(w => w is DirectoryInfo || w.Name != FolderMetadataConstants.MetadataFileName)
            .OrderBy(o => o.FullName, StringComparer.Ordinal);
    }
    
    public static IEnumerable<FileInfo> MsEnumerateFiles(this DirectoryInfo directoryInfo)
    {
        return directoryInfo.EnumerateFiles("*", FileSystemEnumeration.Options)
            .Where(w => w.Name != FolderMetadataConstants.MetadataFileName)
            .OrderBy(o => o.FullName, StringComparer.Ordinal);
    }
    
    public static IEnumerable<DirectoryInfo> MsEnumerateDirectories(this DirectoryInfo directoryInfo)
    {
        return directoryInfo.EnumerateDirectories("*", FileSystemEnumeration.Options)
            .OrderBy(o => o.FullName, StringComparer.Ordinal);
    }

    public static IEnumerable<T> MsEnumerateFileSystem<T>(this DirectoryInfo directoryInfo,
        Page? page = null,
        IFolderSort? sort = null)
        where T : FileSystemInfo
    {
        if (typeof(T) == typeof(FileInfo))
        {
            return directoryInfo.MsEnumerateFiles()
                .Cast<T>()
                .MsEnumerateFileSystem(page, sort);
        }

        if (typeof(T) == typeof(DirectoryInfo))
        {
            return directoryInfo.MsEnumerateDirectories()
                .Cast<T>()
                .MsEnumerateFileSystem(page, sort);
        }

        return directoryInfo.MsEnumerateFileSystemInfos()
            .Cast<T>()
            .MsEnumerateFileSystem(page, sort);
    }

    public static IEnumerable<T> MsEnumerateFileSystem<T>(this IEnumerable<T> fsEnumerable,
        Page? page = null,
        IFolderSort? sort = null)
        where T : FileSystemInfo
    {
        var internalSort = sort ?? FolderSortModel.Default;
        
        Func<T, object> func = internalSort.SortMode switch
        {
            FolderSortMode.Name => i => i.Name,
            FolderSortMode.Created => i => i.CreationTimeUtc,
            _ => i => i.Name
        };

        var (directoryIndex, fileIndex) = internalSort.SortMode == FolderSortMode.Name
            ? (0, 1)
            : (1, 0);

        var values = fsEnumerable.OrderBy(o => o is DirectoryInfo ? directoryIndex : fileIndex);

        values = internalSort.Descending
            ? values.ThenByDescending(func)
            : values.ThenBy(func);
        
        IEnumerable<T> output = values;

        if (page is not null)
        {
            output = output.Skip(page.PageIndex * page.PageSize)
                           .Take(page.PageSize);
        }

        return output;
    }
}
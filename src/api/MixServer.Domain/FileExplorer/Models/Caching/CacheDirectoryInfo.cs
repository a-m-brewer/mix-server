namespace MixServer.Domain.FileExplorer.Models.Caching;

public interface ICacheDirectoryInfo : ICacheFileSystemInfo
{
}

public class CacheDirectoryInfo(DirectoryInfo info)
    : CacheFileSystemInfo(info.Name, info.FullName, info.CreationTimeUtc, info.Parent?.FullName, info.Exists), ICacheDirectoryInfo
{
    
}
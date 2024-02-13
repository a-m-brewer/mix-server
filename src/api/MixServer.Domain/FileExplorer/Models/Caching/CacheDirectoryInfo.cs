namespace MixServer.Domain.FileExplorer.Models.Caching;

public interface ICacheDirectoryInfo : ICacheFileSystemInfo
{
}

public class CacheDirectoryInfo : CacheFileSystemInfo, ICacheDirectoryInfo
{
    
}
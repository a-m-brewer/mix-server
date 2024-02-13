namespace MixServer.Domain.FileExplorer.Models.Caching;

public interface ICacheFileInfo : ICacheFileSystemInfo
{
    string MimeType { get; }
}

public class CacheFileInfo(FileInfo fileInfo, string mimeType) 
    : CacheFileSystemInfo(fileInfo.Name, fileInfo.FullName, fileInfo.CreationTimeUtc, fileInfo.Directory?.FullName, fileInfo.Exists), ICacheFileInfo
{
    public string MimeType { get; } = mimeType;
}
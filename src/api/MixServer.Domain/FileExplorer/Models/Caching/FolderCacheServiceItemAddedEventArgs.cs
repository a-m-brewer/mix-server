namespace MixServer.Domain.FileExplorer.Models.Caching;

public class FolderCacheServiceItemAddedEventArgs(ICacheDirectoryInfo parent, ICacheFileSystemInfo item) 
    : FolderCacheServiceEventArgs(parent)
{
    public ICacheFileSystemInfo Item { get; } = item;
}
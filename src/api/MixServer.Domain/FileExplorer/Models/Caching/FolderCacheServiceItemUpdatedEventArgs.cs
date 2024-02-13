namespace MixServer.Domain.FileExplorer.Models.Caching;

public class FolderCacheServiceItemUpdatedEventArgs(ICacheDirectoryInfo parent, ICacheFileSystemInfo item, string oldFullName)
    : FolderCacheServiceItemAddedEventArgs(parent, item)
{
    public string OldFullName { get; } = oldFullName;
}
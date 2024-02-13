namespace MixServer.Domain.FileExplorer.Models.Caching;

public class FolderItemUpdatedEventArgs(ICacheFileSystemInfo item, string oldFullPath)
{
    public ICacheFileSystemInfo Item { get; } = item;

    public string OldFullPath { get; } = oldFullPath;
}
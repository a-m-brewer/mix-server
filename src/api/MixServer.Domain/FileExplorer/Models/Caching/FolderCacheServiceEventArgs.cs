namespace MixServer.Domain.FileExplorer.Models.Caching;

public abstract class FolderCacheServiceEventArgs(ICacheDirectoryInfo parent) : EventArgs
{
    public ICacheDirectoryInfo Parent { get; } = parent;
}
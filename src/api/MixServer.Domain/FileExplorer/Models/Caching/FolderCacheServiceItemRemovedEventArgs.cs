namespace MixServer.Domain.FileExplorer.Models.Caching;

public class FolderCacheServiceItemRemovedEventArgs(ICacheDirectoryInfo parent, string fullName) : FolderCacheServiceEventArgs(parent)
{
    public string FullName { get; } = fullName;
}
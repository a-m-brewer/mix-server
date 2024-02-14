namespace MixServer.Domain.FileExplorer.Models.Caching;

public class FolderCacheServiceItemUpdatedEventArgs(IFileExplorerNode item, string oldFullName)
{
    public IFileExplorerNode Item { get; } = item;
    public string OldFullName { get; } = oldFullName;
}
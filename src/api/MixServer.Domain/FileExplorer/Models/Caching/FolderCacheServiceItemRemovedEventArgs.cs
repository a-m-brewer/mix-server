namespace MixServer.Domain.FileExplorer.Models.Caching;

public class FolderCacheServiceItemRemovedEventArgs(IFileExplorerFolderNode parent, string fullName)
{
    public string FullName { get; } = fullName;
    
    public IFileExplorerFolderNode Parent { get; } = parent;
}
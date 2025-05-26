namespace MixServer.Domain.FileExplorer.Models.Caching;

public class FolderCacheServiceItemRemovedEventArgs : EventArgs
{
    public required IFileExplorerNode Node { get; init; }
    
    public required IFileExplorerFolderNode Parent { get; init; }
}
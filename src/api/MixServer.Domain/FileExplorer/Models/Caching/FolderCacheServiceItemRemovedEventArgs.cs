namespace MixServer.Domain.FileExplorer.Models.Caching;

public class FolderCacheServiceItemRemovedEventArgs : EventArgs
{
    public required NodePath Path { get; init; }
    
    public required IFileExplorerFolderNode Parent { get; init; }
}
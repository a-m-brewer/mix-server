namespace MixServer.Domain.FileExplorer.Models.Caching;

public class FolderCacheServiceItemUpdatedEventArgs
{
    public required IFileExplorerNode Item { get; init; }
    public required NodePath OldPath { get; init; }
}
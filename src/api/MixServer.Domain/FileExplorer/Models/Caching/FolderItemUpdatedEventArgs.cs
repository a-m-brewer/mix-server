namespace MixServer.Domain.FileExplorer.Models.Caching;

public class FolderItemUpdatedEventArgs(IFileExplorerNode item, NodePath oldPath)
{
    public IFileExplorerNode Item { get; } = item;

    public NodePath OldPath { get; } = oldPath;
}
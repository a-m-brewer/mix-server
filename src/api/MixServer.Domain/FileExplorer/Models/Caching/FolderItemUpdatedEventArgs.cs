namespace MixServer.Domain.FileExplorer.Models.Caching;

public class FolderItemUpdatedEventArgs(IFileExplorerNode item, string oldFullPath)
{
    public IFileExplorerNode Item { get; } = item;

    public string OldFullPath { get; } = oldFullPath;
}
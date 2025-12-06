namespace MixServer.Domain.FileExplorer.Models;

public interface IFileExplorerFolderRange
{
    IFileExplorerFolderNode Node { get; }
    
    IReadOnlyCollection<IFileExplorerNode> Items { get; }
    
    IFolderSort Sort { get; }
}

public class FileExplorerFolderRange : IFileExplorerFolderRange
{
    public required IFileExplorerFolderNode Node { get; init; }

    public required IReadOnlyCollection<IFileExplorerNode> Items { get; init; }

    public required IFolderSort Sort { get; init; }
}
namespace MixServer.Domain.FileExplorer.Models;

public interface IFileExplorerFolderPage
{
    IFileExplorerFolderNode Node { get; }
    
    IChildPage Page { get; }
    
    IFolderSort Sort { get; }
}

public interface IChildPage 
{
    int PageIndex { get; }
    
    IReadOnlyCollection<IFileExplorerNode> Children { get; }
}

public class FileExplorerFolderPage : IFileExplorerFolderPage
{
    public required IFileExplorerFolderNode Node { get; init; }
    public required IChildPage Page { get; init; }

    public required IFolderSort Sort { get; init; }
}

public class FileExplorerFolderChildPage : IChildPage
{
    public required int PageIndex { get; init; }
    public required IReadOnlyCollection<IFileExplorerNode> Children { get; init; }
}
namespace MixServer.Domain.FileExplorer.Models;

public class FolderDiff
{
    public required FolderHeader FileSystemHeader { get; init; }
    
    public required FolderHeader? DatabaseHeader { get; init; }
    
    public bool Dirty => DatabaseHeader is null || !FileSystemHeader.Equals(DatabaseHeader);
}
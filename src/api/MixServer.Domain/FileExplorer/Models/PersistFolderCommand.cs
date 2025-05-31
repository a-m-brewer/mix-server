namespace MixServer.Domain.FileExplorer.Models;

public class PersistFolderCommand
{
    public required DirectoryInfo Directory { get; init; }
    
    public required List<FileSystemInfo> Children { get; init; }
}
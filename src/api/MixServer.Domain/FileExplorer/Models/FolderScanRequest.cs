namespace MixServer.Domain.FileExplorer.Models;

public class ScanFolderRequest
{
    public required NodePath NodePath { get; init; }
    
    public required bool Recursive { get; init; }
}
using MixServer.Domain.Interfaces;

namespace MixServer.Domain.FileExplorer.Models;

public class PersistFolderCommand : IChannelMessage
{
    public string Identifier => DirectoryPath.AbsolutePath;
    public required NodePath DirectoryPath { get; init; }
    public required DirectoryInfo Directory { get; init; }
    
    public required List<FileSystemInfo> Children { get; init; }
}
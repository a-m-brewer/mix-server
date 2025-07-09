using MixServer.Domain.Interfaces;

namespace MixServer.Domain.FileExplorer.Models;

public class ScanFolderRequest : IChannelMessage
{
    public string Identifier => NodePath.AbsolutePath;
    
    public required NodePath NodePath { get; init; }
    
    public required bool Recursive { get; init; }
}
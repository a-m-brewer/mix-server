using MixServer.Application.FileExplorer.Dtos;

namespace MixServer.Application.FileExplorer.Commands.CopyNode;

public class CopyNodeCommand
{
    public required NodePathDto SourcePath { get; set; }
    
    public required NodePathDto DestinationPath { get; set; }
    
    public bool Move { get; set; }
    
    public bool Overwrite { get; set; }
}
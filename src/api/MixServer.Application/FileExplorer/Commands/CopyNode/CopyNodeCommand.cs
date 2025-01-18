namespace MixServer.Application.FileExplorer.Commands.CopyNode;

public class CopyNodeCommand
{
    public string SourceAbsolutePath { get; set; } = string.Empty;
    
    public string DestinationFolder { get; set; } = string.Empty;
    
    public string DestinationName { get; set; } = string.Empty;
    
    public bool Move { get; set; }
    
    public bool Overwrite { get; set; }
}
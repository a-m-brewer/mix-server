namespace MixServer.Application.FileExplorer.Dtos;

public class NodePathDto
{
    public required string ParentAbsolutePath { get; init; }
    
    public required string FileName { get; init; }
    
    public required string AbsolutePath { get; init; }
}
namespace MixServer.Application.FileExplorer.Dtos;

public class NodePathDto : NodePathRequestDto
{
    public required string FileName { get; init; }
    
    public required string AbsolutePath { get; init; }
    
    public required string Extension { get; init; }
    
    public required NodePathHeaderDto Parent { get; init; }
    
    public required bool IsRoot { get; init; }
    
    public required bool IsRootChild { get; init; }
}
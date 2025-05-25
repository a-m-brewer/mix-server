namespace MixServer.Application.FileExplorer.Dtos;

public class NodePathHeaderDto : NodePathRequestDto
{
    public required string AbsolutePath { get; init; }
}
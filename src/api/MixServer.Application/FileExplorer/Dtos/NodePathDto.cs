namespace MixServer.Application.FileExplorer.Dtos;

public class NodePathDto
{
    public required string RootPath { get; init; }
    public required string RelativePath { get; init; }
}
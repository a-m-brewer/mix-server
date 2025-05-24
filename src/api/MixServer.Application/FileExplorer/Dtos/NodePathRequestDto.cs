namespace MixServer.Application.FileExplorer.Dtos;

public class NodePathRequestDto
{
    public string? RootPath { get; set; }
    public string? RelativePath { get; set; }
}
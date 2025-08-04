namespace MixServer.Application.FileExplorer.Dtos;

public class PagedNodePathRequestDto : NodePathRequestDto
{
    public PageDto Page { get; set; } = new();
}
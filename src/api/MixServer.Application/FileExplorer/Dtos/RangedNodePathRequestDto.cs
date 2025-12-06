namespace MixServer.Application.FileExplorer.Dtos;

public class RangedNodePathRequestDto : NodePathRequestDto
{
    public required RangeDto Range { get; init; }
}
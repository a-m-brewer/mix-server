namespace MixServer.Domain.FileExplorer.Models;

public class Page
{
    public required int PageIndex { get; init; }
    
    public required int PageSize { get; init; }
}
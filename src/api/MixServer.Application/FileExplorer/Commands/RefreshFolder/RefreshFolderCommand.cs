using MixServer.Application.FileExplorer.Dtos;
using MixServer.Domain.FileExplorer.Constants;

namespace MixServer.Application.FileExplorer.Commands.RefreshFolder;

public class RefreshFolderCommand
{
    public NodePathRequestDto? NodePath { get; set; }
    
    public int PageSize { get; set; } = FileExplorerPageConstants.DefaultPageSize;

    public bool Recursive { get; set; } = false;
}
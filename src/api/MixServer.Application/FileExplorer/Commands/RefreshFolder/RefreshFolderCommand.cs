using MixServer.Application.FileExplorer.Dtos;

namespace MixServer.Application.FileExplorer.Commands.RefreshFolder;

public class RefreshFolderCommand
{
    public NodePathRequestDto? NodePath { get; set; }
    
    public bool Recursive { get; set; } = false;
}
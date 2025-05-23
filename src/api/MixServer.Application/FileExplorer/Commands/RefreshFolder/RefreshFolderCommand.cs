using MixServer.Domain.FileExplorer.Models;

namespace MixServer.Application.FileExplorer.Commands.RefreshFolder;

public class RefreshFolderCommand
{
    public NodePath? NodePath { get; set; }
}
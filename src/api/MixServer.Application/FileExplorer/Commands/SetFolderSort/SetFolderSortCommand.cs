using MixServer.Application.FileExplorer.Dtos;
using MixServer.Domain.FileExplorer.Enums;

namespace MixServer.Application.FileExplorer.Commands.SetFolderSort;

public class SetFolderSortCommand
{
    public required NodePathDto NodePath { get; set; }

    public bool Descending { get; set; }

    public FolderSortMode SortMode { get; set; }
}
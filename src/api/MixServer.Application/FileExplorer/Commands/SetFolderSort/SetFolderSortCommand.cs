using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;

namespace MixServer.Application.FileExplorer.Commands.SetFolderSort;

public class SetFolderSortCommand : IFolderSortRequest
{
    public string AbsoluteFolderPath { get; set; } = string.Empty;
    
    public bool Descending { get; set; }

    public FolderSortMode SortMode { get; set; }
}
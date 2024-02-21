using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;

namespace MixServer.Application.FileExplorer.Dtos;

public class FolderSortDto(bool descending, FolderSortMode sortMode) : IFolderSort
{
    public FolderSortDto(IFolderSort folderSort) : this(folderSort.Descending, folderSort.SortMode)
    {
    }
    
    public bool Descending { get; } = descending;
    public FolderSortMode SortMode { get; } = sortMode;

    public static FolderSortDto Default => new FolderSortDto(FolderSortModel.Default);
}
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;

namespace MixServer.Application.FileExplorer.Dtos;

public class FolderSortDto : IFolderSort
{
    public FolderSortDto(bool descending, FolderSortMode sortMode)
    {
        Descending = descending;
        SortMode = sortMode;
    }

    public FolderSortDto(IFolderSort folderSort)
    {
        Descending = folderSort.Descending;
        SortMode = folderSort.SortMode;
    }
    
    public bool Descending { get; }
    public FolderSortMode SortMode { get; }

    public static FolderSortDto Default => new FolderSortDto(FolderSortModel.Default);
}
using MixServer.Domain.FileExplorer.Enums;

namespace MixServer.Domain.FileExplorer.Models;

public class FolderSortModel(bool descending, FolderSortMode sortMode) : IFolderSort
{
    public bool Descending { get; } = descending;
    public FolderSortMode SortMode { get; } = sortMode;

    public static FolderSortModel Default => new(false, FolderSortMode.Name);
}
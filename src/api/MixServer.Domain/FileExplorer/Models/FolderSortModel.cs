using MixServer.Domain.FileExplorer.Enums;

namespace MixServer.Domain.FileExplorer.Models;

public class FolderSortModel : IFolderSort
{
    public FolderSortModel(bool descending, FolderSortMode sortMode)
    {
        Descending = descending;
        SortMode = sortMode;
    }

    public bool Descending { get; }
    public FolderSortMode SortMode { get; }

    public static FolderSortModel Default => new FolderSortModel(false, FolderSortMode.Name);
}
using MixServer.Domain.FileExplorer.Enums;

namespace MixServer.Domain.FileExplorer.Models;

public interface IFolderSort
{
    public bool Descending { get; }

    public FolderSortMode SortMode { get; }
}
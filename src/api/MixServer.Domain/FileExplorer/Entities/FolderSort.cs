using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;

namespace MixServer.Domain.FileExplorer.Entities;

public class FolderSort : IFolderSortRequest
{
    public FolderSort(
        Guid id,
        string absoluteFolderPath,
        bool descending,
        FolderSortMode sortMode)
    {
        Id = id;
        AbsoluteFolderPath = absoluteFolderPath;
        Descending = descending;
        SortMode = sortMode;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    protected FolderSort()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
    }
    
    public Guid Id { get; protected set; }
    public string AbsoluteFolderPath { get; protected set; }
    public bool Descending { get; protected set; }
    public FolderSortMode SortMode { get; protected set;  }

    public string UserId { get; set; } = string.Empty;

    public void Update(IFolderSort updatedSort)
    {
        Descending = updatedSort.Descending;
        SortMode = updatedSort.SortMode;
    }
}
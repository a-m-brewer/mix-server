using System.ComponentModel.DataAnnotations.Schema;
using MixServer.Domain.FileExplorer.Enums;
using MixServer.Domain.FileExplorer.Models;

namespace MixServer.Domain.FileExplorer.Entities;

public class FolderSort : IFolderSort
{
    public required Guid Id { get; init; }
    
    // TODO: Make this non-nullable after migration to new root, relative paths is complete.
    public FileExplorerFolderNodeEntity? Node { get; set; }
    public Guid? NodeId { get; set; }

    [Obsolete("Use Node instead.")]
    public string AbsoluteFolderPath { get; set; } = string.Empty;
    public required bool Descending { get; set; }
    public required FolderSortMode SortMode { get; set;  }

    public required string UserId { get; set; } 

    // Workaround for application code to pretend Node is not nullable
    [NotMapped]
    public required FileExplorerFolderNodeEntity NodeEntity
    {
        get => Node ?? throw new InvalidOperationException("Node is not set.");
        set => Node = value;
    }
    
    [NotMapped]
    public required Guid NodeIdEntity
    {
        get => NodeId ?? throw new InvalidOperationException("NodeId is not set.");
        set => NodeId = value;
    }
    
    public void Update(IFolderSort updatedSort)
    {
        Descending = updatedSort.Descending;
        SortMode = updatedSort.SortMode;
    }
}
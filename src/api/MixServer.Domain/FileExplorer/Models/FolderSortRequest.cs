using MixServer.Domain.FileExplorer.Enums;

namespace MixServer.Domain.FileExplorer.Models;

public class FolderSortRequest : IFolderSortRequest
{
    public required NodePath Path { get; init; }
    public required bool Descending { get; init; }
    public required FolderSortMode SortMode { get; init; }
}
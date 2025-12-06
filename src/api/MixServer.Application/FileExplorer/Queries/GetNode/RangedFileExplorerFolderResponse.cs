using MixServer.Application.FileExplorer.Dtos;

namespace MixServer.Application.FileExplorer.Queries.GetNode;

public class RangedFileExplorerFolderResponse
{
    public required FileExplorerFolderNodeResponse Node { get; init; }
    
    public required IReadOnlyCollection<FileExplorerNodeResponse> Items { get; init; }
    
    public required FolderSortDto Sort { get; init; }
}
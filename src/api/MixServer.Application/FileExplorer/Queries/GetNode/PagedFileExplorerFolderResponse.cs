using MixServer.Application.FileExplorer.Dtos;

namespace MixServer.Application.FileExplorer.Queries.GetNode;

public class FileExplorerFolderChildPageResponse
{
    public required int PageIndex { get; init; }
    
    public required IReadOnlyCollection<FileExplorerNodeResponse> Children { get; init; }
}

public class PagedFileExplorerFolderResponse
{
    public required FileExplorerFolderNodeResponse Node { get; init; }
    
    public required FileExplorerFolderChildPageResponse Page { get; init; }
    
    public required FolderSortDto Sort { get; init; }
}
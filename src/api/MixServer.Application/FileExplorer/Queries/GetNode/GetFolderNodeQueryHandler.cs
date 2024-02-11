using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Queries.GetNode;

public class GetFolderNodeQueryHandler : IQueryHandler<GetFolderNodeQuery, FolderNodeResponse>
{
    private readonly IConverter<IFileExplorerFolderNode, FolderNodeResponse> _folderNodeConverter;
    private readonly IFileService _fileService;

    public GetFolderNodeQueryHandler(
        IConverter<IFileExplorerFolderNode, FolderNodeResponse> folderNodeConverter,
        IFileService fileService)
    {
        _folderNodeConverter = folderNodeConverter;
        _fileService = fileService;
    }
    
    public async Task<FolderNodeResponse> HandleAsync(GetFolderNodeQuery request)
    {
        var folder = await _fileService.GetFolderOrRootAsync(request.AbsolutePath);

        var result = _folderNodeConverter.Convert(folder);

        return result;
    }
}
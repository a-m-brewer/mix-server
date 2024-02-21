using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Queries.GetNode;

public class GetFolderNodeQueryHandler(
    IConverter<IFileExplorerFolder, FileExplorerFolderResponse> folderNodeConverter,
    IFileService fileService)
    : IQueryHandler<GetFolderNodeQuery, FileExplorerFolderResponse>
{
    public async Task<FileExplorerFolderResponse> HandleAsync(GetFolderNodeQuery request)
    {
        var folder = await fileService.GetFolderOrRootAsync(request.AbsolutePath);

        var result = folderNodeConverter.Convert(folder);

        return result;
    }
}
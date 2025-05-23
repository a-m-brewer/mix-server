using MixServer.Application.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Queries.GetNode;

public class GetFolderNodeQueryHandler(
    IConverter<IFileExplorerFolder, FileExplorerFolderResponse> folderNodeConverter,
    INodePathDtoConverter nodePathConverter,
    IFileService fileService)
    : IQueryHandler<GetFolderNodeQuery, FileExplorerFolderResponse>
{
    public async Task<FileExplorerFolderResponse> HandleAsync(GetFolderNodeQuery request)
    {
        var folder = await fileService.GetFolderOrRootAsync(
            request.NodePath is null
                ? null
                : nodePathConverter.Convert(request.NodePath));

        var result = folderNodeConverter.Convert(folder);

        return result;
    }
}
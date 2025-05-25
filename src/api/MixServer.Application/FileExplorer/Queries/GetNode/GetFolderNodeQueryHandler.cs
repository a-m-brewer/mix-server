using MixServer.Application.FileExplorer.Converters;
using MixServer.Application.FileExplorer.Dtos;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Queries.GetNode;

public class GetFolderNodeQueryHandler(
    IConverter<IFileExplorerFolder, FileExplorerFolderResponse> folderNodeConverter,
    INodePathDtoConverter nodePathConverter,
    IFileService fileService)
    : IQueryHandler<NodePathRequestDto, FileExplorerFolderResponse>
{
    public async Task<FileExplorerFolderResponse> HandleAsync(NodePathRequestDto request)
    {
        var folder = await fileService.GetFolderOrRootAsync(nodePathConverter.Convert(request));

        var result = folderNodeConverter.Convert(folder);

        return result;
    }
}
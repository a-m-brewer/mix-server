using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MixServer.Application.FileExplorer.Converters;
using MixServer.Application.FileExplorer.Dtos;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Queries.GetNode;

public class GetFolderNodeQueryHandler(
    IConverter<IFileExplorerFolder, FileExplorerFolderResponse> folderNodeConverter,
    ILogger<GetFolderNodeQueryHandler> logger,
    INodePathDtoConverter nodePathConverter,
    IFileService fileService)
    : IQueryHandler<NodePathRequestDto, FileExplorerFolderResponse>
{
    public async Task<FileExplorerFolderResponse> HandleAsync(NodePathRequestDto request, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var folder = await fileService.GetFolderOrRootAsync(nodePathConverter.Convert(request), cancellationToken);

        var result = folderNodeConverter.Convert(folder);

        logger.LogInformation("GetFolderNodeQueryHandler executed in {ElapsedMilliseconds} ms for path: {Path}",
            sw.ElapsedMilliseconds, Path.Join(request.RootPath, request.RelativePath));
        return result;
    }
}
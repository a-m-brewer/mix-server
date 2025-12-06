using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MixServer.Application.FileExplorer.Converters;
using MixServer.Application.FileExplorer.Dtos;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;

namespace MixServer.Application.FileExplorer.Queries.GetNode;

public class GetFolderNodeQueryHandler(
    IPagedFileExplorerResponseConverter folderNodeConverter,
    ILogger<GetFolderNodeQueryHandler> logger,
    INodePathDtoConverter nodePathConverter,
    IFileService fileService,
    IUnitOfWork unitOfWork)
    : IQueryHandler<RangedNodePathRequestDto, RangedFileExplorerFolderResponse>
{
    public async Task<RangedFileExplorerFolderResponse> HandleAsync(RangedNodePathRequestDto request, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        var folder = await fileService.GetFolderOrRootRangeAsync(nodePathConverter.Convert(request), nodePathConverter.ConvertToRange(request), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        var result = folderNodeConverter.Convert(folder);

        logger.LogInformation("GetFolderNodeQueryHandler executed in {ElapsedMilliseconds} ms for path: {Path}",
            sw.ElapsedMilliseconds, Path.Join(request.RootPath, request.RelativePath));
        return result;
    }
}
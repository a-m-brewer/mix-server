using System.Diagnostics;
using FluentValidation;
using Microsoft.Extensions.Logging;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Queries.GetNode;

public class GetFolderNodeQueryHandler(
    IConverter<IFileExplorerFolder, FileExplorerFolderResponse> folderNodeConverter,
    ILogger<GetFolderNodeQueryHandler> logger,
    IFileService fileService,
    IValidator<GetFolderNodeQuery> validator)
    : IQueryHandler<GetFolderNodeQuery, FileExplorerFolderResponse>
{
    public async Task<FileExplorerFolderResponse> HandleAsync(GetFolderNodeQuery request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var sw = Stopwatch.StartNew();

        var nodePath = new NodePath(
            request.RootPath ?? string.Empty,
            request.RelativePath ?? string.Empty);

        var folder = await fileService.GetFolderOrRootAsync(nodePath, cancellationToken);

        var result = folderNodeConverter.Convert(folder);

        if (request.StartIndex.HasValue && request.EndIndex.HasValue)
        {
            var start = request.StartIndex.Value;
            var end = Math.Min(request.EndIndex.Value, result.TotalCount);
            result.Children = result.Children.Skip(start).Take(end - start).ToList();
        }

        logger.LogInformation("GetFolderNodeQueryHandler executed in {ElapsedMilliseconds} ms for path: {Path}",
            sw.ElapsedMilliseconds, Path.Join(request.RootPath, request.RelativePath));
        return result;
    }
}
using Microsoft.Extensions.Logging;
using MixServer.Domain.Callbacks;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services.Caching;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Commands.RemoveMediaMetadata;

public class RemoveMediaMetadataCommandHandler(
    ICallbackService callbackService,
    ILogger<RemoveMediaMetadataCommandHandler> logger,
    IMediaInfoCache mediaInfoCache) : ICommandHandler<RemoveMediaMetadataRequest>
{
    public async Task HandleAsync(RemoveMediaMetadataRequest request, CancellationToken cancellationToken)
    {
        var removedItems = mediaInfoCache.Remove(request.NodePaths);
        await callbackService.MediaInfoRemoved(removedItems);
    }
}
using MixServer.Domain.Callbacks;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Commands.RemoveMediaMetadata;

public class RemoveMediaMetadataCommandHandler(ICallbackService callbackService) : ICommandHandler<RemoveMediaMetadataRequest>
{
    public async Task HandleAsync(RemoveMediaMetadataRequest request, CancellationToken cancellationToken)
    {
        await callbackService.MediaInfoRemoved(request.NodePaths);
    }
}
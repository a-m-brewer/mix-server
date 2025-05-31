using Microsoft.Extensions.Logging;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Commands.PersistFolder;

public class PersistFolderCommandHandler(
    ILogger<PersistFolderCommandHandler> logger) : ICommandHandler<PersistFolderCommand>
{
    public Task HandleAsync(PersistFolderCommand request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Received persist folder request for {Directory} with {ChildrenCount} children",
            request.Directory.FullName, request.Children.Count);
        

        return Task.CompletedTask;
    }
}
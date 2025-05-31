using Microsoft.Extensions.Logging;
using MixServer.Domain.FileExplorer.Models.Indexing;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Commands.ProcessRootChildChange;

public class ProcessRootChildChangeCommandHandler(ILogger<ProcessRootChildChangeCommandHandler> logger) : ICommandHandler<RootChildChangeEvent>
{
    public Task HandleAsync(RootChildChangeEvent request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("{FullName} has changed: {RootFolderChangeType} {WatcherChangeType} (Old: {OldFullName})", 
            request.FullName,
            request.RootFolderChangeType,
            request.WatcherChangeType,
            request.OldFullName);
        
        return Task.CompletedTask;
    }
}
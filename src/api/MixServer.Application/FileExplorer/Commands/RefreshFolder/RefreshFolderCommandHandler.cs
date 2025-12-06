using MixServer.Application.FileExplorer.Converters;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Commands.RefreshFolder;

public class RefreshFolderCommandHandler(
    IFolderScanTrackingStore folderScanTrackingStore,
    INodePathDtoConverter nodePathDtoConverter,
    IRootFileExplorerFolder rootFolder,
    IScanFolderRequestChannel scanFolderRequestChannel) : ICommandHandler<RefreshFolderCommand>
{
    public async Task HandleAsync(RefreshFolderCommand request, CancellationToken cancellationToken = default)
    {
        var nodePath = request.NodePath is null ? null : nodePathDtoConverter.Convert(request.NodePath);

        if (folderScanTrackingStore.ScanInProgress)
        {
            throw new InvalidRequestException("A folder scan is already in progress. Please wait until it completes.");
        }

        if (request.Recursive && (nodePath is null || nodePath.IsRoot))
        {
            throw new InvalidRequestException("Recursive refresh is not allowed for the root folder.");
        }
        
        if (nodePath is null || nodePath.IsRoot)
        {
            rootFolder.RefreshChildren();
        }
        else
        {
            // Send request for a full update of the folder
            await scanFolderRequestChannel.WriteAsync(new ScanFolderRequest
            {
                NodePath = nodePath,
                Recursive = request.Recursive
            }, cancellationToken);
        }
    }
}
using MixServer.Application.FileExplorer.Converters;
using MixServer.Application.FileExplorer.Queries.GetNode;
using MixServer.Domain.Callbacks;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Persistence;
using MixServer.Domain.Users.Repositories;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.FileExplorer.Commands.RefreshFolder;

public class RefreshFolderCommandHandler(
    ICurrentDeviceRepository currentDeviceRepository,
    ICurrentUserRepository currentUserRepository,
    ICallbackService callbackService,
    IFileExplorerResponseConverter fileExplorerResponseConverter,
    IFileService fileService,
    INodePathDtoConverter nodePathDtoConverter,
    IRootFileExplorerFolder rootFolder,
    IScanFolderRequestChannel scanFolderRequestChannel,
    IUnitOfWork unitOfWork) : ICommandHandler<RefreshFolderCommand, FileExplorerFolderResponse>
{
    public async Task<FileExplorerFolderResponse> HandleAsync(RefreshFolderCommand request, CancellationToken cancellationToken = default)
    {
        var nodePath = request.NodePath is null ? null : nodePathDtoConverter.Convert(request.NodePath);
        
        // Return the current state of the folder
        var folder = await fileService.GetFolderOrRootAsync(nodePath, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        if (nodePath is null || nodePath.IsRoot)
        {
            rootFolder.RefreshChildren();
        }
        else
        {
            // Send request for a full update of the folder
            _ = scanFolderRequestChannel.WriteAsync(new ScanFolderRequest
            {
                NodePath = nodePath,
                Recursive = false
            }, cancellationToken);
        }

        // TODO: make a way of refreshing all users folders at once on cache invalidation with their folder sorts
        await callbackService.FolderRefreshed(
            currentUserRepository.CurrentUserId,
            currentDeviceRepository.DeviceId,
            folder);

        return fileExplorerResponseConverter.Convert(folder);
    }
}
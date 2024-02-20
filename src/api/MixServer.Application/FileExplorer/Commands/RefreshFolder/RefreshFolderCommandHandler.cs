using FluentValidation;
using MixServer.Application.FileExplorer.Queries.GetNode;
using MixServer.Domain.Callbacks;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.FileExplorer.Services.Caching;
using MixServer.Domain.Interfaces;
using MixServer.Domain.Users.Repositories;
using MixServer.Infrastructure.Users.Repository;

namespace MixServer.Application.FileExplorer.Commands.RefreshFolder;

public class RefreshFolderCommandHandler(
    ICurrentDeviceRepository currentDeviceRepository,
    ICurrentUserRepository currentUserRepository,
    ICallbackService callbackService,
    IFileExplorerResponseConverter fileExplorerResponseConverter,
    IFolderCacheService folderCacheService,
    IFileService fileService,
    IRootFileExplorerFolder rootFolder) : ICommandHandler<RefreshFolderCommand, FileExplorerFolderResponse>
{
    public async Task<FileExplorerFolderResponse> HandleAsync(RefreshFolderCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.AbsolutePath))
        {
            rootFolder.RefreshChildren();
        }
        else
        {
            folderCacheService.InvalidateFolder(request.AbsolutePath);
        }
        
        var folder = await fileService.GetFolderOrRootAsync(request.AbsolutePath);

        // TODO: make a way of refreshing all users folders at once on cache invalidation with their folder sorts
        await callbackService.FolderRefreshed(
            currentUserRepository.CurrentUserId,
            currentDeviceRepository.DeviceId,
            folder);

        return fileExplorerResponseConverter.Convert(folder);
    }
}
using MixServer.Application.FileExplorer.Queries.GetNode;
using MixServer.Domain.Callbacks;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Caching;
using MixServer.Domain.FileExplorer.Services;
using MixServer.Domain.FileExplorer.Services.Caching;
using MixServer.Domain.Interfaces;

namespace MixServer.Application.FileExplorer.Services;

public interface IFileNotificationService
{
    void Initialize();
}

public class FileNotificationService(
    ICallbackService callbackService,
    IFileService fileService,
    IFileSystemInfoConverter fileSystemInfoConverter,
    IFolderCacheService folderCacheService)
    : IFileNotificationService
{
    public void Initialize()
    {
        folderCacheService.ItemAdded += FolderCacheServiceOnItemAdded;
        folderCacheService.ItemUpdated += FolderCacheServiceOnItemUpdated;
        folderCacheService.ItemRemoved += FolderCacheServiceOnItemRemoved;
    }

    private async void FolderCacheServiceOnItemAdded(object? sender, FolderCacheServiceItemAddedEventArgs e)
    {
        IFileExplorerNode node;
        switch (e.Item)
        {
            case ICacheDirectoryInfo directoryInfo:
                node = fileSystemInfoConverter.ConvertToFolderNode(directoryInfo, fileService.IsChildOfRoot(directoryInfo.FullName));
                break;
            case ICacheFileInfo fileInfo:
                var parent =
                    fileSystemInfoConverter.ConvertToFolderNode(e.Parent, fileService.IsChildOfRoot(e.Parent.FullName));
                node = fileSystemInfoConverter.ConvertToFileNode(fileInfo, parent);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e));
        }
        
        var dto = nodeResponseConverter.Convert(node);
        
        await callbackService.FileExplorerNodeAdded(dto);
    }

    private void FolderCacheServiceOnItemUpdated(object? sender, FolderCacheServiceItemUpdatedEventArgs e)
    {
    }

    private void FolderCacheServiceOnItemRemoved(object? sender, FolderCacheServiceItemRemovedEventArgs e)
    {
    }
}
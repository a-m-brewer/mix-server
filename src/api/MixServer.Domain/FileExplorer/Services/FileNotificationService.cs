using MixServer.Domain.Callbacks;
using MixServer.Domain.FileExplorer.Converters;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Caching;
using MixServer.Domain.FileExplorer.Services.Caching;

namespace MixServer.Domain.FileExplorer.Services;

public interface IFileNotificationService
{
    void Initialize();
}

public class FileNotificationService(
    ICallbackService callbackService,
    IRootFolderService rootFolderService,
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
        var node = ConvertToNode(e);
        await callbackService.FileExplorerNodeAdded(node);
    }

    private async void FolderCacheServiceOnItemUpdated(object? sender, FolderCacheServiceItemUpdatedEventArgs e)
    {
        var node = ConvertToNode(e);
        await callbackService.FileExplorerNodeUpdated(node, e.OldFullName);
    }

    private async void FolderCacheServiceOnItemRemoved(object? sender, FolderCacheServiceItemRemovedEventArgs e)
    {
        var parentNode = ConvertToFolderNode(e.Parent);
        await callbackService.FileExplorerNodeDeleted(parentNode, e.FullName);
    }

    private IFileExplorerNode ConvertToNode(FolderCacheServiceItemAddedEventArgs e)
    {
        IFileExplorerNode node;
        switch (e.Item)
        {
            case ICacheDirectoryInfo directoryInfo:
                node = ConvertToFolderNode(directoryInfo);
                break;
            case ICacheFileInfo fileInfo:
                var parent = ConvertToFolderNode(e.Parent);
                node = fileSystemInfoConverter.ConvertToFileNode(fileInfo, parent);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e));
        }

        return node;
    }

    private IFileExplorerFolderNode ConvertToFolderNode(ICacheDirectoryInfo directoryInfo)
    {
        return fileSystemInfoConverter.ConvertToFolderNode(directoryInfo, rootFolderService.IsChildOfRoot(directoryInfo.FullName));
    }
}
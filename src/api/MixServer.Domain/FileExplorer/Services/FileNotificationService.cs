using MixServer.Domain.Callbacks;
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
    IFolderCacheService folderCacheService)
    : IFileNotificationService
{
    public void Initialize()
    {
        folderCacheService.ItemAdded += FolderCacheServiceOnItemAdded;
        folderCacheService.ItemUpdated += FolderCacheServiceOnItemUpdated;
        folderCacheService.ItemRemoved += FolderCacheServiceOnItemRemoved;
    }

    private async void FolderCacheServiceOnItemAdded(object? sender, IFileExplorerNode e)
    {
        await callbackService.FileExplorerNodeAdded(e);
    }

    private async void FolderCacheServiceOnItemUpdated(object? sender, FolderCacheServiceItemUpdatedEventArgs e)
    {
        await callbackService.FileExplorerNodeUpdated(e.Item, e.OldFullName);
    }

    private async void FolderCacheServiceOnItemRemoved(object? sender, FolderCacheServiceItemRemovedEventArgs e)
    {
        await callbackService.FileExplorerNodeDeleted(e.Parent, e.FullName);
    }
}
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Caching;
using MixServer.Domain.FileExplorer.Repositories;
using MixServer.Domain.FileExplorer.Services.Caching;

namespace MixServer.Services;

public class FolderMediaMetadataSubscriber(
    IFolderCacheService folderCacheService,
    IRemoveMediaMetadataChannel removeChannel,
    IUpdateMediaMetadataChannel updateChannel) : IHostedService
{

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        
        folderCacheService.FolderAdded += FolderCacheServiceOnFolderAdded;
        folderCacheService.FolderRemoved += FolderCacheServiceOnFolderRemoved;
        folderCacheService.ItemAdded += FolderCacheServiceOnItemAdded;
        folderCacheService.ItemUpdated += FolderCacheServiceOnItemUpdated;
        folderCacheService.ItemRemoved += FolderCacheServiceOnItemRemoved;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        
        folderCacheService.FolderAdded -= FolderCacheServiceOnFolderAdded;
        folderCacheService.FolderRemoved -= FolderCacheServiceOnFolderRemoved;
        folderCacheService.ItemAdded -= FolderCacheServiceOnItemAdded;
        folderCacheService.ItemUpdated -= FolderCacheServiceOnItemUpdated;
        folderCacheService.ItemRemoved -= FolderCacheServiceOnItemRemoved;
    }
    
    private void FolderCacheServiceOnFolderAdded(object? sender, IFileExplorerFolder e)
    {
        foreach (var file in e.Children
                     .OfType<IFileExplorerFileNode>()
                     .Where(w => w.Metadata.IsMedia))
        {
            _ = updateChannel.WriteAsync(new UpdateMediaMetadataRequest(file.Path));
        }
    }

    private void FolderCacheServiceOnFolderRemoved(object? sender, IFileExplorerFolder e)
    {
        foreach (var file in e.Children
                     .OfType<IFileExplorerFileNode>()
                     .Where(w => w.Metadata.IsMedia))
        {
            _ = removeChannel.WriteAsync(new RemoveMediaMetadataRequest(file.Path));
        }
    }

    private void FolderCacheServiceOnItemAdded(object? sender, IFileExplorerNode e)
    {
        if (e is not IFileExplorerFileNode fileNode || !fileNode.Metadata.IsMedia)
        {
            return;
        }

        _ = updateChannel.WriteAsync(new UpdateMediaMetadataRequest(fileNode.Path));
    }
    
    private void FolderCacheServiceOnItemUpdated(object? sender, FolderCacheServiceItemUpdatedEventArgs e)
    {
        if (e.Item is not IFileExplorerFileNode fileNode || !fileNode.Metadata.IsMedia)
        {
            return;
        }
        
        if (!e.OldPath.IsEqualTo(e.Item.Path))
        {
            _ = removeChannel.WriteAsync(new RemoveMediaMetadataRequest(e.OldPath));
        }
        
        _ = updateChannel.WriteAsync(new UpdateMediaMetadataRequest(fileNode.Path));
    }
    
    private void FolderCacheServiceOnItemRemoved(object? sender, FolderCacheServiceItemRemovedEventArgs e)
    {
        if (e.Node is not IFileExplorerFileNode fileNode || !fileNode.Metadata.IsMedia)
        {
            return;
        }
        
        _ = removeChannel.WriteAsync(new RemoveMediaMetadataRequest(fileNode.Path));
    }
}
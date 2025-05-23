using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Callbacks;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Caching;
using MixServer.Domain.FileExplorer.Services.Caching;
using MixServer.Domain.Sessions.Repositories;
using MixServer.Domain.Streams.Caches;
using MixServer.Domain.Streams.Events;
using MixServer.Domain.Utilities;

namespace MixServer.Domain.FileExplorer.Services;

public interface IFileNotificationService : INotificationService
{
}

public class FileNotificationService(
    ICallbackService callbackService,
    IFolderCacheService folderCacheService,
    ILogger<FileNotificationService> logger,
    IServiceProvider serviceProvider,
    ITranscodeCache transcodeCache)
    : NotificationService<FileNotificationService>(logger, serviceProvider), IFileNotificationService
{
    public override void Initialize()
    {
        folderCacheService.ItemAdded += CreateHandler<IFileExplorerNode>(FolderCacheServiceOnItemAdded);
        folderCacheService.ItemUpdated += CreateHandler<FolderCacheServiceItemUpdatedEventArgs>(FolderCacheServiceOnItemUpdated);
        folderCacheService.ItemRemoved += CreateHandler<FolderCacheServiceItemRemovedEventArgs>(FolderCacheServiceOnItemRemoved);
        transcodeCache.TranscodeStatusUpdated += CreateHandler<TranscodeStatusUpdatedEventArgs>(TranscodeCacheOnTranscodeStatusUpdated);
    }

    private async Task FolderCacheServiceOnItemAdded(object? sender, IServiceProvider sp, IFileExplorerNode e)
    {
        if (!IsFileExplorerFolder(sender, out var parent))
        {
            return;
        }

        var expectedIndexes = await GetExpectedIndexesAsync(sp, parent, e);
        
        await callbackService.FileExplorerNodeUpdated(expectedIndexes, e, null);
    }

    private async Task FolderCacheServiceOnItemUpdated(object? sender, IServiceProvider sp, FolderCacheServiceItemUpdatedEventArgs e)
    {
        if (!IsFileExplorerFolder(sender, out var parent))
        {
            return;
        }
        
        var expectedIndexes = await GetExpectedIndexesAsync(sp, parent, e.Item);
        
        await callbackService.FileExplorerNodeUpdated(expectedIndexes, e.Item, e.OldPath);
    }

    private async Task FolderCacheServiceOnItemRemoved(object? sender, IServiceProvider sp, FolderCacheServiceItemRemovedEventArgs e)
    {
        await callbackService.FileExplorerNodeDeleted(e.Parent, e.Path);
    }
    
    private async Task TranscodeCacheOnTranscodeStatusUpdated(object? sender, IServiceProvider sp, TranscodeStatusUpdatedEventArgs e)
    {
        var (parent, file) = folderCacheService.GetFileAndFolder(e.Path);
        
        var expectedIndexes = await GetExpectedIndexesAsync(sp, parent, file);
        
        await callbackService.FileExplorerNodeUpdated(expectedIndexes, file, null);
    }
    
    private bool IsFileExplorerFolder(object? sender, [MaybeNullWhen(false)] out IFileExplorerFolder parent)
    {
        if (sender is not IFileExplorerFolder fileExplorerFolder)
        {
            Logger.LogWarning("event raised by non-folder item Sender: {Sender}",
                sender?.GetType().Name ?? "null");
            parent = null;
            return false;
        }

        parent = fileExplorerFolder;
        return true;
    }

    private async Task<Dictionary<string, int>> GetExpectedIndexesAsync(IServiceProvider sp, IFileExplorerFolder parent, IFileExplorerNode node)
    {
        var sorts = await sp.GetRequiredService<IFolderSortRepository>()
            .GetFolderSortsAsync(callbackService.ConnectedUserIds, parent.Node.Path.AbsolutePath);

        var expectedIndexes = new Dictionary<string, int>();
        foreach (var (userId, sort) in sorts)
        {
            var list = parent.GenerateSortedChildren<IFileExplorerNode>(sort).ToList();
            var index = list.FindIndex(f => f.Path.RootPath == node.Path.RootPath && f.Path.RelativePath == node.Path.RelativePath);
            expectedIndexes[userId] = index;
        }
        
        return expectedIndexes;
    }
}
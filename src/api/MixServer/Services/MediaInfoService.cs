using MixServer.Domain.Callbacks;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Models.Caching;
using MixServer.Domain.FileExplorer.Models.Metadata;
using MixServer.Domain.FileExplorer.Services.Caching;
using MixServer.Domain.Tracklists.Services;

// ReSharper disable AsyncVoidMethod

namespace MixServer.Services;

public class MediaInfoService(
    ILogger<MediaInfoService> logger,
    IMediaInfoCache mediaInfoCache,
    IServiceProvider serviceProvider,
    // ITagBuilderFactory tagBuilderFactory,
    ITracklistTagService tracklistTagService) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        
        // folderCacheService.FolderAdded += FolderCacheServiceOnFolderAdded;
        // folderCacheService.FolderRemoved += FolderCacheServiceOnFolderRemoved;
        // folderCacheService.ItemAdded += FolderCacheServiceOnItemAdded;
        // folderCacheService.ItemUpdated += FolderCacheServiceOnItemUpdated;
        // folderCacheService.ItemRemoved += FolderCacheServiceOnItemRemoved;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        
        // folderCacheService.FolderAdded -= FolderCacheServiceOnFolderAdded;
        // folderCacheService.FolderRemoved -= FolderCacheServiceOnFolderRemoved;
        // folderCacheService.ItemAdded -= FolderCacheServiceOnItemAdded;
        // folderCacheService.ItemUpdated -= FolderCacheServiceOnItemUpdated;
        // folderCacheService.ItemRemoved -= FolderCacheServiceOnItemRemoved;
    }
    
    private async void FolderCacheServiceOnFolderAdded(object? sender, IFileExplorerFolder e)
    {
        var mediaFiles = e.Children
            .OfType<IFileExplorerFileNode>()
            .Where(w => w.Metadata.IsMedia)
            .ToList();
        await LoadMediaInfoAsync(mediaFiles);
    }

    private async void FolderCacheServiceOnFolderRemoved(object? sender, IFileExplorerFolder e)
    {
        var mediaFiles = e.Children
            .OfType<IFileExplorerFileNode>()
            .Where(w => w.Metadata.IsMedia);
        await RemoveMediaInfoAsync(mediaFiles);
    }

    private async void FolderCacheServiceOnItemAdded(object? sender, IFileExplorerNode e)
    {
        if (e is not IFileExplorerFileNode fileNode || !fileNode.Metadata.IsMedia)
        {
            return;
        }
        
        await LoadMediaInfoAsync([fileNode]);
    }
    
    private async void FolderCacheServiceOnItemUpdated(object? sender, FolderCacheServiceItemUpdatedEventArgs e)
    {
        if (e.OldFullName != e.Item.AbsolutePath)
        {
            await RemoveMediaInfoAsync([e.OldFullName]);
        }
        
        if (e.Item is not IFileExplorerFileNode fileNode || !fileNode.Metadata.IsMedia)
        {
            return;
        }
        
        await LoadMediaInfoAsync([fileNode]);
    }
    
    private async void FolderCacheServiceOnItemRemoved(object? sender, FolderCacheServiceItemRemovedEventArgs e)
    {
        await RemoveMediaInfoAsync([e.FullName]);
    }
    
    private async Task LoadMediaInfoAsync(ICollection<IFileExplorerFileNode> mediaFiles)
    {
        try
        {
            logger.LogInformation("Loading media metadata for {Count} media files", mediaFiles.Count());
            
            var mediaInfo = new List<MediaInfo>();
            foreach (var chunk in mediaFiles.Chunk(10))
            {
                var tasks = chunk.Select(LoadMediaInfoFromFileAsync);
                var mediaInfos = await Task.WhenAll(tasks);
                mediaInfo.AddRange(mediaInfos.Where(w => w is not null).Select(s => s!));
            }
            
            logger.LogInformation("Loaded {Count} media metadata items", mediaInfo.Count);
            mediaInfoCache.AddOrReplace(mediaInfo);
            
            await InvokeCallback(s => s.MediaInfoUpdated(mediaInfo));
        } 
        catch (Exception e)
        {
            logger.LogError(e, "Error loading media metadata");
        }
    }

    private Task RemoveMediaInfoAsync(IEnumerable<IFileExplorerFileNode> mediaFiles)
    {
        return RemoveMediaInfoAsync(mediaFiles.Select(s => s.AbsolutePath));
    }
    
    private async Task RemoveMediaInfoAsync(IEnumerable<string> absoluteFilePaths)
    {
        var removedItems = mediaInfoCache.Remove(absoluteFilePaths);
        await InvokeCallback(cb => cb.MediaInfoRemoved(removedItems));
    }
    
    private Task<MediaInfo?> LoadMediaInfoFromFileAsync(IFileExplorerFileNode arg)
    {
        throw new NotImplementedException();
        try
        {
            // using var tb = tagBuilderFactory.CreateReadOnly(arg.AbsolutePath);
            // var tracklist = tracklistTagService.GetTracklist(tb);
            // var mediaInfo = new MediaInfo
            // {
            //     NodePath = new NodePath(arg.Parent.AbsolutePath, arg.Name),
            //     Bitrate = tb.Bitrate,
            //     Duration = tb.Duration,
            //     Tracklist = tracklist
            // };
            //
            // return Task.FromResult<MediaInfo?>(mediaInfo);
        } 
        catch (Exception e)
        {
            logger.LogError(e, "Error loading media metadata for {Path}", arg.AbsolutePath);
            return Task.FromResult<MediaInfo?>(null);
        }
    }

    private async Task InvokeCallback(Func<ICallbackService, Task> callback)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var callbackService = scope.ServiceProvider.GetRequiredService<ICallbackService>();
            await callback(callbackService);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error invoking callback");
        }
    }
}
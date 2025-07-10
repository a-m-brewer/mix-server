using DebounceThrottle;
using MixServer.Domain.Callbacks;
using MixServer.Domain.FileExplorer.Repositories;

namespace MixServer.Services;

public class FolderScanTrackingService(
    ILogger<FolderScanTrackingService> logger,
    IScanFolderRequestChannel scanChannel,
    IFolderScanTrackingStore folderScanTrackingStore,
    IPersistFolderCommandChannel persistChannel,
    IRemoveMediaMetadataChannel removeMediaMetadataChannel,
    IServiceProvider serviceProvider,
    IUpdateMediaMetadataChannel updateMediaMetadataChannel) : IHostedService
{
    private readonly DebounceDispatcher _scanCompletionDebouncer = new(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        folderScanTrackingStore.ScanInProgressChanged += FolderScanTrackingStoreOnScanInProgressChanged;
        
        scanChannel.RequestsChanged += RequestsChanged;
        persistChannel.RequestsChanged += RequestsChanged;
        removeMediaMetadataChannel.RequestsChanged += RequestsChanged;
        updateMediaMetadataChannel.RequestsChanged += RequestsChanged;
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        scanChannel.RequestsChanged -= RequestsChanged;
        persistChannel.RequestsChanged -= RequestsChanged;
        removeMediaMetadataChannel.RequestsChanged -= RequestsChanged;
        updateMediaMetadataChannel.RequestsChanged -= RequestsChanged;
        
        folderScanTrackingStore.ScanInProgressChanged -= FolderScanTrackingStoreOnScanInProgressChanged;
        
        return Task.CompletedTask;
    }
    
    private void RequestsChanged(object? sender, EventArgs e)
    {
        if (GetCurrentRequestCount() > 0)
        {
            folderScanTrackingStore.ScanInProgress = true;
        }
        else
        {
            _scanCompletionDebouncer.Debounce(() =>
            {
                if (GetCurrentRequestCount() != 0)
                {
                    logger.LogDebug("Folder scan completion debounced, but requests are still in flight.");
                    return;
                }

                folderScanTrackingStore.ScanInProgress = false;
                logger.LogInformation("Folder scan completed, no requests in flight elapsed: {Elapsed}",
                    folderScanTrackingStore.ScanDuration);
            });
        }
    }

    private int GetCurrentRequestCount()
    {
        var scans = scanChannel.Requests;
        var persist = persistChannel.Requests;
        var removeMetadataRequests = removeMediaMetadataChannel.Requests;
        var updateMetadataRequests = updateMediaMetadataChannel.Requests;
        
        logger.LogDebug("In flight folder scan Scans: {ScanCount}, Persists: {PersistCount}, Remove Metadata: {RemoveMetadataCount}, Update Metadata: {UpdateMetadataCount}",
            scans.Count, 
            persist.Count, 
            removeMetadataRequests.Count,
            updateMetadataRequests.Count);
        
        return scans.Count + persist.Count + removeMetadataRequests.Count + updateMetadataRequests.Count;
    }
    
    private async void FolderScanTrackingStoreOnScanInProgressChanged(object? sender, EventArgs e)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ICallbackService>()
                .FolderScanStatusChanged(folderScanTrackingStore.ScanInProgress);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing scan in progress change.");
        }
    }
}
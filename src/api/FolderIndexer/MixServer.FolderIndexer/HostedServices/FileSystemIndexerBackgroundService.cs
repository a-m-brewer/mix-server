using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MixServer.FolderIndexer.Persistence.InMemory;
using MixServer.FolderIndexer.Services;

namespace MixServer.FolderIndexer.HostedServices;

internal class FileSystemIndexerBackgroundService(
    FileSystemIndexerChannelStore channelStore,
    ILogger<FileSystemIndexerBackgroundService> logger,
    IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = Enumerable.Range(0, Environment.ProcessorCount).Select(_ => ListenAsync(stoppingToken));
        
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task ListenAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested == false && 
               await channelStore.ScannerChannel.Reader.WaitToReadAsync(stoppingToken))
        {
            while (channelStore.ScannerChannel.Reader.TryRead(out var dir))
            {
                try
                {
                    using var scope = serviceProvider.CreateScope();
                    var fileSystemScannerService = scope.ServiceProvider.GetRequiredService<IFileSystemScannerService>();
                    await fileSystemScannerService.ScanAsync(dir, stoppingToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error while scanning directory: {Directory}", dir);
                }
            }
        }
    }
}
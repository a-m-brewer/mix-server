using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MixServer.FolderIndexer.Domain.Repositories;
using MixServer.FolderIndexer.Persistence.InMemory;
using MixServer.FolderIndexer.Services;

namespace MixServer.FolderIndexer.HostedServices;

internal class FieSystemIndexerPersistenceBackgroundService(
    FileSystemIndexerChannelStore channelStore,
    ILogger<FieSystemIndexerPersistenceBackgroundService> logger,
    IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeRootsAsync(stoppingToken).ConfigureAwait(false);
        await StartScanningRootsAsync(stoppingToken).ConfigureAwait(false);

        var tasks = Enumerable.Range(0, Environment.ProcessorCount).Select(_ => ListenAsync(stoppingToken));

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task InitializeRootsAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var fileSystemRootPersistenceService =
            scope.ServiceProvider.GetRequiredService<IFileSystemRootPersistenceService>();
        await fileSystemRootPersistenceService.InitializeAsync(stoppingToken).ConfigureAwait(false);
    }
    
    private async Task StartScanningRootsAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var fileSystemInfoRepository =
            scope.ServiceProvider.GetRequiredService<IFileSystemInfoRepository>();

        var roots = await fileSystemInfoRepository
            .GetAllRootFoldersAsync(stoppingToken)
            .ConfigureAwait(false);
        
        foreach (var root in roots)
        {
            await channelStore.ScannerChannel.Writer.WriteAsync(root.RelativePath, stoppingToken)
                .ConfigureAwait(false);
        }
    }

    private async Task ListenAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested == false &&
               await channelStore.FileSystemInfoChannel.Reader.WaitToReadAsync(stoppingToken))
        {
            while (channelStore.FileSystemInfoChannel.Reader.TryRead(out var dir))
            {
                try
                {
                    var (directory, children) = dir;
                    using var scope = serviceProvider.CreateScope();
                    var fileSystemPersistenceService =
                        scope.ServiceProvider.GetRequiredService<IFileSystemPersistenceService>();
                    await fileSystemPersistenceService.AddOrUpdateFolderAsync(directory, children, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while processing directory {Directory}", dir);
                }
            }
        }
    }
}
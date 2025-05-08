using MixServer.Domain.FileExplorer.Services.Indexing;

namespace MixServer.Services;

public class FieSystemIndexerPersistenceBackgroundService(
    FileSystemIndexerChannelStore channelStore,
    ILogger<FieSystemIndexerPersistenceBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = Enumerable.Range(0, Environment.ProcessorCount).Select(_ => ListenAsync(stoppingToken));
        
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task ListenAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested == false && 
               await channelStore.FileSystemInfoChannel.Reader.WaitToReadAsync(stoppingToken))
        {
            while (channelStore.FileSystemInfoChannel.Reader.TryRead(out var dir))
            {
                var (directory, children) = dir;
                logger.LogInformation("Processing directory: {Directory} found {Count}", directory.FullName, children.Count);
            }
        }
    }
}
using MixServer.Domain.FileExplorer.Services.Indexing;

namespace MixServer.Services;

public class FieSystemIndexerPersistenceBackgroundService(
    FileSystemIndexerChannelStore channelStore,
    ILogger<FieSystemIndexerPersistenceBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var (parent, children) in channelStore.FileSystemInfoChannel.Reader.ReadAllAsync(stoppingToken))
        {
            logger.LogInformation("Found {Count} items in {Path}", children.Count, parent.FullName);
        }
    }
}
using System.Collections.Concurrent;
using MixServer.Domain.FileExplorer.Repositories;
using MixServer.Domain.FileExplorer.Services;

namespace MixServer.Services;

public class RootChildDirectoryWatcherService(
    IRootChildDirectoryChangeChannel channel,
    ILogger<RootChildDirectoryWatcherService> logger,
    ILoggerFactory loggerFactory,
    IServiceProvider serviceProvider) : IHostedService
{
    private readonly ConcurrentDictionary<string, RootChildDirectoryWatcher> _watchers = new();
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting root child directory watchers...");
        
        using var scope = serviceProvider.CreateScope();
        var rootChildren = await scope.ServiceProvider.GetRequiredService<IRootChildFolderService>()
            .SyncRootChildFoldersAsync(cancellationToken);

        foreach (var rootChild in rootChildren)
        {
            var watcher = new RootChildDirectoryWatcher(
                rootChild.RelativePath,
                loggerFactory.CreateLogger<RootChildDirectoryWatcher>(),
                channel);
            _watchers[rootChild.RelativePath] = watcher;
        }
        
        logger.LogInformation("Root child directory watchers started for {Count} directories", _watchers.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var watcher in _watchers.Values)
        {
            watcher.Dispose();
        }
        
        channel.Complete();
        
        logger.LogInformation("Root child directory watchers stopped");

        return Task.CompletedTask;
    }
}
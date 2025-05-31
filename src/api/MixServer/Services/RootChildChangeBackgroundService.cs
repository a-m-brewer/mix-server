using MixServer.Domain.FileExplorer.Models.Indexing;
using MixServer.Domain.FileExplorer.Repositories;

namespace MixServer.Services;

public class RootChildChangeBackgroundService(
    IRootChildDirectoryChangeChannel channel,
    ILogger<RootChildChangeBackgroundService> logger,
    IServiceProvider serviceProvider)
    : ChannelBackgroundService<RootChildChangeEvent>(channel, serviceProvider, logger, workers: 1); // Only one worker as order is important
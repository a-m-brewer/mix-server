using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories;

namespace MixServer.Services;

public class PersistFolderBackgroundService(
    IPersistFolderCommandChannel requestChannel,
    IServiceProvider serviceProvider,
    ILogger<ScanFolderBackgroundService> logger) : ChannelBackgroundService<PersistFolderCommand>(requestChannel, serviceProvider, logger);
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories;

namespace MixServer.Services;

public class ScanFolderBackgroundService(
    IScanFolderRequestChannel requestChannel,
    IServiceProvider serviceProvider,
    ILogger<ScanFolderBackgroundService> logger) : ChannelBackgroundService<ScanFolderRequest>(requestChannel, serviceProvider, logger, 1);
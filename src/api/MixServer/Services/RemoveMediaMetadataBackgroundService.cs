using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories;

namespace MixServer.Services;

public class RemoveMediaMetadataBackgroundService(
    IRemoveMediaMetadataChannel channel,
    IServiceProvider serviceProvider,
    ILogger<RemoveMediaMetadataBackgroundService> logger) : ChannelBackgroundService<RemoveMediaMetadataRequest>(channel, serviceProvider, logger, 1);
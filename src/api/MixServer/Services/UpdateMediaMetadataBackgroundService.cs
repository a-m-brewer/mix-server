using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories;

namespace MixServer.Services;

public class UpdateMediaMetadataBackgroundService(
    IUpdateMediaMetadataChannel channel,
    IServiceProvider serviceProvider,
    ILogger<UpdateMediaMetadataBackgroundService> logger) : ChannelBackgroundService<UpdateMediaMetadataRequest>(channel, serviceProvider, logger, 1);
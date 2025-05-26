using Microsoft.Extensions.Options;
using MixServer.Domain.Settings;
using MixServer.Domain.Streams.Models;
using MixServer.Domain.Streams.Repositories;

namespace MixServer.Services;

public class TranscodeBackgroundService(
    IOptions<CacheFolderSettings> cacheFolderSettings,
    ILogger<TranscodeBackgroundService> logger,
    ITranscodeChannel transcodeChannel,
    IServiceProvider serviceProvider
    ) : ChannelBackgroundService<TranscodeRequest>(transcodeChannel, serviceProvider, logger, cacheFolderSettings.Value.TranscodeWorkers);
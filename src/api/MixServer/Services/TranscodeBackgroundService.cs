using Microsoft.Extensions.Options;
using MixServer.Domain.Settings;
using MixServer.Domain.Streams.Models;
using MixServer.Domain.Streams.Repositories;
using MixServer.Domain.Streams.Services;

namespace MixServer.Services;

public class TranscodeBackgroundService(
    IOptions<CacheFolderSettings> cacheFolderSettings,
    ILogger<TranscodeBackgroundService> logger,
    ITranscodeChannel transcodeChannel,
    IServiceProvider serviceProvider
    ) : ChannelBackgroundService<TranscodeRequest>(transcodeChannel, serviceProvider, logger, cacheFolderSettings.Value.TranscodeWorkers)
{
    protected override async Task ProcessRequestAsync(TranscodeRequest request, IServiceProvider serviceProvider, CancellationToken stoppingToken)
    {
        var transcodeWorkerService = serviceProvider.GetRequiredService<ITranscodeWorkerService>();
        await transcodeWorkerService.ProcessTranscodeRequestAsync(request, stoppingToken);
    }
}
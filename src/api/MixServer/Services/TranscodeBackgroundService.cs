using Microsoft.Extensions.Options;
using MixServer.Domain.Settings;
using MixServer.Domain.Streams.Repositories;
using MixServer.Domain.Streams.Services;

namespace MixServer.Services;

public class TranscodeBackgroundService(
    IOptions<CacheFolderSettings> cacheFolderSettings,
    ILogger<TranscodeBackgroundService> logger,
    ITranscodeChannel transcodeChannel,
    IServiceProvider serviceProvider
    ) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = Enumerable
            .Range(0, cacheFolderSettings.Value.TranscodeWorkers)
            .Select(_ => ListenAsync(stoppingToken));
        
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task ListenAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (stoppingToken.IsCancellationRequested == false &&
                   await transcodeChannel.WaitToReadAsync(stoppingToken))
            {
                while (transcodeChannel.TryRead(out var request))
                {
                    try
                    {
                        using var scope = serviceProvider.CreateScope();
                        var transcodeWorkerService =
                            scope.ServiceProvider.GetRequiredService<ITranscodeWorkerService>();
                        await transcodeWorkerService.ProcessTranscodeRequestAsync(request, stoppingToken);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Error while transcode directory: {Directory}", request);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // This is expected when the service is stopped.
            logger.LogInformation("Transcode background service stopped");
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred in the transcode background service");
        }
    }
}
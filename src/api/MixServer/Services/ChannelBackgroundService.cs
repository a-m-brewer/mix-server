using MixServer.Domain.Interfaces;

namespace MixServer.Services;

public abstract class ChannelBackgroundService<T>(
    IChannel<T> channel,
    IServiceProvider serviceProvider,
    ILogger logger,
    int? workers = null)
    : BackgroundService where T : notnull
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = Enumerable
            .Range(0, workers ?? Environment.ProcessorCount)
            .Select(_ => ListenAsync(stoppingToken));
        
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task ListenAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (stoppingToken.IsCancellationRequested == false &&
                   await channel.WaitToReadAsync(stoppingToken))
            {
                while (channel.TryRead(out var request))
                {
                    try
                    {
                        using var scope = serviceProvider.CreateScope();
                        await scope.ServiceProvider.GetRequiredService<ICommandHandler2<T>>()
                            .HandleAsync(request.Request, stoppingToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Error while transcode directory: {Directory}", request.Request);
                    }
                    finally
                    {
                        request.Dispose();
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
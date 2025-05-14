namespace MixServer.Services;

public abstract class HostedLifecycleServiceBase : IHostedLifecycleService
{
    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public virtual Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public virtual Task StartedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public virtual Task StartingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public virtual Task StoppedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public virtual Task StoppingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
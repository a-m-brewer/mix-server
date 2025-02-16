using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MixServer.Domain.Utilities;

public interface INotificationService
{
    void Initialize();
}

public abstract class NotificationService<T>(
    ILogger<T> logger,
    IServiceProvider serviceProvider) : INotificationService
{
    protected ILogger<T> Logger { get; } = logger;

    protected IServiceProvider ServiceProvider { get; } = serviceProvider;
    
    public abstract void Initialize();
    
    protected EventHandler<TEvent> CreateHandler<TEvent>(Func<object?, IServiceProvider, TEvent, Task> handler)
    {
        return (sender, e) => HandleEventFunc(sender, e, handler);
    }
    
    private async void HandleEventFunc<TEvent>(object? sender, TEvent e, Func<object?, IServiceProvider, TEvent, Task> handler)
    {
        try
        {
            using var scope = ServiceProvider.CreateScope();
            await handler(sender, scope.ServiceProvider, e);
        } catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling event {EventType}", typeof(TEvent).Name);
        }
    }
}
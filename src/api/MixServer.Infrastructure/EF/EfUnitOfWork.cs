using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Callbacks;
using MixServer.Domain.Persistence;

namespace MixServer.Infrastructure.EF;

public class EfUnitOfWork<TDbContext>(
    ICallbackService callbackService,
    TDbContext context,
    ILogger<EfUnitOfWork<TDbContext>> logger,
    IServiceProvider serviceProvider)
    : IUnitOfWork
    where TDbContext : DbContext
{
    private readonly List<Expression<Func<CancellationToken, Task>>> _deferredCommands = [];

    public TRepository GetRepository<TRepository>() where TRepository : IRepository =>
        serviceProvider.GetRequiredService<TRepository>();

    public void OnSaved(Expression<Func<CancellationToken, Task>> command) => _deferredCommands.Add(command);
    public void OnSaved(Expression<Func<Task>> command) =>
        OnSaved(cancellationToken => command.Compile().Invoke());

    public void OnSaved(Expression<Action> command)
    {
        OnSaved(() => InvokeSyncOnSaved(command));
    }

    private static Task InvokeSyncOnSaved(Expression<Action> command)
    {
        command.Compile().Invoke();
        return Task.CompletedTask;
    }

    public void InvokeCallbackOnSaved(Func<ICallbackService, Task> callback) =>
        OnSaved(() => callback.Invoke(callbackService));

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);

        await Task.WhenAll(_deferredCommands.Select(async s =>
        {
            try
            {
                await s.Compile().Invoke(cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error running deferred command");
            }
        }));
    }
}

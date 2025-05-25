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
    private readonly List<Expression<Func<Task>>> _deferredCommands = [];

    public TRepository GetRepository<TRepository>() where TRepository : IRepository =>
        serviceProvider.GetRequiredService<TRepository>();

    public void OnSaved(Expression<Func<Task>> command) => _deferredCommands.Add(command);
    
    public void InvokeCallbackOnSaved(Func<ICallbackService, Task> callback)
    {
        OnSaved(() => callback.Invoke(callbackService));
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);

        await Task.WhenAll(_deferredCommands.Select(async s =>
        {
            try
            {
                await s.Compile().Invoke();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error running deferred command");
            }
        }));
    }
}

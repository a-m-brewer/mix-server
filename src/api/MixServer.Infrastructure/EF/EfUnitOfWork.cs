using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MixServer.Domain.Persistence;

namespace MixServer.Infrastructure.EF;

public class EfUnitOfWork<TDbContext>(
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

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();

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

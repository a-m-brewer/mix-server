using System.Linq.Expressions;
using MixServer.Domain.Callbacks;

namespace MixServer.Domain.Persistence;

public interface IUnitOfWork
{
    TRepository GetRepository<TRepository>() where TRepository : IRepository;
    void OnSaved(Expression<Func<CancellationToken, Task>> command);
    void OnSaved(Expression<Func<Task>> command);
    void InvokeCallbackOnSaved(Func<ICallbackService, Task> callback);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
using System.Linq.Expressions;

namespace MixServer.Domain.Persistence;

public interface IUnitOfWork
{
    TRepository GetRepository<TRepository>() where TRepository : IRepository;
    void OnSaved(Expression<Func<Task>> command);
    Task SaveChangesAsync();
}
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Persistence;

namespace MixServer.Domain.Sessions.Repositories;

public interface IFolderExplorerNodeEntityRepository : ITransientRepository
{
    Task<TEntity?> GetOrDefaultAsync<TEntity>(NodePath nodePath, CancellationToken cancellationToken)
        where TEntity : FileExplorerNodeEntity;

    Task<FileExplorerRootChildNodeEntity?> GetRootChildOrDefaultAsync(NodePath rootChild, CancellationToken cancellationToken);
    Task AddAsync(FileExplorerNodeEntityBase nodeEntity, CancellationToken cancellationToken);
}
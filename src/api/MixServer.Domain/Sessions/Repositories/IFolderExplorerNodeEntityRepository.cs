using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Persistence;

namespace MixServer.Domain.Sessions.Repositories;

public interface IFolderExplorerNodeEntityRepository : ITransientRepository
{
    Task<TEntity?> GetOrDefaultAsync<TEntity>(NodePath nodePath, CancellationToken cancellationToken)
        where TEntity : FileExplorerNodeEntity;
    Task<List<FileExplorerNodeEntity>> GetNodesAsync(
        string rootPath,
        List<string> relativePaths,
        CancellationToken cancellationToken);
    Task<FolderHeader?> GetFolderHeaderOrDefaultAsync(FolderHeader header, CancellationToken cancellationToken);
    Task<Dictionary<NodePath, FolderHeader>> GetFolderHeadersAsync(List<NodePath> childNodePaths, CancellationToken cancellationToken);
    Task<FileExplorerFolderNodeEntity?> GetFolderNodeOrDefaultAsync(
        Guid id, 
        bool includeChildren = true,
        CancellationToken cancellationToken = default);
    Task<FileExplorerFolderNodeEntity?> GetFolderNodeOrDefaultAsync(
        NodePath nodePath, 
        bool includeChildren = true,
        CancellationToken cancellationToken = default);
    Task<FileExplorerRootChildNodeEntity> GetRootChildOrThrowAsync(string rootPath, CancellationToken cancellationToken);
    Task<FileExplorerRootChildNodeEntity?> GetRootChildOrDefaultAsync(NodePath rootChild, CancellationToken cancellationToken);
    Task<ICollection<FileExplorerRootChildNodeEntity>> GetAllRootChildrenAsync(CancellationToken cancellationToken);
    Task AddAsync(FileExplorerNodeEntityBase nodeEntity, CancellationToken cancellationToken);
    void RemoveRange(IEnumerable<FileExplorerNodeEntity> nodes);
}
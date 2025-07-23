using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories.DbQueryOptions;
using MixServer.Domain.Persistence;

namespace MixServer.Domain.FileExplorer.Repositories;

public interface IFileExplorerNodeRepository : ITransientRepository
{
    Task<FileExplorerFileNodeEntity> GetFileNodeAsync(NodePath nodePath, GetFileQueryOptions options, CancellationToken cancellationToken);
    Task<ICollection<FileExplorerFileNodeEntity>> GetFileNodesAsync(IEnumerable<NodePath> nodePaths, GetFileQueryOptions options, CancellationToken cancellationToken);
    Task<ICollection<FileExplorerFileNodeEntity>> GetFileNodesAsync(NodePath parentNodePath, GetFileQueryOptions options, CancellationToken cancellationToken);
    Task<FileExplorerFileNodeEntity?> GetFileNodeOrDefaultAsync(NodePath nodePath, GetFileQueryOptions options, CancellationToken cancellationToken);
    Task<List<FileExplorerFileNodeEntity>> GetFileNodesAsync(
        string rootPath,
        IEnumerable<string> relativePaths,
        GetFileQueryOptions options,
        CancellationToken cancellationToken);
    Task<List<FileExplorerFolderNodeEntity>> GetFolderNodesAsync(string rootPath,
        IEnumerable<Guid> folderIds,
        GetFolderQueryOptions options,
        CancellationToken cancellationToken);
    Task<List<FileExplorerFolderNodeEntity>> GetFolderNodesAsync(IEnumerable<NodePath> nodePaths,
        CancellationToken cancellationToken);
    Task<List<FileExplorerFolderNodeEntity>> GetFolderNodesAsync(NodePath parentNodePath, GetFolderQueryOptions options, CancellationToken cancellationToken);
    Task<FileExplorerFolderNodeEntity?> GetFolderNodeOrDefaultAsync(NodePath nodePath, GetFolderQueryOptions options, CancellationToken cancellationToken);
    Task<FileExplorerRootChildNodeEntity?> GetRootChildFolderNodeOrDefaultAsync(NodePath nodePath, GetFolderQueryOptions options, CancellationToken cancellationToken);
    Task<FileExplorerFolderNodeEntity?> GetFolderNodeOrDefaultAsync(Guid nodeId, GetFolderQueryOptions options, CancellationToken cancellationToken);
    Task<FileExplorerRootChildNodeEntity?> GetRootChildFolderNodeOrDefaultAsync(Guid nodeId, GetFolderQueryOptions options, CancellationToken cancellationToken);
    Task<IEnumerable<FileExplorerFileNodeEntity>> GetFileNodesAsync(List<Guid> fileIds, CancellationToken cancellationToken);
    Task<FolderHeader?> GetFolderHeaderOrDefaultAsync(FolderHeader header, CancellationToken cancellationToken);
    Task<Dictionary<NodePath, FolderHeader>> GetFolderHeadersAsync(List<NodePath> childNodePaths, CancellationToken cancellationToken);
    Task<FileExplorerRootChildNodeEntity> GetRootChildOrThrowAsync(string rootPath, CancellationToken cancellationToken);
    Task<FileExplorerRootChildNodeEntity?> GetRootChildOrDefaultAsync(NodePath rootChild, CancellationToken cancellationToken);
    Task<ICollection<FileExplorerRootChildNodeEntity>> GetRootChildrenAsync(IEnumerable<string> rootPaths, CancellationToken cancellationToken);
    Task<ICollection<FileExplorerRootChildNodeEntity>> GetAllRootChildrenAsync(CancellationToken cancellationToken);
    Task AddAsync(FileExplorerNodeEntityBase nodeEntity, CancellationToken cancellationToken);
    Task AddRangeAsync(ICollection<FileExplorerNodeEntity> addedChildren, CancellationToken cancellationToken);
    void RemoveRange(IEnumerable<FileExplorerNodeEntity> nodes);
}
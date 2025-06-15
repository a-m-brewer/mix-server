using Microsoft.EntityFrameworkCore;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.FileExplorer.Repositories;
using MixServer.Domain.Sessions.Repositories;
using MixServer.Infrastructure.EF.Extensions;

namespace MixServer.Infrastructure.EF.Repositories;

public class EfFolderExplorerNodeEntityRepository(MixServerDbContext context) : IFolderExplorerNodeEntityRepository
{
    public async Task<TEntity?> GetOrDefaultAsync<TEntity>(NodePath nodePath, CancellationToken cancellationToken) where TEntity : FileExplorerNodeEntity
    {
        IQueryable<TEntity> query = context.Nodes
            .OfType<TEntity>()
            .Include(i => i.RootChild)
            .Include(i => i.Parent);
        
        if (typeof(TEntity).IsAssignableTo(typeof(FileExplorerFileNodeEntity)))
        {
            query = query
                .Cast<FileExplorerFileNodeEntity>()
                .Include(i => i.Transcode)
                .Cast<TEntity>();
        }
        
        return await query
            .FirstOrDefaultAsync(f => f.RelativePath == nodePath.RelativePath && f.RootChild.RelativePath == nodePath.RootPath, cancellationToken);
    }

    public Task<List<FileExplorerFileNodeEntity>> GetFileNodesAsync(string rootPath, IEnumerable<string> relativePaths, CancellationToken cancellationToken)
    {
        return context.Nodes
            .OfType<FileExplorerFileNodeEntity>()
            .IncludeParents()
            .Include(i => i.Metadata)
            .Where(w => w.RootChild.RelativePath == rootPath && relativePaths.Contains(w.RelativePath))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FileExplorerFileNodeEntity>> GetFileNodesAsync(List<Guid> fileIds, CancellationToken cancellationToken)
    {
        return await context.Nodes
            .OfType<FileExplorerFileNodeEntity>()
            .IncludeParents()
            .Include(i => i.Metadata)
            .Include(i => i.Tracklist)
            .Where(w => fileIds.Contains(w.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<List<FileExplorerFolderNodeEntity>> GetFolderNodesAsync(string rootPath, IEnumerable<Guid> folderIds,
        CancellationToken cancellationToken)
    {
        return context.Nodes
            .OfType<FileExplorerFolderNodeEntity>()
            .IncludeParents()
            .Where(w => w.RootChild.RelativePath == rootPath && folderIds.Contains(w.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<FolderHeader?> GetFolderHeaderOrDefaultAsync(
        FolderHeader header,
        CancellationToken cancellationToken)
    {
        if (header.NodePath.IsRoot)
        {
            return null; // Root folder has no hash
        }

        if (header.NodePath.IsRootChild)
        {
            return await context.Nodes.OfType<FileExplorerRootChildNodeEntity>()
                .Where(w => w.RelativePath == header.NodePath.RootPath || w.Hash == header.Hash)
                .Select(s => new FolderHeader
                {
                    NodePath = new NodePath(s.RelativePath, string.Empty),
                    Hash = s.Hash
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
        
        return await context.Nodes.OfType<FileExplorerFolderNodeEntity>()
            .Include(i => i.RootChild)
            .Where(w => 
                (w.RelativePath == header.NodePath.RelativePath && w.RootChild.RelativePath == header.NodePath.RootPath) ||
                w.Hash == header.Hash)
            .Select(s => new FolderHeader
            {
                NodePath = new NodePath(s.RootChild.RelativePath, s.RelativePath),
                Hash = s.Hash
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Dictionary<NodePath, FolderHeader>> GetFolderHeadersAsync(List<NodePath> childNodePaths,
        CancellationToken cancellationToken)
    {
        if (childNodePaths.Count == 0)
        {
            return [];
        }

        var pathPairs = childNodePaths
            .Select(np => new { np.RootPath, np.RelativePath })
            .ToList();

        return await context.Nodes.OfType<FileExplorerFolderNodeEntity>()
            .Include(i => i.RootChild)
            .Where(w => pathPairs.Contains(new { RootPath = w.RootChild.RelativePath, w.RelativePath }))
            .Select(s => new FolderHeader
            {
                NodePath = new NodePath(s.RootChild.RelativePath, s.RelativePath),
                Hash = s.Hash
            })
            .ToDictionaryAsync(k => k.NodePath, cancellationToken);
    }
    
    public async Task<FileExplorerFolderNodeEntity?> GetFolderNodeOrDefaultAsync(
        Guid id,
        bool includeChildren = true,
        CancellationToken cancellationToken = default)
    {
        return await GetFolderNodeQuery(includeChildren)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public Task<FileExplorerFolderNodeEntity?> GetFolderNodeOrDefaultAsync(NodePath nodePath, bool includeChildren = true, CancellationToken cancellationToken = default)
    {
        return GetFolderNodeQuery(includeChildren)
            .FirstOrDefaultAsync(f => f.RelativePath == nodePath.RelativePath && f.RootChild.RelativePath == nodePath.RootPath, cancellationToken);
    }

    private IQueryable<FileExplorerFolderNodeEntity> GetFolderNodeQuery(bool includeChildren)
    {
        var query = context.Nodes
            .OfType<FileExplorerFolderNodeEntity>()
            .IncludeParents();

        if (includeChildren)
        {
            query = query.Include(i => i.Children);
        }
        
        return query;
    }

    public async Task<FileExplorerRootChildNodeEntity> GetRootChildOrThrowAsync(string rootPath, CancellationToken cancellationToken)
    {
        var entity = await GetRootChildOrDefaultAsync(new NodePath(rootPath, string.Empty), cancellationToken);

        if (entity is null)
        {
            throw new NotFoundException(nameof(context.Nodes), rootPath);
        }
        
        return entity;
    }

    public async Task<FileExplorerRootChildNodeEntity?> GetRootChildOrDefaultAsync(NodePath rootChild, CancellationToken cancellationToken)
    {
        return await context.Nodes
            .OfType<FileExplorerRootChildNodeEntity>()
            .Include(i => i.Children)
            .FirstOrDefaultAsync(f => f.RelativePath == rootChild.RootPath, cancellationToken);
    }

    public async Task<ICollection<FileExplorerRootChildNodeEntity>> GetAllRootChildrenAsync(CancellationToken cancellationToken)
    {
        return await context.Nodes
            .OfType<FileExplorerRootChildNodeEntity>()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FileExplorerNodeEntityBase nodeEntity, CancellationToken cancellationToken)
    {
        if (nodeEntity is FileExplorerFileNodeEntity { Metadata: not null } file)
        {
            await context.FileMetadata.AddAsync(file.Metadata, cancellationToken);
        }
        await context.Nodes.AddAsync(nodeEntity, cancellationToken);
    }

    public async Task AddRangeAsync(ICollection<FileExplorerNodeEntity> addedChildren, CancellationToken cancellationToken)
    {
        var metadata = addedChildren
            .OfType<FileExplorerFileNodeEntity>()
            .Select(s => s.Metadata)
            .Where(w => w is not null)
            .Cast<FileMetadataEntity>()
            .ToList();
        await context.FileMetadata.AddRangeAsync(metadata, cancellationToken);

        await context.Nodes.AddRangeAsync(addedChildren, cancellationToken);
    }

    public void RemoveRange(IEnumerable<FileExplorerNodeEntity> nodes)
    {
        context.Nodes.RemoveRange(nodes);
    }
}
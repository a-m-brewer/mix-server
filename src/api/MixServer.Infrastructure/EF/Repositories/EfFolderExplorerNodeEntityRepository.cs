using Microsoft.EntityFrameworkCore;
using MixServer.Domain.Exceptions;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
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

    public Task<List<FileExplorerNodeEntity>> GetNodesAsync(string rootPath, List<string> relativePaths, CancellationToken cancellationToken)
    {
        if (relativePaths.Count == 0)
        {
            return Task.FromResult(new List<FileExplorerNodeEntity>());
        }

        return context.Nodes
            .OfType<FileExplorerNodeEntity>()
            .IncludeParents()
            .Where(w => w.RootChild.RelativePath == rootPath && relativePaths.Contains(w.RelativePath))
            .ToListAsync(cancellationToken);
    }

    public async Task<string?> GetHashOrDefaultAsync(NodePath nodePath, CancellationToken cancellationToken)
    {
        if (nodePath.IsRoot)
        {
            return string.Empty; // Root folder has no hash
        }

        if (nodePath.IsRootChild)
        {
            return await context.Nodes.OfType<FileExplorerRootChildNodeEntity>()
                .Where(w => w.RelativePath == nodePath.RootPath)
                .Select(s => s.Hash)
                .FirstOrDefaultAsync(cancellationToken);
        }
        
        return await context.Nodes.OfType<FileExplorerFolderNodeEntity>()
            .Include(i => i.RootChild)
            .Where(w => w.RelativePath == nodePath.RelativePath && w.RootChild.RelativePath == nodePath.RootPath)
            .Select(s => s.Hash)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Dictionary<NodePath, string>> GetHashesAsync(List<NodePath> childNodePaths, CancellationToken cancellationToken)
    {
        if (childNodePaths.Count == 0)
        {
            return new Dictionary<NodePath, string>();
        }

        var pathPairs = childNodePaths
            .Select(np => new { np.RootPath, np.RelativePath })
            .ToList();

        return await context.Nodes.OfType<FileExplorerFolderNodeEntity>()
            .Include(i => i.RootChild)
            .Where(w => pathPairs.Contains(new { RootPath = w.RootChild.RelativePath, w.RelativePath }))
            .Select(s => new { Path = new NodePath(s.RootChild.RelativePath, s.RelativePath), Hash = s.Hash})
            .ToDictionaryAsync(k => k.Path, s => s.Hash, cancellationToken);
    }
    
    public async Task<FileExplorerFolderNodeEntity?> GetFolderNodeOrDefaultAsync(
        NodePath nodePath,
        bool includeChildren = true,
        CancellationToken cancellationToken = default)
    {
        var query = context.Nodes
            .OfType<FileExplorerFolderNodeEntity>()
            .IncludeParents();

        if (includeChildren)
        {
            query = query.Include(i => i.Children);
        }
        
        return await query
            .FirstOrDefaultAsync(f => f.RelativePath == nodePath.RelativePath && f.RootChild.RelativePath == nodePath.RootPath, cancellationToken);
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
        await context.Nodes.AddAsync(nodeEntity, cancellationToken);
    }

    public void RemoveRange(IEnumerable<FileExplorerNodeEntity> nodes)
    {
        context.Nodes.RemoveRange(nodes);
    }
}
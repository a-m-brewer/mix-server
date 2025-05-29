using Microsoft.EntityFrameworkCore;
using MixServer.Domain.FileExplorer.Entities;
using MixServer.Domain.FileExplorer.Models;
using MixServer.Domain.Sessions.Repositories;

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

    public async Task<FileExplorerRootChildNodeEntity?> GetRootChildOrDefaultAsync(NodePath rootChild, CancellationToken cancellationToken)
    {
        return await context.Nodes
            .OfType<FileExplorerRootChildNodeEntity>()
            .FirstOrDefaultAsync(f => f.RelativePath == rootChild.RootPath, cancellationToken);
    }

    public async Task AddAsync(FileExplorerNodeEntityBase nodeEntity, CancellationToken cancellationToken)
    {
        await context.Nodes.AddAsync(nodeEntity, cancellationToken);
    }
}
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MixServer.FolderIndexer.Domain.Entities;
using MixServer.FolderIndexer.Domain.Exceptions;
using MixServer.FolderIndexer.Domain.Models;
using MixServer.FolderIndexer.Domain.Repositories;

namespace MixServer.FolderIndexer.Data.EF.Repositories;

public class EfFileSystemInfoRepository(IFolderIndexerDbContext context) : IFileSystemInfoRepository
{
    public async Task<ICollection<RootDirectoryInfoEntity>> GetAllRootFoldersAsync(CancellationToken cancellationToken)
    {
        return await context.FileSystemNodes
            .OfType<RootDirectoryInfoEntity>()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FileSystemInfoEntity fileSystemInfo, CancellationToken cancellationToken)
    {
        await context.FileSystemNodes.AddAsync(fileSystemInfo, cancellationToken);
    }

    public void Remove(FileSystemInfoEntity fileSystemInfo)
    {
        context.FileSystemNodes.Remove(fileSystemInfo);
    }

    public async Task<RelatedDirectoryEntities<TEntity>> GetDirectoriesAsync<TEntity>(string fullName, CancellationToken cancellationToken)
        where TEntity : FileSystemInfoEntity
    {
        var root = await GetEntityAsync<RootDirectoryInfoEntity>(
            f => fullName.StartsWith(f.RelativePath),
            cancellationToken
        );

        if (root is null)
        {
            throw new FolderIndexerEntityNotFoundException(nameof(context.FileSystemNodes), "Root: " + fullName);
        }
        
        if (root.RelativePath == fullName)
        {
            return new RelatedDirectoryEntities<TEntity>
            {
                FullName = fullName,
                Root = root,
                Parent = root,
                Entity = root as TEntity
            };
        }
        
        var parentDirName = Path.GetDirectoryName(fullName);
        var parentIsRoot = parentDirName == root.RelativePath;
        var parentRelativePath = Path.GetRelativePath(root.RelativePath, parentDirName!);
        
        var parent = parentIsRoot
            ? root
            : await GetEntityAsync<DirectoryInfoEntity>(
                f => f.RelativePath == parentRelativePath && f.RootId == root.Id,
                cancellationToken);
        
        var relativePath = Path.GetRelativePath(root.RelativePath, fullName);
        var directory = await GetEntityAsync<TEntity>(
            f => f.RelativePath == relativePath && f.RootId == root.Id,
            cancellationToken);

        return new RelatedDirectoryEntities<TEntity>
        {
            FullName = fullName,
            Root = root,
            Parent = parent,
            Entity = directory
        };
    }

    private async Task<TEntity?> GetEntityAsync<TEntity>(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        where TEntity : FileSystemInfoEntity
    {
        IQueryable<TEntity> query = context.FileSystemNodes
                .OfType<TEntity>()
                .Include(i => i.Root)
                .Include(i => i.Parent);
        
        // If querying directories, include their children
        if (typeof(TEntity).IsAssignableTo(typeof(DirectoryInfoEntity)))
        {
            query = query
                .Cast<DirectoryInfoEntity>()
                .Include(d => d.Children)
                .Cast<TEntity>();
        }

        // If querying files directly, include their metadata
        if (typeof(TEntity).IsAssignableTo(typeof(FileInfoEntity)))
        {
            query = query
                .Cast<FileInfoEntity>()
                .Include(f => f.Metadata)
                .Cast<TEntity>();
        }
        
        var entity = await query
            .FirstOrDefaultAsync(predicate, cancellationToken);

        if (entity is DirectoryInfoEntity directoryInfo)
        {
            var fileChildren = directoryInfo
                .Children
                .OfType<FileInfoEntity>()
                .Select(f => f.Id)
                .ToList();

            if (fileChildren.Count != 0)
            {
                // Bulk-load metadata for file children
                await context.FileSystemNodes
                    .Where(f => fileChildren.Contains(f.Id))
                    .OfType<FileInfoEntity>()
                    .Include(f => f.Metadata)
                    .LoadAsync(cancellationToken);   
            }
        }
        
        return entity;
    }
}
using Microsoft.EntityFrameworkCore;
using MixServer.FolderIndexer.Data.EF.Extensions;
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
        var root = await context.FileSystemNodes
            .OfType<RootDirectoryInfoEntity>()
            .Include(i => i.Children)
            .FirstOrDefaultAsync(f => fullName.StartsWith(f.RelativePath), cancellationToken: cancellationToken);

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
            : await context.FileSystemNodes
                .OfType<DirectoryInfoEntity>()
                .Include(i => i.Children)
                .Include(i => i.Parent)
                .Include(i => i.Root)
                .FirstOrDefaultAsync(f => f.RelativePath == parentRelativePath && f.RootId == root.Id, cancellationToken);
        
        var dirRelativePath = Path.GetRelativePath(root.RelativePath, fullName);
        var directory = await context.FileSystemNodes
            .OfType<TEntity>()
            .IncludeChildren()
            .Include(i => i.Parent)
            .Include(i => i.Root)
            .FirstOrDefaultAsync(f => f.RelativePath == dirRelativePath && f.RootId == root.Id, cancellationToken);

        return new RelatedDirectoryEntities<TEntity>
        {
            FullName = fullName,
            Root = root,
            Parent = parent,
            Entity = directory
        };
    }
}
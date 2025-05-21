using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MixServer.FolderIndexer.Domain.Entities;

namespace MixServer.FolderIndexer.Data.EF;

public interface IFolderIndexerDbContext
{
    DbSet<FileSystemInfoEntity> FileSystemNodes { get; }
    
    DbSet<FileMetadataEntity> FileMetadata { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
}
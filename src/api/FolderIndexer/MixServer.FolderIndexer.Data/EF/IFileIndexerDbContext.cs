using Microsoft.EntityFrameworkCore;
using MixServer.FolderIndexer.Domain.Entities;

namespace MixServer.FolderIndexer.Data.EF;

public interface IFileIndexerDbContext
{
    DbSet<FileSystemInfoEntity> FileSystemNodes { get; }
    DbSet<FileSystemRootEntity> FileSystemRoots { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
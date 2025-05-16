using Microsoft.EntityFrameworkCore;
using MixServer.FolderIndexer.Domain.Entities;

namespace MixServer.FolderIndexer.Data.EF;

public interface IFolderIndexerDbContext
{
    DbSet<FileSystemInfoEntity> FileSystemNodes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
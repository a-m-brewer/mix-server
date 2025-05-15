using Microsoft.EntityFrameworkCore;
using MixServer.FolderIndexer.Domain.Entities;
using MixServer.FolderIndexer.Domain.Exceptions;
using MixServer.FolderIndexer.Domain.Repositories;

namespace MixServer.FolderIndexer.Data.EF.Repositories;

public class EfFileSystemRootRepository(IFileIndexerDbContext context) : IFileSystemRootRepository
{
    public Task<List<FileSystemRootEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return context.FileSystemRoots
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FileSystemRootEntity fileSystemRoot, CancellationToken cancellationToken = default)
    {
        await context.FileSystemRoots.AddAsync(fileSystemRoot, cancellationToken);
    }

    public void Remove(FileSystemRootEntity fileSystemRoot)
    {
        context.FileSystemRoots.Remove(fileSystemRoot);
    }

    public async Task<FileSystemRootEntity> FindChildRootAsync(string directoryAbsolutePath, CancellationToken cancellationToken = default)
    {
        return (await context.FileSystemRoots
            .Include(i => i.Directories)
            .FirstOrDefaultAsync(i => directoryAbsolutePath.StartsWith(i.AbsolutePath), cancellationToken))
            ?? throw new FolderIndexerEntityNotFoundException(nameof(context.FileSystemRoots), directoryAbsolutePath);
    }
}
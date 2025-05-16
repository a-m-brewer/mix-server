using Microsoft.EntityFrameworkCore;
using MixServer.FolderIndexer.Domain.Entities;
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
}
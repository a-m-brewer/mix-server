using MixServer.FolderIndexer.Domain.Entities;
using MixServer.FolderIndexer.Domain.Models;

namespace MixServer.FolderIndexer.Domain.Repositories;

public interface IFileSystemInfoRepository
{
    Task<ICollection<RootDirectoryInfoEntity>> GetAllRootFoldersAsync(CancellationToken cancellationToken);
    Task AddAsync(FileSystemInfoEntity fileSystemInfo, CancellationToken cancellationToken);
    void Remove(FileSystemInfoEntity fileSystemInfo);
    Task<RelatedDirectoryEntities<TEntity>> GetDirectoriesAsync<TEntity>(
        string fullName,
        CancellationToken cancellationToken) 
        where TEntity : FileSystemInfoEntity;
}
using MixServer.FolderIndexer.Domain.Entities;

namespace MixServer.FolderIndexer.Domain.Repositories;

public interface IFileSystemInfoRepository
{
    Task<ICollection<RootDirectoryInfoEntity>> GetAllRootFoldersAsync(CancellationToken cancellationToken);
    Task AddAsync(FileSystemInfoEntity fileSystemInfo, CancellationToken cancellationToken);
    void Remove(FileSystemInfoEntity fileSystemInfo);
}
using MixServer.FolderIndexer.Domain.Entities;

namespace MixServer.FolderIndexer.Domain.Repositories;

public interface IFileSystemRootRepository
{
    Task<List<FileSystemRootEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    
    Task AddAsync(FileSystemRootEntity fileSystemRoot, CancellationToken cancellationToken = default);
    
    void Remove(FileSystemRootEntity fileSystemRoot);
    Task<FileSystemRootEntity> FindChildRootAsync(string directoryAbsolutePath, CancellationToken cancellationToken = default);
}
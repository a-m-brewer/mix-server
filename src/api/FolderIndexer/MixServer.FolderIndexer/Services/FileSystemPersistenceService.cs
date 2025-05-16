using MixServer.FolderIndexer.Domain;
using MixServer.FolderIndexer.Domain.Repositories;

namespace MixServer.FolderIndexer.Services;

internal interface IFileSystemPersistenceService
{
    Task AddOrUpdateFolderAsync(DirectoryInfo directoryInfo, ICollection<FileSystemInfo> children, CancellationToken cancellationToken = default);
}

internal class FileSystemPersistenceService(
    IFileSystemInfoRepository fileSystemInfoRepository,
    IFileIndexerUnitOfWork unitOfWork)
    : IFileSystemPersistenceService
{
    public async Task AddOrUpdateFolderAsync(
        DirectoryInfo directoryInfo,
        ICollection<FileSystemInfo> children,
        CancellationToken cancellationToken = default)
    {
    }
}
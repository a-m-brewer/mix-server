using MixServer.FolderIndexer.Domain;
using MixServer.FolderIndexer.Domain.Repositories;

namespace MixServer.FolderIndexer.Services;

internal interface IFileSystemPersistenceService
{
    Task AddOrUpdateFolderAsync(DirectoryInfo directoryInfo, ICollection<FileSystemInfo> children, CancellationToken cancellationToken = default);
}

internal class FileSystemPersistenceService(
    IFileSystemRootRepository fileSystemRootRepository,
    IFileSystemInfoRepository fileSystemInfoRepository,
    IFileIndexerUnitOfWork unitOfWork)
    : IFileSystemPersistenceService
{
    public async Task AddOrUpdateFolderAsync(DirectoryInfo directoryInfo, ICollection<FileSystemInfo> children, CancellationToken cancellationToken = default)
    {
        var root = await fileSystemRootRepository.FindChildRootAsync(directoryInfo.FullName, cancellationToken);
    }
}
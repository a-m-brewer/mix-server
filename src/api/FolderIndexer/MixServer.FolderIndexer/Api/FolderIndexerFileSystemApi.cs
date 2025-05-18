using MixServer.FolderIndexer.Domain.Entities;
using MixServer.FolderIndexer.Domain.Exceptions;
using MixServer.FolderIndexer.Domain.Repositories;
using MixServer.FolderIndexer.Interface.Api;
using MixServer.FolderIndexer.Interface.Models;

namespace MixServer.FolderIndexer.Api;

internal class FolderIndexerFileSystemApi(IFileSystemInfoRepository fileSystemInfoRepository) : IFolderIndexerFileSystemApi
{
    public async Task<IDirectoryInfo> GetDirectoryInfoAsync(string absolutePath, CancellationToken cancellationToken = default)
    {
        var dirs = await fileSystemInfoRepository.GetDirectoriesAsync<DirectoryInfoEntity>(absolutePath, cancellationToken);
        
        return dirs.Entity ?? throw new FolderIndexerEntityNotFoundException(nameof(IDirectoryInfo), "Directory: " + absolutePath);
    }

    public async Task<IFileInfo> GetFileInfoAsync(string absolutePath, CancellationToken cancellationToken = default)
    {
        var dirs = await fileSystemInfoRepository.GetDirectoriesAsync<FileInfoEntity>(absolutePath, cancellationToken);
        
        return dirs.Entity ?? throw new FolderIndexerEntityNotFoundException(nameof(IFileInfo), "File: " + absolutePath);
    }

    public async Task<IReadOnlyCollection<IRootDirectoryInfo>> GetRootDirectoriesAsync()
    {
        var roots = await fileSystemInfoRepository.GetAllRootFoldersAsync(CancellationToken.None);

        return roots.Cast<IRootDirectoryInfo>().ToList();
    }
}
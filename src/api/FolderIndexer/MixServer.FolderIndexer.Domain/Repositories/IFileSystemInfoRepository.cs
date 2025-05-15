using MixServer.FolderIndexer.Domain.Entities;

namespace MixServer.FolderIndexer.Domain.Repositories;

public interface IFileSystemInfoRepository
{
    Task<DirectoryInfoEntity> GetDirectoryOrDefaultAsync(string path);
}
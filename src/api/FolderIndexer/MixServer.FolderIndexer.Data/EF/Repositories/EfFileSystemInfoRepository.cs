using MixServer.FolderIndexer.Domain.Entities;
using MixServer.FolderIndexer.Domain.Repositories;

namespace MixServer.FolderIndexer.Data.EF.Repositories;

public class EfFileSystemInfoRepository : IFileSystemInfoRepository
{
    public Task<DirectoryInfoEntity> GetDirectoryOrDefaultAsync(string path)
    {
        throw new NotImplementedException();
    }
}
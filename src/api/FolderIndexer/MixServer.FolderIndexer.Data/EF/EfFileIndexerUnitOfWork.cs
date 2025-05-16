using MixServer.FolderIndexer.Domain;

namespace MixServer.FolderIndexer.Data.EF;

public class EfFileIndexerUnitOfWork(IFolderIndexerDbContext context) : IFileIndexerUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}